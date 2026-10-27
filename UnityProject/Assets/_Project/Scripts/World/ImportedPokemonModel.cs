using System.Collections.Generic;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Fits imported art to the gameplay root, preserving its mesh, skin and textures.</summary>
    public class ImportedPokemonModel : MonoBehaviour
    {
        readonly List<Material> ownedMaterials = new List<Material>();
        Vector3 restScale;
        Quaternion restRotation;
        CreatureMotor motor;
        float phase;
        float walkPhase;
        float walkWeight;
        Combatant owner;
        readonly List<BoneMotion> bones = new List<BoneMotion>();
        struct BoneMotion
        {
            public Transform Bone;
            public Quaternion Rest;
            public Vector3 Axis;
            public int Kind; // 0: pata superior, 1: rodilla, 2: brazo, 3: cola, 4: ala
            public float Side;
            public float Delay;
        }

        void BindBones()
        {
            var unique = new HashSet<Transform>();
            int tailIndex = 0;
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            foreach (var bone in skin.bones)
            {
                if (bone == null || !unique.Add(bone)) continue;
                string name = bone.name.ToLowerInvariant();
                int colon = name.LastIndexOf(':');
                if (colon >= 0) name = name.Substring(colon + 1);
                if (name.StartsWith("end") || name.Contains("finger") || name.Contains("toe")) continue;
                int kind = -1;
                if (name.Contains("tail") || name.Contains("sipo")) kind = 3;
                else if (name.Contains("wing") && !name.Contains("tip")) kind = 4;
                else if (name.Contains("thigh") || name.EndsWith("leg_01")) kind = 0;
                else if (name == "lleg" || name == "rleg" || name.Contains("knee") || name.EndsWith("leg_02")) kind = 1;
                else if (name == "larm" || name == "rarm" || name.EndsWith("arm_01")) kind = 2;
                if (kind < 0) continue;
                float side = name.StartsWith("l") || name.Contains("left") ? 1f : -1f;
                Vector3 worldAxis = kind == 3 ? transform.parent.up : kind == 4 ? transform.parent.forward : transform.parent.right;
                bones.Add(new BoneMotion { Bone = bone, Rest = bone.localRotation,
                    Axis = bone.InverseTransformDirection(worldAxis).normalized, Kind = kind, Side = side,
                    Delay = kind == 3 ? tailIndex++ * .55f : 0f });
            }
        }

        public static void Configure(GameObject instance, float height, bool shiny, bool animateFallback)
        {
            // Establish the imported skeleton's pose before measuring its skinned geometry.
            foreach (var animation in instance.GetComponentsInChildren<Animation>())
            {
                if (animation["idle"] == null) continue;
                animation.Stop();
                animation.Play("idle");
                animation["idle"].time = 0;
                animation.Sample();
            }
            // Measure in the gameplay parent's coordinates: team/world rotation cannot alter the size.
            var parent = instance.transform.parent;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = BoundsInParent(parent, renderers);
            if (bounds.size.y > .001f) instance.transform.localScale *= height / bounds.size.y;
            bounds = BoundsInParent(parent, renderers);
            instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            var adapter = instance.AddComponent<ImportedPokemonModel>();
            adapter.restScale = instance.transform.localScale;
            adapter.restRotation = instance.transform.localRotation;
            adapter.phase = Random.value * Mathf.PI * 2;
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.updateWhenOffscreen = true;
            if (shiny)
            {
                // The supplied archive has no complete shiny set. Preserve the textures and tint copies.
                foreach (var renderer in renderers)
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        var copy = new Material(slots[i]);
                        copy.color = new Color(.78f, 1f, .84f, copy.color.a);
                        adapter.ownedMaterials.Add(copy);
                        slots[i] = copy;
                    }
                    renderer.sharedMaterials = slots;
                }
            }
            bool hasClip = PokemonAnimationDriver.TryAttach(instance);
            if (!hasClip) foreach (var animation in instance.GetComponentsInChildren<Animation>())
            {
                AnimationState first = null;
                foreach (AnimationState state in animation)
                {
                    if (first == null) first = state;
                    if (state.name.ToLowerInvariant().Contains("idle")) { first = state; break; }
                }
                if (first == null) continue;
                first.wrapMode = WrapMode.Loop;
                animation.Play(first.name);
                hasClip = true;
            }
            adapter.BindBones();
            adapter.enabled = !hasClip && (animateFallback || adapter.bones.Count > 0);
        }

        static Bounds BoundsInParent(Transform parent, Renderer[] renderers)
        {
            Bounds result = default;
            bool initialized = false;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer is SkinnedMeshRenderer skin)
                {
                    var baked = new Mesh();
                    // BakeMesh(mesh) retains the skin's scale in its vertices. We apply the
                    // transform matrix below, so request scale compensation to avoid squaring it.
                    skin.BakeMesh(baked, true);
                    var skinMatrix = parent.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                    foreach (var vertex in baked.vertices)
                    {
                        var point = skinMatrix.MultiplyPoint3x4(vertex);
                        if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                        else result.Encapsulate(point);
                    }
                    if (Application.isPlaying) Object.Destroy(baked);
                    else Object.DestroyImmediate(baked);
                    continue;
                }
                var local = renderer.localBounds;
                var matrix = parent.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3(x,y,z)));
                            if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                            else result.Encapsulate(point);
                        }
            }
            return result;
        }

        void Start()
        {
            motor = GetComponentInParent<CreatureMotor>();
            owner = GetComponentInParent<Combatant>();
        }
        void LateUpdate()
        {
            if (Time.timeScale <= 0f) return;
            phase += Time.deltaTime;
            float speed = motor == null ? 0 : new Vector3(motor.Velocity.x, 0, motor.Velocity.z).magnitude;
            float stride = owner != null && owner.IsDead ? 0f : Mathf.Clamp01(speed / (motor != null ? Mathf.Max(1f, motor.MoveSpeed) : 4f));
            walkWeight = Mathf.MoveTowards(walkWeight, stride, Time.deltaTime * 5f);
            walkPhase += Time.deltaTime * Mathf.Lerp(5f, 12f, stride);
            bool dead = owner != null && owner.IsDead;
            foreach (var motion in bones)
            {
                if (motion.Bone == null) continue;
                float step = Mathf.Sin(walkPhase) * motion.Side;
                float angle = motion.Kind == 0 ? step * 30f * walkWeight
                    : motion.Kind == 1 ? Mathf.Max(0f, -step) * 24f * walkWeight
                    : motion.Kind == 2 ? -step * 24f * walkWeight + Mathf.Sin(phase * 2f) * 2f
                    : motion.Kind == 3 ? Mathf.Sin(phase * (2.5f + walkWeight * 2f) - motion.Delay) * (5f + walkWeight * 7f)
                    : Mathf.Sin(phase * 9f) * motion.Side * 22f;
                motion.Bone.localRotation = motion.Rest * Quaternion.AngleAxis(dead ? 0f : angle, motion.Axis);
            }
            float breath = Mathf.Sin(phase * 2.2f) * .012f;
            transform.localScale = Vector3.Scale(restScale, new Vector3(1 - breath * .3f, 1 + breath, 1 - breath * .3f));
            transform.localRotation = restRotation * Quaternion.Euler(0, 0, 0f);
        }
        void OnDestroy()
        {
            foreach (var material in ownedMaterials) if (material != null) Destroy(material);
        }
    }
}
