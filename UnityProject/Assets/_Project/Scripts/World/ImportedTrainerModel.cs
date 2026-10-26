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

        public static ImportedTrainerModel TryBuild(Transform root)
        {
            var asset = Resources.Load<GameObject>(Folder + "N02_321out");
            if (asset == null) return null;
            var adapter = root.gameObject.AddComponent<ImportedTrainerModel>();
            var model = Instantiate(asset, root);
            model.name = "Master Roshi";
            adapter.Configure(model.transform);
            return adapter;
        }

        Transform Find(Transform model, string name)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        void Configure(Transform model)
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
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.localScale *= 1.95f / Mathf.Max(.01f, bounds.size.y);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                model.position += new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y, transform.position.z - bounds.center.z);
            }
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
            foreach (var name in new[] { "JointHipL", "JointHipR", "JointShoulderL", "JointShoulderR" })
            {
                var joint = Find(model, name);
                if (joint == null) continue;
                joints.Add(joint); rest.Add(joint.localRotation);
                amplitudes.Add(name.EndsWith("L") ? 1f : -1f);
            }
        }

        public void Animate(float walking)
        {
            float speed = Mathf.Clamp01(walking);
            phase += Time.deltaTime * 8f * speed;
            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                joint.localRotation = rest[i];
                // Girar en el eje lateral del personaje conservando las rotaciones del bind pose.
                Vector3 axis = joint.InverseTransformDirection(transform.right);
                float angle = Mathf.Sin(phase) * amplitudes[i] * (joint.name.Contains("Hip") ? 20f : -10f) * speed;
                joint.localRotation = rest[i] * Quaternion.AngleAxis(angle, axis);
            }
        }
        void OnDestroy() { foreach (var material in owned) if (material != null) Destroy(material); }
    }
}
