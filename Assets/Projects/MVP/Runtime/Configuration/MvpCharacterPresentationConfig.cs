using System;
using UnityEngine;

namespace BounceFit.Mvp
{
    /// <summary>Presentation-only assembly and pose data for the modular player sprites.</summary>
    [CreateAssetMenu(menuName = "Bounce Fit/Config/Character Presentation", fileName = "CharacterPresentationConfig")]
    public sealed class MvpCharacterPresentationConfig : ScriptableObject
    {
        [Serializable]
        public struct Part
        {
            public Vector3 position;
            public Vector3 scale;
            [Tooltip("Normalized sprite anchor. (0.5, 0.5) is the centered source-png pivot.")]
            public Vector2 pivot;
        }

        [Serializable]
        public struct Pose
        {
            public Part head;
            public Part body;
            public Part tail;
            public Part weapon;
            [Tooltip("Local projectile origin before horizontal mirroring.")]
            public Vector3 muzzle;
        }

        public Pose back = DefaultBack();
        public Pose diagonal = DefaultDiagonal();
        public Pose side = DefaultSide();
        [Header("Direction selection")]
        [Range(0f, 80f)] public float backYawThreshold = 20f;
        [Range(0f, 89f)] public float diagonalYawThreshold = 60f;
        [Range(0f, 1f)] public float weaponAimTurnFraction = .4f;
        [Header("Fire response")]
        [Min(0.01f)] public float recoilDistance = 0.12f;
        [Min(0.01f)] public float headRecoilDistance = .06f;
        [Min(0.01f)] public float recoilSeconds = 0.1f;
        [Min(0f)] public float tailSwayDelay = .025f;
        [Range(0f, 45f)] public float tailSwayDegrees = 10f;

        static MvpCharacterPresentationConfig cachedDefault;
        public static MvpCharacterPresentationConfig Default
        {
            get
            {
                if (!cachedDefault) cachedDefault = Resources.Load<MvpCharacterPresentationConfig>("MVP/CharacterPresentation");
                if (!cachedDefault)
                {
                    cachedDefault = CreateInstance<MvpCharacterPresentationConfig>();
                    cachedDefault.hideFlags = HideFlags.HideAndDontSave;
                }
                return cachedDefault;
            }
        }

        public Pose PoseFor(MvpCharacterDirection direction) => direction == MvpCharacterDirection.Back ? back : direction == MvpCharacterDirection.Diagonal ? diagonal : side;
        public void Validate()
        {
            back = Valid(back, DefaultBack());
            diagonal = Valid(diagonal, DefaultDiagonal());
            side = Valid(side, DefaultSide());
            backYawThreshold = MvpConfigValidation.Number(backYawThreshold, 0f, 80f, 20f);
            diagonalYawThreshold = MvpConfigValidation.Number(diagonalYawThreshold, backYawThreshold, 89f, 60f);
            weaponAimTurnFraction = MvpConfigValidation.Number(weaponAimTurnFraction, 0f, 1f, .4f);
            recoilDistance = MvpConfigValidation.Number(recoilDistance, 0.01f, 2f, 0.12f);
            headRecoilDistance = MvpConfigValidation.Number(headRecoilDistance, 0.01f, 2f, .06f);
            recoilSeconds = MvpConfigValidation.Number(recoilSeconds, 0.01f, 1f, .1f);
            tailSwayDelay = MvpConfigValidation.Number(tailSwayDelay, 0f, recoilSeconds, .025f);
            tailSwayDegrees = MvpConfigValidation.Number(tailSwayDegrees, 0f, 45f, 10f);
        }
        static Pose Valid(Pose value, Pose fallback) => new Pose { head = Valid(value.head, fallback.head), body = Valid(value.body, fallback.body), tail = Valid(value.tail, fallback.tail), weapon = Valid(value.weapon, fallback.weapon), muzzle = MvpConfigValidation.Vector(value.muzzle, fallback.muzzle) };
        static Part Valid(Part value, Part fallback) => new Part { position = MvpConfigValidation.Vector(value.position, fallback.position), scale = new Vector3(MvpConfigValidation.Number(value.scale.x, .01f, 10f, fallback.scale.x), MvpConfigValidation.Number(value.scale.y, .01f, 10f, fallback.scale.y), MvpConfigValidation.Number(value.scale.z, .01f, 10f, fallback.scale.z)), pivot = new Vector2(MvpConfigValidation.Number(value.pivot.x, 0f, 1f, fallback.pivot.x), MvpConfigValidation.Number(value.pivot.y, 0f, 1f, fallback.pivot.y)) };
        static Part P(float x, float y, float sx, float sy) => new Part { position = new Vector3(x, y, 0), scale = new Vector3(sx, sy, 1), pivot = new Vector2(.5f, .5f) };
        static Part WeaponPart() => new Part { position = new Vector3(-.34f, .48f, 0), scale = new Vector3(.68f, .68f, 1), pivot = new Vector2(.57f, .37f) };
        // The generated sources are 512px at 256 pixels/unit; these are the common assembled-source baseline.
        static Pose DefaultBack() => new Pose { head = P(0, 1f, .55f, .55f), body = P(0, .32f, .44f, .44f), tail = P(.52f, .36f, .40f, .40f), weapon = WeaponPart(), muzzle = new Vector3(-.435f, 1.038f, 0) };
        static Pose DefaultDiagonal() => new Pose { head = P(0, 1f, .55f, .55f), body = P(0, .32f, .44f, .44f), tail = P(.52f, .36f, .40f, .40f), weapon = WeaponPart(), muzzle = new Vector3(-.435f, 1.038f, 0) };
        static Pose DefaultSide() => new Pose { head = P(0, 1f, .55f, .55f), body = P(0, .32f, .44f, .44f), tail = P(.52f, .36f, .40f, .40f), weapon = WeaponPart(), muzzle = new Vector3(-.435f, 1.038f, 0) };
        void OnValidate() => Validate();
    }

    public enum MvpCharacterDirection { Back, Diagonal, Side }
}
