using System;
using System.IO;
using System.Linq;
using PokeMoba.Game;
using PokeMoba.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokeMoba.Editor
{
    public static class AshImportValidation
    {
        [MenuItem("PokeMoba/Validar Ash animado")]
        public static void Run()
        {
            string output = Path.GetFullPath("../tmp/ash-import");
            var args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-ashValidationOutput");
            if (option >= 0 && option + 1 < args.Length) output = args[option + 1];
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"result.txt"),"RUNNING\n");
            foreach(string path in Directory.GetFiles(AshModelImporter.Folder,"*.png"))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in new[]{"Ash.fbx","Run.fbx"})
                AssetDatabase.ImportAsset(AshModelImporter.Folder+name, ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var scene=EditorSceneManager.NewPreviewScene();
            var host=new GameObject("AshValidation");SceneManager.MoveGameObjectToScene(host,scene);
            try
            {
                var root=TrainerModelBuilder.Build(host.transform,new SaveData{TrainerCharacter=AshTrainerMotion.CharacterKey},false);
                var driver=root.GetComponentInChildren<AshTrainerMotion>();
                if(driver==null)throw new Exception("Ash failed to build through TrainerModelBuilder");
                var player=root.GetComponentInChildren<Animation>();
                foreach(string clip in new[]{"idle","run"})
                    if(player[clip]==null||player[clip].length<=0)throw new Exception("Missing Ash clip: "+clip);
                foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                    foreach(var material in skin.sharedMaterials)
                        if(material==null||material.mainTexture==null||!material.shader.isSupported)
                            throw new Exception("Untextured/unsupported material in "+skin.name);
                player.Stop();player.Play("idle");player["idle"].time=0;player.Sample();driver.ConstrainRootMotion();
                var idle=Points(root.gameObject);
                var lo=idle.Aggregate(Vector3.Min);var hi=idle.Aggregate(Vector3.Max);
                if(Mathf.Abs(hi.y-lo.y-1.95f)>.04f||Mathf.Abs(lo.y)>.04f)throw new Exception("Incorrect Ash height/ground: "+lo+" "+hi);
                player.Stop();player.Play("run");player["run"].time=player["run"].length*.3f;player.Sample();driver.ConstrainRootMotion();
                var run=Points(root.gameObject);
                float displacement=idle.Zip(run,(a,b)=>Vector3.Distance(a,b)).Max();
                if(displacement<.01f)throw new Exception("Run clip is not deforming Ash's skeleton");
                var hips=root.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith(":Hips")||t.name=="Hips");
                var first=hips.localPosition;
                player["run"].time=player["run"].length*.8f;player.Sample();driver.ConstrainRootMotion();
                if(new Vector2(first.x-hips.localPosition.x,first.z-hips.localPosition.z).magnitude>.001f)
                    throw new Exception("Run animation moves gameplay root");
                player.Stop();player.Play("idle");player["idle"].time=.2f;player.Sample();driver.ConstrainRootMotion();
                RenderPreview(scene,output);
                string report=$"PASS: Ash builds through the character selector path; idle {player["idle"].length:F2}s; run {player["run"].length:F2}s; height {hi.y-lo.y:F3}m; all materials textured; run vertex displacement {displacement:F3}m; horizontal root motion constrained.";
                File.WriteAllText(Path.Combine(output,"result.txt"),report);Debug.Log(report);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL: "+e);throw;}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        static Vector3[] Points(GameObject root)
        {
            var result=new System.Collections.Generic.List<Vector3>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh,true);
                result.AddRange(mesh.vertices.Select(v=>skin.transform.TransformPoint(v)));
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            return result.ToArray();
        }
        static void RenderPreview(Scene scene,string output)
        {
            var go=new GameObject("Camera");SceneManager.MoveGameObjectToScene(go,scene);
            var camera=go.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.12f,.16f,.22f,0);camera.orthographic=true;camera.orthographicSize=1.18f;
            camera.transform.position=new Vector3(2.4f,1.55f,4);camera.transform.LookAt(new Vector3(0,1,0));
            var lamp=new GameObject("Key");SceneManager.MoveGameObjectToScene(lamp,scene);
            var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;lamp.transform.rotation=Quaternion.Euler(35,-30,0);
            var rt=new RenderTexture(512,512,24);camera.targetTexture=rt;
            var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
            try
            {
                camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(output,"ash-preview.png"),texture.EncodeToPNG());
            }
            finally{RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
