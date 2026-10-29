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
    public static class TrainerPackageValidation
    {
        [MenuItem("PokeMoba/Validar nuevo paquete de entrenadores")]
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-trainerValidationOutput");
            string output=index>=0?args[index+1]:Path.GetFullPath("../tmp/trainers-package/validation");
            Directory.CreateDirectory(output);Directory.CreateDirectory(output+"/icons");File.WriteAllText(output+"/result.txt","RUNNING");
            if(TrainerCharacterCatalog.Keys.Length!=32||TrainerCharacterCatalog.Keys.Distinct().Count()!=32)throw new Exception("Incorrect selector coverage");
            if(TrainerCharacterCatalog.Resolve("Ash/Ash")!="Ash/Ash"||TrainerCharacterCatalog.Resolve("Procedural")!="Procedural")throw new Exception("Protected character key changed");
            var scene=EditorSceneManager.NewPreviewScene();
            var cameraObject=new GameObject("Camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();
            camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.12f,.19f,0);
            camera.orthographic=true;camera.orthographicSize=1.17f;camera.transform.position=new Vector3(1.9f,1.6f,4);camera.transform.LookAt(new Vector3(0,1,0));
            var lamp=new GameObject("Key");SceneManager.MoveGameObjectToScene(lamp,scene);lamp.AddComponent<Light>().type=LightType.Directional;lamp.GetComponent<Light>().intensity=1.5f;lamp.transform.rotation=Quaternion.Euler(30,-35,0);
            var fill=new GameObject("Fill");SceneManager.MoveGameObjectToScene(fill,scene);fill.AddComponent<Light>().type=LightType.Directional;fill.GetComponent<Light>().intensity=.6f;fill.transform.rotation=Quaternion.Euler(20,130,0);
            var rt=new RenderTexture(256,320,24);camera.targetTexture=rt;var sheet=new Texture2D(256*6,320*6,TextureFormat.RGBA32,false);
            int count=0;var lines=new System.Collections.Generic.List<string>();
            try
            {
                foreach(var entry in TrainerCharacterCatalog.Package)
                {
                    string directory=TrainerPackageImporter.Folder+entry.Key.Substring("Package/".Length);
                    foreach(string image in Directory.GetFiles(directory+"/tex","*.png"))AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);
                    AssetDatabase.ImportAsset(directory+"/Model.fbx",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    var host=new GameObject("Host");SceneManager.MoveGameObjectToScene(host,scene);
                    try
                    {
                        var model=TrainerModelBuilder.Build(host.transform,new SaveData{TrainerCharacter=entry.Key},false);
                        var motion=model.GetComponentInChildren<PackageTrainerMotion>();if(motion==null)throw new Exception("Failed catalog route "+entry.Key);
                        var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0||skins.Sum(s=>s.bones.Length)<10)throw new Exception("Missing skeleton "+entry.Key);
                        foreach(var skin in skins)foreach(var material in skin.sharedMaterials)
                            if(material==null||material.mainTexture==null||!material.shader.isSupported)throw new Exception("Missing texture "+entry.Key);
                        var before=Points(model.gameObject);var low=before.Aggregate(Vector3.Min);var high=before.Aggregate(Vector3.Max);
                        if(Mathf.Abs(high.y-low.y-1.95f)>.04f||Mathf.Abs(low.y)>.04f)throw new Exception("Incorrect size/ground "+entry.Key+" "+low+" "+high);
                        motion.Advance(1,.13f);var after=Points(model.gameObject);float moved=before.Zip(after,(a,b)=>Vector3.Distance(a,b)).Max();
                        if(moved<.001f)throw new Exception("Locomotion doesn't move skin "+entry.Key);
                        motion.Advance(0,0);camera.Render();RenderTexture.active=rt;
                        var icon=new Texture2D(256,320,TextureFormat.RGBA32,false);icon.ReadPixels(new Rect(0,0,256,320),0,0);icon.Apply();
                        string folder=entry.Key.Substring("Package/".Length);File.WriteAllBytes(output+"/icons/"+folder+".png",icon.EncodeToPNG());
                        sheet.SetPixels((count%6)*256,(5-count/6)*320,256,320,icon.GetPixels());UnityEngine.Object.DestroyImmediate(icon);
                        lines.Add($"PASS {entry.Key}: {entry.Name}; {before.Length} vertices; height {high.y-low.y:F3}; locomotion delta {moved:F4}");count++;
                    }
                    finally{UnityEngine.Object.DestroyImmediate(host);}
                }
                sheet.Apply();File.WriteAllBytes(output+"/gallery.png",sheet.EncodeToPNG());
                File.WriteAllLines(output+"/details.txt",lines);File.WriteAllText(output+"/result.txt",$"PASS: {count} new trainers, skeletons, textured materials, 1.95m height, ground placement, locomotion and generated icons. 32 selector entries including unchanged Roshi/Ash keys.");
                Debug.Log(File.ReadAllText(output+"/result.txt"));
            }
            catch(Exception e){File.WriteAllText(output+"/result.txt","FAIL: "+e);throw;}
            finally{RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static Vector3[] Points(GameObject model)
        {
            var vertices=new System.Collections.Generic.List<Vector3>();
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh,true);vertices.AddRange(mesh.vertices.Select(v=>skin.transform.TransformPoint(v)));UnityEngine.Object.DestroyImmediate(mesh);
            }
            return vertices.ToArray();
        }
    }
}
