using System;
using System.Collections.Generic;
using System.Linq;
using PokeMoba.AI;
using PokeMoba.Combat;
using PokeMoba.World;
using UnityEngine;

namespace PokeMoba.Input
{
    /// <summary>
    /// Control del entrenador en la Aventura (estilo Legends: Z-A): el joystick mueve al entrenador,
    /// los 4 botones de movimiento dan órdenes al Pokémon compañero; Espacio ordena esquivar al Pokémon
    /// en combate y hace saltar al entrenador durante la exploración.
    /// Fijar elige el objetivo (cada toque pasa al siguiente enemigo cercano; al final, lo suelta).
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class TrainerController : MonoBehaviour
    {
        const float LockRange = 14f;

        public Combatant LockedTarget => companion != null ? companion.Target : lockedWithoutCompanion;
        /// <summary>Se llama al fijar o soltar un objetivo (para la cámara y el panel del rival).</summary>
        public event Action<Combatant> TargetChanged;
        public Func<bool> EnsureCompanion;
        public Action<bool> ContextChanged;
        public bool BattleActive { get; set; }
        public bool IsInCombat => BattleActive || Time.time < combatUntil ||
            (LockedTarget != null && !LockedTarget.IsDead && LockedTarget.gameObject.activeInHierarchy &&
                CombatQueries.FlatDistance(LockedTarget.transform.position, transform.position) <= LockRange + 4f);

        Combatant self;
        CreatureMotor motor;
        TrainerRig rig;
        VirtualJoystick joystick;
        Camera cam;
        CompanionAI companion;
        Combatant lockedWithoutCompanion;
        TargetMarker marker;
        Combatant companionUnit;
        float combatUntil;

        void Awake()
        {
            self = GetComponent<Combatant>();
            motor = GetComponent<CreatureMotor>();
            rig = GetComponentInChildren<TrainerRig>();
            marker = TargetMarker.Create();
            self.HitBy += OnHit;
        }

        void OnDestroy()
        {
            if (marker != null) Destroy(marker.gameObject);
            if (self != null) self.HitBy -= OnHit;
            if (companionUnit != null) companionUnit.HitBy -= OnHit;
        }

        public void Bind(VirtualJoystick joystick, AbilityButton[] moveButtons, AbilityButton dodgeButton, Camera cam)
        {
            this.joystick = joystick;
            this.cam = cam;

            for (int i = 0; i < moveButtons.Length; i++)
            {
                int slot = i;
                moveButtons[i].Tapped = () => Order(slot);
                moveButtons[i].AimChanged = null;
                moveButtons[i].AimReleased = _ => Order(slot);
            }

            dodgeButton.Tapped = () => PerformContextAction(motor.MoveInput);
            dodgeButton.AimChanged = null;
            dodgeButton.AimReleased = aim => PerformContextAction(ScreenToWorld(aim));
        }

        /// <summary>Nuevo Pokémon compañero (al empezar o al cambiar): conserva el objetivo fijado.</summary>
        public void SetCompanion(Combatant pokemon)
        {
            var previous = LockedTarget;
            if (companionUnit != null) companionUnit.HitBy -= OnHit;
            companionUnit = pokemon;
            if (companionUnit != null) companionUnit.HitBy += OnHit;
            companion = pokemon != null ? pokemon.GetComponent<CompanionAI>() : null;
            SetLock(previous);
        }

        /// <summary>Botón Fijar: siguiente enemigo cercano, o soltar si ya se pasó por todos.</summary>
        public void CycleLock()
        {
            var candidates = Combatant.All
                .Where(c => CombatQueries.IsEnemy(self, c) && c.gameObject.activeInHierarchy)
                .Where(c => CombatQueries.FlatDistance(c.transform.position, transform.position) <= LockRange)
                .OrderBy(c => CombatQueries.FlatDistance(c.transform.position, transform.position))
                .ToList();

            var current = LockedTarget;
            int index = current != null ? candidates.IndexOf(current) : -1;
            SetLock(index + 1 < candidates.Count ? candidates[index + 1] : null);
        }

        public void SetLock(Combatant target)
        {
            if (companion != null) companion.Target = target;
            else lockedWithoutCompanion = target;
            marker.SetTarget(target);
            TargetChanged?.Invoke(target);
        }

        void Order(int slot)
        {
            if (self.IsDead) return;
            if (companion == null && (EnsureCompanion == null || !EnsureCompanion())) return;
            if (companion == null) return;
            if (LockedTarget == null)
            {
                // Sin objetivo fijado: se fija automáticamente el enemigo más cercano.
                var nearest = CombatQueries.NearestEnemy(self, LockRange);
                if (nearest != null) SetLock(nearest);
            }
            companion.Command(slot);
            if (LockedTarget != null) combatUntil = Time.time + 5f;
        }

        void OnHit(Combatant victim, Combatant attacker) => combatUntil = Time.time + 5f;

        public bool PerformContextAction(Vector3 direction)
        {
            if (self == null || self.IsDead || Time.timeScale <= 0f || PokeMoba.UI.MenuScreens.AnyOpen) return false;
            if (!IsInCombat) return motor.TryJump();
            if (companion == null && (EnsureCompanion == null || !EnsureCompanion())) return false;
            return companion != null && companion.Dodge(direction);
        }

        void Update()
        {
            // El objetivo murió o desapareció (capturado): soltarlo.
            var locked = LockedTarget;
            if (locked != null && (locked.IsDead || !locked.gameObject.activeInHierarchy)) SetLock(null);
            ContextChanged?.Invoke(IsInCombat);

            if (self.IsDead || Time.timeScale <= 0f || PokeMoba.UI.MenuScreens.AnyOpen || (!Application.isMobilePlatform && Cursor.lockState != CursorLockMode.Locked))
            {
                if (rig != null) rig.Animate(0f);
                motor.SetMoveInput(Vector3.zero);
                return;
            }

            Vector2 input = InputCompat.KeyboardMove();
            if (joystick != null) input += joystick.Value;
            motor.SetMoveInput(ScreenToWorld(Vector2.ClampMagnitude(input, 1f)));

            if (InputCompat.WasPressed(GameKey.Basic)) Order(0);
            if (InputCompat.WasPressed(GameKey.Skill1)) Order(1);
            if (InputCompat.WasPressed(GameKey.Skill2)) Order(2);
            if (InputCompat.WasPressed(GameKey.Skill3)) Order(3);
            if (InputCompat.WasPressed(GameKey.Dodge)) PerformContextAction(motor.MoveInput);
            if (InputCompat.WasPressed(GameKey.LockOn)) CycleLock();

            // Con objetivo fijado y quieto, el entrenador lo mira.
            if (LockedTarget != null && motor.MoveInput.sqrMagnitude < 0.01f)
                motor.FaceDirection(LockedTarget.transform.position - transform.position);

            if (rig != null) rig.Animate(motor.Velocity.magnitude / Mathf.Max(1f, motor.MoveSpeed));
        }

        Vector3 ScreenToWorld(Vector2 v)
        {
            if (cam == null) return new Vector3(v.x, 0f, v.y);
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();
            return right * v.x + forward * v.y;
        }
    }
}
