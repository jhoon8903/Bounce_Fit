using UnityEngine;
namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName="Bounce Fit/Config/Presentation",fileName="PresentationConfig")]
    public sealed class MvpPresentationConfig : ScriptableObject
    {
        [Header("Live camera")]
        public Vector3 cameraPosition=new(0,22,-17),cameraEuler=new(48,0,0);
        [Range(15,100)] public float fieldOfView=47;
        [Range(15,240)] public int targetFrameRate=60;
        [Header("Live visual scale (not collision size)")]
        [Min(.1f)] public float playerScale=1.3f,enemyDiameter=1.2f,bossDiameter=3.1f;
        [Min(.05f)] public float ballDiameter=.8f,fusionBallDiameter=1,enemyBulletDiameter=.7f,smallXpDiameter=.48f,largeXpDiameter=.7f;
        [Range(0,1)] public float playerTurnFraction=.65f;
        [Min(.1f)] public float aimMarkerDistance=1.1f;
        public void Validate()
        {
            cameraPosition=MvpConfigValidation.Vector(cameraPosition,new Vector3(0,22,-17));cameraEuler=MvpConfigValidation.Vector(cameraEuler,new Vector3(48,0,0));
            fieldOfView=MvpConfigValidation.Number(fieldOfView,15,100,47);targetFrameRate=Mathf.Clamp(targetFrameRate,15,240);
            playerScale=Scale(playerScale,1.3f);enemyDiameter=Scale(enemyDiameter,1.2f);bossDiameter=Scale(bossDiameter,3.1f);
            ballDiameter=Scale(ballDiameter,.8f);fusionBallDiameter=Scale(fusionBallDiameter,1);enemyBulletDiameter=Scale(enemyBulletDiameter,.7f);
            smallXpDiameter=Scale(smallXpDiameter,.48f);largeXpDiameter=Scale(largeXpDiameter,.7f);
            playerTurnFraction=MvpConfigValidation.Number(playerTurnFraction,0,1,.65f);aimMarkerDistance=Scale(aimMarkerDistance,1.1f);
        }
        static float Scale(float value,float fallback)=>MvpConfigValidation.Number(value,.05f,20,fallback);
        void OnValidate()=>Validate();
    }
}
