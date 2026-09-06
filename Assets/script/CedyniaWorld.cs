using System.Collections.Generic;
using UnityEngine;

public class CedyniaWorld : MonoBehaviour
{
    public const float GordScale = 8f;
    public const float GordRadius = 52f;
    const float WorldSize = 340f;
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
        BuildRiver(mats);
        BuildForest(mats);
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

    static Material Standard(Texture tex, float gloss, Color tint)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = tex;
        mat.color = tint;
        mat.SetFloat("_Glossiness", gloss);
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
        {
            Debug.LogWarning("Cedynia: nie znaleziono modelu grodziska.");
            return null;
        }

        if (Mathf.Abs(gord.transform.localScale.x - GordScale) > 0.05f)
            gord.transform.localScale = Vector3.one * GordScale;

        var animator = gord.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        foreach (var mf in gord.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
                continue;

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
                EnsureUVs(live, ObjectTile(mf.gameObject.name));
                mf.sharedMesh = live;
            }

            var renderer = mf.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterials = MapMaterials(mf.gameObject.name, renderer.sharedMaterials, mats);

            var col = mf.GetComponent<MeshCollider>();
            if (col == null)
                col = mf.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = live;
        }

        CedyniaSlavicBuildings.Replace(gord, mats.logWood, mats.thatchMoss, mats.woodDark);
        return gord;
    }

    static bool IsReplacedBuilding(string name)
    {
        string n = name.ToLowerInvariant();
        return n.StartsWith("hut") || n.Contains("gatetower") || n == "gate";
    }

    static float ObjectTile(string name)
    {
        string n = name.ToLowerInvariant();
        if (n.Contains("ground") || n.Contains("rampart") || n.Contains("moat"))
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
        string s = (matName + " " + objectName).ToLowerInvariant();
        if (s.Contains("water") || s.Contains("moat"))
            return mats.water;
        if (s.Contains("thatch"))
            return mats.thatch;
        if (s.Contains("grass") || s.Contains("ground"))
            return mats.grass;
        if (s.Contains("earth") || s.Contains("rampart"))
            return mats.earth;
        if (s.Contains("wooddark") || s.Contains("wood_dark") || s.Contains("wooddark"))
            return mats.woodDark;
        if (s.Contains("wood") || s.Contains("palisade") || s.Contains("bridge") || s.Contains("gate") || s.Contains("hut"))
            return s.Contains("dark") ? mats.woodDark : mats.wood;
        if (s.Contains("hut") || s.Contains("tower"))
            return mats.wood;
        return mats.earth;
    }

    static void EnsureUVs(Mesh mesh, float tile)
    {
        var verts = mesh.vertices;
        if (verts == null || verts.Length == 0)
            return;

        var existing = mesh.uv;
        if (existing != null && existing.Length == verts.Length)
        {
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].sqrMagnitude > 0.0001f)
                    return;
            }
        }

        var norms = mesh.normals;
        bool hasN = norms != null && norms.Length == verts.Length;
        var uv = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 p = verts[i];
            Vector3 n = hasN ? norms[i] : Vector3.up;
            Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
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
        return -60f + 13f * Mathf.Sin(z * 0.03f) + 5f * Mathf.Sin(z * 0.071f + 0.8f);
    }

    public static float SampleHeight(float x, float z)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        float hills = (Mathf.PerlinNoise(x * 0.018f + 20f, z * 0.018f) - 0.5f) * 1.1f
                      + (Mathf.PerlinNoise(x * 0.007f + 8f, z * 0.007f) - 0.42f) * 2.2f;
        float h = Mathf.Max(-0.15f, hills);

        if (d < GordRadius - 1.2f)
            return -0.22f;

        if (d < GordRadius + 12f)
        {
            float t = Mathf.InverseLerp(GordRadius - 1.2f, GordRadius + 12f, d);
            t = t * t * (3f - 2f * t);
            h = Mathf.Lerp(-0.22f, h, t);
        }

        float rd = Mathf.Abs(x - RiverX(z));
        float riverW = 9.5f + 2.4f * Mathf.Sin(z * 0.045f);
        if (rd < riverW + 12f)
        {
            float valley = 1f - Mathf.SmoothStep(riverW * 0.4f, riverW + 12f, rd);
            h -= valley * 2.5f;
        }

        return h;
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

                float rd = Mathf.Abs(wx - RiverX(wz));
                if (rd < 16f && wy < 0.15f)
                    colors[i] = new Color(0.85f, 0.8f, 0.7f);
                else
                    colors[i] = Color.white;
            }
        }

        int t = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = z * (res + 1) + x;
                tris[t++] = i;
                tris[t++] = i + res + 1;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + res + 1;
                tris[t++] = i + res + 2;
            }
        }

        var mesh = new Mesh { name = "Krajobraz", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Krajobraz");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = mats.grass;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void BuildRiver(Mats mats)
    {
        const int segs = 90;
        const int across = 7;
        float half = WorldSize * 0.5f;
        var verts = new Vector3[(segs + 1) * (across + 1)];
        var uv = new Vector2[verts.Length];
        var tris = new int[segs * across * 6];

        for (int i = 0; i <= segs; i++)
        {
            float z = Mathf.Lerp(-half, half, i / (float)segs);
            float cx = RiverX(z);
            float width = 10.5f + 2.6f * Mathf.Sin(z * 0.045f);
            Vector3 center = new Vector3(cx, -1.05f, z);
            float dx = RiverX(z + 1.2f) - RiverX(z - 1.2f);
            Vector3 tangent = new Vector3(dx, 0f, 2.4f).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;

            for (int a = 0; a <= across; a++)
            {
                float u = a / (float)across;
                int idx = i * (across + 1) + a;
                verts[idx] = center + side * Mathf.Lerp(-width, width, u);
                verts[idx].y = -1.05f;
                uv[idx] = new Vector2(u * 2f, i / (float)segs * 18f);
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
            float r = Mathf.Lerp(GordRadius + 9f, 158f, Mathf.Pow(Random.value, 0.62f));
            float x = Mathf.Cos(ang) * r;
            float z = Mathf.Sin(ang) * r;
            if (Mathf.Abs(x - RiverX(z)) < 15f)
                continue;
            float h = SampleHeight(x, z);
            if (h < 0.08f)
                continue;

            bool pine = Random.value > 0.38f;
            float scale = Random.Range(0.72f, 1.45f);
            PlaceTree(root.transform, new Vector3(x, h, z), pine, scale, mats);
            placed++;
        }

        StaticBatchingUtility.Combine(root);
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
        Vector3 guess = new Vector3(0f, 28f, 2f);
        if (gord != null)
            guess = gord.transform.TransformPoint(new Vector3(0.15f, 3.2f, 0.35f));

        if (Physics.Raycast(guess + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 80f))
            spawnPoint = hit.point;
        else
            spawnPoint = new Vector3(guess.x, SampleHeight(guess.x, guess.z) + 0.1f, guess.z);

        var player = new GameObject("Gracz");
        player.tag = "Player";
        player.transform.position = spawnPoint;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 55f;
        cc.stepOffset = 0.4f;
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

        var walker = player.AddComponent<FirstPersonWalker>();
        walker.BindEyes(camGo.transform);
        walker.RememberSpawn(spawnPoint);

        if (Camera.main != null && Camera.main != cam)
            Camera.main.gameObject.SetActive(false);
    }
}
