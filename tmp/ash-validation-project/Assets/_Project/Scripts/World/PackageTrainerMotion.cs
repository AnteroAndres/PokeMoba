using System.Collections.Generic;
using UnityEngine;

namespace PokeMoba.World
{
    /// <summary>Locomotion for the new TRMDL package; leaves the separate Ash and Roshi rigs alone.</summary>
    public sealed class PackageTrainerMotion : MonoBehaviour
    {
        struct Joint { public Transform Bone; public Quaternion Rest; public Vector3 Axis; public float Sign; public int Kind; }
        readonly List<Joint> joints = new List<Joint>();
        float phase;
        public static PackageTrainerMotion Configure(GameObject model)
        {
            foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            var motion = model.AddComponent<PackageTrainerMotion>();
            var bones = model.GetComponentsInChildren<Transform>();
            foreach (string side in new[]{"left","right"})
            {
                var arm = System.Array.Find(bones,b=>b.name==side+"_arm_01");
                var elbow = System.Array.Find(bones,b=>b.name==side+"_arm_02");
                if (arm == null || elbow == null) continue;
                Vector3 towardElbow = elbow.position-arm.position;
                Vector3 relaxed = (-model.transform.parent.up+model.transform.parent.right*(side=="left"?-.18f:.18f)).normalized;
                if (towardElbow.sqrMagnitude > .001f) arm.rotation = Quaternion.FromToRotation(towardElbow.normalized,relaxed)*arm.rotation;
            }
            ImportedPokemonModel.FitToHeight(model,1.95f);
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=true;
            foreach(var bone in bones)
            {
                int kind = bone.name.EndsWith("_leg_01")?0:bone.name.EndsWith("_leg_02")?1:bone.name.EndsWith("_arm_01")?2:-1;
                if(kind<0)continue;
                motion.joints.Add(new Joint{Bone=bone,Rest=bone.localRotation,Axis=bone.InverseTransformDirection(model.transform.parent.right).normalized,Sign=bone.name.StartsWith("left")?1:-1,Kind=kind});
            }
            return motion;
        }
        public void Animate(float walking) => Advance(walking, Time.deltaTime);
        public void Advance(float walking, float deltaTime)
        {
            float speed=Mathf.Clamp01(walking);phase+=deltaTime*9f*speed;
            foreach(var joint in joints)
            {
                if(joint.Bone==null)continue;
                float angle=Mathf.Sin(phase)*joint.Sign*(joint.Kind==0?25:joint.Kind==1?12:-18)*speed;
                joint.Bone.localRotation=joint.Rest*Quaternion.AngleAxis(angle,joint.Axis);
            }
        }
    }
}
