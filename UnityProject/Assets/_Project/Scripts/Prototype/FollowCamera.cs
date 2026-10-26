using UnityEngine;

namespace PokeMoba.Prototype
{
    /// <summary>Cámara isométrica fija que sigue al jugador, desplazada un poco hacia el rival.</summary>
    public class FollowCamera : MonoBehaviour
    {
        static readonly Vector3 Offset = new Vector3(0f, 13f, -9.5f);
        const float EnemyBias = 0.3f;
        const float Smoothing = 6f;

        Transform player;
        Transform enemy;

        void Awake() => transform.rotation = Quaternion.LookRotation(-Offset);

        public void SetTargets(Transform player, Transform enemy)
        {
            this.player = player;
            this.enemy = enemy;
            transform.position = Focus() + Offset;
        }

        void LateUpdate()
        {
            if (player == null) return;
            Vector3 desired = Focus() + Offset;
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
