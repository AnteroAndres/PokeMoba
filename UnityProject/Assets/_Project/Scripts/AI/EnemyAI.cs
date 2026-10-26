using System.Collections.Generic;
using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>
    /// IA del rival del prototipo: se acerca a la distancia de su movimiento, ataca,
    /// rodea al jugador y esquiva proyectiles y áreas con cierta probabilidad.
    /// </summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class EnemyAI : MonoBehaviour
    {
        /// <summary>Probabilidad de esquivar cada amenaza (0–1). Es lo que define la dificultad.</summary>
        public float DodgeChance = 0.55f;
        public float ReactionTime = 0.18f;

        const float ProjectileLookAhead = 6f;

        Combatant self;
        Combatant target;
        CreatureMotor motor;
        AbilityCaster caster;

        readonly HashSet<Object> handledThreats = new HashSet<Object>();
        float pendingDodgeTimer = -1f;
        Vector3 pendingDodgeDirection;
        float thinkTimer;
        float strafeTimer;
        float strafeSign = 1f;

        void Awake()
        {
            self = GetComponent<Combatant>();
            motor = GetComponent<CreatureMotor>();
            caster = GetComponent<AbilityCaster>();
        }

        public void SetTarget(Combatant newTarget) => target = newTarget;

        void Update()
        {
            if (self.IsDead || target == null || target.IsDead)
            {
                motor.SetMoveInput(Vector3.zero);
                return;
            }

            float dt = Time.deltaTime;

            if (pendingDodgeTimer >= 0f)
            {
                pendingDodgeTimer -= dt;
                if (pendingDodgeTimer < 0f) motor.TryDodge(pendingDodgeDirection);
            }

            DetectThreats();

            // Si está dentro de un área enemiga, salir de ella antes que nada.
            foreach (var area in AreaTelegraph.Active)
            {
                if (area.Owner == null || area.Owner.Team == self.Team || !area.Contains(transform.position)) continue;
                Vector3 away = transform.position - area.transform.position;
                away.y = 0f;
                motor.SetMoveInput(away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitSphere);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 toTargetDir = distance > 0.001f ? toTarget / distance : transform.forward;

            strafeTimer -= dt;
            if (strafeTimer <= 0f)
            {
                strafeTimer = Random.Range(0.8f, 1.8f);
                strafeSign = Random.value < 0.5f ? -1f : 1f;
            }
            Vector3 strafe = Vector3.Cross(Vector3.up, toTargetDir) * strafeSign;

            int slot = ChooseSlot();
            if (slot < 0)
            {
                // Todo en recarga: mantener distancia media y moverse de lado.
                float keepAway = distance < 4f ? -0.6f : distance > 7f ? 0.6f : 0f;
                motor.SetMoveInput(strafe + toTargetDir * keepAway);
                return;
            }

            var move = caster.GetMove(slot);
            float desired = DesiredRange(move);
            if (distance > desired)
            {
                motor.SetMoveInput(toTargetDir + strafe * 0.25f);
                return;
            }

            motor.SetMoveInput(strafe * 0.4f);

            thinkTimer -= dt;
            if (thinkTimer > 0f) return;
            thinkTimer = Random.Range(0.15f, 0.45f);

            caster.TryCast(slot, toTarget, target.transform.position);
        }

        int ChooseSlot()
        {
            for (int slot = AbilityCaster.SlotCount - 1; slot >= 0; slot--)
                if (caster.IsReady(slot)) return slot;
            return -1;
        }

        static float DesiredRange(MoveData move)
        {
            switch (move.Shape)
            {
                case MoveShape.Melee: return move.Range * 0.8f;
                case MoveShape.Cone: return move.Range * 0.8f;
                case MoveShape.Dash: return move.Range * 0.9f;
                default: return move.Range * 0.7f;
            }
        }

        void DetectThreats()
        {
            handledThreats.RemoveWhere(threat => threat == null);
            Vector3 position = transform.position;

            foreach (var projectile in Projectile.Active)
            {
                if (projectile.Owner == null || projectile.Owner.Team == self.Team) continue;
                if (handledThreats.Contains(projectile)) continue;

                Vector3 toMe = position - projectile.transform.position;
                toMe.y = 0f;
                float along = Vector3.Dot(toMe, projectile.Direction);
                if (along < 0f || along > ProjectileLookAhead) continue;

                float sideways = (toMe - projectile.Direction * along).magnitude;
                if (sideways > projectile.HitRadius + CombatTuning.BodyRadius + 0.3f) continue;

                handledThreats.Add(projectile);
                if (Random.value >= DodgeChance) continue;

                Vector3 side = Vector3.Cross(Vector3.up, projectile.Direction);
                if (Vector3.Dot(side, toMe) < 0f) side = -side;
                ScheduleDodge(side, ReactionTime);
            }

            foreach (var area in AreaTelegraph.Active)
            {
                if (area.Owner == null || area.Owner.Team == self.Team) continue;
                if (handledThreats.Contains(area) || !area.Contains(position)) continue;

                handledThreats.Add(area);
                if (Random.value >= DodgeChance) continue;

                Vector3 away = position - area.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
                ScheduleDodge(away, Mathf.Min(ReactionTime, Mathf.Max(0f, area.Remaining - 0.15f)));
            }
        }

        void ScheduleDodge(Vector3 direction, float delay)
        {
            if (pendingDodgeTimer >= 0f) return;
            pendingDodgeDirection = direction;
            pendingDodgeTimer = delay;
        }
    }
}
