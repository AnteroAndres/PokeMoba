using UnityEngine;
using PokeMoba.Input;

namespace PokeMoba.Prototype
{
    /// <summary>Cámara orbital al hombro en Aventura e isométrica en los demás modos.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public static readonly Vector3 DefaultOffset = new Vector3(0f, 13f, -9.5f);

        const float EnemyBias = 0.3f;
        const float Smoothing = 6f;

        public bool ThirdPerson { get; private set; }
        bool mouseReleased;
        float yaw;
        float pitch = 10f;
        Vector3 smoothPivot;
        bool resetPivot = true;
        const float Distance = 5.2f;

        public void SetThirdPerson(bool enabled)
        {
            ThirdPerson = enabled;
            mouseReleased = false;
            if (!enabled) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            resetPivot = true;
            var camera = GetComponent<Camera>();
            if (camera != null) camera.fieldOfView = enabled ? 58f : 50f;
            if (enabled)
            {
                yaw = player != null ? player.eulerAngles.y : 0f;
                pitch = 10f;
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
            if (this.player != player) resetPivot = true;
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
                if (!Application.isMobilePlatform)
                {
                    if (PokeMoba.UI.MenuScreens.AnyOpen || Time.timeScale <= 0f)
                    { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; }
                    if (InputCompat.ReleaseMouse()) mouseReleased = true;
                    else if (InputCompat.CaptureMouse()) mouseReleased = false;
                    Cursor.lockState = mouseReleased ? CursorLockMode.None : CursorLockMode.Locked;
                    Cursor.visible = mouseReleased;
                    if (mouseReleased) return;
                }
                if (Time.timeScale <= 0f) return;
                RotateLook(InputCompat.CameraLook());
                Vector3 targetPivot = player.position + Vector3.up * 1.65f;
                smoothPivot = resetPivot ? targetPivot : Vector3.Lerp(smoothPivot, targetPivot, 1f - Mathf.Exp(-12f * Time.deltaTime));
                resetPivot = false;
                Vector3 pivot = smoothPivot;
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 boom = rotation * new Vector3(.55f, 0f, -Distance);
                Vector3 back = boom.normalized;
                float distance = boom.magnitude;
                foreach (var hit in Physics.SphereCastAll(pivot, 0.2f, back, distance, ~0, QueryTriggerInteraction.Ignore))
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

        void OnApplicationFocus(bool focused)
        {
            if (!focused) { mouseReleased = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

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
