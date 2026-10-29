using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PokeMoba.Editor
{
    public class TrainerPackageImporter : AssetPostprocessor
    {
        public const string Folder="Assets/_Project/Resources/Models/Trainers/Package/";
        [Serializable] class Binding {public string material;public string texture;}
        [Serializable] class Info {public Binding[] bindings;}
        bool IsPackage=>assetPath.StartsWith(Folder,StringComparison.Ordinal);
        public override uint GetVersion()=>2;
        void OnPreprocessModel()
        {
            if(!IsPackage)return;
            var m=(ModelImporter)assetImporter;m.importAnimation=false;m.importCameras=false;m.importLights=false;m.addCollider=false;
            m.preserveHierarchy=true;m.optimizeGameObjects=false;m.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            m.materialLocation=ModelImporterMaterialLocation.InPrefab;m.materialSearch=ModelImporterMaterialSearch.Local;
        }
        void OnPreprocessTexture()
        {
            if(!IsPackage)return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;
            t.sRGBTexture=true;t.mipmapEnabled=true;t.alphaIsTransparency=true;t.maxTextureSize=1024;t.npotScale=TextureImporterNPOTScale.None;
        }
        void OnPostprocessMaterial(Material material)
        {
            if(!IsPackage)return;
            string directory=Path.GetDirectoryName(assetPath).Replace('\\','/');string metadata=directory+"/import.json";
            if(!File.Exists(metadata))return;
            var info=JsonUtility.FromJson<Info>(File.ReadAllText(metadata));
            foreach(var binding in info.bindings)
            {
                if(material.name!=binding.material)continue;
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(directory+"/"+binding.texture);
                if(texture==null)continue;
                material.shader=Shader.Find("Standard");material.color=Color.white;material.mainTexture=texture;
                material.SetFloat("_Glossiness",.15f);material.SetFloat("_Metallic",0);
                material.SetFloat("_Mode",1);material.SetFloat("_Cutoff",.3f);
                material.SetOverrideTag("RenderType","TransparentCutout");
                material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
            }
        }
        static readonly System.Collections.Generic.HashSet<string> Pending=new System.Collections.Generic.HashSet<string>();
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] old)
        {
            foreach(string path in imported)
            {
                if(!path.StartsWith(Folder,StringComparison.Ordinal)||!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))continue;
                string rest=path.Substring(Folder.Length);int slash=rest.IndexOf('/');
                if(slash>0)Pending.Add(Folder+rest.Substring(0,slash)+"/Model.fbx");
            }
            if(Pending.Count==0)return;
            EditorApplication.delayCall-=Rebind;EditorApplication.delayCall+=Rebind;
        }
        static void Rebind(){var paths=new System.Collections.Generic.List<string>(Pending);Pending.Clear();foreach(string p in paths)AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate);}
    }
}
