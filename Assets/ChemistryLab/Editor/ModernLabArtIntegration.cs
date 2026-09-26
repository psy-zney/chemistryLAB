using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChemistryLab.Desktop.Editor
{
    public static class ModernLabArtIntegration
    {
        private const string Art = "Assets/ChemistryLab/Art/ModernLab/";
        private const string ResourcesRoot = "Assets/ChemistryLab/Resources/Art/";

        [MenuItem("Chemistry Lab/Desktop/Integrate Modern Lab Art")]
        public static void Integrate()
        {
            Directory.CreateDirectory(ResourcesRoot);
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "ModernLabKit.fbx");
            if (importer == null) throw new InvalidOperationException("Generate ModernLabKit.fbx in Blender first.");
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importAnimation = false;
            importer.generateSecondaryUV = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "ModernLabKit.fbx");
            var palette = new Dictionary<string, Material>();
            Make(palette,"Ivory",new Color(.79f,.81f,.79f),0f,.28f,"Paint");
            Make(palette,"Teal",new Color(.10f,.33f,.34f),.12f,.34f);
            Make(palette,"Steel",new Color(.48f,.53f,.55f),.74f,.48f);
            Make(palette,"DarkSteel",new Color(.16f,.20f,.22f),.6f,.38f);
            Make(palette,"Worktop",new Color(.065f,.085f,.09f),.06f,.32f,"Worktop");
            Make(palette,"Rubber",new Color(.06f,.075f,.075f),0f,.16f);
            Make(palette,"Glass",new Color(.85f,.94f,.96f,.035f),0f,.9f);
            Make(palette,"Amber",new Color(.46f,.26f,.10f,.18f),0f,.8f);
            Make(palette,"Diffuser",new Color(.9f,.96f,1f),0f,.3f);
            Make(palette,"Window",new Color(.67f,.82f,.88f),0f,.25f);
            Make(palette,"Tile",new Color(.67f,.70f,.69f),0f,.25f,"Floor");
            Make(palette,"Ceramic",new Color(.88f,.90f,.86f),0f,.35f);
            Make(palette,"Label",new Color(.93f,.93f,.88f),0f,.18f);
            var particleShader = AssetDatabase.LoadAssetAtPath<Shader>(Art + "LabParticle.shader");
            SaveMaterial("ReactionParticle", particleShader, Color.white);
            var liquid = SaveMaterial("Liquid", Shader.Find("Standard"), Color.white);
            liquid.SetFloat("_Glossiness",.72f);
            Transparent(liquid,3000);
            EditorUtility.SetDirty(liquid);
            foreach (Transform child in model.transform)
            {
                // FBX carries a -90 degree axis conversion. Keep it on a visual child;
                // a metre-scale identity root is required for Unity liquid/label children.
                var instance = new GameObject(child.name);
                var visual = UnityEngine.Object.Instantiate(child.gameObject,instance.transform,false);
                visual.name="Blender visual";
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    var key = renderer.name.Substring(renderer.name.LastIndexOf('_') + 1);
                    if (!palette.TryGetValue(key, out var material))
                        throw new InvalidOperationException("Unmapped Blender material: " + renderer.name);
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = key == "Glass" || key == "Amber" || key == "Window" || key == "Diffuser"
                        ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    renderer.receiveShadows = key != "Glass" && key != "Amber";
                }
                PrefabUtility.SaveAsPrefabAsset(instance, ResourcesRoot + child.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(instance);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MODERN_LAB_UNITY_INTEGRATION_PASS modules=" + model.transform.childCount);
        }

        public static void ValidateAssetsOrThrow()
        {
            foreach(var name in new[]{"Workbench","Hood","StorageLeft","StorageRight","Sink","RoomDetail",
                "PreparationTray","BenchTools","ReagentBottle"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+name+".prefab");
                if(prefab==null || prefab.transform.localScale!=Vector3.one
                    || Quaternion.Angle(prefab.transform.localRotation,Quaternion.identity)>.01f)
                    throw new InvalidOperationException("Invalid metre/axis art root: "+name);
                if(prefab.GetComponentsInChildren<Collider>().Length>0)
                    throw new InvalidOperationException("Decorative art must not intercept interactions: "+name);
                foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial==null || renderer.sharedMaterial.shader==null)
                        throw new InvalidOperationException("Missing runtime material: "+name);
            }
            var bottle=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+"ReagentBottle.prefab");
            var bounds=new Bounds();var first=true;
            foreach(var renderer in bottle.GetComponentsInChildren<Renderer>())
            { if(first) { bounds=renderer.bounds;first=false; } else bounds.Encapsulate(renderer.bounds); }
            if(bounds.size.y<.18f || bounds.size.y>.32f)
                throw new InvalidOperationException("Bottle scale out of contract: "+bounds.size);
            Debug.Log("MODERN_LAB_ASSET_VALIDATION_PASS bottleMetres="+bounds.size);
        }

        private static void Make(Dictionary<string,Material> palette,string name,Color colour,
            float metallic,float smoothness,string texture = null)
        {
            var shader = name == "Glass" || name == "Amber"
                ? AssetDatabase.LoadAssetAtPath<Shader>(Art + "LabGlass.shader") : Shader.Find("Standard");
            var material = SaveMaterial(name,shader,colour);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",smoothness);
            if (texture != null)
            {
                ConfigureTexture(texture + "_Albedo.png", false);
                ConfigureTexture(texture + "_Normal.png", true);
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + texture + "_Albedo.png");
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art + texture + "_Normal.png"));
                material.SetFloat("_BumpScale",.2f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (name == "Diffuser" || name == "Window")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",colour*(name == "Window" ? .28f : .55f));
            }
            EditorUtility.SetDirty(material);
            palette.Add(name,material);
        }
        private static Material SaveMaterial(string name,Shader shader,Color colour)
        {
            if (shader == null) throw new InvalidOperationException("Missing art shader: " + name);
            var path=ResourcesRoot+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader=shader; material.color=colour;
            EditorUtility.SetDirty(material);
            return material;
        }
        private static void ConfigureTexture(string name,bool normal)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name);
            importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=!normal;
            importer.mipmapEnabled=true;
            importer.maxTextureSize=1024;
            importer.anisoLevel=4;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        private static void Transparent(Material material,int queue)
        {
            material.SetFloat("_Mode",2f);
            material.SetOverrideTag("RenderType","Transparent");
            material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue=queue;
        }
    }
}
