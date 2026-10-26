using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;

namespace PokeMoba.Input
{
    /// <summary>Guía en el suelo mientras se apunta una habilidad.</summary>
    public class AimIndicator : MonoBehaviour
    {
        Transform line;
        Transform circle;

        public static AimIndicator Create()
        {
            var root = new GameObject("Indicador de apuntado");
            var indicator = root.AddComponent<AimIndicator>();

            var color = new Color(1f, 1f, 1f);
            indicator.line = Vfx.Primitive(PrimitiveType.Cube, "Linea", color).transform;
            indicator.line.SetParent(root.transform, false);

            indicator.circle = Vfx.Primitive(PrimitiveType.Cylinder, "Circulo", color).transform;
            indicator.circle.SetParent(root.transform, false);

            indicator.Hide();
            return indicator;
        }

        public void Show(MoveData move, Vector3 origin, Vector3 direction, Vector3 targetPoint)
        {
            gameObject.SetActive(true);
            origin.y = 0.03f;

            bool area = move.Shape == MoveShape.GroundArea;
            float length = area ? Vector3.Distance(Flat(origin), Flat(targetPoint)) : move.Range;
            float width = move.Shape == MoveShape.Cone
                ? 2f * move.Range * Mathf.Tan(move.Radius * 0.5f * Mathf.Deg2Rad)
                : area ? 0.15f : Mathf.Max(0.3f, move.Radius * 2f);

            line.gameObject.SetActive(length > 0.05f);
            line.position = origin + direction * (length * 0.5f);
            line.rotation = Quaternion.LookRotation(direction);
            line.localScale = new Vector3(width, 0.02f, length);

            circle.gameObject.SetActive(area);
            if (area)
            {
                circle.position = new Vector3(targetPoint.x, 0.04f, targetPoint.z);
                circle.localScale = new Vector3(move.Radius * 2f, 0.01f, move.Radius * 2f);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
