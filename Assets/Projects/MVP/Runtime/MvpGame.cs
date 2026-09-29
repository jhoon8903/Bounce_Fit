using System;
using System.Collections;
using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.Mathematics;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BounceFit.Mvp
{
    /// <summary>Hybrid presentation host. Combat rents SubScene-baked entities from the default World.</summary>
    public sealed class MvpGame : MonoBehaviour
    {
        [Tooltip("Edit child SO values live. Replacing this root set takes effect on the next run.")]
        public MvpConfigSet config;
        MvpConfigSet boundConfig;
        public MvpConfigSet ActiveConfig => boundConfig ? boundConfig : (config ? config : MvpConfigSet.Default);
        public RenderPipelineAsset pipeline;
        public Material solidMaterial;
        public Material spriteMaterial;
        public MvpCombat Combat { get; private set; }
        public MvpProgression Progression { get; private set; }
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.up;
        public Vector2 Move { get; private set; }
        public bool FastMode { get; private set; }
        public Camera ViewCamera { get; private set; }
        public MvpHud Hud { get; private set; }
        /// <summary>Presentation muzzle for optional projectile effects; simulation still owns projectile spawning.</summary>
        public Vector3 MuzzleWorldPosition => characterView ? characterView.MuzzleWorldPosition : (player ? player.position : Vector3.zero);
        public void TriggerPlayerWeaponRecoil() => characterView?.TriggerWeaponRecoil();
        RenderPipelineAsset previousPipeline;
        readonly Dictionary<int, SpriteRenderer> visuals = new();
        readonly Stack<SpriteRenderer> pool = new();
        readonly HashSet<int> seen = new();
        readonly List<int> retired = new();
        readonly List<Transform> scenery = new();
        readonly List<Material> materials = new();
        Sprite[] enemies, balls;
        Transform player, aimMarker, farBoundary, rearBoundary, leftBoundary, rightBoundary, trail;
        MvpCharacterView characterView;
        int seenXp;
        float accumulator;
        int leftFinger = -1, rightFinger = -1;
        Vector2 leftOrigin, rightOrigin;
        bool focusPaused;

        void Awake()
        {
            previousPipeline = QualitySettings.renderPipeline;
            if (pipeline) QualitySettings.renderPipeline = pipeline;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            boundConfig=config ? config : MvpConfigSet.Default;config=boundConfig;boundConfig.ValidateAll();
            Application.targetFrameRate = boundConfig.presentation.targetFrameRate;
#if UNITY_EDITOR
            Application.runInBackground = true;
#endif
            EnhancedTouchSupport.Enable();
            }
        IEnumerator Start()
        {
            // SubScene loading and EntityPoolSystem prewarming complete asynchronously.
            while (!MvpCombatPools.IsReady(World.DefaultGameObjectInjectionWorld)) yield return null;
            Combat = new MvpCombat((uint)boundConfig.stage.randomSeed,boundConfig,World.DefaultGameObjectInjectionWorld);
            Combat.UsePlayerLaunchOffset=true;
            Progression = new MvpProgression(boundConfig.stage.randomSeed,boundConfig);
            enemies = LoadSprites("Fluffy", "Spider", "StoneBug", "ForestDeer");
            balls = LoadSprites("Ball_Nomal", "Ball_Fire", "Ball_Ice", "Ball_Laser", "Ball_Cluster");
            CreateWorld();
            Hud = gameObject.AddComponent<MvpHud>();
            Hud.Initialize(this);
        }
        Sprite[] LoadSprites(params string[] names)
        {
            var result = new Sprite[names.Length];
            for (int i=0;i<names.Length;i++) result[i] = Resources.Load<Sprite>("MVP/Sprites/" + names[i]);
            return result;
        }
        public void StartRun(bool fast = false)
        {
            boundConfig=config ? config : MvpConfigSet.Default;boundConfig.ValidateAll();
            if (Combat == null) return;
            Combat?.Dispose();Combat=new MvpCombat((uint)boundConfig.stage.randomSeed,boundConfig,World.DefaultGameObjectInjectionWorld);Combat.UsePlayerLaunchOffset=true;
            if(fast)Combat.BossAtSeconds=boundConfig.stage.fastBossAtSeconds;
            Progression=new MvpProgression(boundConfig.stage.randomSeed,boundConfig);seenXp=0;accumulator=0;
            Started=true; Paused=false; FastMode=fast; Aim=Vector2.up;
            ResetInput(); SyncVisuals(0f);
        }
        public void TogglePause() { if (Started && !Combat.Won && !Combat.Lost) { Paused=!Paused; ResetInput(); } }
        public void Choose(int index) { if (Progression.Choose(index)) { accumulator=0; ResetInput(); } }
        void ResetInput() { Move=Vector2.zero; leftFinger=rightFinger=-1; }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && Started) { Paused=true; focusPaused=true; ResetInput(); }
            else if(focused && focusPaused) { focusPaused=false; }
        }
        void Update()
        {
            if (Combat == null) return;
            ActiveConfig.ValidateAll();
            Progression.RefreshConfiguration();Combat.RefreshConfiguration();
            if(FastMode)Combat.BossAtSeconds=ActiveConfig.stage.fastBossAtSeconds;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) TogglePause();
            bool running=Started && !Paused && !Progression.IsChoosing && !Combat.Won && !Combat.Lost;
            if(running)
            {
                ReadInput();
                accumulator += Mathf.Min(Time.unscaledDeltaTime,ActiveConfig.stage.maxFrameCatchup);
                float step=ActiveConfig.stage.fixedStep;
                while(accumulator>=step)
                {
                    SyncCharacterAimAndLaunchOffset();
                    int shotsBefore=Combat.ShotsFired;
                    Combat.Step(step,new float2(Move.x,Move.y),new float2(Aim.x,Aim.y),Progression.Stats);
                    if(Combat.ShotsFired>shotsBefore) characterView?.TriggerWeaponRecoil();
                    accumulator-=step;
                    int xp=Combat.CollectedXp-seenXp;
                    if(xp>0) { Progression.AddXp(xp); seenXp=Combat.CollectedXp; }
                    if(Progression.IsChoosing || Combat.Won || Combat.Lost) { accumulator=0; ResetInput(); break; }
                }
            }
            else { accumulator=0; Move=Vector2.zero; }
            SyncVisuals(running ? Time.unscaledDeltaTime : 0f);
            Hud.Refresh();
        }
        void ReadInput()
        {
            Vector2 movement=Vector2.zero;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                movement=new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
            }
            float radius=Mathf.Min(Screen.width,Screen.height)*ActiveConfig.input.stickRadiusScreenFraction;
            foreach(var touch in Touch.activeTouches)
            {
                int id=touch.touchId;
                if(touch.phase==UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if(touch.screenPosition.y > Screen.height*ActiveConfig.input.touchAreaHeight) continue;
                    if(touch.screenPosition.x<Screen.width*.5f && leftFinger<0) { leftFinger=id; leftOrigin=touch.screenPosition; }
                    else if(touch.screenPosition.x>=Screen.width*.5f && rightFinger<0) { rightFinger=id; rightOrigin=touch.screenPosition; }
                }
                bool ended=touch.phase==UnityEngine.InputSystem.TouchPhase.Ended || touch.phase==UnityEngine.InputSystem.TouchPhase.Canceled;
                if(id==leftFinger) { if(ended) leftFinger=-1; else { Vector2 d=(touch.screenPosition-leftOrigin)/radius; if(d.magnitude>ActiveConfig.input.aimDeadZone) SetAim(d); } }
                if(id==rightFinger) { if(ended) rightFinger=-1; else movement=Vector2.ClampMagnitude((touch.screenPosition-rightOrigin)/radius,1); }
            }
            var mouse=Mouse.current;
            if(mouse!=null && Touch.activeTouches.Count==0)
            {
                var ray=ViewCamera.ScreenPointToRay(mouse.position.ReadValue());
                if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))
                { var p=ray.GetPoint(distance); SetAim(new Vector2(p.x-Combat.PlayerPosition.x,p.z-Combat.PlayerPosition.y)); }
            }
            Move=Vector2.ClampMagnitude(movement,1);
        }
        public static Vector2 ClampAim(Vector2 value, float halfAngle=80f)
        {
            if(value.sqrMagnitude<.001f) return Vector2.up;
            float angle=Mathf.Clamp(Mathf.Atan2(value.x,value.y)*Mathf.Rad2Deg,-halfAngle,halfAngle)*Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));
        }
        void SetAim(Vector2 value) { Aim=ClampAim(value,ActiveConfig.stage.aimHalfAngle); }
        void SyncCharacterAimAndLaunchOffset()
        {
            if(!player || !characterView || !ViewCamera || Combat==null) return;
            var playerPosition=Combat.PlayerPosition;
            player.position=new Vector3(playerPosition.x,0,playerPosition.y);
            characterView.UpdatePresentation(ViewCamera,Aim,0f);
            var ray=ViewCamera.ScreenPointToRay(ViewCamera.WorldToScreenPoint(characterView.MuzzleWorldPosition));
            if(new Plane(Vector3.up,new Vector3(0,.18f,0)).Raycast(ray,out float distance))
            {
                var launch=ray.GetPoint(distance);
                Combat.PlayerLaunchOffset=new float2(launch.x-playerPosition.x,launch.z-playerPosition.y);
            }
        }
        public Vector2 StickOffset(bool left) => left ? (leftFinger>=0?Aim:Vector2.zero) : Move;
        Material ColorMaterial(Color color)
        {
            var mat=new Material(solidMaterial); mat.color=color; materials.Add(mat); return mat;
        }
        Transform Shape(string label, PrimitiveType type, Vector3 position, Vector3 size, Color color, Transform parent=null)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=label; obj.transform.SetParent(parent?parent:transform,false);
            obj.transform.localPosition=position; obj.transform.localScale=size;
            Destroy(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial=ColorMaterial(color);
            return obj.transform;
        }
        void CreateWorld()
        {
            var existing=Camera.main;
            ViewCamera=existing?existing:new GameObject("MVP Camera").AddComponent<Camera>();
            ViewCamera.tag="MainCamera";
            ViewCamera.transform.position=ActiveConfig.presentation.cameraPosition;
            ViewCamera.transform.rotation=Quaternion.Euler(ActiveConfig.presentation.cameraEuler);
            ViewCamera.orthographic=false; ViewCamera.fieldOfView=ActiveConfig.presentation.fieldOfView;
            ViewCamera.nearClipPlane=.1f; ViewCamera.farClipPlane=100;
            ViewCamera.backgroundColor=new Color(.055f,.105f,.085f);
            ViewCamera.clearFlags=CameraClearFlags.SolidColor;
            Shape("Forest floor",PrimitiveType.Cube,new Vector3(0,-.22f,7),new Vector3(50,.3f,65),new Color(.13f,.25f,.19f));
            trail=Shape("Forest trail",PrimitiveType.Cube,new Vector3(0,-.035f,7),new Vector3(14,.05f,65),new Color(.34f,.39f,.24f));
            farBoundary=Shape("Far reflection boundary",PrimitiveType.Cube,new Vector3(0,.08f,13.5f),new Vector3(14,.16f,.12f),new Color(.65f,.75f,.39f));
            rearBoundary=Shape("Rear recovery boundary",PrimitiveType.Cube,new Vector3(0,.04f,-6.4f),new Vector3(14,.08f,.1f),new Color(.4f,.8f,.7f));
            for(int side=-1;side<=1;side+=2)
            {
                var sideBoundary=Shape("Ball reflection boundary",PrimitiveType.Cube,new Vector3(side*7.15f,.1f,5),new Vector3(.13f,.25f,38),new Color(.65f,.75f,.39f));
                if(side<0)leftBoundary=sideBoundary;else rightBoundary=sideBoundary;
                for(int i=0;i<15;i++)
                {
                    var tree=new GameObject("Scrolling forest").transform; tree.SetParent(transform); tree.position=new Vector3(side*(8.2f+i%3*1.1f),0,i*2.4f-10);
                    Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(.32f,.55f,.32f),new Color(.27f,.21f,.14f),tree);
                    Shape("Crown",PrimitiveType.Sphere,new Vector3(0,1.65f,0),new Vector3(1.7f,2.2f,1.5f),new Color(.17f+i%3*.025f,.37f+i%3*.025f,.23f),tree);
                    scenery.Add(tree);
                }
            }
            player=new GameObject("Raccoon archmage").transform; player.SetParent(transform);player.localScale=Vector3.one*ActiveConfig.presentation.playerScale;
            characterView=player.gameObject.AddComponent<MvpCharacterView>();
            characterView.Initialize(ViewCamera,spriteMaterial);
            aimMarker=Shape("Aim marker",PrimitiveType.Sphere,Vector3.zero,new Vector3(.16f,.08f,.45f),new Color(.9f,.95f,.65f));
            Shape("Player hit center",PrimitiveType.Cylinder,new Vector3(0,.025f,0),new Vector3(.5f,.015f,.5f),new Color(.9f,.97f,.85f),player);
        }
        void SyncVisuals(float animationDelta = 0f)
        {
            if(Combat==null) return;
            ApplyLivePresentation();
            var view=ActiveConfig.presentation;
            player.position=new Vector3(Combat.PlayerPosition.x,0,Combat.PlayerPosition.y);
            player.rotation=Quaternion.identity;
            characterView?.UpdatePresentation(ViewCamera,Aim,animationDelta);
            aimMarker.position=player.position+new Vector3(Aim.x,.08f,Aim.y)*view.aimMarkerDistance;
            aimMarker.rotation=Quaternion.Euler(0,Mathf.Atan2(Aim.x,Aim.y)*Mathf.Rad2Deg,0);
            for(int i=0;i<scenery.Count;i++) { var p=scenery[i].position; p.z=Mathf.Repeat((i%15)*2.4f-Combat.ScrollDistance+10,36)-10; scenery[i].position=p; }
            seen.Clear();
            foreach(var item in Combat.Visuals)
            {
                seen.Add(item.Id);
                if(!visuals.TryGetValue(item.Id,out var renderer))
                {
                    renderer=pool.Count>0?pool.Pop():new GameObject("Pooled combat sprite").AddComponent<SpriteRenderer>();
                    renderer.transform.SetParent(transform); renderer.sharedMaterial=spriteMaterial;
                    renderer.gameObject.SetActive(true); visuals.Add(item.Id,renderer);
                }
                Sprite sprite; Color tint=Color.white;
                float diameter=item.Radius*2;
                if(item.Kind==0) { sprite=enemies[item.Variant==2?3:(item.Id%3)]; diameter=item.Variant==2?view.bossDiameter:view.enemyDiameter; }
                else if(item.Kind==1) { sprite=balls[Mathf.Clamp(item.Variant,0,balls.Length-1)]; diameter=item.Variant==4?view.fusionBallDiameter:view.ballDiameter; }
                else { sprite=balls[0]; tint=item.Kind==2?new Color(1,.26f,.3f):(item.Variant>=ActiveConfig.experience.rareValue?new Color(.83f,.4f,1):item.Variant>=ActiveConfig.experience.largeValue?new Color(1,.83f,.25f):new Color(.25f,1,.65f)); diameter=item.Kind==2?view.enemyBulletDiameter:item.Variant>ActiveConfig.experience.smallValue?view.largeXpDiameter:view.smallXpDiameter; }
                if(item.Returning) tint=new Color(.5f,1f,.9f,.8f);
                if(item.Kind==0 && item.Telegraph01>0) tint=Color.Lerp(Color.white,new Color(1,.3f,.2f),item.Telegraph01);
                renderer.sprite=sprite; renderer.color=tint;
                renderer.transform.position=new Vector3(item.Position.x,item.Kind==0?.65f:.18f,item.Position.y);
                renderer.transform.rotation=ViewCamera.transform.rotation;
                float scale=sprite?diameter/Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y):1;
                renderer.transform.localScale=Vector3.one*scale;
                renderer.sortingOrder=1000-Mathf.RoundToInt(item.Position.y*20)+(item.Kind==2?400:0);
            }
            retired.Clear(); foreach(var pair in visuals) if(!seen.Contains(pair.Key)) retired.Add(pair.Key);
            foreach(int key in retired) { var value=visuals[key];value.gameObject.SetActive(false);pool.Push(value);visuals.Remove(key); }
        }
        void ApplyLivePresentation()
        {
            var view=ActiveConfig.presentation;var stage=ActiveConfig.stage;
            Application.targetFrameRate=view.targetFrameRate;
            ViewCamera.transform.SetPositionAndRotation(view.cameraPosition,Quaternion.Euler(view.cameraEuler));
            ViewCamera.fieldOfView=view.fieldOfView;player.localScale=Vector3.one*view.playerScale;
            float center=(stage.left+stage.right)*.5f,width=stage.right-stage.left;
            float middle=(stage.rear+stage.top)*.5f,length=stage.top-stage.rear;
            farBoundary.position=new Vector3(center,.08f,stage.top);farBoundary.localScale=new Vector3(width,.16f,.12f);
            rearBoundary.position=new Vector3(center,.04f,stage.rear);rearBoundary.localScale=new Vector3(width,.08f,.1f);
            leftBoundary.position=new Vector3(stage.left-.15f,.1f,middle);rightBoundary.position=new Vector3(stage.right+.15f,.1f,middle);
            leftBoundary.localScale=rightBoundary.localScale=new Vector3(.13f,.25f,length);
            trail.position=new Vector3(center,-.035f,middle);trail.localScale=new Vector3(width,.05f,length+40);
        }
        void OnGUI()
        {
            if (Combat == null) GUI.Label(new Rect(24, 24, 500, 40), "Loading baked combat pools…");
        }
        void OnDestroy()
        {
            Combat?.Dispose(); EnhancedTouchSupport.Disable(); QualitySettings.renderPipeline=previousPipeline;
            foreach(var mat in materials) if(mat) Destroy(mat);
        }
    }
}
