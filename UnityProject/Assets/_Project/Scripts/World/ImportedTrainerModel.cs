using System.Collections.Generic;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Adaptador del modelo con skin suministrado por el usuario. Sin clips originales.</summary>
    public class ImportedTrainerModel : MonoBehaviour
    {
        const string Folder = "Models/Trainers/MasterRoshi/";
        readonly List<Material> owned = new List<Material>();
        readonly List<Transform> joints = new List<Transform>();
        readonly List<Quaternion> rest = new List<Quaternion>();
        readonly List<float> amplitudes = new List<float>();
        float phase;
        AshTrainerMotion animatedAsh;
        PackageTrainerMotion packageMotion;

        public static ImportedTrainerModel TryBuild(Transform root)
        {
            return TryBuild(root, "MasterRoshi/N02_321out");
        }

        public static ImportedTrainerModel TryBuild(Transform root, string modelPath)
        {
            modelPath = PokeMoba.Game.TrainerCharacterCatalog.Resolve(modelPath);
            bool fromPackage = modelPath != null && modelPath.StartsWith("Package/", System.StringComparison.Ordinal);
            var asset = fromPackage ? Resources.Load<GameObject>("Models/Trainers/" + modelPath + "/Model")
                : Resources.Load<GameObject>("Models/Trainers/ImportedFbx/" + modelPath);
            if (asset == null && modelPath == "MasterRoshi/N02_321out")
                asset = Resources.Load<GameObject>(Folder + "N02_321out");
            if (asset == null) return null;
            var adapter = root.gameObject.AddComponent<ImportedTrainerModel>();
            var model = Instantiate(asset, root);
            model.name = modelPath;
            if (fromPackage)
            {
                adapter.packageMotion = PackageTrainerMotion.Configure(model);
                return adapter;
            }
            if (modelPath == AshTrainerMotion.CharacterKey)
            {
                adapter.animatedAsh = AshTrainerMotion.Configure(model);
                if (adapter.animatedAsh == null)
                {
                    Destroy(model);
                    Destroy(adapter);
                    return null;
                }
                return adapter;
            }
            adapter.Configure(model.transform, modelPath);
            return adapter;
        }

        Transform Find(Transform model, string name)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        void Configure(Transform model, string modelPath)
        {
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Destroy(collider);
            foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            var head = Find(model, "JointHead");
            var left = Find(model, "JointHipL"); var right = Find(model, "JointHipR");
            if (head != null && left != null && right != null)
            {
                var direction = head.position - (left.position + right.position) * .5f;
                direction.y = 0f;
                if (direction.sqrMagnitude > .001f)
                    model.rotation = Quaternion.FromToRotation(direction.normalized, transform.forward) * model.rotation;
            }
            var renderers = model.GetComponentsInChildren<Renderer>();
            foreach (var group in model.GetComponentsInChildren<LODGroup>()) group.ForceLOD(0);
            var visible = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                // These FBX files contain coincident LOD0/1/2 meshes, not separate characters.
                bool lowerLod = false;
                for (var node = renderer.transform; node != null && node != model; node = node.parent)
                    lowerLod |= System.Text.RegularExpressions.Regex.IsMatch(node.name, @"_lod[1-9]\d*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (lowerLod) { renderer.enabled = false; continue; }
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.quality = SkinQuality.Bone4;
                    skin.updateWhenOffscreen = true;
                }
                if (renderer.enabled) visible.Add(renderer);
            }
            renderers = visible.ToArray();
            if (modelPath.StartsWith("tr")) RepairMaterials(renderers, modelPath);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.localScale *= 1.95f / Mathf.Max(.01f, bounds.size.y);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.position += new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y, transform.position.z - bounds.center.z);
            }
            // The legacy character's atlas must never overwrite the other trainers' materials.
            if (modelPath == "MasterRoshi/N02_321out")
            {
            var textures = new[] { Resources.Load<Texture2D>(Folder + "N50_514a1"), Resources.Load<Texture2D>(Folder + "N50_514a2") };
            var materials = new Material[2];
            for (int i = 0; i < 2; i++)
            {
                materials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                materials[i].color = Color.white; materials[i].mainTexture = textures[i];
                materials[i].SetFloat("_Glossiness", 0f); materials[i].SetFloat("_Smoothness", 0f);
                owned.Add(materials[i]);
            }
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = materials[slots[i] != null && slots[i].name.Contains("514a2") ? 1 : 0];
                renderer.sharedMaterials = slots;
                if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
            }
            }
            // El archivo viene en pose de referencia con brazos abiertos.
            foreach (var side in new[] { "L", "R" })
            {
                var shoulder = Find(model, "JointShoulder" + side);
                var elbow = Find(model, "JointElbow" + side);
                if (shoulder == null || elbow == null) continue;
                var current = elbow.position - shoulder.position;
                var relaxed = -transform.up + transform.right * (side == "L" ? -.15f : .15f);
                if (current.sqrMagnitude > .0001f)
                    shoulder.rotation = Quaternion.FromToRotation(current.normalized, relaxed.normalized) * shoulder.rotation;
            }
            foreach (var side in new[] { "left", "right" })
            {
                var shoulder = Find(model, side + "_arm_01");
                var elbow = Find(model, side + "_arm_02");
                if (shoulder != null && elbow != null)
                {
                    Vector3 direction = elbow.position - shoulder.position;
                    if (direction.sqrMagnitude > .0001f)
                        shoulder.rotation = Quaternion.FromToRotation(direction.normalized,
                            (-transform.up + transform.right * (side == "left" ? -.18f : .18f)).normalized) * shoulder.rotation;
                }
            }
            foreach (var name in new[] { "JointHipL", "JointHipR", "JointShoulderL", "JointShoulderR",
                "left_leg_01", "right_leg_01", "left_arm_01", "right_arm_01", "left_leg_02", "right_leg_02" })
            {
                var joint = Find(model, name);
                if (joint == null) continue;
                joints.Add(joint); rest.Add(joint.localRotation);
                amplitudes.Add(name.EndsWith("L") || name.StartsWith("left") ? 1f : -1f);
            }
        }

        void RepairMaterials(Renderer[] renderers, string modelPath)
        {
            var slash = modelPath.IndexOf('/');
            if (slash < 0) return;
            string folder = modelPath.Substring(0, slash);
            string textureRoot = "Models/Trainers/ImportedFbx/" + folder + "/";
            var textures = Resources.LoadAll<Texture2D>(textureRoot);
            Texture2D FindTexture(string key)
            {
                for (int i = 0; i < textures.Length; i++)
                    if (textures[i].name.ToLowerInvariant() == key || textures[i].name.ToLowerInvariant().Contains("_" + key + "_alb")) return textures[i];
                return null;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var repaired = new Dictionary<Material, Material>();
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var original = slots[i];
                    if (original == null) continue;
                    if (repaired.TryGetValue(original, out var cached)) { slots[i] = cached; continue; }
                    string name = original.name.ToLowerInvariant();
                    Texture2D texture = original.mainTexture != null ? FindTexture(original.mainTexture.name.ToLowerInvariant()) : null;
                    if (texture == null) texture = name.Contains("face") ? FindTexture("face")
                        : name.Contains("eye") ? FindTexture("eye")
                        : name.Contains("hair") ? FindTexture("hairstyle")
                        : name.Contains("shoe") ? FindTexture("shoe")
                        : name.Contains("baggage") ? FindTexture("baggage")
                        : name.Contains("cap") ? FindTexture("cap")
                        : name.Contains("obj01") ? FindTexture("obj01")
                        : name.Contains("obj02") ? FindTexture("obj02")
                        : name.Contains("obj") ? FindTexture("obj")
                        : name.Contains("obi") ? FindTexture("obi")
                        : name.Contains("ring") ? FindTexture("ring")
                        : name.Contains("body") ? FindTexture("body") : null;
                    if (texture == null && name.Contains("shoe")) texture = FindTexture("shoes");
                    if (texture == null)
                    {
                        Debug.LogWarning($"Textura no resuelta: {modelPath} / {original.name}. Se conserva el material original.");
                        continue;
                    }
                    var material = new Material(shader) { name = original.name + " Fixed" };
                    material.color = Color.white;
                    material.mainTexture = texture;
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Glossiness", 0f);
                    material.SetFloat("_Smoothness", 0f);
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", .3f);
                    material.SetFloat("_Cull", 0f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    owned.Add(material);
                    repaired[original] = material;
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
            }
        }

        public void Animate(float walking)
        {
            if (packageMotion != null) { packageMotion.Animate(walking); return; }
            if (animatedAsh != null) { animatedAsh.Animate(walking); return; }
            float speed = Mathf.Clamp01(walking);
            phase += Time.deltaTime * 8f * speed;
            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                joint.localRotation = rest[i];
                // Girar en el eje lateral del personaje conservando las rotaciones del bind pose.
                Vector3 axis = joint.InverseTransformDirection(transform.right);
                float angle = Mathf.Sin(phase) * amplitudes[i] * (joint.name.Contains("Hip") || joint.name.Contains("leg_01") ? 25f : joint.name.Contains("leg_02") ? 12f : -18f) * speed;
                joint.localRotation = rest[i] * Quaternion.AngleAxis(angle, axis);
            }
        }
        void OnDestroy() { foreach (var material in owned) if (material != null) Destroy(material); }
    }
}
