using UnityEngine;
using PokeMoba.Input;

namespace PokeMoba.Prototype
{
    /// <summary>Cámara isométrica fija que sigue al jugador, desplazada un poco hacia el rival si lo hay.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public static readonly Vector3 DefaultOffset = new Vector3(0f, 13f, -9.5f);

        const float EnemyBias = 0.3f;
        const float Smoothing = 6f;

        public bool ThirdPerson { get; private set; }
        float yaw;
        float pitch = 14f;
        const float Distance = 6f;

        public void SetThirdPerson(bool enabled)
        {
            ThirdPerson = enabled;
            if (enabled)
            {
                yaw = player != null ? player.eulerAngles.y : 0f;
                pitch = 14f;
            }
        }

        public void RotateLook(Vector2 delta)
        {
            if (!ThirdPerson) return;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -12f, 55f);
        }

        Vector3 offset = DefaultOffset;
        Transform player;
        Transform enemy;

        void Awake() => transform.rotation = Quaternion.LookRotation(-offset);

        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
            transform.rotation = Quaternion.LookRotation(-offset);
        }

        public void SetTargets(Transform player, Transform enemy)
        {
            this.player = player;
            this.enemy = enemy;
            if (!ThirdPerson) transform.position = Focus() + offset;
        }

        /// <summary>Encuadre fijo (menú principal, escaparates).</summary>
        public void LookAtPoint(Vector3 point, Vector3 fromOffset)
        {
            player = null;
            enemy = null;
            transform.position = point + fromOffset;
            transform.rotation = Quaternion.LookRotation(-fromOffset);
        }

        void LateUpdate()
        {
            if (player == null) return;
            if (ThirdPerson)
            {
                if (Time.timeScale <= 0f) return;
                RotateLook(InputCompat.CameraLook());
                Vector3 pivot = player.position + Vector3.up * 1.5f;
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 back = rotation * Vector3.back;
                float distance = Distance;
                foreach (var hit in Physics.SphereCastAll(pivot, 0.2f, back, Distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<PokeMoba.Combat.Combatant>() != null ||
                        hit.collider.GetComponentInParent<PokeMoba.World.TrainerFollower>() != null) continue;
                    distance = Mathf.Min(distance, Mathf.Max(0.4f, hit.distance - 0.15f));
                }
                transform.position = pivot + back * distance;
                transform.rotation = rotation;
                return;
            }
            Vector3 desired = Focus() + offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-Smoothing * Time.deltaTime));
        }

        Vector3 Focus()
        {
            if (player == null) return Vector3.zero;
            Vector3 focus = player.position;
            if (enemy != null) focus += (enemy.position - player.position) * EnemyBias;
            focus.y = 0f;
            return focus;
        }
    }
}
