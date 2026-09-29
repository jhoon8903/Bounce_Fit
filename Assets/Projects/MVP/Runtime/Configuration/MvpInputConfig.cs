using UnityEngine;
namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName="Bounce Fit/Config/Input",fileName="InputConfig")]
    public sealed class MvpInputConfig : ScriptableObject
    {
        [Range(.02f,.3f)] public float stickRadiusScreenFraction=.115f;
        [Range(0,.9f)] public float aimDeadZone=.15f;
        [Range(.1f,1)] public float touchAreaHeight=.48f;
        public void Validate()
        {
            stickRadiusScreenFraction=MvpConfigValidation.Number(stickRadiusScreenFraction,.02f,.3f,.115f);
            aimDeadZone=MvpConfigValidation.Number(aimDeadZone,0,.9f,.15f);
            touchAreaHeight=MvpConfigValidation.Number(touchAreaHeight,.1f,1,.48f);
        }
        void OnValidate()=>Validate();
    }
}
