using UnityEngine;
using PokeMoba.Combat;
using PokeMoba.Data;
namespace PokeMoba.World
{
    public class FlightVisual : MonoBehaviour
    {
        float height;
        Combatant owner;
        public static void Add(Transform model, SpeciesData species)
        {
            // Modo de locomoción visual: no todos los tipos Volador vuelan (Doduo/Dodrio).
            int[] flying = { 6, 12, 15, 16, 17, 18, 21, 22, 41, 42, 49, 81, 82, 92, 93, 94, 109, 110, 123, 142, 144, 145, 146, 149, 151 };
            if (System.Array.IndexOf(flying, species.DexNumber) < 0) return;
            var visual = model.gameObject.AddComponent<FlightVisual>();
            visual.height = Mathf.Clamp(species.Size * .65f, .55f, 1.3f);
            visual.owner = model.GetComponentInParent<Combatant>();
        }
        void Start() => owner = GetComponentInParent<Combatant>();
        void LateUpdate()
        {
            if (owner != null && owner.IsDead) return;
            // CreatureMotor reinicia la posición cada Update; sin motor, la vista previa usa altura absoluta.
            var position = transform.localPosition;
            position.y = height + Mathf.Sin(Time.time * 2.8f) * .10f;
            transform.localPosition = position;
        }
    }
}
