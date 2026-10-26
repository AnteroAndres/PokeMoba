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

        public event Action<Combatant> Died;

        public SpeciesData Species { get; private set; }
        public CombatStats Stats { get; private set; }
        public int Team { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public StatusEffect Status { get; private set; }
        public CreatureMotor Motor { get; private set; }
        public bool IsDead => Hp <= 0f;

        float statusTimer;
        float tickTimer;
        float flashTimer;
        Transform model;
        Material[] materials;
        Color[] baseColors;

        public void Init(SpeciesData species, int team, int level)
        {
            Species = species;
            Team = team;
            Stats = CombatStats.FromBase(species.BaseStats, level);
            MaxHp = Stats.MaxHp * CombatTuning.HpMultiplier;
            Hp = MaxHp;
            Status = StatusEffect.None;

            Motor = GetComponent<CreatureMotor>();
            Motor.MoveSpeed = CombatTuning.BaseMoveSpeed + Stats.Speed * CombatTuning.SpeedStatFactor;

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

        /// <summary>Devuelve false si el golpe no entró (muerto o esquivando).</summary>
        public bool ReceiveHit(Combatant attacker, MoveData move, Vector3 hitDirection)
        {
            if (IsDead || attacker == null) return false;

            if (Motor.IsInvulnerable)
            {
                AnyDodged?.Invoke(this);
                return false;
            }

            var result = DamageCalculator.Calculate(
                move,
                attacker.Stats, attacker.Species.Types, attacker.Status == StatusEffect.Quemado,
                Stats, Species.Types);

            ApplyDamage(result.Damage);
            AnyDamaged?.Invoke(this, result);

            if (result.Damage > 0)
            {
                flashTimer = 0.12f;
                hitDirection.y = 0f;
                if (hitDirection.sqrMagnitude > 0.0001f)
                    Motor.ApplyKnockback(hitDirection.normalized * CombatTuning.KnockbackForce);
            }

            if (!IsDead && move.Status != StatusEffect.None && Status == StatusEffect.None
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

        void ApplyDamage(float amount)
        {
            if (IsDead) return;
            Hp = Mathf.Max(0f, Hp - amount);
            if (IsDead) Die();
        }

        void Die()
        {
            Status = StatusEffect.None;
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(0f, 0f, 90f);
                model.localPosition = new Vector3(0f, 0.3f, 0f);
            }
            Died?.Invoke(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (materials != null)
            {
                flashTimer -= dt;
                bool flashing = flashTimer > 0f;
                for (int i = 0; i < materials.Length; i++)
                    materials[i].color = flashing ? Color.white : baseColors[i];
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
