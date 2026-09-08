using System.Collections.Generic;
using UnityEngine;

public class CedyniaWorld : MonoBehaviour
{
    public const float GordScale = 1f;
    public const float GordRadius = 50f;
    public const float MoatRadius = 27f;
    public const float GroundY = 0.05f;
    public const float WaterY = 0.12f;
    public const float RiverDrop = 0.32f;
    public const float BankWidth = 5.5f;
    public const float WorldSize = 340f;
    public const float WorldLimit = 162f;
    public const float FlyMaxY = 72f;
    const int TerrainRes = 150;
    const int TreeCount = 720;

    struct Mats
    {
        public Material grass, earth, wood, woodDark, thatch, water, bark, leaves, sand, logWood, thatchMoss;
    }

    Mesh trunkMesh;
    Mesh coneMesh;
    Mesh sphereMesh;
    Vector3 spawnPoint;

    void Start()
    {
        Random.InitState(972);
        QualitySettings.shadowDistance = 140f;

        var mats = CreateMaterials();
        var gord = PrepareGord(mats);
        BuildLandscape(mats);
        BuildRiver(mats, gord);
        BuildForest(mats);
        BuildWorldBounds();
        SpawnPlayer(gord);
    }

    Mats CreateMaterials()
    {
        var m = new Mats
        {
            grass = Standard(CedyniaTextures.LoadOrCreate("grass", 512, CedyniaTextures.Grass), 0.18f, Color.white),
            earth = Standard(CedyniaTextures.LoadOrCreate("earth", 512, CedyniaTextures.Earth), 0.16f, Color.white),
            wood = Standard(CedyniaTextures.LoadOrCreate("wood", 512, n => CedyniaTextures.Wood(n, false)), 0.28f, Color.white),
            woodDark = Standard(CedyniaTextures.LoadOrCreate("wood_dark", 512, n => CedyniaTextures.Wood(n, true)), 0.24f, Color.white),
            thatch = Standard(CedyniaTextures.LoadOrCreate("thatch", 512, CedyniaTextures.Thatch), 0.12f, Color.white),
            bark = Standard(CedyniaTextures.LoadOrCreate("bark", 256, CedyniaTextures.Bark), 0.18f, Color.white),
            leaves = Standard(CedyniaTextures.LoadOrCreate("leaves", 256, CedyniaTextures.Leaves), 0.14f, Color.white),
            sand = Standard(CedyniaTextures.LoadOrCreate("sand", 256, CedyniaTextures.Sand), 0.22f, Color.white),
            logWood = Standard(CedyniaTextures.LoadOrCreate("logwood", 512, CedyniaTextures.LogWood), 0.22f, new Color(0.92f, 0.84f, 0.68f)),
            thatchMoss = Standard(CedyniaTextures.LoadOrCreate("thatch_moss", 512, CedyniaTextures.MossThatch), 0.10f, Color.white)
        };

        var waterShader = Shader.Find("Cedynia/Water");
        if (waterShader == null)
            waterShader = Shader.Find("Standard");
        m.water = new Material(waterShader);
        m.water.mainTexture = CedyniaTextures.LoadOrCreate("water", 512, CedyniaTextures.Water);
        m.water.color = new Color(0.14f, 0.36f, 0.40f, 0.80f);
        if (m.water.HasProperty("_Glossiness"))
            m.water.SetFloat("_Glossiness", 0.86f);
        if (m.water.HasProperty("_Metallic"))
            m.water.SetFloat("_Metallic", 0.12f);
        m.water.mainTextureScale = new Vector2(4f, 4f);
        return m;
    }

    static Shader LitShader()
    {
        return Shader.Find("Standard")
               ?? Shader.Find("Legacy Shaders/Diffuse")
               ?? Shader.Find("Diffuse")
               ?? Shader.Find("Unlit/Color")
               ?? Shader.Find("Sprites/Default");
    }

    static Material Standard(Texture tex, float gloss, Color tint)
    {
        var shader = LitShader();
        var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
        if (tex != null)
            mat.mainTexture = tex;
        mat.color = tint;
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", gloss);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    GameObject PrepareGord(Mats mats)
    {
        var gord = GameObject.Find("Grodzisko_Cedynia");
        if (gord == null)
        {
            foreach (var mf in FindObjectsOfType<MeshFilter>())
            {
                if (mf.gameObject.name == "Ground")
                {
                    gord = mf.transform.root.gameObject;
                    gord.name = "Grodzisko_Cedynia";
                    break;
                }
            }
        }

        if (gord == null)
            gord = InstantiateGordPrefab();

        if (gord == null)
        {
            Debug.LogWarning("Cedynia: nie znaleziono modelu grodziska.");
            return null;
        }

        float scale = DetectGordScale(gord);
        if (Mathf.Abs(gord.transform.localScale.x - scale) > 0.05f)
            gord.transform.localScale = Vector3.one * scale;

        var animator = gord.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        var bakedHouses = FindDeep(gord.transform, "Chaty_Slowianskie");
        var exitRamp = FindDeep(gord.transform, "Most_wyjscia");
        bool modelHouses = HasModelHouses(gord);

        foreach (var mf in gord.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;
            if (bakedHouses != null && mf.transform.IsChildOf(bakedHouses))
                continue;
            if (exitRamp != null && mf.transform.IsChildOf(exitRamp))
                continue;

            if (modelHouses && IsModelHousePart(mf.gameObject.name))
            {
                Mesh houseMesh = mf.sharedMesh;
                if (mf.sharedMesh.isReadable)
                {
                    houseMesh = Instantiate(mf.sharedMesh);
                    houseMesh.name = mf.sharedMesh.name + "_walk";
                    EnsureUVs(houseMesh, ObjectTile(mf.gameObject.name), mf.gameObject.name);
                    mf.sharedMesh = houseMesh;
                }
                var houseRend = mf.GetComponent<MeshRenderer>();
                if (houseRend != null)
                    houseRend.sharedMaterials = MapMaterials(mf.gameObject.name, houseRend.sharedMaterials, mats);
                if (SkipWalkCollider(mf.gameObject.name))
                    continue;
                var houseCol = mf.GetComponent<MeshCollider>();
                if (houseCol == null)
                    houseCol = mf.gameObject.AddComponent<MeshCollider>();
                houseCol.sharedMesh = houseMesh;
                continue;
            }

            if (IsReplacedBuilding(mf.gameObject.name))
            {
                var rend = mf.GetComponent<MeshRenderer>();
                if (rend != null)
                    rend.enabled = false;
                var oldCol = mf.GetComponent<Collider>();
                if (oldCol != null)
                    oldCol.enabled = false;
                continue;
            }

            Mesh live = mf.sharedMesh;
            if (mf.sharedMesh.isReadable)
            {
                live = Instantiate(mf.sharedMesh);
                live.name = mf.sharedMesh.name + "_walk";
                EnsureUVs(live, ObjectTile(mf.gameObject.name), mf.gameObject.name);
                mf.sharedMesh = live;
            }

            var renderer = mf.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterials = MapMaterials(mf.gameObject.name, renderer.sharedMaterials, mats);

            if (SkipWalkCollider(mf.gameObject.name))
            {
                var skipCol = mf.GetComponent<Collider>();
                if (skipCol != null)
                    skipCol.enabled = false;
                continue;
            }

            var col = mf.GetComponent<MeshCollider>();
            if (col == null)
                col = mf.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = live;
        }

        if (modelHouses)
        {
            var leftover = GameObject.Find("Chaty_Slowianskie");
            if (leftover != null)
                Destroy(leftover);
        }
        else if (bakedHouses != null && bakedHouses.childCount > 0)
            CedyniaSlavicBuildings.KeepExisting(bakedHouses.gameObject);
        else
            CedyniaSlavicBuildings.Replace(gord, mats.logWood, mats.thatchMoss, mats.woodDark);

        if (!modelHouses)
            CedyniaSlavicBuildings.HideOriginals(gord);
        OpenExitThroughGate(gord);
        bool hasBridge = FindDeep(gord.transform, "Most") != null
                         || FindNameContains(gord.transform, "kladka") != null;
        if (!hasBridge && FindDeep(gord.transform, "Most_wyjscia") == null)
            CedyniaSlavicBuildings.BuildExitRamp(gord, mats.wood);
        return gord;
    }

    static GameObject InstantiateGordPrefab()
    {
#if UNITY_EDITOR
        string[] paths =
        {
            "Assets/scene/cedynia_gotowa.obj",
            "Assets/scene/cedynia_ulepszona.obj",
            "Assets/scene/cedynia.obj"
        };
        foreach (var path in paths)
        {
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
                continue;
            var gord = Instantiate(model);
            gord.name = "Grodzisko_Cedynia";
            gord.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var animator = gord.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = false;
            return gord;
        }
#endif
        return null;
    }

    public static float DetectGordScale(GameObject gord)
    {
        if (gord == null)
            return GordScale;
        foreach (var mf in gord.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.gameObject.name != "Ground")
                continue;
            float span = Mathf.Max(mf.sharedMesh.bounds.size.x, mf.sharedMesh.bounds.size.z);
            return span > 20f ? 1f : 8f;
        }
        return GordScale;
    }

    static void OpenExitThroughGate(GameObject gord)
    {
        Vector3 dir = Vector3.right;
        float alongMin = 11.2f;
        float alongMax = 14.8f;
        float halfWidth = 1.85f;

        var most = FindDeep(gord.transform, "Most");
        if (most == null)
        {
            dir = new Vector3(-0.33f, 0f, -2.0f).normalized;
            alongMin = 1.1f;
            alongMax = 5.2f;
            halfWidth = 0.32f;
        }

        var built = FindDeep(gord.transform, "Chaty_Slowianskie");
        if (built == null)
        {
            var sceneHouses = GameObject.Find("Chaty_Slowianskie");
            if (sceneHouses != null)
                built = sceneHouses.transform;
        }

        foreach (var mf in gord.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;
            if (built != null && mf.transform.IsChildOf(built))
                continue;

            string n = mf.gameObject.name.ToLowerInvariant();
            if (!n.Contains("palisade"))
                continue;

            if (!mf.sharedMesh.isReadable)
                continue;

            Mesh cut = CutPassageMesh(mf.sharedMesh, dir, alongMin, alongMax, halfWidth);
            mf.sharedMesh = cut;
            var col = mf.GetComponent<MeshCollider>();
            if (col != null)
                col.sharedMesh = cut;
        }
    }

    static Mesh CutPassageMesh(Mesh src, Vector3 dir, float alongMin, float alongMax, float halfWidth)
    {
        var verts = src.vertices;
        var tris = src.triangles;
        var keep = new System.Collections.Generic.List<int>(tris.Length);
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = verts[tris[i]];
            Vector3 b = verts[tris[i + 1]];
            Vector3 c = verts[tris[i + 2]];
            Vector3 mid = (a + b + c) / 3f;
            Vector3 flat = new Vector3(mid.x, 0f, mid.z);
            float along = Vector3.Dot(flat, dir);
            float side = (flat - dir * along).magnitude;
            if (along > alongMin && along < alongMax && side < halfWidth)
                continue;
            keep.Add(tris[i]);
            keep.Add(tris[i + 1]);
            keep.Add(tris[i + 2]);
        }

        var mesh = Instantiate(src);
        mesh.name = src.name + "_exit";
        mesh.triangles = keep.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    static Transform FindNameContains(Transform root, string part)
    {
        if (root == null)
            return null;
        if (root.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return root;
        foreach (Transform child in root)
        {
            var found = FindNameContains(child, part);
            if (found != null)
                return found;
        }
        return null;
    }

    public static bool HasModelHouses(GameObject gord)
    {
        if (gord == null)
            return false;
        foreach (var t in gord.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.IndexOf("Chata_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    static bool IsModelHousePart(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("chata_") || n.Contains("podmurowka") || n.Contains("wiazki_strzechy")
               || n.Contains("kalenica") || n.Contains("brama_");
    }

    static bool IsReplacedBuilding(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("chata") || n.Contains("brama") || n.Contains("chaty"))
            return false;
        return n.StartsWith("hut") || n.Contains("gatetower") || n == "gate";
    }

    static float ObjectTile(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("ground") || n.Contains("rampart") || n.Contains("moat") || n.Contains("laka")
            || n.Contains("poszerzony") || n.Contains("droga") || n.Contains("brzeg"))
            return 0.85f;
        return 1.25f;
    }

    static Material[] MapMaterials(string objectName, Material[] current, Mats mats)
    {
        if (current == null || current.Length == 0)
            return new[] { PickMaterial(objectName, "", mats) };

        var mapped = new Material[current.Length];
        for (int i = 0; i < current.Length; i++)
        {
            string matName = current[i] != null ? current[i].name : "";
            mapped[i] = PickMaterial(objectName, matName, mats);
        }
        return mapped;
    }

    static Material PickMaterial(string objectName, string matName, Mats mats)
    {
        string mat = (matName ?? "").ToLowerInvariant().Replace("|", " ");
        string obj = (objectName ?? "").ToLowerInvariant().Replace("|", " ");
        string s = mat.Length > 2 ? mat : (mat + " " + obj);

        if (s.Contains("woda") || s.Contains("fosa") || s.Contains("refleks") || s.Contains("water")
            || obj == "moat" || obj.Contains("rzeka  woda") || obj.Contains("odnoga_rzeki  woda"))
            return mats.water;
        if (s.Contains("gont"))
            return mats.woodDark;
        if (s.Contains("strzecha") || s.Contains("thatch") || s.Contains("dach") || s.Contains("trzcina"))
            return mats.thatchMoss != null ? mats.thatchMoss : mats.thatch;
        if (s.Contains("liscie") || obj.Contains("korona"))
            return mats.leaves;
        if (obj.Contains("pien") || s.Contains("bark"))
            return mats.bark;
        if (s.Contains("laka") || s.Contains("poszerzony") || s.Contains("grass")
            || (obj.Contains("ground") && !obj.Contains("chata")))
            return mats.grass;
        if (s.Contains("kamien") || s.Contains("podmurowka") || s.Contains("prog") || s.Contains("brzeg") || s.Contains("piasek"))
            return mats.sand;
        if (s.Contains("ubita") || s.Contains("ziemia") || s.Contains("earth") || obj.Contains("rampart") || obj.Contains("droga"))
            return mats.earth;
        if (s.Contains("wnetrze") || s.Contains("okno") || s.Contains("postarzale"))
            return mats.woodDark;
        if (s.Contains("wooddark") || s.Contains("wood_dark"))
            return mats.woodDark;
        if (s.Contains("drewno") || s.Contains("belki") || s.Contains("drzwi") || s.Contains("slup")
            || s.Contains("bale") || s.Contains("kalenica") || s.Contains("nadproze") || s.Contains("deski")
            || obj.Contains("most") || obj.Contains("kladka") || obj.Contains("plot") || obj.Contains("palisade"))
            return mats.logWood != null ? mats.logWood : mats.wood;
        if (s.Contains("wood") || obj.Contains("brama") || obj.Contains("bridge") || obj.Contains("gate") || obj.Contains("hut"))
            return s.Contains("dark") ? mats.woodDark : mats.wood;
        if (obj.Contains("chata"))
            return mats.logWood != null ? mats.logWood : mats.wood;
        return mats.earth;
    }

    static bool SkipWalkCollider(string objectName)
    {
        string n = (objectName ?? "").ToLowerInvariant().Replace("|", " ");
        if (n.Contains("korona") || n.Contains("liscie") || n.Contains("trzcina") || n.Contains("refleks") || n.Contains("gont"))
            return true;
        if (n.Contains("woda") || n == "moat" || n.Contains("rzeka  woda") || n.Contains("odnoga_rzeki  woda"))
            return true;
        return false;
    }

    static void EnsureUVs(Mesh mesh, float tile, string objectName = "")
    {
        var verts = mesh.vertices;
        if (verts == null || verts.Length == 0)
            return;

        string n = objectName.ToLowerInvariant();
        bool forceTriplanar = n.Contains("ground") || n.Contains("rampart") || n.Contains("poszerzony") || n.Contains("laka") || n.Contains("droga");

        var existing = mesh.uv;
        if (!forceTriplanar && existing != null && existing.Length == verts.Length)
        {
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].sqrMagnitude > 0.0001f)
                    return;
            }
        }

        ApplyTriplanarUVs(mesh, tile);
    }

    static void ApplyTriplanarUVs(Mesh mesh, float tile)
    {
        var verts = mesh.vertices;
        var norms = mesh.normals;
        bool hasN = norms != null && norms.Length == verts.Length;
        var uv = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = verts[i];
            Vector3 nn = hasN ? norms[i] : Vector3.up;
            Vector3 a = new Vector3(Mathf.Abs(nn.x), Mathf.Abs(nn.y), Mathf.Abs(nn.z));
            if (a.y >= a.x && a.y >= a.z)
                uv[i] = new Vector2(p.x, p.z) * tile;
            else if (a.x >= a.z)
                uv[i] = new Vector2(p.z, p.y) * tile;
            else
                uv[i] = new Vector2(p.x, p.y) * tile;
        }
        mesh.uv = uv;
    }

    public static float RiverX(float z)
    {
        float far = -82f;
        float join = -29f;
        float t = 1f - Mathf.SmoothStep(0f, 90f, Mathf.Abs(z));
        return Mathf.Lerp(far, join, t);
    }

    public static float DistToWater(float x, float z)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        float toRiver = Mathf.Abs(x - RiverX(z));
        float toMoat = Mathf.Abs(d - MoatRadius);
        if (d < GordRadius)
            return Mathf.Min(toRiver, toMoat);
        return toRiver;
    }

    public static float SampleHeight(float x, float z)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        if (d < GordRadius - 2f)
            return GroundY;

        float rd = DistToWater(x, z);
        if (rd >= BankWidth)
            return GroundY;
        float t = Mathf.SmoothStep(0f, 1f, rd / BankWidth);
        return Mathf.Lerp(GroundY - RiverDrop, GroundY, t);
    }

    static void FlattenMound(Mesh mesh, string objectName)
    {
        string n = objectName.ToLowerInvariant();
        var v = mesh.vertices;
        if (v == null || v.Length == 0)
            return;

        if (n.Contains("chata") || n.Contains("brama") || n.Contains("bale") || n.Contains("strzecha") || n.Contains("detal") || n.Contains("most"))
            return;

        if (n.Contains("ground") || n.Contains("rampart"))
        {
            for (int i = 0; i < v.Length; i++)
                v[i].y = GroundY / GordScale;
        }
        else if (!n.Contains("moat") && !n.Contains("water"))
        {
            const float drop = 1.30f;
            for (int i = 0; i < v.Length; i++)
                v[i].y -= drop;
        }

        mesh.vertices = v;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    void BuildLandscape(Mats mats)
    {
        int res = TerrainRes;
        float half = WorldSize * 0.5f;
        var verts = new Vector3[(res + 1) * (res + 1)];
        var uv = new Vector2[verts.Length];
        var colors = new Color[verts.Length];
        var tris = new int[res * res * 6];

        for (int z = 0; z <= res; z++)
        {
            for (int x = 0; x <= res; x++)
            {
                int i = z * (res + 1) + x;
                float wx = Mathf.Lerp(-half, half, x / (float)res);
                float wz = Mathf.Lerp(-half, half, z / (float)res);
                float wy = SampleHeight(wx, wz);
                verts[i] = new Vector3(wx, wy, wz);
                uv[i] = new Vector2(wx, wz) * 0.085f;

                float rd = DistToWater(wx, wz);
                if (rd < 14f && wy < 0.12f)
                    colors[i] = new Color(0.85f, 0.8f, 0.7f);
                else
                    colors[i] = Color.white;
            }
        }

        int t = 0;
        float hole = GordRadius - 3f;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = z * (res + 1) + x;
                Vector3 a = verts[i];
                Vector3 b = verts[i + res + 1];
                Vector3 c = verts[i + 1];
                Vector3 d = verts[i + res + 2];
                float ra = Mathf.Sqrt(a.x * a.x + a.z * a.z);
                float rb = Mathf.Sqrt(b.x * b.x + b.z * b.z);
                float rc = Mathf.Sqrt(c.x * c.x + c.z * c.z);
                float rd2 = Mathf.Sqrt(d.x * d.x + d.z * d.z);
                if (ra < hole && rb < hole && rc < hole && rd2 < hole)
                    continue;
                tris[t++] = i;
                tris[t++] = i + res + 1;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + res + 1;
                tris[t++] = i + res + 2;
            }
        }
        System.Array.Resize(ref tris, t);

        var mesh = new Mesh { name = "Krajobraz", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ApplyTriplanarUVs(mesh, 0.085f);

        var go = new GameObject("Krajobraz");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = mats.grass;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void BuildRiver(Mats mats, GameObject gord)
    {
        const int segs = 120;
        const int across = 7;
        float half = WorldSize * 0.5f;
        var verts = new Vector3[(segs + 1) * (across + 1)];
        var uv = new Vector2[verts.Length];
        var tris = new int[segs * across * 6];

        for (int i = 0; i <= segs; i++)
        {
            float z = Mathf.Lerp(-half, half, i / (float)segs);
            float cx = RiverX(z);
            float d = Mathf.Sqrt(cx * cx + z * z);
            float width = d < GordRadius
                ? 7.2f
                : 10.5f + 2.6f * Mathf.Sin(z * 0.045f);
            float dx = RiverX(z + 1.4f) - RiverX(z - 1.4f);
            Vector3 tangent = new Vector3(dx, 0f, 2.8f).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            Vector3 center = new Vector3(cx, WaterY, z);

            for (int a = 0; a <= across; a++)
            {
                float u = a / (float)across;
                int idx = i * (across + 1) + a;
                verts[idx] = center + side * Mathf.Lerp(-width, width, u);
                verts[idx].y = WaterY;
                uv[idx] = new Vector2(u * 2f, i / (float)segs * 18f);
            }
        }

        int t = 0;
        for (int i = 0; i < segs; i++)
        {
            for (int a = 0; a < across; a++)
            {
                int i0 = i * (across + 1) + a;
                Vector3 mid = (verts[i0] + verts[i0 + across + 1] + verts[i0 + 1]) / 3f;
                if (Mathf.Sqrt(mid.x * mid.x + mid.z * mid.z) < GordRadius - 1f)
                    continue;
                tris[t++] = i0;
                tris[t++] = i0 + across + 1;
                tris[t++] = i0 + 1;
                tris[t++] = i0 + 1;
                tris[t++] = i0 + across + 1;
                tris[t++] = i0 + across + 2;
            }
        }
        System.Array.Resize(ref tris, t);

        var mesh = new Mesh { name = "Rzeka" };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Rzeka_Odra");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = mats.water;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        bool hasMoat = gord != null && FindDeep(gord.transform, "Moat") != null;
        if (!hasMoat)
            BuildMoatRing(mats.water);
    }

    void BuildMoatRing(Material water)
    {
        const int segs = 72;
        const int across = 5;
        float inner = MoatRadius - 6.5f;
        float outer = MoatRadius + 6.5f;
        var verts = new Vector3[(segs + 1) * (across + 1)];
        var uv = new Vector2[verts.Length];
        var tris = new int[segs * across * 6];

        for (int i = 0; i <= segs; i++)
        {
            float ang = i / (float)segs * Mathf.PI * 2f;
            float ca = Mathf.Cos(ang);
            float sa = Mathf.Sin(ang);
            for (int a = 0; a <= across; a++)
            {
                float u = a / (float)across;
                float r = Mathf.Lerp(inner, outer, u);
                int idx = i * (across + 1) + a;
                verts[idx] = new Vector3(ca * r, WaterY, sa * r);
                uv[idx] = new Vector2(u * 2f, i / (float)segs * 8f);
            }
        }

        int t = 0;
        for (int i = 0; i < segs; i++)
        {
            for (int a = 0; a < across; a++)
            {
                int i0 = i * (across + 1) + a;
                tris[t++] = i0;
                tris[t++] = i0 + across + 1;
                tris[t++] = i0 + 1;
                tris[t++] = i0 + 1;
                tris[t++] = i0 + across + 1;
                tris[t++] = i0 + across + 2;
            }
        }

        var mesh = new Mesh { name = "Fosa" };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Fosa_woda");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = water;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void BuildForest(Mats mats)
    {
        CachePrimitives();
        var root = new GameObject("Las");
        root.transform.SetParent(transform, false);

        int placed = 0;
        int attempts = 0;
        while (placed < TreeCount && attempts < 5000)
        {
            attempts++;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Lerp(GordRadius + 22f, 158f, Mathf.Pow(Random.value, 0.62f));
            float x = Mathf.Cos(ang) * r;
            float z = Mathf.Sin(ang) * r;
            if (DistToWater(x, z) < 18f)
                continue;
            float h = SampleHeight(x, z);
            if (h < GroundY - 0.01f)
                continue;

            bool pine = Random.value > 0.38f;
            float scale = Random.Range(0.72f, 1.45f);
            PlaceTree(root.transform, new Vector3(x, h, z), pine, scale, mats);
            placed++;
        }

        StaticBatchingUtility.Combine(root);
    }

    void BuildWorldBounds()
    {
        float half = WorldSize * 0.5f;
        float wallH = 90f;
        float thick = 6f;
        var root = new GameObject("Granice_swiata");
        root.transform.SetParent(transform, false);

        AddBoundWall(root.transform, new Vector3(half + thick * 0.5f, wallH * 0.5f, 0f), new Vector3(thick, wallH, WorldSize + thick * 2f));
        AddBoundWall(root.transform, new Vector3(-half - thick * 0.5f, wallH * 0.5f, 0f), new Vector3(thick, wallH, WorldSize + thick * 2f));
        AddBoundWall(root.transform, new Vector3(0f, wallH * 0.5f, half + thick * 0.5f), new Vector3(WorldSize + thick * 2f, wallH, thick));
        AddBoundWall(root.transform, new Vector3(0f, wallH * 0.5f, -half - thick * 0.5f), new Vector3(WorldSize + thick * 2f, wallH, thick));
        AddBoundWall(root.transform, new Vector3(0f, FlyMaxY + 2f, 0f), new Vector3(WorldSize + 8f, 4f, WorldSize + 8f));
    }

    static void AddBoundWall(Transform parent, Vector3 pos, Vector3 size)
    {
        var go = new GameObject("Sciana");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
    }

    void CachePrimitives()
    {
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkMesh = Instantiate(cyl.GetComponent<MeshFilter>().sharedMesh);
        Destroy(cyl);

        var sph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereMesh = Instantiate(sph.GetComponent<MeshFilter>().sharedMesh);
        Destroy(sph);

        coneMesh = CreateCone(9);
    }

    static Mesh CreateCone(int seg)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();

        verts.Add(new Vector3(0f, 1f, 0f));
        norms.Add(Vector3.up);
        uv.Add(new Vector2(0.5f, 1f));

        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts.Add(p);
            norms.Add((p + Vector3.up * 0.35f).normalized);
            uv.Add(new Vector2(i / (float)seg, 0f));
        }

        for (int i = 0; i < seg; i++)
        {
                tris.Add(0);
                tris.Add(2 + i);
                tris.Add(1 + i);
        }

        var mesh = new Mesh { name = "Cone" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void PlaceTree(Transform parent, Vector3 pos, bool pine, float scale, Mats mats)
    {
        var tree = new GameObject(pine ? "Sosna" : "Dab");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        tree.transform.localScale = Vector3.one * scale;

        var trunk = new GameObject("Pien");
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, pine ? 1.15f : 1.35f, 0f);
        trunk.transform.localScale = pine
            ? new Vector3(0.22f, 1.15f, 0.22f)
            : new Vector3(0.30f, 1.35f, 0.30f);
        trunk.AddComponent<MeshFilter>().sharedMesh = trunkMesh;
        trunk.AddComponent<MeshRenderer>().sharedMaterial = mats.bark;

        if (pine)
        {
            AddCrown(tree.transform, coneMesh, mats.leaves, new Vector3(0f, 2.5f, 0f), new Vector3(1.55f, 2.1f, 1.55f));
            AddCrown(tree.transform, coneMesh, mats.leaves, new Vector3(0f, 3.7f, 0f), new Vector3(1.15f, 1.7f, 1.15f));
            AddCrown(tree.transform, coneMesh, mats.leaves, new Vector3(0f, 4.7f, 0f), new Vector3(0.72f, 1.25f, 0.72f));
        }
        else
        {
            AddCrown(tree.transform, sphereMesh, mats.leaves, new Vector3(0f, 3.5f, 0f), new Vector3(2.05f, 1.7f, 2.05f));
            AddCrown(tree.transform, sphereMesh, mats.leaves, new Vector3(0.35f, 4.4f, -0.15f), new Vector3(1.4f, 1.2f, 1.4f));
        }

        var cap = tree.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1.4f, 0f);
        cap.radius = 0.24f;
        cap.height = 2.8f;
    }

    static void AddCrown(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject("Korona");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = mat;
    }

    void SpawnPlayer(GameObject gord)
    {
        Vector3 guess = new Vector3(0f, 8.2f, 0f);
        Vector3 lookTarget = guess + Vector3.forward;
        if (gord != null)
        {
            Transform chata = null;
            foreach (var t in gord.GetComponentsInChildren<Transform>())
            {
                if (t.name.IndexOf("Chata_01", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    chata = t;
                    break;
                }
            }
            if (chata == null)
            {
                var houses = FindDeep(gord.transform, "Chaty_Slowianskie");
                if (houses != null && houses.childCount > 0)
                    chata = houses.GetChild(0);
            }
            if (chata != null)
            {
                var rend = chata.GetComponent<Renderer>();
                if (rend == null)
                    rend = chata.GetComponentInChildren<Renderer>();
                Vector3 house = rend != null ? rend.bounds.center : chata.position;
                float floorY = rend != null ? rend.bounds.min.y : house.y;
                Vector3 inward = new Vector3(-house.x, 0f, -house.z);
                if (inward.sqrMagnitude < 0.01f)
                    inward = Vector3.back;
                inward.Normalize();
                guess = new Vector3(house.x, floorY + 1.7f, house.z) + inward * 4.2f;
                lookTarget = new Vector3(house.x, floorY + 1.4f, house.z);
            }
            else
            {
                guess = gord.transform.TransformPoint(new Vector3(0f, 8.1f, 0.4f));
                lookTarget = gord.transform.TransformPoint(new Vector3(4f, 7.8f, 0f));
            }
        }

        Physics.SyncTransforms();
        if (Physics.Raycast(guess + Vector3.up * 12f, Vector3.down, out RaycastHit hit, 50f))
            spawnPoint = hit.point;
        else
            spawnPoint = guess;

        var player = new GameObject("Gracz");
        player.tag = "Player";
        player.transform.position = spawnPoint;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 80f;
        cc.stepOffset = 0.55f;
        cc.minMoveDistance = 0f;
        cc.skinWidth = 0.06f;

        var camGo = new GameObject("Kamera");
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.07f;
        cam.farClipPlane = 420f;
        cam.fieldOfView = 68f;
        camGo.AddComponent<AudioListener>();

        Vector3 look = lookTarget - spawnPoint;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(look);

        var walker = player.AddComponent<FirstPersonWalker>();
        walker.BindEyes(camGo.transform);
        walker.RememberSpawn(spawnPoint);

        if (Camera.main != null && Camera.main != cam)
            Camera.main.gameObject.SetActive(false);
    }
}
