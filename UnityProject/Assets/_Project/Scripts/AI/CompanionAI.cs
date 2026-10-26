using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>
    /// Pokémon compañero de la Aventura (estilo Legends: Z-A): sigue al entrenador y solo ataca cuando
    /// recibe una orden. Con una orden, se acerca a la distancia del movimiento y lo lanza contra el objetivo fijado.
    /// </summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class CompanionAI : MonoBehaviour
    {
        const float FollowDistance = 2.2f;
        const float LeashDistance = 16f;
        const float OrderTimeout = 2.5f;

        /// <summary>Objetivo fijado por el entrenador (puede ser null).</summary>
        public Combatant Target { get; set; }

        Combatant self;
        CreatureMotor motor;
        AbilityCaster caster;
        Transform trainer;
        int pendingSlot = -1;
        float pendingTimer;

        public void Setup(Transform trainerTransform)
        {
            self = GetComponent<Combatant>();
            motor = GetComponent<CreatureMotor>();
            caster = GetComponent<AbilityCaster>();
            trainer = trainerTransform;
        }

        public bool HasPendingOrder => pendingSlot >= 0;

        /// <summary>Orden del entrenador: usar el movimiento de la ranura indicada.</summary>
        public void Command(int slot)
        {
            if (self == null || self.IsDead || !caster.IsReady(slot)) return;
            pendingSlot = slot;
            pendingTimer = OrderTimeout;
        }

        void Update()
        {
            if (self == null || self.IsDead || trainer == null) return;
            if (Target != null && (Target.IsDead || !Target.gameObject.activeInHierarchy)) Target = null;

            float toTrainer = CombatQueries.FlatDistance(transform.position, trainer.position);

            if (pendingSlot >= 0 && toTrainer < LeashDistance)
            {
                ExecuteOrder();
                return;
            }

            // Sin orden: si hay objetivo, se queda frente a él a media distancia; si no, sigue al entrenador.
            if (Target != null && toTrainer < LeashDistance)
            {
                Vector3 toTarget = Flat(Target.transform.position - transform.position);
                float distance = toTarget.magnitude - Target.Radius;
                float keep = Mathf.Max(2.5f, caster.GetMove(AbilityCaster.BasicSlot).Range * 0.9f);
                motor.FaceDirection(toTarget);
                motor.SetMoveInput(distance > keep + 1.5f ? toTarget.normalized : Vector3.zero);
                return;
            }

            FollowTrainer();
        }

        void ExecuteOrder()
        {
            pendingTimer -= Time.deltaTime;
            if (pendingTimer <= 0f || !caster.IsReady(pendingSlot))
            {
                pendingSlot = -1;
                return;
            }

            var move = caster.GetMove(pendingSlot);

            // Sin objetivo: lanza hacia donde mira el entrenador.
            if (Target == null)
            {
                Vector3 forward = Flat(trainer.forward);
                if (caster.TryCast(pendingSlot, forward, transform.position + forward * move.Range * 0.7f)) pendingSlot = -1;
                return;
            }

            Vector3 toTarget = Flat(Target.transform.position - transform.position);
            float reach = toTarget.magnitude - Target.Radius;
            if (reach > DesiredRange(move))
            {
                motor.SetMoveInput(toTarget.normalized);
                return;
            }

            motor.SetMoveInput(Vector3.zero);
            if (caster.TryCast(pendingSlot, toTarget, Target.transform.position)) pendingSlot = -1;
        }

        void FollowTrainer()
        {
            // Se coloca un poco detrás y a un lado del entrenador.
            Vector3 slot = trainer.position - trainer.forward * 1.2f + trainer.right * 1.4f;
            Vector3 toSlot = Flat(slot - transform.position);
            float distance = toSlot.magnitude;
            if (distance < 0.6f)
            {
                motor.SetMoveInput(Vector3.zero);
                return;
            }
            float speed = distance > FollowDistance * 2f ? 1f : Mathf.Clamp01(distance / FollowDistance);
            motor.SetMoveInput(toSlot.normalized * speed);
        }

        static float DesiredRange(MoveData move)
        {
            switch (move.Shape)
            {
                case MoveShape.Melee: return move.Range * 0.8f;
                case MoveShape.Cone: return move.Range * 0.8f;
                case MoveShape.Dash: return move.Range * 0.9f;
                default: return move.Range * 0.75f;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
