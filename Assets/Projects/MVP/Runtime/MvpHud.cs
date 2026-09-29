using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BounceFit.Mvp
{
    public sealed class MvpHud : MonoBehaviour
    {
        MvpGame game;
        Canvas canvas;
        Font font;
        Text status, slots, bossText, stats, modalTitle, modalBody;
        Image hp, xp, boss;
        GameObject modal, startPanel, pausePanel, resultPanel, choicePanel;
        Text resultTitle, resultBody, instructions;
        readonly Button[] cards=new Button[3];
        readonly Text[] cardTexts=new Text[3];
        RectTransform leftKnob,rightKnob;
        readonly Color ink=new(.88f,.94f,.85f), panel=new(.055f,.105f,.10f,.95f), accent=new(.69f,.87f,.39f);
        public void Initialize(MvpGame owner)
        {
            game=owner; font=Resources.Load<Font>("MVP/PretendardVariable");
            if(!font) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var obj=new GameObject("MVP interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            obj.transform.SetParent(transform); canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.ViewCamera;canvas.planeDistance=1;canvas.sortingOrder=32000;
            var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            if(!FindFirstObjectByType<EventSystem>()) new GameObject("MVP EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var root=Panel("Safe area",canvas.transform,new Vector2(0,0),new Vector2(1,1),Color.clear);
            var safe=root.gameObject.AddComponent<MvpSafeArea>();
            var top=Panel("Status",root,new Vector2(.015f,.86f),new Vector2(.985f,.985f),panel);
            status=Label(top,"",new Vector2(.02f,.52f),new Vector2(.72f,.94f),23,TextAnchor.MiddleLeft);
            hp=Bar(top,new Vector2(.02f,.17f),new Vector2(.27f,.36f),new Color(.94f,.48f,.43f));
            xp=Bar(top,new Vector2(.3f,.17f),new Vector2(.72f,.36f),accent);
            MakeButton(top,"일시정지",new Vector2(.83f,.16f),new Vector2(.98f,.85f),()=>game.TogglePause());
            slots=Label(root,"",new Vector2(.025f,.72f),new Vector2(.37f,.845f),17,TextAnchor.UpperLeft);
            bossText=Label(root,"숲 파괴자",new Vector2(.4f,.795f),new Vector2(.7f,.835f),19,TextAnchor.MiddleCenter);
            boss=Bar(root,new Vector2(.4f,.774f),new Vector2(.7f,.79f),new Color(.95f,.39f,.29f));
            stats=Label(root,"",new Vector2(.74f,.73f),new Vector2(.97f,.845f),15,TextAnchor.UpperRight);
            leftKnob=Stick(root,true);rightKnob=Stick(root,false);
            modal=Panel("Modal",root,Vector2.zero,Vector2.one,new Color(.015f,.03f,.025f,.79f)).gameObject;
            startPanel=Panel("Start",modal.transform,new Vector2(.2f,.15f),new Vector2(.8f,.85f),panel).gameObject;
            Label(startPanel.transform,"FOREST • ARCHMAGE",new Vector2(.07f,.8f),new Vector2(.93f,.94f),36,TextAnchor.MiddleCenter);
            Label(startPanel.transform,"너구리 대마법사",new Vector2(.08f,.65f),new Vector2(.92f,.8f),29,TextAnchor.MiddleCenter);
            instructions=Label(startPanel.transform,"왼손 조준 · 오른손 이동 · 공격은 자동\n전방 160°로 Ball을 쏘고 탄막을 피하세요.\n경험치 구슬을 모아 강화 · 최종 보스 처치 시 승리\n\nPC: WASD 이동 / 마우스로 조준 (클릭 불필요) / Esc 일시정지",new Vector2(.08f,.27f),new Vector2(.92f,.65f),20,TextAnchor.MiddleCenter);
            MakeButton(startPanel.transform,"숲으로 출발  ·  약 7분",new Vector2(.12f,.09f),new Vector2(.88f,.23f),()=>game.StartRun());
            pausePanel=Panel("Pause",modal.transform,new Vector2(.3f,.25f),new Vector2(.7f,.75f),panel).gameObject;
            Label(pausePanel.transform,"잠시 숨 고르기",new Vector2(.08f,.7f),new Vector2(.92f,.9f),32,TextAnchor.MiddleCenter);
            MakeButton(pausePanel.transform,"계속하기",new Vector2(.13f,.4f),new Vector2(.87f,.61f),()=>game.TogglePause());
            MakeButton(pausePanel.transform,"처음부터",new Vector2(.13f,.12f),new Vector2(.87f,.32f),()=>game.StartRun(game.FastMode));
            choicePanel=Panel("Level up",modal.transform,new Vector2(.08f,.13f),new Vector2(.92f,.88f),panel).gameObject;
            modalTitle=Label(choicePanel.transform,"",new Vector2(.03f,.8f),new Vector2(.97f,.96f),30,TextAnchor.MiddleCenter);
            modalBody=Label(choicePanel.transform,"전체 후보 중 무작위 3장 · 하나를 선택하세요",new Vector2(.03f,.7f),new Vector2(.97f,.8f),19,TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++)
            {
                int index=i;
                cards[i]=MakeButton(choicePanel.transform,"",new Vector2(.035f+i*.325f,.09f),new Vector2(.315f+i*.325f,.65f),()=>game.Choose(index));
                cardTexts[i]=cards[i].GetComponentInChildren<Text>();cardTexts[i].fontSize=23;
            }
            resultPanel=Panel("Result",modal.transform,new Vector2(.25f,.21f),new Vector2(.75f,.79f),panel).gameObject;
            resultTitle=Label(resultPanel.transform,"",new Vector2(.05f,.68f),new Vector2(.95f,.91f),36,TextAnchor.MiddleCenter);
            resultBody=Label(resultPanel.transform,"",new Vector2(.05f,.31f),new Vector2(.95f,.65f),22,TextAnchor.MiddleCenter);
            MakeButton(resultPanel.transform,"다시 도전",new Vector2(.15f,.1f),new Vector2(.85f,.27f),()=>game.StartRun(game.FastMode));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var debug=Panel("Development controls",root,new Vector2(.32f,.01f),new Vector2(.68f,.075f),panel);
            MakeButton(debug,"XP +30",new Vector2(0,0),new Vector2(.3f,1),()=>{if(game.Started)game.Combat.DebugGrantXp(30);});
            MakeButton(debug,"보스",new Vector2(.34f,0),new Vector2(.64f,1),()=>{if(game.Started)game.Combat.DebugAdvanceToBoss();});
            MakeButton(debug,"45초 런",new Vector2(.68f,0),new Vector2(1,1),()=>game.StartRun(true));
            debug.SetAsLastSibling();
#endif
        }
        RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=obj.GetComponent<Image>();image.color=color;image.raycastTarget=color.a>.1f; return rect;
        }
        Text Label(Transform parent,string value,Vector2 min,Vector2 max,int size,TextAnchor alignment)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=new Vector2(8,2);rect.offsetMax=new Vector2(-8,-2);
            var text=obj.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=ink;text.alignment=alignment;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
        Button MakeButton(Transform parent,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(value,parent,min,max,new Color(.20f,.31f,.25f,1));var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(action);
            var colors=button.colors;colors.highlightedColor=new Color(.85f,1,.75f);colors.pressedColor=new Color(.6f,.8f,.5f);button.colors=colors;
            Label(rect,value,new Vector2(.035f,.04f),new Vector2(.965f,.96f),21,TextAnchor.MiddleCenter);return button;
        }
        Image Bar(Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var bg=Panel("Bar background",parent,min,max,new Color(.1f,.15f,.12f));
            var fill=Panel("Fill",bg,Vector2.zero,Vector2.one,color).GetComponent<Image>();return fill;
        }
        RectTransform Stick(Transform parent,bool left)
        {
            float x=left?.11f:.89f;
            var rect=Panel(left?"Left aim":"Right move",parent,new Vector2(x-.064f,.07f),new Vector2(x+.064f,.255f),new Color(.07f,.14f,.11f,.55f));rect.GetComponent<Image>().raycastTarget=false;
            Label(parent,left?"조준":"이동",new Vector2(x-.07f,.025f),new Vector2(x+.07f,.068f),19,TextAnchor.MiddleCenter);
            var knob=Panel("Thumb",rect,new Vector2(.34f,.34f),new Vector2(.66f,.66f),new Color(.72f,.86f,.58f,.8f));knob.GetComponent<Image>().raycastTarget=false;return knob;
        }
        static void Fill(Image image,float value) { var rect=image.rectTransform;rect.anchorMax=new Vector2(Mathf.Clamp01(value),1); }
        public void Refresh()
        {
            var c=game.Combat;var p=game.Progression;
            instructions.text=$"왼손 조준 · 오른손 이동 · 공격은 자동\n전방 {game.ActiveConfig.stage.aimHalfAngle*2:0}°로 Ball을 쏘고 탄막을 피하세요.\n경험치 구슬을 모아 강화 · 최종 보스 처치 시 승리\n\nPC: WASD 이동 / 마우스로 조준 (클릭 불필요) / Esc 일시정지";
            modalBody.text=$"전체 후보 중 무작위 {p.ChoiceCount}장 · 하나를 선택하세요";
            status.text=$"LV {p.Level}   ·   {Mathf.CeilToInt(c.Health)} / {c.MaxHealth:0} HP      {((int)c.Elapsed/60):00}:{((int)c.Elapsed%60):00}   ·   처치 {c.Kills}";
            Fill(hp,c.Health/c.MaxHealth);Fill(xp,(float)p.Xp/p.NextXp);
            slots.text="액티브  "+p.ActiveCount+" / "+p.MaxActiveSlots+"\n"+string.Join(" · ",p.ActiveLabels)+"\n패시브  "+p.PassiveCount+" / "+p.MaxPassiveSlots+"  "+string.Join(" · ",p.PassiveLabels);
            stats.text=$"XP {p.Xp} / {p.NextXp}\n"+(c.BossActive?"보스를 처치하세요":$"보스까지 {Mathf.Max(0,Mathf.CeilToInt(c.BossAtSeconds-c.Elapsed))}초")+(game.FastMode?"\n개발용 단축 런":"");
            bossText.gameObject.SetActive(c.BossActive);boss.transform.parent.gameObject.SetActive(c.BossActive);Fill(boss,c.BossHealth01);
            leftKnob.anchoredPosition=game.StickOffset(true)*40;rightKnob.anchoredPosition=game.StickOffset(false)*40;
            bool ended=c.Won||c.Lost;
            modal.SetActive(!game.Started||game.Paused||p.IsChoosing||ended);
            startPanel.SetActive(!game.Started);pausePanel.SetActive(game.Started&&game.Paused&&!ended);
            choicePanel.SetActive(game.Started&&!game.Paused&&p.IsChoosing&&!ended);resultPanel.SetActive(game.Started&&ended);
            if(p.IsChoosing)
            {
                modalTitle.text=$"LEVEL UP  ·  Lv.{p.Level}";
                for(int i=0;i<3;i++) { bool valid=i<p.Choices.Count;cards[i].gameObject.SetActive(valid); if(valid) { var card=p.Choices[i];cardTexts[i].text=$"{card.Category}\n\n{card.Title}\n\n<size=19>{card.Description}</size>"; } }
            }
            if(ended) { resultTitle.text=c.Won?"숲을 지켜냈습니다":"다음 숲길을 준비하세요";resultBody.text=$"{c.Elapsed/60:0.0}분 생존 · {c.Kills}마리 처치\nLv.{p.Level} · 경험치 {c.CollectedXp}\n"+(c.Won?"최종 보스를 쓰러뜨렸습니다.":"경험치 수집과 탄막 회피의 균형을 찾아보세요."); }
        }
    }
    public sealed class MvpSafeArea : MonoBehaviour
    {
        Rect last; Vector2Int lastSize;
        void Update()
        {
            var area=Screen.safeArea;var size=new Vector2Int(Screen.width,Screen.height);if(area==last&&size==lastSize)return;
            last=area;lastSize=size;var rect=(RectTransform)transform;
            rect.anchorMin=new Vector2(area.xMin/size.x,area.yMin/size.y);rect.anchorMax=new Vector2(area.xMax/size.x,area.yMax/size.y);
        }
    }
}
