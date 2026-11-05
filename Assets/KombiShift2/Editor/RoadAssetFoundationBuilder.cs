#if UNITY_EDITOR
using System.Collections.Generic;
using KombiShift2.Roadworks;
using UnityEditor;
using UnityEngine;

namespace KombiShift2.Editor
{
    public static class RoadAssetFoundationBuilder
    {
        private const string SourceRoot = "Assets/KombiShift2/ThirdParty/PolyHaven/WornAsphalt/Textures";
        private const string MaterialRoot = "Assets/KombiShift2/Art/Roads/Materials";
        private const string MeshRoot = "Assets/KombiShift2/Art/Roads/Meshes";
        private const string PrefabRoot = "Assets/KombiShift2/Prefabs/Gameplay/Roads";

        [MenuItem("Kombi Shift/Assets/Build Road Asset Foundation")]
        public static void Build()
        {
            EnsureFolder("Assets/KombiShift2/Art");
            EnsureFolder("Assets/KombiShift2/Art/Roads");
            EnsureFolder(MaterialRoot);
            EnsureFolder(MeshRoot);
            EnsureFolder("Assets/KombiShift2/Prefabs");
            EnsureFolder("Assets/KombiShift2/Prefabs/Gameplay");
            EnsureFolder(PrefabRoot);

            ConfigureTexture($"{SourceRoot}/worn_asphalt_diff_2k.jpg", false, true);
            ConfigureTexture($"{SourceRoot}/worn_asphalt_nor_gl_2k.jpg", true, false);
            ConfigureTexture($"{SourceRoot}/worn_asphalt_rough_2k.jpg", false, false);
            ConfigureTexture($"{SourceRoot}/worn_asphalt_ao_2k.jpg", false, false);
            ConfigureTexture($"{SourceRoot}/worn_asphalt_disp_2k.jpg", false, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Material asphalt = CreateAsphaltMaterial();
            CreateRoadPrefab(asphalt);
            CreatePotholePrefab(asphalt);
            AssetDatabase.SaveAssets();
            Debug.Log("KOMBI_ASSETS Road foundation built successfully.");
        }

        private static Material CreateAsphaltMaterial()
        {
            string path = $"{MaterialRoot}/WornAsphalt.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Kombi Shift/Road Surface") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "Worn Asphalt" };
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;

            material.SetTexture("_MainTex", LoadTexture("worn_asphalt_diff_2k.jpg"));
            material.SetTexture("_BumpMap", LoadTexture("worn_asphalt_nor_gl_2k.jpg"));
            material.SetTexture("_RoughnessMap", LoadTexture("worn_asphalt_rough_2k.jpg"));
            material.SetTexture("_OcclusionMap", LoadTexture("worn_asphalt_ao_2k.jpg"));
            material.SetFloat("_NormalStrength", 0.8f);
            material.SetFloat("_RoughnessStrength", 0.95f);
            material.SetFloat("_OcclusionStrength", 0.7f);
            material.SetTextureScale("_MainTex", new Vector2(2.5f, 8f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateRoadPrefab(Material material)
        {
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSurface_Worn_Straight";
            road.transform.localScale = new Vector3(8f, 0.2f, 24f);
            road.GetComponent<Renderer>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(road, $"{PrefabRoot}/RoadSurface_Worn_Straight.prefab");
            Object.DestroyImmediate(road);
        }

        private static void CreatePotholePrefab(Material material)
        {
            const string meshPath = MeshRoot + "/PotholeBowl.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = BuildPotholeMesh();
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            GameObject root = new("Pothole_Hazard");
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, -0.04f, 0f);
            trigger.size = new Vector3(1.6f, 0.35f, 1.6f);
            root.AddComponent<PotholeHazard>();

            GameObject visual = new("Pothole Bowl", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            visual.transform.SetParent(root.transform, false);
            visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            visual.GetComponent<MeshCollider>().sharedMesh = mesh;
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/Pothole_Hazard.prefab");
            Object.DestroyImmediate(root);
        }

        private static Mesh BuildPotholeMesh()
        {
            const int segments = 48;
            const int rings = 7;
            const float radius = 0.9f;
            const float depth = 0.18f;
            List<Vector3> vertices = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            for (int ring = 0; ring <= rings; ring++)
            {
                float t = ring / (float)rings;
                float currentRadius = radius * t;
                float height = -depth * Mathf.Pow(1f - t, 2.2f);
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * currentRadius, height, Mathf.Sin(angle) * currentRadius));
                    uvs.Add(new Vector2(0.5f + Mathf.Cos(angle) * t * 0.5f, 0.5f + Mathf.Sin(angle) * t * 0.5f));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = ring * segments + segment;
                    int b = ring * segments + next;
                    int c = (ring + 1) * segments + segment;
                    int d = (ring + 1) * segments + next;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            Mesh mesh = new() { name = "Pothole Bowl" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D LoadTexture(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{SourceRoot}/{name}");

        private static void ConfigureTexture(string path, bool normalMap, bool srgb)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path[..split];
            string name = path[(split + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
