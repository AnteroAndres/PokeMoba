using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Plays the supplied clips from actual movement and cast events, without moving gameplay roots.</summary>
    public class PokemonAnimationDriver : MonoBehaviour
    {
        Animation animationPlayer;
        CreatureMotor motor;
        Combatant owner;
        AbilityCaster caster;
        string current;
        float actionUntil;
        bool sleeping;

        public static bool TryAttach(GameObject model)
        {
            foreach (var player in model.GetComponentsInChildren<Animation>())
            {
                if (player["idle"] == null || player["walk"] == null || player["attack"] == null) continue;
                var driver = model.AddComponent<PokemonAnimationDriver>();
                driver.animationPlayer = player;
                foreach (AnimationState state in player)
                    state.wrapMode = state.name == "attack" || state.name == "happy" ? WrapMode.Once : WrapMode.Loop;
                player.playAutomatically = false;
                player.Stop();
                driver.Play("idle");
                return true;
            }
            return false;
        }

        void Start()
        {
            motor = GetComponentInParent<CreatureMotor>();
            owner = GetComponentInParent<Combatant>();
            caster = GetComponentInParent<AbilityCaster>();
            if (caster != null) caster.Casted += OnCast;
        }

        void OnCast(int slot)
        {
            if (owner != null && owner.IsDead) return;
            sleeping = false;
            float duration = .55f;
            if (caster != null) duration = Mathf.Clamp(caster.GetMove(slot).Windup + .3f, .3f, 1.4f);
            var attack = animationPlayer["attack"];
            attack.speed = attack.length / duration;
            actionUntil = Time.time + duration;
            current = null; // A second successful cast restarts the attack rather than leaving the old pose.
            Play("attack");
        }

        public void SetSleeping(bool value) => sleeping = value;
        public void Celebrate()
        {
            if (animationPlayer["happy"] == null || owner != null && owner.IsDead) return;
            actionUntil = Time.time + animationPlayer["happy"].length;
            current = null;
            Play("happy");
        }

        void Update()
        {
            if (animationPlayer == null || Time.timeScale <= 0) return;
            if (owner != null && owner.IsDead)
            {
                // Combatant already positions the defeated model. Do not continue an attack in that pose.
                foreach (AnimationState state in animationPlayer) state.speed = 0;
                return;
            }
            if (current == "attack" && motor != null && motor.IsDodging) actionUntil = 0;
            if (Time.time < actionUntil) return;
            float speed = motor == null ? 0 : new Vector3(motor.Velocity.x, 0, motor.Velocity.z).magnitude;
            string target = sleeping && animationPlayer["sleep"] != null ? "sleep"
                : speed < .12f ? "idle"
                : speed > (motor != null ? motor.MoveSpeed * .65f : 2.5f) && animationPlayer["run"] != null ? "run" : "walk";
            var stateToPlay = animationPlayer[target];
            stateToPlay.speed = target == "walk" || target == "run"
                ? Mathf.Clamp(speed / (motor != null ? Mathf.Max(1, motor.MoveSpeed) : 4), .6f, 1.6f) : 1;
            Play(target);
        }

        void Play(string clip)
        {
            if (clip == current) return;
            current = clip;
            animationPlayer.CrossFade(clip, .12f, PlayMode.StopAll);
        }

        void OnDestroy()
        {
            if (caster != null) caster.Casted -= OnCast;
        }
    }
}
