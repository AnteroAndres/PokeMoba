using System.Collections.Generic;
using PokeMoba.Combat;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>
    /// Animación procedural de los modelos generados: respiración, aleteo, patas al andar,
    /// cola, cuerpo de serpiente, llama parpadeante y flotar (fantasmas e insectos).
    /// </summary>
    public class CreatureAnimator : MonoBehaviour
    {
        Transform rig;
        readonly List<Transform> wings = new List<Transform>();
        readonly List<Transform> legs = new List<Transform>();
        readonly List<Transform> segments = new List<Transform>();
        readonly List<Quaternion> wingRest = new List<Quaternion>();
        readonly List<Quaternion> legRest = new List<Quaternion>();
        readonly List<Vector3> segmentRest = new List<Vector3>();
        Transform tail;
        Quaternion tailRest;
        Transform flame;
        Vector3 flameScale;
        bool floating;

        CreatureMotor motor;
        float phase;
        float walk;

        public void Setup(Transform rig, List<Transform> wings, List<Transform> legs, Transform tail,
            List<Transform> segments, Transform flame, bool floating)
        {
            this.rig = rig;
            this.wings.AddRange(wings);
            this.legs.AddRange(legs);
            this.segments.AddRange(segments);
            foreach (var w in wings) wingRest.Add(w.localRotation);
            foreach (var l in legs) legRest.Add(l.localRotation);
            foreach (var s in segments) segmentRest.Add(s.localPosition);
            this.tail = tail;
            if (tail != null) tailRest = tail.localRotation;
            this.flame = flame;
            if (flame != null) flameScale = flame.localScale;
            this.floating = floating;
            phase = Random.value * 10f;
        }

        void Start() => motor = GetComponentInParent<CreatureMotor>();

        void Update()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;
            phase += dt;

            float speed = motor != null ? new Vector3(motor.Velocity.x, 0f, motor.Velocity.z).magnitude : 0f;
            walk = Mathf.MoveTowards(walk, Mathf.Clamp01(speed / 4f), dt * 4f);

            // Respiración y flotar
            float breathe = 1f + Mathf.Sin(phase * 2.2f) * 0.025f;
            rig.localScale = new Vector3(1f, breathe, 1f);
            rig.localPosition = floating ? new Vector3(0f, 0.35f + Mathf.Sin(phase * 2f) * 0.12f, 0f) : Vector3.zero;

            // Alas: aleteo constante, más rápido al moverse
            float flap = Mathf.Sin(phase * (8f + walk * 8f)) * (25f + walk * 15f);
            for (int i = 0; i < wings.Count; i++)
            {
                float side = wings[i].localPosition.x < 0f ? 1f : -1f;
                wings[i].localRotation = wingRest[i] * Quaternion.Euler(0f, 0f, flap * side);
            }

            // Patas: alternas al andar
            float stride = Mathf.Sin(phase * 12f) * 30f * walk;
            for (int i = 0; i < legs.Count; i++)
            {
                float sign = (i % 2 == 0) ? 1f : -1f;
                if (legs.Count == 4 && i >= 2) sign = -sign; // cuadrúpedos: diagonal
                legs[i].localRotation = legRest[i] * Quaternion.Euler(stride * sign, 0f, 0f);
            }

            // Cola
            if (tail != null)
                tail.localRotation = tailRest * Quaternion.Euler(0f, Mathf.Sin(phase * 3f) * (12f + walk * 15f), 0f);

            // Serpientes y tentáculos: ondulación
            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 rest = segmentRest[i];
                float wave = Mathf.Sin(phase * 5f - i * 0.8f) * (0.08f + walk * 0.12f) * (i + 1) * 0.35f;
                segments[i].localPosition = rest + new Vector3(wave, 0f, 0f);
            }

            // Llama
            if (flame != null)
                flame.localScale = flameScale * (1f + Mathf.Sin(phase * 18f) * 0.12f + Mathf.Sin(phase * 7f) * 0.08f);
        }
    }
}
