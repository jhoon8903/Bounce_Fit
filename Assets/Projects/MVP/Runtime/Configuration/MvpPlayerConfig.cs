using UnityEngine;
namespace BounceFit.Mvp
{
    [CreateAssetMenu(menuName="Bounce Fit/Config/Player",fileName="PlayerConfig")]
    public sealed class MvpPlayerConfig : ScriptableObject
    {
        [Header("Live movement / health")]
        [Min(0)] public float moveSpeed=4.1f;
        [Min(1)] public float maxHealth=100f;
        [Min(.01f)] public float hitRadius=.25f;
        [Min(.01f)] public float contactRadius=.32f;
        [Min(0)] public float invulnerabilitySeconds=.7f;
        [Tooltip("Applied on the next run/reset. World X / forward Z.")]
        public Vector2 startPosition=new(0,-3);
        public void Validate()
        {
            moveSpeed=MvpConfigValidation.Number(moveSpeed,0,100,4.1f);
            maxHealth=MvpConfigValidation.Number(maxHealth,1,1000000,100);
            hitRadius=MvpConfigValidation.Number(hitRadius,.01f,3,.25f);
            contactRadius=MvpConfigValidation.Number(contactRadius,.01f,3,.32f);
            invulnerabilitySeconds=MvpConfigValidation.Number(invulnerabilitySeconds,0,60,.7f);
            startPosition=MvpConfigValidation.Vector(startPosition,new Vector2(0,-3));
        }
        void OnValidate()=>Validate();
    }
    internal static class MvpConfigValidation
    {
        public static float Number(float value,float min,float max,float fallback)
            => Mathf.Clamp(float.IsNaN(value)||float.IsInfinity(value)?fallback:value,min,max);
        public static Vector2 Vector(Vector2 value,Vector2 fallback)=>new(Number(value.x,-1000,1000,fallback.x),Number(value.y,-1000,1000,fallback.y));
        public static Vector3 Vector(Vector3 value,Vector3 fallback)=>new(Number(value.x,-1000,1000,fallback.x),Number(value.y,-1000,1000,fallback.y),Number(value.z,-1000,1000,fallback.z));
    }
}
