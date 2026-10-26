using System;
using System.Collections;
using System.Collections.Generic;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Ejecuta los movimientos según su plantilla de forma y lleva los tiempos de recarga.</summary>
    [RequireComponent(typeof(Combatant))]
    public class AbilityCaster : MonoBehaviour
    {
        public const int BasicSlot = 0;
        public const int SlotCount = 3;

        public event Action<int> Casted;

        Combatant self;
        CreatureMotor motor;
        readonly float[] cooldowns = new float[SlotCount];
        bool casting;

        void Awake()
        {
            self = GetComponent<Combatant>();
            motor = GetComponent<CreatureMotor>();
        }

        public MoveData GetMove(int slot) => self.Species.Moves[slot];

        public float CooldownRemaining(int slot) => cooldowns[slot];

        public float CooldownFraction(int slot)
        {
            float total = GetMove(slot).Cooldown;
            return total <= 0f ? 0f : Mathf.Clamp01(cooldowns[slot] / total);
        }

        public bool IsReady(int slot) => cooldowns[slot] <= 0f;

        public bool CanCast(int slot) => !self.IsDead && !casting && !motor.IsDodging && IsReady(slot);

        void Update()
        {
            for (int i = 0; i < SlotCount; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - Time.deltaTime);
        }

        /// <param name="direction">Dirección de lanzamiento en el plano XZ.</param>
        /// <param name="targetPoint">Punto objetivo (solo lo usan las áreas en el suelo).</param>
        public bool TryCast(int slot, Vector3 direction, Vector3 targetPoint)
        {
            if (!CanCast(slot)) return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = motor.Facing;
            direction.Normalize();

            var move = GetMove(slot);
            cooldowns[slot] = move.Cooldown;
            StartCoroutine(CastRoutine(move, direction, targetPoint));
            Casted?.Invoke(slot);
            return true;
        }

        IEnumerator CastRoutine(MoveData move, Vector3 direction, Vector3 targetPoint)
        {
            casting = true;
            motor.FaceDirection(direction);
            motor.LockMovement(move.Windup + 0.05f);

            yield return new WaitForSeconds(move.Windup);

            // Esquivar durante la preparación cancela el movimiento (la recarga se mantiene).
            if (self.IsDead || motor.IsDodging)
            {
                casting = false;
                yield break;
            }

            switch (move.Shape)
            {
                case MoveShape.Melee:
                    HitAll(move, CombatQueries.EnemiesInCone(self, transform.position, direction, move.Range, CombatTuning.MeleeAngle));
                    Vfx.Slash(transform.position + Vector3.up * 0.8f + direction * 1.3f, direction, Color.white, move.Radius * 1.4f);
                    break;

                case MoveShape.Projectile:
                    Projectile.Spawn(self, move, transform.position + Vector3.up * 0.8f + direction * 0.9f, direction);
                    break;

                case MoveShape.Cone:
                    HitAll(move, CombatQueries.EnemiesInCone(self, transform.position, direction, move.Range, move.Radius));
                    Vfx.ConeBurst(transform.position + Vector3.up * 0.8f, direction, move.Range, move.Radius, move.Type.TypeColor());
                    break;

                case MoveShape.GroundArea:
                    AreaTelegraph.Spawn(self, move, ClampToRange(targetPoint, move.Range));
                    break;

                case MoveShape.Dash:
                    yield return DashRoutine(move, direction);
                    break;
            }

            casting = false;
        }

        IEnumerator DashRoutine(MoveData move, Vector3 direction)
        {
            float duration = move.Range / CombatTuning.DashSpeed;
            motor.LockMovement(duration);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (self.IsDead) yield break;

                motor.Displace(direction * CombatTuning.DashSpeed * Time.deltaTime);

                var hits = CombatQueries.EnemiesInRadius(self, transform.position, move.Radius);
                if (hits.Count > 0)
                {
                    HitAll(move, hits);
                    Vfx.Burst(transform.position + Vector3.up * 0.8f + direction, move.Type.TypeColor(), 1.6f);
                    motor.LockMovement(0.15f);
                    yield break;
                }

                yield return null;
            }
        }

        void HitAll(MoveData move, List<Combatant> targets)
        {
            foreach (var target in targets)
                target.ReceiveHit(self, move, target.transform.position - transform.position);
        }

        Vector3 ClampToRange(Vector3 point, float range)
        {
            Vector3 origin = transform.position;
            Vector3 offset = point - origin;
            offset.y = 0f;
            return origin + Vector3.ClampMagnitude(offset, range);
        }
    }
}
