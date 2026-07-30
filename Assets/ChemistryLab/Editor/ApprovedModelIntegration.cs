using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChemistryLab.Desktop.Editor
{
    public static class ApprovedModelIntegration
    {
        private const string NativeAssetRoot =
            "Assets/ChemistryLab/ExternalAssets/Pixabay/ArunangshuBanerjee/Native";
        private const string PrefabRoot =
            "Assets/ChemistryLab/Resources/Models/Glassware";
        private const string GlassMaterialPath =
            NativeAssetRoot + "/LaboratoryGlass.mat";
        private const string ReportPath =
            "BuildReports/approved-model-integration.json";

        [MenuItem("Chemistry Lab/Desktop/Integrate Approved 3D Models")]
        public static void Integrate()
        {
            EnsureAssetFolder(NativeAssetRoot);
            EnsureAssetFolder(PrefabRoot);
            var glassMaterial = CreateOrUpdateGlassMaterial();
            var erlenmeyer = CreatePrefab(
                NativeAssetRoot + "/ErlenmeyerFlaskMesh.asset",
                PrefabRoot + "/ErlenmeyerFlask.prefab",
                "Erlenmeyer Flask",
                0.18f,
                glassMaterial,
                AddErlenmeyerColliders);
            var testTube = CreatePrefab(
                NativeAssetRoot + "/TestTubeMesh.asset",
                PrefabRoot + "/TestTube.prefab",
                "Test Tube",
                0.15f,
                glassMaterial,
                AddTestTubeCollider);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "BuildReports");
            var report = new ApprovedModelIntegrationReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                result = "succeeded",
                models = new[] { erlenmeyer, testTube }
            };
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Debug.Log("APPROVED_MODEL_INTEGRATION_PASS report=" + ReportPath);
        }

        private static IntegratedModel CreatePrefab(
            string nativeMeshPath,
            string prefabPath,
            string displayName,
            float targetHeightMetres,
            Material material,
            Action<GameObject> addColliders)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(nativeMeshPath);
            if (mesh == null)
            {
                throw new InvalidOperationException(
                    "Approved native mesh is missing. Run the source bake once: " + nativeMeshPath);
            }

            var root = new GameObject(displayName);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial = material;
            NormalizeToTabletop(root, visual, targetHeightMetres);
            ConfigureRenderers(root);
            addColliders(root);

            var bounds = CalculateRendererBounds(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not save approved model prefab: " + prefabPath);
            }

            return new IntegratedModel
            {
                nativeMeshPath = nativeMeshPath,
                prefabPath = prefabPath,
                targetHeightMetres = targetHeightMetres,
                finalBoundsMetres = new SerializableVector3(bounds.size),
                rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length,
                colliderCount = prefab.GetComponentsInChildren<Collider>(true).Length
            };
        }

        private static Material CreateOrUpdateGlassMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("The built-in Standard shader is unavailable.");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "Laboratory Glass"
                };
                AssetDatabase.CreateAsset(material, GlassMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.color = new Color(0.72f, 0.86f, 0.92f, 0.3f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.92f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void NormalizeToTabletop(
            GameObject root,
            GameObject visual,
            float targetHeightMetres)
        {
            var bounds = CalculateRendererBounds(root);
            if (bounds.size.y <= 0.0001f)
            {
                throw new InvalidOperationException("Approved model has no measurable renderer bounds.");
            }

            var uniformScale = targetHeightMetres / bounds.size.y;
            visual.transform.localScale *= uniformScale;
            bounds = CalculateRendererBounds(root);
            visual.transform.position += new Vector3(
                -bounds.center.x,
                -bounds.min.y,
                -bounds.center.z);
        }

        private static void AddErlenmeyerColliders(GameObject root)
        {
            var baseCollider = root.AddComponent<SphereCollider>();
            baseCollider.center = new Vector3(0f, 0.055f, 0f);
            baseCollider.radius = 0.059f;

            var neckCollider = root.AddComponent<BoxCollider>();
            neckCollider.center = new Vector3(0f, 0.132f, 0f);
            neckCollider.size = new Vector3(0.044f, 0.096f, 0.044f);
        }

        private static void AddTestTubeCollider(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.center = new Vector3(0f, 0.075f, 0f);
            collider.radius = 0.012f;
            collider.height = 0.15f;
        }

        private static void ConfigureRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var transparent = Array.Exists(
                    renderer.sharedMaterials,
                    material => material != null && material.renderQueue >= 3000);
                renderer.shadowCastingMode = transparent
                    ? ShadowCastingMode.Off
                    : ShadowCastingMode.On;
                renderer.receiveShadows = !transparent;
            }
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void EnsureAssetFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        [Serializable]
        private sealed class ApprovedModelIntegrationReport
        {
            public string generatedAtUtc;
            public string unityVersion;
            public string result;
            public IntegratedModel[] models;
        }

        [Serializable]
        private sealed class IntegratedModel
        {
            public string nativeMeshPath;
            public string prefabPath;
            public float targetHeightMetres;
            public SerializableVector3 finalBoundsMetres;
            public int rendererCount;
            public int colliderCount;
        }

        [Serializable]
        private struct SerializableVector3
        {
            public float x;
            public float y;
            public float z;

            public SerializableVector3(Vector3 value)
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }
    }
}
