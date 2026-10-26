using System;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>
    /// Pokémon salvaje de la Aventura: pasea cerca de donde apareció y pelea si lo atacan
    /// (o si es agresivo y te acercas). Ataca a tu Pokémon; algunos agresivos van a por el entrenador
    /// (como en los Legends). Deja de perseguir si el entrenador se aleja.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class WildBehaviour : MonoBehaviour
    {
        const float AggroRange = 5f;
        const float LeashRange = 18f;
        const float WanderRadius = 6f;
        const float RetargetInterval = 0.5f;

        public int Level { get; private set; }
        public bool Boss { get; private set; }
        public bool Aggro { get; private set; }
        /// <summary>Va a por el entrenador en vez de a por su Pokémon.</summary>
        public bool HuntsTrainer { get; private set; }

        Combatant self;
        EnemyAI ai;
        CreatureMotor motor;
        Func<Combatant> pokemonTarget;
        Func<Combatant> trainer;
        bool aggressive;
        Vector3 home;
        Vector3 wanderTarget;
        float wanderTimer;
        float retargetTimer;

        /// <param name="pokemonTarget">A quién atacar normalmente (tu Pokémon, o el entrenador si no hay).</param>
        /// <param name="trainerProvider">El entrenador: distancia de detección y objetivo de los que le cazan.</param>
        public void Setup(Func<Combatant> pokemonTarget, Func<Combatant> trainerProvider, int level, bool boss,
            bool isAggressive, bool huntsTrainer)
        {
            self = GetComponent<Combatant>();
            ai = GetComponent<EnemyAI>();
            motor = GetComponent<CreatureMotor>();
            this.pokemonTarget = pokemonTarget;
            trainer = trainerProvider;
            Level = level;
            Boss = boss;
            aggressive = isAggressive;
            HuntsTrainer = huntsTrainer;
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
            ai.SetTarget(ChooseTarget());
        }

        void Disengage()
        {
            Aggro = false;
            ai.SetTarget(null);
        }

        Combatant ChooseTarget()
        {
            var t = trainer();
            if (HuntsTrainer && t != null && !t.IsDead) return t;
            return pokemonTarget();
        }

        void Update()
        {
            if (self == null || self.IsDead) return;

            var t = trainer();
            if (t == null || t.IsDead)
            {
                if (Aggro) Disengage();
                return;
            }

            float distance = CombatQueries.FlatDistance(transform.position, t.transform.position);
            if (!Aggro && aggressive && distance < AggroRange) Engage();
            else if (Aggro && distance > LeashRange) Disengage();
            else if (Aggro)
            {
                // El objetivo puede cambiar (cambio de Pokémon, debilitado): revisarlo de vez en cuando.
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    retargetTimer = RetargetInterval;
                    var target = ChooseTarget();
                    if (ai.Target != target) ai.SetTarget(target);
                }
            }
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
