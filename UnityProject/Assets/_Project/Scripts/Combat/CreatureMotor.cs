using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Movimiento, esquiva, empujes y estamina. No usa físicas: todo se resuelve en el plano XZ.</summary>
    public class CreatureMotor : MonoBehaviour
    {
        public float MoveSpeed { get; set; } = CombatTuning.BaseMoveSpeed;
        public float Stamina { get; private set; } = CombatTuning.MaxStamina;
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector3 MoveInput { get; private set; }
        public Vector3 Velocity { get; private set; }

        public bool IsDodging => dodgeTimer > 0f;
        public bool IsJumping => jumpTimer > 0f;
        public bool IsInvulnerable => invulnerableTimer > 0f;
        public bool IsMovementLocked => lockTimer > 0f;

        Combatant combatant;
        Transform model;
        Vector3 dodgeDirection;
        Vector3 knockback;
        float dodgeTimer;
        float invulnerableTimer;
        float dodgeCooldownTimer;
        float lockTimer;
        float bobPhase;
        const float JumpDuration = .75f;
        const float JumpHeight = 1.2f;
        float jumpTimer;

        /// <summary>Salto de exploración: arco vertical sin invulnerabilidad ni coste de esquiva.</summary>
        public bool TryJump()
        {
            if (combatant == null || combatant.IsDead || combatant.IsStructure || IsDodging || IsJumping) return false;
            jumpTimer = JumpDuration;
            return true;
        }

        // Start y no Awake: Combatant añade este componente por RequireComponent
        // y en ese momento todavía no existe.
        void Start()
        {
            combatant = GetComponent<Combatant>();
            RefreshModel();
            Facing = transform.forward;
        }

        public void RefreshModel() => model = transform.Find("Model");

        /// <summary>Dirección en el mundo (XZ); magnitud 0–1.</summary>
        public void SetMoveInput(Vector3 direction)
        {
            direction.y = 0f;
            MoveInput = Vector3.ClampMagnitude(direction, 1f);
        }

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) Facing = direction.normalized;
        }

        public void LockMovement(float seconds) => lockTimer = Mathf.Max(lockTimer, seconds);

        public void ApplyKnockback(Vector3 impulse) => knockback += impulse;

        /// <summary>Desplazamiento forzado (embestidas). Respeta límites y obstáculos.</summary>
        public void Displace(Vector3 delta) =>
            transform.position = PlayArea.Clamp(transform.position + delta, Radius);

        /// <summary>Coloca al combatiente en un punto sin animación (reapariciones).</summary>
        public void Teleport(Vector3 position)
        {
            knockback = Vector3.zero;
            dodgeTimer = 0f;
            jumpTimer = 0f;
            transform.position = PlayArea.Clamp(position, Radius);
        }

        public bool TryDodge(Vector3 direction)
        {
            if (combatant == null || combatant.IsDead || combatant.IsStructure || IsDodging
                || dodgeCooldownTimer > 0f || Stamina < CombatTuning.DodgeCost)
                return false;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = -Facing;

            dodgeDirection = direction.normalized;
            Stamina -= CombatTuning.DodgeCost;
            dodgeTimer = CombatTuning.DodgeDuration;
            invulnerableTimer = CombatTuning.DodgeInvulnerability;
            dodgeCooldownTimer = CombatTuning.DodgeDuration + CombatTuning.DodgeCooldown;
            lockTimer = 0f;
            return true;
        }

        float Radius => combatant != null ? combatant.Radius : CombatTuning.BodyRadius;

        void Update()
        {
            if (combatant == null) return;

            float dt = Time.deltaTime;
            dodgeTimer -= dt;
            invulnerableTimer -= dt;
            dodgeCooldownTimer -= dt;
            lockTimer -= dt;
            jumpTimer = Mathf.Max(0f, jumpTimer - dt);

            if (combatant.IsDead || combatant.IsStructure)
            {
                Velocity = Vector3.zero;
                return;
            }

            if (!IsDodging)
                Stamina = Mathf.Min(CombatTuning.MaxStamina, Stamina + CombatTuning.StaminaRegen * dt);

            Vector3 velocity;
            if (IsDodging)
            {
                velocity = dodgeDirection * CombatTuning.DodgeSpeed;
            }
            else if (IsMovementLocked)
            {
                velocity = Vector3.zero;
            }
            else
            {
                velocity = MoveInput * MoveSpeed;
                if (MoveInput.sqrMagnitude > 0.01f) FaceDirection(MoveInput);
            }

            velocity += knockback;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, CombatTuning.KnockbackDecay * dt);
            Velocity = velocity;

            Vector3 position = transform.position + velocity * dt;
            if (!IsDodging) position = SeparateFromOthers(position);
            transform.position = PlayArea.Clamp(position, Radius);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(Facing), 1f - Mathf.Exp(-15f * dt));

            AnimateModel(dt, velocity);
        }

        void AnimateModel(float dt, Vector3 velocity)
        {
            if (model == null) return;

            if (IsJumping && !IsDodging)
            {
                float t = 1f - jumpTimer / JumpDuration;
                model.localRotation = Quaternion.identity;
                model.localPosition = Vector3.up * (4f * JumpHeight * t * (1f - t));
                return;
            }

            if (IsDodging)
            {
                // Giro y salto mientras esquiva.
                float t = 1f - dodgeTimer / CombatTuning.DodgeDuration;
                model.localRotation = Quaternion.Euler(0f, 360f * t, 0f);
                model.localPosition = new Vector3(0f, Mathf.Sin(t * Mathf.PI) * 0.6f, 0f);
                return;
            }

            model.localRotation = Quaternion.identity;
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            if (speed > 0.1f)
            {
                bobPhase += dt * 12f;
                model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobPhase)) * 0.015f, 0f);
            }
            else
            {
                model.localPosition = Vector3.Lerp(model.localPosition, Vector3.zero, 10f * dt);
            }
        }

        Vector3 SeparateFromOthers(Vector3 position)
        {
            foreach (var other in Combatant.All)
            {
                if (other == combatant || other.IsDead) continue;
                float minDistance = Radius + other.Radius;
                Vector3 offset = position - other.transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance < minDistance && distance > 0.0001f)
                    position = other.transform.position + offset / distance * minDistance;
            }
            return position;
        }
    }
}
