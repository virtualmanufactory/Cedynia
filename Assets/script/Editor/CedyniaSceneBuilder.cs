using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class CedyniaSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Cedynia.unity";
    const string ModelPath = "Assets/scene/cedynia.obj";
    const string MarkerPath = "Assets/Scenes/.cedynia_ready";
    const string TexDir = "Assets/Resources/Cedynia";

    static CedyniaSceneBuilder()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Cedynia/Zbuduj scene grodziska")]
    public static void BuildFromMenu()
    {
        if (File.Exists(MarkerPath))
            File.Delete(MarkerPath);
        Build();
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (File.Exists(MarkerPath) && File.Exists(ScenePath))
            return;
        Build();
    }

    static void Build()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogWarning("Cedynia: brak modelu " + ModelPath);
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
        gord.transform.localScale = Vector3.one * CedyniaWorld.GordScale;

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
        AssetDatabase.Refresh();
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
