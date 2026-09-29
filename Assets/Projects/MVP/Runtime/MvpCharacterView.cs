using UnityEngine;

namespace BounceFit.Mvp
{
    /// <summary>Camera-facing, modular player art. The owning game keeps simulation and hit feedback separate.</summary>
    public sealed class MvpCharacterView : MonoBehaviour
    {
        public const string ResourcePath = "MVP/Characters/Raccoon/";
        Transform billboard, muzzle;
        SpriteRenderer head, body, tail, weapon;
        MvpCharacterPresentationConfig config;
        float recoilRemaining;
        readonly Sprite[,] sprites = new Sprite[3, 4];

        public Vector3 MuzzleWorldPosition => muzzle ? muzzle.position : transform.position;
        public MvpCharacterPresentationConfig PresentationConfig => config;

        public void Initialize(Camera camera, Material material, MvpCharacterPresentationConfig presentation = null)
        {
            config = presentation ? presentation : MvpCharacterPresentationConfig.Default;
            billboard = new GameObject("Raccoon billboard").transform;
            billboard.SetParent(transform, false);
            tail = CreatePart("Tail", material);
            body = CreatePart("Body", material);
            head = CreatePart("Head", material);
            weapon = CreatePart("Weapon", material);
            CacheSprites();
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(billboard, false);
            UpdatePresentation(camera, Vector2.up, 0f);
        }

        public void UpdatePresentation(Camera camera, Vector2 aim, float animationDelta = 0f)
        {
            if (!billboard || !camera) return;
            if (animationDelta > 0f && recoilRemaining > 0f) recoilRemaining = Mathf.Max(0f, recoilRemaining - animationDelta);
            billboard.rotation = camera.transform.rotation;
            float yaw = Mathf.Atan2(aim.x, aim.y) * Mathf.Rad2Deg;
            float absoluteYaw = Mathf.Abs(yaw);
            var direction = absoluteYaw <= config.backYawThreshold ? MvpCharacterDirection.Back : absoluteYaw <= config.diagonalYawThreshold ? MvpCharacterDirection.Diagonal : MvpCharacterDirection.Side;
            bool right = yaw > 0f;
            var pose = config.PoseFor(direction);
            int sortingBase = 1000 - Mathf.RoundToInt(transform.position.z * 20f);
            Apply(tail, pose.tail, direction, 2, right, sortingBase);
            Apply(body, pose.body, direction, 1, right, sortingBase + 1);
            Apply(head, pose.head, direction, 0, right, sortingBase + 2);
            Apply(weapon, pose.weapon, direction, 3, right, sortingBase + 3);
            float recoil01 = Recoil01;
            head.transform.localPosition += Vector3.right * config.headRecoilDistance * recoil01 * (right ? -1f : 1f);
            float tail01 = TailSway01;
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, config.tailSwayDegrees * tail01 * (right ? 1f : -1f));
            Quaternion weaponRotation = Quaternion.Euler(0f, 0f, -yaw * config.weaponAimTurnFraction);
            Vector3 weaponAnchor = pose.weapon.position;
            weaponAnchor.x *= right ? -1f : 1f;
            weaponAnchor += Vector3.right * WeaponRecoilX(right);
            weapon.transform.localPosition = weaponAnchor + weaponRotation * (weapon.transform.localPosition - weaponAnchor);
            weapon.transform.localRotation = weaponRotation;
            Vector3 muzzlePosition = pose.muzzle;
            muzzlePosition.x *= right ? -1f : 1f;
            muzzlePosition += Vector3.right * WeaponRecoilX(right);
            muzzle.localPosition = weaponAnchor + weaponRotation * (muzzlePosition - weaponAnchor);
        }

        public void TriggerWeaponRecoil() => recoilRemaining = config ? config.recoilSeconds : .08f;

        void CacheSprites()
        {
            for (int direction = 0; direction < 3; direction++)
            {
                var name = (MvpCharacterDirection)direction;
                sprites[direction, 0] = Resources.Load<Sprite>(ResourcePath + name + "_Head");
                sprites[direction, 1] = Resources.Load<Sprite>(ResourcePath + name + "_Body");
                sprites[direction, 2] = Resources.Load<Sprite>(ResourcePath + name + "_Tail");
                sprites[direction, 3] = Resources.Load<Sprite>(ResourcePath + name + "_Weapon");
            }
        }

        SpriteRenderer CreatePart(string label, Material material)
        {
            var part = new GameObject(label).AddComponent<SpriteRenderer>();
            part.transform.SetParent(billboard, false);
            if (material) part.sharedMaterial = material;
            return part;
        }

        void Apply(SpriteRenderer renderer, MvpCharacterPresentationConfig.Part part, MvpCharacterDirection direction, int partIndex, bool right, int sortingOrder)
        {
            renderer.sprite = sprites[(int)direction, partIndex];
            renderer.flipX = right;
            Vector3 position = part.position;
            position.x *= right ? -1f : 1f;
            if (renderer.sprite)
            {
                var spriteSize = renderer.sprite.bounds.size;
                var pivotOffset = new Vector3((.5f - part.pivot.x) * spriteSize.x * part.scale.x, (.5f - part.pivot.y) * spriteSize.y * part.scale.y, 0f);
                pivotOffset.x *= right ? -1f : 1f;
                position += pivotOffset;
            }
            if (partIndex == 3) position += Vector3.right * WeaponRecoilX(right);
            renderer.transform.localPosition = position;
            renderer.transform.localScale = part.scale;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.sortingOrder = sortingOrder;
        }

        float Recoil01 => recoilRemaining <= 0f || !config ? 0f : recoilRemaining / config.recoilSeconds;
        float TailSway01
        {
            get
            {
                if (!config || recoilRemaining <= 0f) return 0f;
                float elapsed = config.recoilSeconds - recoilRemaining;
                if (elapsed < config.tailSwayDelay) return 0f;
                float duration = Mathf.Max(.001f, config.recoilSeconds - config.tailSwayDelay);
                return Mathf.Sin(Mathf.Clamp01((elapsed - config.tailSwayDelay) / duration) * Mathf.PI);
            }
        }
        float WeaponRecoilX(bool right) => config.recoilDistance * Recoil01 * (right ? -1f : 1f);
    }
}
