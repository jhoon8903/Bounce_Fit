using UnityEngine;
namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName="Bounce Fit/Config/Stage",fileName="StageConfig")]
    public sealed class MvpStageConfig : ScriptableObject
    {
        [Header("Live arena / scroll")]
        [Min(0)] public float scrollSpeed=1.2f;
        public float left=-7,right=7,rear=-6.4f,top=13.5f;
        public Vector2 playerMin=new(-6.4f,-5),playerMax=new(6.4f,6);
        public float cleanupMinY=-7,cleanupMaxY=16,cleanupAbsX=9;
        [Header("Live run timing")]
        [Min(0)] public float bossAtSeconds=360;
        [Min(0)] public float fastBossAtSeconds=45;
        [Range(1,89)] public float aimHalfAngle=80;
        [Header("Simulation / capacities")]
        [Range(.005f,.05f)] public float fixedStep=1f/60f;
        [Range(.005f,.1f)] public float maxSimulationStep=.05f;
        [Range(.05f,.5f)] public float maxFrameCatchup=.1f;
        [Min(1)] public int spawnEntityCap=220,xpEntityCap=300;
        [Tooltip("Used when starting a new run.")] public int randomSeed=17;
        public void Validate()
        {
            scrollSpeed=MvpConfigValidation.Number(scrollSpeed,0,100,1.2f);
            left=MvpConfigValidation.Number(left,-100,98,-7);right=MvpConfigValidation.Number(right,left+2,100,7);
            rear=MvpConfigValidation.Number(rear,-100,97,-6.4f);top=MvpConfigValidation.Number(top,rear+3,100,13.5f);
            playerMin=MvpConfigValidation.Vector(playerMin,new Vector2(-6.4f,-5));playerMax=MvpConfigValidation.Vector(playerMax,new Vector2(6.4f,6));
            playerMin=new Vector2(Mathf.Clamp(playerMin.x,left+.1f,right-.2f),Mathf.Clamp(playerMin.y,rear+.1f,top-.2f));
            playerMax=new Vector2(Mathf.Clamp(playerMax.x,playerMin.x+.1f,right-.1f),Mathf.Clamp(playerMax.y,playerMin.y+.1f,top-.1f));
            cleanupMinY=MvpConfigValidation.Number(cleanupMinY,-1000,rear-.1f,-7);cleanupMaxY=MvpConfigValidation.Number(cleanupMaxY,top+.1f,1000,16);
            cleanupAbsX=MvpConfigValidation.Number(cleanupAbsX,Mathf.Max(Mathf.Abs(left),Mathf.Abs(right))+.1f,1000,9);
            bossAtSeconds=MvpConfigValidation.Number(bossAtSeconds,0,36000,360);fastBossAtSeconds=MvpConfigValidation.Number(fastBossAtSeconds,0,36000,45);
            aimHalfAngle=MvpConfigValidation.Number(aimHalfAngle,1,89,80);
            maxSimulationStep=MvpConfigValidation.Number(maxSimulationStep,.005f,.1f,.05f);
            fixedStep=MvpConfigValidation.Number(fixedStep,.005f,maxSimulationStep,1f/60f);
            maxFrameCatchup=MvpConfigValidation.Number(maxFrameCatchup,maxSimulationStep,.5f,.1f);
            spawnEntityCap=Mathf.Clamp(spawnEntityCap,1,10000);xpEntityCap=Mathf.Clamp(xpEntityCap,1,20000);
        }
        void OnValidate()=>Validate();
    }
}
