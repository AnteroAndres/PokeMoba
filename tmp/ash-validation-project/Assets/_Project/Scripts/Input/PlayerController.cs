using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.Input
{
    /// <summary>Conecta los controles táctiles (y el teclado en computadora) con el Pokémon del jugador.</summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class PlayerController : MonoBehaviour
    {
        const int NotAiming = -1;

        Combatant self;
        CreatureMotor motor;
        AbilityCaster caster;
        VirtualJoystick joystick;
        Camera cam;
        AimIndicator indicator;

        int aimingSlot = NotAiming;
        Vector2 aimVector;

        void Awake()
        {
            self = GetComponent<Combatant>();
            motor = GetComponent<CreatureMotor>();
            caster = GetComponent<AbilityCaster>();
            indicator = AimIndicator.Create();
        }

        void OnDestroy()
        {
            if (indicator != null) Destroy(indicator.gameObject);
        }

        public void Bind(VirtualJoystick joystick, AbilityButton[] abilityButtons, AbilityButton dodgeButton, Camera cam)
        {
            this.joystick = joystick;
            this.cam = cam;

            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int slot = i;
                abilityButtons[i].Tapped = () => CastAutoAim(slot);
                abilityButtons[i].AimChanged = aim => { aimingSlot = slot; aimVector = aim; };
                abilityButtons[i].AimReleased = aim => CastAimed(slot, aim);
            }

            dodgeButton.Tapped = () => motor.TryDodge(motor.MoveInput);
            dodgeButton.AimChanged = null;
            dodgeButton.AimReleased = aim => motor.TryDodge(ScreenToWorld(aim));
        }

        void Update()
        {
            if (Time.timeScale <= 0f)
            {
                motor.SetMoveInput(Vector3.zero);
                return;
            }

            if (self.IsDead)
            {
                motor.SetMoveInput(Vector3.zero);
                aimingSlot = NotAiming;
                indicator.Hide();
                return;
            }

            Vector2 input = InputCompat.KeyboardMove();
            if (joystick != null) input += joystick.Value;
            motor.SetMoveInput(ScreenToWorld(Vector2.ClampMagnitude(input, 1f)));

            if (InputCompat.WasPressed(GameKey.Basic)) CastAutoAim(0);
            if (InputCompat.WasPressed(GameKey.Skill1)) CastAutoAim(1);
            if (InputCompat.WasPressed(GameKey.Skill2)) CastAutoAim(2);
            if (InputCompat.WasPressed(GameKey.Skill3)) CastAutoAim(3);
            if (InputCompat.WasPressed(GameKey.Dodge)) motor.TryDodge(motor.MoveInput);

            if (aimingSlot != NotAiming)
            {
                GetAim(aimingSlot, aimVector, out var direction, out var point);
                indicator.Show(caster.GetMove(aimingSlot), transform.position, direction, point);
            }
        }

        void CastAutoAim(int slot)
        {
            var move = caster.GetMove(slot);
            var target = CombatQueries.NearestEnemy(self);
            Vector3 direction;
            Vector3 point;

            if (target != null)
            {
                direction = target.transform.position - transform.position;
                point = target.transform.position;
            }
            else
            {
                direction = motor.Facing;
                point = transform.position + motor.Facing * move.Range * 0.6f;
            }

            caster.TryCast(slot, direction, point);
        }

        void CastAimed(int slot, Vector2 aim)
        {
            aimingSlot = NotAiming;
            indicator.Hide();
            GetAim(slot, aim, out var direction, out var point);
            caster.TryCast(slot, direction, point);
        }

        void GetAim(int slot, Vector2 aim, out Vector3 direction, out Vector3 point)
        {
            var move = caster.GetMove(slot);
            direction = ScreenToWorld(aim);
            if (direction.sqrMagnitude < 0.0001f) direction = motor.Facing;
            direction.Normalize();
            point = transform.position + direction * (move.Range * aim.magnitude);
        }

        /// <summary>Convierte un vector de pantalla (x derecha, y arriba) al plano del suelo según la cámara.</summary>
        Vector3 ScreenToWorld(Vector2 v)
        {
            if (cam == null) return new Vector3(v.x, 0f, v.y);
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();
            return right * v.x + forward * v.y;
        }
    }
}
