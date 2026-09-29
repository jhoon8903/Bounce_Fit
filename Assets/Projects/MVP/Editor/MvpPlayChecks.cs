using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BounceFit.Mvp.Editor
{
    public static class MvpPlayChecks
    {
        static MvpGame game;
        static int phase;
        static double deadline;
        static float pausedTime;
        static string results;
        [MenuItem("Bounce Fit/MVP/Run Play Smoke Checks")]
        public static void Run()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Enter ForestMvp Play mode first.");
            game=UnityEngine.Object.FindFirstObjectByType<MvpGame>();
            if(!game) throw new InvalidOperationException("MvpGame missing.");
            results="Play smoke checks\n"; phase=0;
            Click("숲으로 출발  ·  약 7분");
            Check(game.Started,"Start button begins run");deadline=EditorApplication.timeSinceStartup+3;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static void Check(bool condition,string label)
        { if(!condition) throw new Exception("FAIL: "+label);results+="PASS: "+label+"\n"; }
        static void Click(string name)
        {
            foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(button.name==name) { button.onClick.Invoke();return; }
            throw new Exception("Missing button: "+name);
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<deadline)return;
            try
            {
                switch(phase++)
                {
                    case 0:
                        Check(game.Combat.Elapsed>.5f,"Combat clock advances");
                        CheckDesktopInput();
                        Check(game.Combat.Visuals.Count>0,"Combat snapshots populate view");
                        Click("일시정지");pausedTime=game.Combat.Elapsed;break;
                    case 1:
                        Check(Mathf.Approximately(game.Combat.Elapsed,pausedTime),"Pause freezes simulation");
                        Click("계속하기");game.Combat.DebugGrantXp(30);break;
                    case 2:
                        Check(game.Progression.IsChoosing,"XP opens card choice");pausedTime=game.Combat.Elapsed;break;
                    case 3:
                        Check(Mathf.Approximately(game.Combat.Elapsed,pausedTime),"Card choice freezes simulation");
                        while(game.Progression.IsChoosing) game.Choose(0);
                        game.Combat.DebugAdvanceToBoss();Check(game.Combat.BossActive&&!game.Combat.Won,"Boss spawn is not victory");break;
                    case 4:
                        game.Combat.DebugDefeatBoss();break;
                    case 5:
                        Check(game.Combat.Won,"Win result reached");Click("다시 도전");
                        Check(!game.Combat.Won&&!game.Combat.Lost&&game.Progression.Level==1,"Restart resets run");game.Combat.DebugSetHealth(0);break;
                    case 6:
                        Check(game.Combat.Lost,"Defeat result reached");Click("다시 도전");
                        game.TogglePause();
                        System.IO.Directory.CreateDirectory("Captures/MVP");System.IO.File.WriteAllText("Captures/MVP/play-smoke.txt",results);
                        Debug.Log(results);EditorApplication.update-=Tick;return;
                }
                deadline=EditorApplication.timeSinceStartup+.6;
            }
            catch(Exception e) { EditorApplication.update-=Tick;Debug.LogException(e); }
        }
        static void CheckDesktopInput()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var position=game.Combat.PlayerPosition;
                var right=game.ViewCamera.WorldToScreenPoint(new Vector3(position.x+3,0,position.y+4));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(right.x,right.y) });
                InputSystem.Update();game.SendMessage("ReadInput");
                Check(game.Move.x>.9f && game.Aim.x>.2f && !mouse.leftButton.isPressed,"WASD move and mouse aim work without a click");
                var left=game.ViewCamera.WorldToScreenPoint(new Vector3(position.x-3,0,position.y+4));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(left.x,left.y) });
                InputSystem.Update();game.SendMessage("ReadInput");
                Check(game.Move.y>.9f && game.Aim.x<-.2f,"Movement and mouse aim are independent");
            }
            finally { InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard); }
        }
        [MenuItem("Bounce Fit/MVP/Preview Cards")]
        public static void PreviewCards()
        {
            var instance=UnityEngine.Object.FindFirstObjectByType<MvpGame>();if(!instance)return;
            instance.StartRun();instance.Combat.DebugGrantXp(8);
        }
        [MenuItem("Bounce Fit/MVP/Preview Combat")]
        public static void Preview()
        {
            var instance=UnityEngine.Object.FindFirstObjectByType<MvpGame>();if(!instance)return;
            instance.StartRun(true);
        }
    }
}
