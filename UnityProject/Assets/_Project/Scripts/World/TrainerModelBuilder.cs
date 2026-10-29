using System.Collections.Generic;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Colores disponibles para personalizar al entrenador (GDD §12.1, personalización básica).</summary>
    public static class TrainerPalette
    {
        public static readonly Color[] Skin =
        {
            new Color(1f, 0.86f, 0.72f), new Color(0.93f, 0.75f, 0.58f), new Color(0.76f, 0.56f, 0.4f), new Color(0.5f, 0.34f, 0.24f)
        };

        public static readonly Color[] Hair =
        {
            new Color(0.15f, 0.1f, 0.08f), new Color(0.45f, 0.28f, 0.15f), new Color(0.9f, 0.75f, 0.4f),
            new Color(0.75f, 0.2f, 0.15f), new Color(0.3f, 0.35f, 0.75f), new Color(0.85f, 0.85f, 0.85f)
        };

        public static readonly Color[] Eyes =
        {
            new Color(.17f, .24f, .27f), new Color(.20f, .40f, .70f), new Color(.25f, .48f, .25f),
            new Color(.42f, .24f, .12f), new Color(.55f, .35f, .65f), new Color(.7f, .55f, .22f)
        };

        public static readonly Color[] Clothes =
        {
            new Color(0.85f, 0.15f, 0.15f), new Color(0.2f, 0.35f, 0.75f), new Color(0.2f, 0.6f, 0.3f), new Color(0.95f, 0.8f, 0.2f),
            new Color(0.15f, 0.15f, 0.18f), new Color(0.95f, 0.95f, 0.95f), new Color(0.55f, 0.3f, 0.7f), new Color(0.95f, 0.5f, 0.15f)
        };

        public static Color Pick(Color[] palette, int index) => palette[Mathf.Abs(index) % palette.Length];
    }

    public static class TrainerModelBuilder
    {
        public static Transform Build(Transform parent, PokeMoba.Game.SaveData appearance, bool follower = true)
        {
            return Build(parent, TrainerPalette.Pick(TrainerPalette.Skin, appearance.SkinColor),
                TrainerPalette.Pick(TrainerPalette.Hair, appearance.HairColor),
                TrainerPalette.Pick(TrainerPalette.Clothes, appearance.CapColor),
                TrainerPalette.Pick(TrainerPalette.Clothes, appearance.ShirtColor),
                TrainerPalette.Pick(TrainerPalette.Clothes, appearance.PantsColor), follower, appearance);
        }

        /// <param name="follower">true: sigue solo a un Pokémon (escaparates); false: lo controla el jugador (Aventura).</param>
        public static Transform Build(Transform parent, Color skin, Color hair, Color cap, Color shirt, Color pants, bool follower = true, PokeMoba.Game.SaveData appearance = null)
        {
            var root = new GameObject("Entrenador").transform;
            root.SetParent(parent, false);

            // La entrada histórica "Procedural" del selector es Roshi. Su modelo no depende del rol elegido.
            if (appearance != null && !string.IsNullOrEmpty(appearance.TrainerCharacter))
            {
                var imported = appearance.TrainerCharacter != "Procedural"
                    ? ImportedTrainerModel.TryBuild(root, appearance.TrainerCharacter)
                    : ImportedTrainerModel.TryBuild(root);
                if (imported != null)
                {
                    root.gameObject.AddComponent<TrainerRig>().SetupImported(imported);
                    if (follower) root.gameObject.AddComponent<TrainerFollower>();
                    return root;
                }
                Debug.LogWarning("No se encontró el modelo del entrenador seleccionado. Se usa el modelo procedural.");
            }

            var outfit = root.gameObject.AddComponent<TrainerOutfit>();
            outfit.Build(skin, hair, cap, shirt, pants, out var legs, out var arms, appearance);

            root.gameObject.AddComponent<TrainerRig>().Setup(legs, arms);
            if (follower) root.gameObject.AddComponent<TrainerFollower>();
            return root;
        }
    }

    /// <summary>Piernas y brazos del entrenador: los balancea al andar.</summary>
    public class TrainerRig : MonoBehaviour
    {
        List<Transform> legs;
        List<Transform> arms;
        float phase;
        ImportedTrainerModel imported;

        public void SetupImported(ImportedTrainerModel model) => imported = model;

        public void Setup(List<Transform> legs, List<Transform> arms)
        {
            this.legs = legs;
            this.arms = arms;
        }

        /// <param name="walking">0 = quieto, 1 = andando a velocidad normal.</param>
        public void Animate(float walking)
        {
            if (imported != null) { imported.Animate(walking); return; }
            if (legs == null) return;
            phase += Time.deltaTime * 10f * walking;
            float swing = Mathf.Sin(phase) * 30f * Mathf.Clamp01(walking);
            for (int i = 0; i < legs.Count; i++) legs[i].localRotation = Quaternion.Euler(i == 0 ? swing : -swing, 0f, 0f);
            for (int i = 0; i < arms.Count; i++) arms[i].localRotation = Quaternion.Euler(i == 0 ? -swing : swing, 0f, 0f);
        }
    }

    /// <summary>Hace que el entrenador siga a un Pokémon a cierta distancia (escaparates de los menús).</summary>
    [RequireComponent(typeof(TrainerRig))]
    public class TrainerFollower : MonoBehaviour
    {
        const float FollowDistance = 2.4f;
        const float Speed = 7f;

        Transform target;
        TrainerRig rig;

        void Awake() => rig = GetComponent<TrainerRig>();

        public void Follow(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
                transform.position = target.position - target.forward * FollowDistance;
        }

        void Update()
        {
            if (target == null) return;

            Vector3 offset = transform.position - target.position;
            offset.y = 0f;
            float moved = 0f;
            if (offset.magnitude > FollowDistance)
            {
                Vector3 desired = target.position + offset.normalized * FollowDistance;
                Vector3 next = Vector3.MoveTowards(transform.position, desired, Speed * Time.deltaTime);
                moved = Vector3.Distance(next, transform.position);
                transform.position = new Vector3(next.x, 0f, next.z);
            }

            Vector3 look = target.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 8f * Time.deltaTime);

            rig.Animate(moved > 0.001f ? 1f : 0f);
        }
    }
}
