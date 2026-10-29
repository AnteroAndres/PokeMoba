using UnityEngine;

namespace PokeMoba.Combat
{
    /// <summary>Interpola escala (y opcionalmente posición) y se destruye al terminar.</summary>
    public class TimedScale : MonoBehaviour
    {
        Vector3 fromScale, toScale, fromPosition, toPosition;
        float duration, elapsed;
        bool moves;

        public void Setup(Vector3 from, Vector3 to, float seconds, Vector3? moveTo = null)
        {
            fromScale = from;
            toScale = to;
            duration = seconds;
            fromPosition = transform.position;
            moves = moveTo.HasValue;
            toPosition = moveTo ?? fromPosition;
            transform.localScale = from;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            if (moves) transform.position = Vector3.Lerp(fromPosition, toPosition, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
