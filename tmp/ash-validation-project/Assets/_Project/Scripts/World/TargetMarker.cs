using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Anillo en el suelo y flecha flotante sobre el objetivo fijado.</summary>
    public class TargetMarker : MonoBehaviour
    {
        static readonly Color MarkerColor = new Color(1f, 0.85f, 0.2f);

        Transform ring;
        Transform arrow;
        Combatant target;

        public static TargetMarker Create()
        {
            var root = new GameObject("Marcador de objetivo");
            var marker = root.AddComponent<TargetMarker>();

            marker.ring = Vfx.Primitive(PrimitiveType.Cylinder, "Anillo", MarkerColor).transform;
            marker.ring.SetParent(root.transform, false);

            marker.arrow = Vfx.Primitive(PrimitiveType.Cube, "Flecha", MarkerColor).transform;
            marker.arrow.SetParent(root.transform, false);
            marker.arrow.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            root.SetActive(false);
            return marker;
        }

        public void SetTarget(Combatant newTarget)
        {
            target = newTarget;
            gameObject.SetActive(target != null);
        }

        void LateUpdate()
        {
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position = target.transform.position;
            float size = target.Radius * 2.6f;
            ring.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.localScale = new Vector3(size, 0.01f, size);

            float height = 1.6f * target.Species.Size + 1.2f;
            arrow.localPosition = new Vector3(0f, height + Mathf.Sin(Time.time * 4f) * 0.15f, 0f);
            arrow.localRotation = Quaternion.Euler(45f, Time.time * 120f, 45f);
        }
    }
}
