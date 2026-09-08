using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class CedyniaSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Cedynia.unity";
    const string MarkerPath = "Assets/Scenes/.cedynia_ready";
    const string TexDir = "Assets/Resources/Cedynia";
    static readonly string[] ModelPaths =
    {
        "Assets/scene/cedynia_gotowa.obj",
        "Assets/scene/cedynia_ulepszona.obj",
        "Assets/scene/cedynia.obj"
    };

    static CedyniaSceneBuilder()
    {
        EditorApplication.delayCall += TryBuild;
        EditorApplication.delayCall += TryPlaceHouses;
        EditorApplication.delayCall += StripOldProceduralHouses;
    }

    [MenuItem("Cedynia/Zbuduj scene grodziska")]
    public static void BuildFromMenu()
    {
        if (File.Exists(MarkerPath))
            File.Delete(MarkerPath);
        Build();
        TryPlaceHouses();
    }

    [MenuItem("Cedynia/Wstaw chaty zrebowe i brame")]
    public static void PlaceHousesMenu()
    {
        var gord = GameObject.Find("Grodzisko_Cedynia");
        if (gord != null)
        {
            var old = gord.transform.Find("Chaty_Slowianskie");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }
        TryPlaceHouses();
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (File.Exists(MarkerPath) && File.Exists(ScenePath))
            return;
        Build();
    }

    static string ResolveModelPath()
    {
        foreach (var path in ModelPaths)
        {
            if (File.Exists(path))
                return path;
        }
        return ModelPaths[0];
    }

    static void Build()
    {
        string modelPath = ResolveModelPath();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Debug.LogWarning("Cedynia: brak modelu " + modelPath);
            return;
        }

        EnsureTextures();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGo = new GameObject("Slonce");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.82f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(42f, 155f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.52f, 0.62f, 0.74f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.48f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.10f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.60f, 0.68f, 0.64f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 260f;

        var gord = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (gord == null)
            gord = Object.Instantiate(model);
        gord.name = "Grodzisko_Cedynia";
        gord.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        gord.transform.localScale = Vector3.one * CedyniaWorld.DetectGordScale(gord);

        var animator = gord.GetComponent<Animator>();
        if (animator != null)
            Object.DestroyImmediate(animator);

        var world = new GameObject("Swiat_Cedynia");
        world.AddComponent<CedyniaWorld>();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        File.WriteAllText(MarkerPath, "ok");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.Refresh();
        Debug.Log("Cedynia: zapisano scene " + ScenePath + ". Wcisnij Play, zeby chodzic po grodzisku.");
    }

    static void EnsureTextures()
    {
        Directory.CreateDirectory(TexDir);
        WriteIfMissing(TexDir + "/grass.png", () => CedyniaTextures.Grass(512));
        WriteIfMissing(TexDir + "/earth.png", () => CedyniaTextures.Earth(512));
        WriteIfMissing(TexDir + "/wood.png", () => CedyniaTextures.Wood(512, false));
        WriteIfMissing(TexDir + "/wood_dark.png", () => CedyniaTextures.Wood(512, true));
        WriteIfMissing(TexDir + "/thatch.png", () => CedyniaTextures.Thatch(512));
        WriteIfMissing(TexDir + "/water.png", () => CedyniaTextures.Water(512));
        WriteIfMissing(TexDir + "/bark.png", () => CedyniaTextures.Bark(256));
        WriteIfMissing(TexDir + "/leaves.png", () => CedyniaTextures.Leaves(256));
        WriteIfMissing(TexDir + "/sand.png", () => CedyniaTextures.Sand(256));
        WriteIfMissing(TexDir + "/logwood.png", () => CedyniaTextures.LogWood(512));
        WriteIfMissing(TexDir + "/thatch_moss.png", () => CedyniaTextures.MossThatch(512));
        AssetDatabase.Refresh();
    }

    static void StripOldProceduralHouses()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        var gord = GameObject.Find("Grodzisko_Cedynia");
        if (gord == null || !CedyniaWorld.HasModelHouses(gord))
            return;
        var leftover = GameObject.Find("Chaty_Slowianskie");
        if (leftover == null)
            return;
        Object.DestroyImmediate(leftover);
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void TryPlaceHouses()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(ScenePath))
            return;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var gord = GameObject.Find("Grodzisko_Cedynia");
        if (gord == null)
            return;
        if (CedyniaWorld.HasModelHouses(gord))
        {
            var leftover = GameObject.Find("Chaty_Slowianskie");
            if (leftover != null)
                Object.DestroyImmediate(leftover);
            return;
        }
        if (gord.transform.Find("Chaty_Slowianskie") != null)
            return;

        EnsureTextures();
        var logWood = GetOrCreateMat("Assets/Resources/Cedynia/LogWood.mat", "logwood", new Color(0.92f, 0.84f, 0.68f), 0.22f);
        var thatch = GetOrCreateMat("Assets/Resources/Cedynia/ThatchMoss.mat", "thatch_moss", Color.white, 0.10f);
        var dark = GetOrCreateMat("Assets/Resources/Cedynia/WoodDark.mat", "wood_dark", Color.white, 0.24f);

        CedyniaSlavicBuildings.Replace(gord, logWood, thatch, dark);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Cedynia: chaty zrebowe i brama sa w scenie (widoczne bez Play).");
    }

    static Material GetOrCreateMat(string path, string textureName, Color tint, float gloss)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + textureName + ".png");
        if (tex == null)
            tex = Resources.Load<Texture2D>("Cedynia/" + textureName);
        mat.shader = Shader.Find("Standard");
        mat.mainTexture = tex;
        mat.color = tint;
        mat.SetFloat("_Glossiness", gloss);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void WriteIfMissing(string path, System.Func<Texture2D> factory)
    {
        if (File.Exists(path))
            return;
        var tex = factory();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}

public class CedyniaTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.IndexOf("Resources/Cedynia", System.StringComparison.OrdinalIgnoreCase) < 0)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 8;
        importer.mipmapEnabled = true;
        importer.sRGBTexture = true;
        importer.textureType = TextureImporterType.Default;
    }
}
