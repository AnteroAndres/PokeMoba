using System;
using System.Collections.Generic;
using System.IO;
using PokeMoba.Data.Pokedex;
using PokeMoba.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokeMoba.Editor
{
    public static class PokemonVisualDiagnostics
    {
        [MenuItem("PokeMoba/Diagnosticar apariencia Pokemon")]
        public static void Run()
        {
            Directory.CreateDirectory("../tmp/pokemon-visual");
            var output = new List<string>();
            var scene = EditorSceneManager.NewPreviewScene();
            var camObject = new GameObject("DiagnosticCamera"); SceneManager.MoveGameObjectToScene(camObject,scene);
            var camera=camObject.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.18f,.22f,.28f);camera.orthographic=true;camera.orthographicSize=1.25f;
            camera.transform.position=new Vector3(2.7f,1.9f,4f);camera.transform.LookAt(new Vector3(0,.8f,0));
            var lamp=new GameObject("DiagnosticLight");SceneManager.MoveGameObjectToScene(lamp,scene);
            lamp.AddComponent<Light>().type=LightType.Directional;lamp.GetComponent<Light>().intensity=1.5f;
            lamp.transform.rotation=Quaternion.Euler(40,-35,0);
            var rt=new RenderTexture(320,320,24);camera.targetTexture=rt;
            var sheet=new Texture2D(320*4,320*4,TextureFormat.RGB24,false);
            int[] ids={1,4,7,25,16,19,23,35,54,94,95,119,129,133,143,150};
            var ambient=RenderSettings.ambientLight;RenderSettings.ambientLight=Color.gray;
            try
            {
                for(int i=0;i<ids.Length;i++)
                {
                    var host=new GameObject("DiagnosticHost");SceneManager.MoveGameObjectToScene(host,scene);
                    try
                    {
                        var species=PokemonDatabase.Get(ids[i]);
                        string path=$"Models/Pokemon/Animated/{species.DexNumber:000}_{species.Key}/Model";
                        var asset=Resources.Load<GameObject>(path);
                        if(asset==null){output.Add(path+" MISSING");continue;}
                        var model=UnityEngine.Object.Instantiate(asset,host.transform);
                        ImportedPokemonModel.Configure(model,1.6f,false,true);
                        var player=model.GetComponentInChildren<Animation>();
                        string report=ids[i]+" "+species.Key+" before: "+Measure(model)+" rootScale="+model.transform.localScale;
                        foreach(string clip in new[]{"idle","walk","run","attack"})
                        {
                            if(player==null||player[clip]==null){report+=" missing "+clip;continue;}
                            player.Stop();player.Play(clip);player[clip].time=player[clip].length*.33f;player.Sample();
                            report+=" | "+clip+": "+Measure(model)+" rootScale="+model.transform.localScale;
                        }
                        output.Add(report);
                        if(player!=null && player["idle"]!=null){player.Stop();player.Play("idle");player["idle"].time=.2f;player.Sample();}
                        camera.Render();RenderTexture.active=rt;
                        var tile=new Texture2D(320,320,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,320,320),0,0);tile.Apply();
                        sheet.SetPixels((i%4)*320,(3-i/4)*320,320,320,tile.GetPixels());
                        UnityEngine.Object.DestroyImmediate(tile);
                    }
                    finally {UnityEngine.Object.DestroyImmediate(host);}
                }
                sheet.Apply();File.WriteAllBytes("../tmp/pokemon-visual/contact.png",sheet.EncodeToPNG());
                File.WriteAllLines("../tmp/pokemon-visual/diagnostics.txt",output);
                Debug.Log("Pokemon visual diagnostics saved to tmp/pokemon-visual");
            }
            finally
            {
                RenderSettings.ambientLight=ambient;RenderTexture.active=null;camera.targetTexture=null;
                rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        static string Measure(GameObject model)
        {
            var b=new Bounds();bool initialized=false;
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {
                if(r is SkinnedMeshRenderer skin)
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh,true);
                    foreach(var v in mesh.vertices)
                    {
                        var point=skin.transform.TransformPoint(v);
                        if(!initialized){b=new Bounds(point,Vector3.zero);initialized=true;}else b.Encapsulate(point);
                    }
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                else if(!initialized){b=r.bounds;initialized=true;}else b.Encapsulate(r.bounds);
            }
            return $"center={b.center.ToString("F3")} size={b.size.ToString("F3")}";
        }
    }
}


