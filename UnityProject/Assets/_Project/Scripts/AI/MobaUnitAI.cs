using System.Collections.Generic;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>
    /// IA de carril del MOBA para súbditos y bots: avanza por su carril y elige a quién atacar.
    /// Los bots además se retiran a la base con poca vida y no se meten bajo una torre enemiga
    /// sin súbditos aliados delante. El combate en sí lo hace EnemyAI.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class MobaUnitAI : MonoBehaviour
    {
        public enum Kind { Minion, Champion }

        const float MinionSight = 6f;
        const float ChampionSight = 9f;
        const float TowerRange = 9f;

        public Kind UnitKind { get; private set; }

        Combatant self;
        EnemyAI ai;
        CreatureMotor motor;
        Vector3[] path;
        Vector3 basePosition;
        int waypoint;
        bool retreating;
        float thinkTimer;

        public void Setup(Kind kind, Vector3[] lanePath, Vector3 home)
        {
            self = GetComponent<Combatant>();
            ai = GetComponent<EnemyAI>();
            motor = GetComponent<CreatureMotor>();
            UnitKind = kind;
            path = lanePath;
            basePosition = home;
            ai.IdleTick = Walk;
            ResetPath();
        }

        public void ResetPath()
        {
            waypoint = 0;
            retreating = false;
            ai.SetTarget(null);
        }

        void Update()
        {
            if (self == null || self.IsDead) return;

            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = 0.3f;

            if (UnitKind == Kind.Champion)
            {
                float hp = self.Hp / self.MaxHp;
                if (hp < 0.3f) retreating = true;
                else if (retreating && hp > 0.9f) retreating = false;

                if (retreating)
                {
                    ai.SetTarget(null);
                    return;
                }
            }

            ai.SetTarget(ChooseTarget());
        }

        Combatant ChooseTarget()
        {
            float sight = UnitKind == Kind.Minion ? MinionSight : ChampionSight;
            Combatant bestChampion = null, bestMinion = null, bestStructure = null;
            float championScore = float.MaxValue, minionDistance = float.MaxValue, structureDistance = float.MaxValue;

            foreach (var other in Combatant.All)
            {
                if (!CombatQueries.IsEnemy(self, other)) continue;
                float d = CombatQueries.FlatDistance(transform.position, other.transform.position) - other.Radius;
                if (d > sight) continue;

                if (other.IsStructure)
                {
                    if (d < structureDistance) { structureDistance = d; bestStructure = other; }
                }
                else if (other.TryGetComponent(out MobaUnitAI unit) && unit.UnitKind == Kind.Minion)
                {
                    if (d < minionDistance) { minionDistance = d; bestMinion = other; }
                }
                else
                {
                    // Campeones: prefiere al que tiene menos vida
                    float score = other.Hp / other.MaxHp * 10f + d;
                    if (score < championScore) { championScore = score; bestChampion = other; }
                }
            }

            if (UnitKind == Kind.Minion)
            {
                // Los súbditos atacan lo que tengan más cerca, torres incluidas.
                Combatant nearest = bestMinion;
                float nearestDistance = minionDistance;
                if (bestChampion != null && CombatQueries.FlatDistance(transform.position, bestChampion.transform.position) < nearestDistance)
                    nearest = bestChampion;
                if (nearest == null) nearest = bestStructure;
                return nearest;
            }

            if (bestChampion != null && !UnderEnemyTowerAlone(bestChampion.transform.position)) return bestChampion;
            if (bestMinion != null && !UnderEnemyTowerAlone(bestMinion.transform.position)) return bestMinion;
            if (bestStructure != null && (AlliedMinionsNear(bestStructure.transform.position, TowerRange) || bestStructure.Hp / bestStructure.MaxHp < 0.25f))
                return bestStructure;
            return null;
        }

        bool UnderEnemyTowerAlone(Vector3 point)
        {
            foreach (var other in Combatant.All)
            {
                if (!other.IsStructure || !CombatQueries.IsEnemy(self, other)) continue;
                if (CombatQueries.FlatDistance(point, other.transform.position) < TowerRange)
                    return !AlliedMinionsNear(other.transform.position, TowerRange);
            }
            return false;
        }

        bool AlliedMinionsNear(Vector3 point, float range)
        {
            foreach (var other in Combatant.All)
            {
                if (other.Team != self.Team || other.IsDead || other == self) continue;
                other.TryGetComponent(out MobaUnitAI unit);
                if (unit != null && unit.UnitKind == Kind.Minion && CombatQueries.FlatDistance(point, other.transform.position) < range)
                    return true;
            }
            return false;
        }

        /// <summary>Sin objetivo: seguir el carril (o volver a la base si se está retirando).</summary>
        void Walk()
        {
            Vector3 destination;
            if (retreating) destination = basePosition;
            else
            {
                while (waypoint < path.Length - 1 && CombatQueries.FlatDistance(transform.position, path[waypoint]) < 2.5f) waypoint++;
                destination = path[waypoint];
            }

            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            motor.SetMoveInput(toDestination.magnitude < 1f ? Vector3.zero : toDestination.normalized * (UnitKind == Kind.Minion ? 0.7f : 1f));
        }
    }
}
