using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChemistryLab.Desktop.Editor
{
    public static class ModelAssetAudit
    {
        private const string DefaultAuditRoot = "Assets/IncomingAssets";
        private const string DefaultReportPath = "BuildReports/model-asset-audit.json";
        private const string DefaultPreviewDirectory = "BuildReports/model-previews";
        private const int PreviewLayer = 31;

        [MenuItem("Chemistry Lab/Desktop/Audit Staged 3D Models")]
        public static void AuditFromMenu()
        {
            RunAudit(new[] { DefaultAuditRoot }, DefaultReportPath, DefaultPreviewDirectory);
        }

        public static void AuditFromCommandLine()
        {
            var rootsArgument = ReadArgument("-modelAuditRoots");
            var reportPath = ReadArgument("-modelAuditReport");
            var previewDirectory = ReadArgument("-modelAuditPreviews");
            var roots = string.IsNullOrWhiteSpace(rootsArgument)
                ? new[] { DefaultAuditRoot }
                : rootsArgument.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            RunAudit(
                roots,
                string.IsNullOrWhiteSpace(reportPath) ? DefaultReportPath : reportPath,
                string.IsNullOrWhiteSpace(previewDirectory) ? DefaultPreviewDirectory : previewDirectory);
        }

        private static void RunAudit(
            IReadOnlyList<string> roots,
            string reportPath,
            string previewDirectory)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? "BuildReports");
            Directory.CreateDirectory(previewDirectory);

            var assetPaths = roots
                .Where(AssetDatabase.IsValidFolder)
                .SelectMany(root => AssetDatabase.FindAssets("t:GameObject", new[] { root }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsSupportedModel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var report = new ModelAuditReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                unityVersion = Application.unityVersion,
                roots = roots.ToArray(),
                models = assetPaths.Select(path => Inspect(path, previewDirectory)).ToArray()
            };

            report.summary = new ModelAuditSummary
            {
                modelCount = report.models.Length,
                totalSourceBytes = report.models.Sum(model => model.sourceBytes),
                totalVertices = report.models.Sum(model => model.vertices),
                totalTriangles = report.models.Sum(model => model.triangles),
                modelsWithMissingMaterials = report.models.Count(model => model.missingMaterialSlots > 0),
                modelsWithEmbeddedCameras = report.models.Count(model => model.cameraCount > 0),
                modelsWithEmbeddedLights = report.models.Count(model => model.lightCount > 0),
                modelsWithScripts = report.models.Count(model => model.nonTransformComponentCount > 0)
            };

            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Debug.Log("3D model audit written to " + reportPath);
        }

        private static ModelAuditEntry Inspect(string assetPath, string previewDirectory)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath);
            var sourcePath = Path.GetFullPath(assetPath);
            var sourceBytes = File.Exists(sourcePath) ? new FileInfo(sourcePath).Length : 0L;
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var meshes = allAssets.OfType<Mesh>().Distinct().ToArray();
            var clips = allAssets.OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var renderers = prefab == null
                ? Array.Empty<Renderer>()
                : prefab.GetComponentsInChildren<Renderer>(true);
            var materials = renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            var missingMaterialSlots = renderers.Sum(
                renderer => renderer.sharedMaterials.Count(material => material == null));
            var triangles = meshes.Sum(
                mesh => Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(subMesh => (long)mesh.GetIndexCount(subMesh) / 3L));
            var bounds = CalculatePrefabBounds(prefab);
            var previewPath = Path.Combine(
                    previewDirectory,
                    MakeSafeFileName(Path.GetFileNameWithoutExtension(assetPath)) + ".png")
                .Replace('\\', '/');

            var entry = new ModelAuditEntry
            {
                assetPath = assetPath,
                importer = importer == null ? "none" : importer.GetType().Name,
                sourceBytes = sourceBytes,
                vertices = meshes.Sum(mesh => (long)mesh.vertexCount),
                triangles = triangles,
                meshCount = meshes.Length,
                rendererCount = renderers.Length,
                materialCount = materials.Length,
                missingMaterialSlots = missingMaterialSlots,
                animationClipCount = clips.Length,
                colliderCount = prefab == null ? 0 : prefab.GetComponentsInChildren<Collider>(true).Length,
                cameraCount = prefab == null ? 0 : prefab.GetComponentsInChildren<Camera>(true).Length,
                lightCount = prefab == null ? 0 : prefab.GetComponentsInChildren<Light>(true).Length,
                nonTransformComponentCount = CountUnexpectedComponents(prefab),
                boundsMetres = new SerializableVector3(bounds.size),
                smallestDimensionMetres = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z),
                largestDimensionMetres = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
                previewPath = previewPath,
                recommendation = Recommend(sourceBytes, triangles, missingMaterialSlots, bounds)
            };

            if (prefab != null)
            {
                RenderPreview(prefab, bounds, previewPath);
            }

            return entry;
        }

        private static Bounds CalculatePrefabBounds(GameObject prefab)
        {
            if (prefab == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.Length == 0
                ? new Bounds(Vector3.zero, Vector3.zero)
                : renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            UnityEngine.Object.DestroyImmediate(instance);
            return bounds;
        }

        private static void RenderPreview(GameObject prefab, Bounds sourceBounds, string outputPath)
        {
            const int size = 640;
            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instance, PreviewLayer);
            instance.transform.position = -sourceBounds.center;

            var cameraObject = new GameObject("Model Audit Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.052f, 1f);
            camera.cullingMask = 1 << PreviewLayer;
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            var largestDimension = Mathf.Max(
                0.05f,
                Mathf.Max(sourceBounds.size.x, sourceBounds.size.y, sourceBounds.size.z));
            var direction = new Vector3(1.15f, 0.72f, -1.4f).normalized;
            camera.transform.position = direction * largestDimension * 3.1f;
            camera.transform.LookAt(Vector3.zero, Vector3.up);

            var keyObject = new GameObject("Model Audit Key Light");
            keyObject.hideFlags = HideFlags.HideAndDontSave;
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.15f;
            key.color = new Color(1f, 0.97f, 0.9f);
            key.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
            key.cullingMask = 1 << PreviewLayer;

            var fillObject = new GameObject("Model Audit Fill Light");
            fillObject.hideFlags = HideFlags.HideAndDontSave;
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.58f;
            fill.color = new Color(0.62f, 0.78f, 1f);
            fill.transform.rotation = Quaternion.Euler(18f, 145f, 0f);
            fill.cullingMask = 1 << PreviewLayer;

            var renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;

            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(fillObject);
            UnityEngine.Object.DestroyImmediate(keyObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static int CountUnexpectedComponents(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0;
            }

            return prefab.GetComponentsInChildren<Component>(true).Count(component =>
                component != null
                && !(component is Transform)
                && !(component is MeshFilter)
                && !(component is Renderer)
                && !(component is Animator)
                && !(component is Animation)
                && !(component is Collider)
                && !(component is Camera)
                && !(component is Light));
        }

        private static string Recommend(
            long sourceBytes,
            long triangles,
            int missingMaterialSlots,
            Bounds bounds)
        {
            var issues = new List<string>();
            if (triangles > 150000)
            {
                issues.Add("decimate-or-create-lod");
            }
            else if (triangles > 75000)
            {
                issues.Add("use-as-static-prop-with-lod");
            }

            if (sourceBytes > 50L * 1024L * 1024L)
            {
                issues.Add("oversized-source");
            }

            if (missingMaterialSlots > 0)
            {
                issues.Add("repair-materials");
            }

            if (bounds.size == Vector3.zero)
            {
                issues.Add("no-renderable-geometry");
            }
            else if (Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) > 100f
                     || Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) < 0.0001f)
            {
                issues.Add("normalize-scale");
            }

            return issues.Count == 0 ? "technically-usable" : string.Join(",", issues);
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static bool IsSupportedModel(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".glb", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase);
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '-').ToLowerInvariant();
        }

        private static string ReadArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        [Serializable]
        private sealed class ModelAuditReport
        {
            public string generatedAtUtc;
            public string unityVersion;
            public string[] roots;
            public ModelAuditSummary summary;
            public ModelAuditEntry[] models;
        }

        [Serializable]
        private sealed class ModelAuditSummary
        {
            public int modelCount;
            public long totalSourceBytes;
            public long totalVertices;
            public long totalTriangles;
            public int modelsWithMissingMaterials;
            public int modelsWithEmbeddedCameras;
            public int modelsWithEmbeddedLights;
            public int modelsWithScripts;
        }

        [Serializable]
        private sealed class ModelAuditEntry
        {
            public string assetPath;
            public string importer;
            public long sourceBytes;
            public long vertices;
            public long triangles;
            public int meshCount;
            public int rendererCount;
            public int materialCount;
            public int missingMaterialSlots;
            public int animationClipCount;
            public int colliderCount;
            public int cameraCount;
            public int lightCount;
            public int nonTransformComponentCount;
            public SerializableVector3 boundsMetres;
            public float smallestDimensionMetres;
            public float largestDimensionMetres;
            public string previewPath;
            public string recommendation;
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
