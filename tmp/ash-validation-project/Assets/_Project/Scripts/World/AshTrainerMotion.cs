using System;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Original Ash idle/run clips; gameplay locomotion remains on the trainer root.</summary>
    public sealed class AshTrainerMotion : MonoBehaviour
    {
        public const string CharacterKey = "Ash/Ash";
        const string Folder = "Models/Trainers/ImportedFbx/Ash/";
        Animation player;
        Transform hips;
        Vector3 hipsRest;
        Combatant owner;
        string current;

        public static AshTrainerMotion Configure(GameObject model)
        {
            var idle = FindClip("Ash", "idle");
            var run = FindClip("Run", "run");
            if (idle == null || run == null)
            {
                Debug.LogWarning("Ash: faltan los clips de reposo o carrera en el modelo importado.");
                return null;
            }
            foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            var motion = model.AddComponent<AshTrainerMotion>();
            motion.player = model.GetComponent<Animation>() ?? model.AddComponent<Animation>();
            motion.player.playAutomatically = false;
            motion.player.cullingType = AnimationCullingType.AlwaysAnimate;
            motion.player.AddClip(idle, "idle");
            motion.player.AddClip(run, "run");
            motion.player["idle"].wrapMode = WrapMode.Loop;
            motion.player["run"].wrapMode = WrapMode.Loop;
            motion.player.Stop();
            motion.player.Play("idle");
            motion.player["idle"].time = 0;
            motion.player.Sample();
            ImportedPokemonModel.FitToHeight(model, 1.95f);
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.updateWhenOffscreen = true;
            foreach (var bone in model.GetComponentsInChildren<Transform>())
            {
                if (!bone.name.EndsWith(":Hips", StringComparison.Ordinal) && bone.name != "Hips") continue;
                motion.hips = bone;
                motion.hipsRest = bone.localPosition;
                break;
            }
            motion.current = "idle";
            return motion;
        }

        static AnimationClip FindClip(string asset, string clipName)
        {
            AnimationClip fallback = null;
            foreach (var clip in Resources.LoadAll<AnimationClip>(Folder + asset))
            {
                if (clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                if (clip.name == clipName) return clip;
                if (fallback == null) fallback = clip;
            }
            return fallback;
        }

        void Start() => owner = GetComponentInParent<Combatant>();
        public void Animate(float walking)
        {
            if (player == null || owner != null && owner.IsDead) return;
            float speed = Mathf.Clamp01(walking);
            string next = speed > .08f ? "run" : "idle";
            player["run"].speed = Mathf.Lerp(.65f, 1.25f, speed);
            if (current == next) return;
            current = next;
            player.CrossFade(next, .18f, PlayMode.StopAll);
        }

        void LateUpdate()
        {
            if (owner != null && owner.IsDead)
            {
                foreach (AnimationState state in player) state.speed = 0;
                return;
            }
            ConstrainRootMotion();
        }

        public void ConstrainRootMotion()
        {
            if (hips == null) return;
            var position = hips.localPosition;
            position.x = hipsRest.x;
            position.z = hipsRest.z;
            hips.localPosition = position;
        }
    }
}
