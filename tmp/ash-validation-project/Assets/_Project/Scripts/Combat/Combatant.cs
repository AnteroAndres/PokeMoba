using System;
using System.Collections.Generic;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Cualquier cosa que pelea: vida, estadísticas, estados y recepción de golpes.</summary>
    [RequireComponent(typeof(CreatureMotor))]
    public class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();

        public static event Action<Combatant, DamageResult> AnyDamaged;
        public static event Action<Combatant> AnyDodged;
        public static event Action<Combatant, StatusEffect> AnyStatusApplied;
        /// <summary>(víctima, atacante que dio el último golpe o null).</summary>
        public static event Action<Combatant, Combatant> AnyKilled;

        public event Action<Combatant> Died;
        /// <summary>(este combatiente, atacante) cada vez que un golpe entra.</summary>
        public event Action<Combatant, Combatant> HitBy;

        public SpeciesData Species { get; private set; }
        [NonSerialized] public PokemonInstance OwnedInstance;
        public CombatStats Stats { get; private set; }
        public MoveData[] Moves { get; private set; }
        public int Team { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public StatusEffect Status { get; private set; }
        public CreatureMotor Motor { get; private set; }
        public bool IsDead => Hp <= 0f;

        /// <summary>Radio del cuerpo para choques y golpes (crece con el tamaño de la especie).</summary>
        public float Radius { get; set; } = CombatTuning.BodyRadius;
        public bool Shiny { get; set; }
        /// <summary>Torres y bases del MOBA: no se mueven ni reciben empujones.</summary>
        public bool IsStructure { get; set; }
        /// <summary>No recibe daño (por ejemplo, la base mientras sus torres siguen en pie).</summary>
        public bool DamageImmune { get; set; }
        /// <summary>Multiplica la vida máxima (jefes y legendarios).</summary>
        public float HpScale { get; set; } = 1f;
        public string DisplayName => Species != null ? Species.Name : name;

        Combatant lastAttacker;
        float statusTimer;
        float tickTimer;
        float flashTimer;
        Transform model;
        Material[] materials;
        Color[] baseColors;

        public void Init(SpeciesData species, int team, int level, MoveData[] moves = null)
        {
            Team = team;
            Motor = GetComponent<CreatureMotor>();
            Reinitialize(species, level, moves, 1f);
        }

        /// <summary>Cambia especie, nivel o movimientos (evolución, subida de nivel) conservando la fracción de vida.</summary>
        public void Reinitialize(SpeciesData species, int level, MoveData[] moves, float hpFraction)
        {
            Species = species;
            Stats = CombatStats.FromBase(species.BaseStats, level);
            Moves = moves ?? species.Moves;
            MaxHp = Stats.MaxHp * CombatTuning.HpMultiplier * HpScale;
            Hp = MaxHp * Mathf.Clamp01(hpFraction);
            Status = StatusEffect.None;
            if (!IsStructure)
                Radius = CombatTuning.BodyRadius * Mathf.Max(1f, species.Size * 0.85f);
            Motor.MoveSpeed = IsStructure ? 0f : CombatTuning.BaseMoveSpeed + Stats.Speed * CombatTuning.SpeedStatFactor;
            RefreshVisuals();
        }

        /// <summary>Vuelve a buscar el modelo y sus materiales (después de reconstruirlo).</summary>
        public void RefreshVisuals()
        {
            model = transform.Find("Model");
            var renderers = GetComponentsInChildren<Renderer>();
            materials = new Material[renderers.Length];
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].material;
                baseColors[i] = materials[i].color;
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Devuelve false si el golpe no entró (muerto, inmune o esquivando).</summary>
        public bool ReceiveHit(Combatant attacker, MoveData move, Vector3 hitDirection)
        {
            if (IsDead || attacker == null || DamageImmune) return false;

            if (Motor.IsInvulnerable)
            {
                AnyDodged?.Invoke(this);
                return false;
            }

            var result = DamageCalculator.Calculate(
                move,
                attacker.Stats, attacker.Species.Types, attacker.Status == StatusEffect.Quemado,
                Stats, Species.Types);

            lastAttacker = attacker;
            HitBy?.Invoke(this, attacker);
            ApplyDamage(result.Damage);
            AnyDamaged?.Invoke(this, result);

            if (result.Damage > 0)
            {
                flashTimer = 0.12f;
                hitDirection.y = 0f;
                if (hitDirection.sqrMagnitude > 0.0001f && !IsStructure)
                    Motor.ApplyKnockback(hitDirection.normalized * CombatTuning.KnockbackForce);
            }

            if (!IsDead && !IsStructure && move.Status != StatusEffect.None && Status == StatusEffect.None
                && UnityEngine.Random.value < move.StatusChance)
            {
                ApplyStatus(move.Status);
            }

            return true;
        }

        public void ApplyStatus(StatusEffect status)
        {
            Status = status;
            statusTimer = CombatTuning.StatusDuration;
            tickTimer = 1f;
            AnyStatusApplied?.Invoke(this, status);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
        }

        public void SetHpFraction(float fraction)
        {
            Hp = MaxHp * Mathf.Clamp01(fraction);
            if (IsDead) Die();
        }

        /// <summary>Vuelve a la vida con la vida completa (reaparición en el MOBA).</summary>
        public void Revive()
        {
            Hp = MaxHp;
            Status = StatusEffect.None;
            lastAttacker = null;
            if (model != null)
            {
                model.localRotation = Quaternion.identity;
                model.localPosition = Vector3.zero;
            }
        }

#if UNITY_EDITOR
        /// <summary>Only compiled in the owner's Unity Editor, never in a player build.</summary>
        public void SetCreatorVitals(float maximum, float fraction, bool immune)
        {
            maximum = Mathf.Clamp(maximum, 1f, 1000000000f);
            if (MaxHp > 0f) HpScale *= maximum / MaxHp;
            MaxHp = maximum;
            if (IsDead) Revive();
            Hp = maximum * Mathf.Clamp(fraction, .01f, 1f);
            DamageImmune = immune;
        }
#endif

        void ApplyDamage(float amount)
        {
            if (IsDead) return;
            Hp = Mathf.Max(0f, Hp - amount);
            if (IsDead) Die();
        }

        void Die()
        {
            Status = StatusEffect.None;
            if (model != null && !IsStructure)
            {
                model.localRotation = Quaternion.Euler(0f, 0f, 90f);
                model.localPosition = new Vector3(0f, 0.3f, 0f);
            }
            Died?.Invoke(this);
            AnyKilled?.Invoke(this, lastAttacker);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (materials != null)
            {
                flashTimer -= dt;
                bool flashing = flashTimer > 0f;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null) materials[i].color = flashing ? Color.white : baseColors[i];
            }

            if (IsDead || Status == StatusEffect.None) return;

            statusTimer -= dt;
            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer += 1f;
                float fraction = Status == StatusEffect.Quemado
                    ? CombatTuning.BurnDamagePerSecond
                    : CombatTuning.PoisonDamagePerSecond;
                int damage = Mathf.Max(1, Mathf.RoundToInt(MaxHp * fraction));
                ApplyDamage(damage);
                AnyDamaged?.Invoke(this, new DamageResult { Damage = damage, Effectiveness = 1f, FromStatus = true });
            }

            if (statusTimer <= 0f) Status = StatusEffect.None;
        }
    }
}
