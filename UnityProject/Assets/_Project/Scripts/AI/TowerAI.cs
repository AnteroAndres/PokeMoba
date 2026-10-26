using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.AI
{
    /// <summary>Torre o núcleo: dispara al enemigo más cercano en su alcance, prefiriendo súbditos.</summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class TowerAI : MonoBehaviour
    {
        public float Range = 9f;

        Combatant self;
        AbilityCaster caster;

        void Awake()
        {
            self = GetComponent<Combatant>();
            caster = GetComponent<AbilityCaster>();
        }

        void Update()
        {
            if (self.IsDead || !caster.CanCast(0)) return;

            Combatant minion = null, champion = null;
            float minionDistance = Range, championDistance = Range;
            foreach (var other in Combatant.All)
            {
                if (!CombatQueries.IsEnemy(self, other) || other.IsStructure) continue;
                float d = CombatQueries.FlatDistance(transform.position, other.transform.position);
                if (d > Range) continue;
                bool isMinion = other.TryGetComponent(out MobaUnitAI unit) && unit.UnitKind == MobaUnitAI.Kind.Minion;
                if (isMinion && d < minionDistance) { minionDistance = d; minion = other; }
                if (!isMinion && d < championDistance) { championDistance = d; champion = other; }
            }

            var target = minion != null ? minion : champion;
            if (target == null) return;
            caster.TryCast(0, target.transform.position - transform.position, target.transform.position);
        }
    }
}
