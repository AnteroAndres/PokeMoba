using System;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>
    /// Pokémon salvaje de la Aventura: pasea cerca de donde apareció y pelea si lo atacan
    /// (o si es agresivo y el jugador se acerca). Deja de perseguir si el jugador se aleja.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class WildBehaviour : MonoBehaviour
    {
        const float AggroRange = 5f;
        const float LeashRange = 18f;
        const float WanderRadius = 6f;

        public int Level { get; private set; }
        public bool Boss { get; private set; }
        public bool Aggro { get; private set; }

        Combatant self;
        EnemyAI ai;
        CreatureMotor motor;
        Func<Combatant> player;
        bool aggressive;
        Vector3 home;
        Vector3 wanderTarget;
        float wanderTimer;

        public void Setup(Func<Combatant> playerProvider, int level, bool boss, bool isAggressive)
        {
            self = GetComponent<Combatant>();
            ai = GetComponent<EnemyAI>();
            motor = GetComponent<CreatureMotor>();
            player = playerProvider;
            Level = level;
            Boss = boss;
            aggressive = isAggressive;
            home = transform.position;
            wanderTarget = home;
            ai.IdleTick = Wander;
            self.HitBy += (_, attacker) =>
            {
                if (attacker != null && attacker.Team == 0) Engage();
            };
        }

        public void Engage()
        {
            Aggro = true;
            ai.SetTarget(player());
        }

        void Disengage()
        {
            Aggro = false;
            ai.SetTarget(null);
        }

        void Update()
        {
            if (self == null || self.IsDead) return;

            var target = player();
            if (target == null || target.IsDead)
            {
                if (Aggro) Disengage();
                return;
            }

            float distance = CombatQueries.FlatDistance(transform.position, target.transform.position);
            if (!Aggro && aggressive && distance < AggroRange) Engage();
            else if (Aggro && distance > LeashRange) Disengage();
            else if (Aggro && ai.Target != target) ai.SetTarget(target); // el jugador cambió de Pokémon
        }

        void Wander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = UnityEngine.Random.Range(2f, 5f);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * WanderRadius;
                wanderTarget = PlayArea.Clamp(home + new Vector3(offset.x, 0f, offset.y), self.Radius);
            }

            Vector3 toTarget = wanderTarget - transform.position;
            toTarget.y = 0f;
            motor.SetMoveInput(toTarget.magnitude < 0.5f ? Vector3.zero : toTarget.normalized * 0.45f);
        }
    }
}
