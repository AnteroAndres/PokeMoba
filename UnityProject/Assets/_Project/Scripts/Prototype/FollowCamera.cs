using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>Cámara isométrica fija que sigue al jugador, desplazada un poco hacia el rival si lo hay.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public static readonly Vector3 DefaultOffset = new Vector3(0f, 13f, -9.5f);

        const float EnemyBias = 0.3f;
        const float Smoothing = 6f;

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
            transform.position = Focus() + offset;
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
