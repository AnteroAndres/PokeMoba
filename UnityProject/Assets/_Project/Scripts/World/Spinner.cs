using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Gira lentamente un objeto (modelos expuestos en la Pokédex y el menú).</summary>
    public class Spinner : MonoBehaviour
    {
        public float DegreesPerSecond = 30f;

        void Update() => transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f);
    }
}
