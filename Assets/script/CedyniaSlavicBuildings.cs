using System.Collections.Generic;
using UnityEngine;

public static class CedyniaSlavicBuildings
{
    public static int Replace(GameObject gord, Material logWood, Material thatch, Material darkWood)
    {
        if (gord == null || logWood == null || thatch == null || darkWood == null)
        {
            Debug.LogWarning("Cedynia: brak grodziska albo materialow — nie wstawiono chat.");
            return 0;
        }

        HideOriginals(gord);

        Transform existing = gord.transform.Find("Chaty_Slowianskie");
        if (existing != null)
        {
            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        var root = new GameObject("Chaty_Slowianskie");
        root.transform.SetParent(gord.transform, false);

        int built = 0;
        foreach (var spec in HouseLayout)
        {
            Vector3 axis = spec.axis;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.forward;
            axis.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, axis);
            bool doorOnPlusX = Vector3.Dot(right, -new Vector3(spec.pos.x, 0f, spec.pos.z)) > 0f;

            var house = BuildLogHouse(spec.length, spec.width, 0.32f, 0.030f, doorOnPlusX, true, logWood, thatch, darkWood);
            house.name = spec.name;
            house.transform.SetParent(root.transform, false);
            house.transform.localPosition = spec.pos;
            house.transform.localRotation = Quaternion.LookRotation(axis, Vector3.up);

            var box = house.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.24f, 0f);
            box.size = new Vector3(spec.width + 0.08f, 0.48f, spec.length + 0.08f);
            built++;
        }

        Vector3 gatePos = new Vector3(-0.33f, 1.278f, -2.00f);
        Vector3 outward = new Vector3(gatePos.x, 0f, gatePos.z).normalized;
        var gate = BuildGatehouse(1.10f, 0.78f, 0.38f, 0.36f, 0.030f, logWood, thatch, darkWood);
        gate.name = "Brama_zrebowa";
        gate.transform.SetParent(root.transform, false);
        gate.transform.localPosition = gatePos;
        gate.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
        built++;

        Debug.Log("Cedynia: wstawiono " + built + " budynkow zrebowych (6 chat + brama).");
        return built;
    }

    public static void HideOriginals(GameObject gord)
    {
        foreach (var mf in gord.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf == null)
                continue;
            if (mf.transform.root != gord.transform && mf.transform != gord.transform && !mf.transform.IsChildOf(gord.transform))
                continue;
            var alreadyBuilt = gord.transform.Find("Chaty_Slowianskie");
            if (alreadyBuilt != null && mf.transform.IsChildOf(alreadyBuilt))
                continue;

            string n = (mf.gameObject.name + " " + (mf.sharedMesh != null ? mf.sharedMesh.name : "")).ToLowerInvariant();
            bool hutOrGate = n.Contains("hut") || n.Contains("gate") || n.Contains("brama") || n.Contains("chata");
            if (!hutOrGate && mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0 && mf.sharedMesh.vertexCount <= 40)
            {
                Vector3 c = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
                Vector3 local = gord.transform.InverseTransformPoint(c);
                if (local.y > 1.1f && new Vector2(local.x, local.z).magnitude < 2.4f)
                    hutOrGate = true;
            }

            if (!hutOrGate)
                continue;

            var rend = mf.GetComponent<MeshRenderer>();
            if (rend != null)
                rend.enabled = false;
            foreach (var col in mf.GetComponents<Collider>())
                col.enabled = false;
        }
    }

    struct HouseSpec
    {
        public string name;
        public Vector3 pos;
        public Vector3 axis;
        public float length;
        public float width;
    }

    static readonly HouseSpec[] HouseLayout =
    {
        new HouseSpec { name = "Chata_1", pos = new Vector3(1.232f, 1.344f, 1.512f), axis = new Vector3(-0.757f, 0f, 0.231f), length = 0.88f, width = 0.60f },
        new HouseSpec { name = "Chata_2", pos = new Vector3(-1.078f, 1.346f, -1.379f), axis = new Vector3(-0.476f, 0f, 0.478f), length = 0.82f, width = 0.56f },
        new HouseSpec { name = "Chata_3", pos = new Vector3(-1.379f, 1.341f, -0.448f), axis = new Vector3(-0.166f, 0f, 0.698f), length = 0.76f, width = 0.52f },
        new HouseSpec { name = "Chata_4", pos = new Vector3(-1.496f, 1.344f, 0.697f), axis = new Vector3(0.279f, 0f, 0.659f), length = 0.76f, width = 0.52f },
        new HouseSpec { name = "Chata_5", pos = new Vector3(-0.900f, 1.348f, 1.559f), axis = new Vector3(0.590f, 0f, 0.457f), length = 0.76f, width = 0.54f },
        new HouseSpec { name = "Chata_6", pos = new Vector3(0.131f, 1.338f, 1.494f), axis = new Vector3(0.710f, 0f, 0.033f), length = 0.82f, width = 0.52f }
    };

    static void BuildHouseFromOriginal(Transform parent, MeshFilter original, Material logWood, Material thatch, Material darkWood)
    {
        var mesh = original.sharedMesh;
        var verts = mesh.vertices;
        if (verts == null || verts.Length < 4)
            return;

        Bounds b = mesh.bounds;
        Vector3 center = b.center;
        float floorY = b.min.y;

        Vector3 ridgeA, ridgeB;
        FindRidge(verts, out ridgeA, out ridgeB);
        Vector3 longAxis = ridgeB - ridgeA;
        longAxis.y = 0f;
        if (longAxis.sqrMagnitude < 0.0001f)
            longAxis = b.size.x >= b.size.z ? Vector3.right : Vector3.forward;
        longAxis.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, longAxis).normalized;
        Vector3 toCenter = -new Vector3(center.x, 0f, center.z);
        bool doorOnPlusX = Vector3.Dot(right, toCenter) > 0f;

        float length = Mathf.Max(b.size.x, b.size.z) * 0.92f;
        float width = Mathf.Min(b.size.x, b.size.z) * 0.82f;
        length = Mathf.Max(length, 0.72f);
        width = Mathf.Max(width, 0.52f);

        var pose = new Pose
        {
            pos = new Vector3(center.x, floorY, center.z),
            rot = Quaternion.LookRotation(longAxis, Vector3.up)
        };

        var house = BuildLogHouse(length, width, 0.30f, 0.028f, doorOnPlusX, true, logWood, thatch, darkWood);
        house.name = original.gameObject.name + "_zreb";
        house.transform.SetParent(parent, false);
        house.transform.localPosition = pose.pos;
        house.transform.localRotation = pose.rot;

        var box = house.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.22f, 0f);
        box.size = new Vector3(width + 0.06f, 0.44f, length + 0.06f);
    }

    static void BuildGateFromOriginal(Transform parent, MeshFilter original, Material logWood, Material thatch, Material darkWood)
    {
        Bounds b = original.sharedMesh.bounds;
        Vector3 center = b.center;
        Vector3 outward = new Vector3(center.x, 0f, center.z);
        if (outward.sqrMagnitude < 0.0001f)
            outward = Vector3.back;
        outward.Normalize();

        var gate = BuildGatehouse(1.05f, 0.72f, 0.36f, 0.34f, 0.028f, logWood, thatch, darkWood);
        gate.name = "Brama_zrebowa";
        gate.transform.SetParent(parent, false);
        gate.transform.localPosition = new Vector3(center.x, b.min.y, center.z);
        gate.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
    }

    static void FindRidge(Vector3[] verts, out Vector3 a, out Vector3 b)
    {
        int i0 = 0, i1 = 1;
        float y0 = verts[0].y, y1 = verts[1].y;
        if (y1 > y0)
        {
            float tmp = y0;
            y0 = y1;
            y1 = tmp;
            int it = i0;
            i0 = i1;
            i1 = it;
        }
        for (int i = 2; i < verts.Length; i++)
        {
            float y = verts[i].y;
            if (y > y0)
            {
                y1 = y0;
                i1 = i0;
                y0 = y;
                i0 = i;
            }
            else if (y > y1)
            {
                y1 = y;
                i1 = i;
            }
        }
        a = verts[i0];
        b = verts[i1];
    }

    static GameObject BuildLogHouse(float length, float width, float wallH, float logR, bool doorOnPlusX, bool firewood, Material logWood, Material thatch, Material darkWood)
    {
        var wood = new Acc();
        var roof = new Acc();
        var dark = new Acc();

        float halfL = length * 0.5f;
        float halfW = width * 0.5f;
        float overlap = logR * 1.65f;
        int courses = Mathf.Max(8, Mathf.RoundToInt(wallH / (logR * 2f)));
        float step = wallH / courses;
        float doorWidth = 0.13f;
        float doorHeight = wallH * 0.78f;
        float doorX = doorOnPlusX ? halfW : -halfW;

        for (int c = 0; c < courses; c++)
        {
            float y = logR + c * step;
            bool longCourse = (c % 2 == 0);
            if (longCourse)
            {
                AddLog(wood, new Vector3(-halfW, y, -halfL - overlap), new Vector3(-halfW, y, halfL + overlap), logR);
                AddLogWithDoorGap(wood, new Vector3(halfW, y, -halfL - overlap), new Vector3(halfW, y, halfL + overlap), logR, doorOnPlusX, y, doorHeight, doorWidth);
            }
            else
            {
                AddLog(wood, new Vector3(-halfW - overlap, y, -halfL), new Vector3(halfW + overlap, y, -halfL), logR);
                AddLog(wood, new Vector3(-halfW - overlap, y, halfL), new Vector3(halfW + overlap, y, halfL), logR);
            }
        }

        float ridgeY = wallH + width * 0.55f;
        float eave = 0.10f;
        float thick = 0.03f;

        AddGableLogs(wood, halfL, halfW, wallH, ridgeY, logR);
        AddGableLogs(wood, -halfL, halfW, wallH, ridgeY, logR);

        AddThatchSide(roof, halfL + eave, -halfW - eave * 0.4f, wallH, ridgeY, thick);
        AddThatchSide(roof, halfL + eave, halfW + eave * 0.4f, wallH, ridgeY, thick);

        int poles = 5;
        for (int i = 0; i < poles; i++)
        {
            float t = (i + 0.5f) / poles;
            AddRoofPole(dark, halfL + eave * 0.75f, halfW, wallH, ridgeY, t, 0.007f);
            AddRoofPole(dark, halfL + eave * 0.75f, -halfW, wallH, ridgeY, t, 0.007f);
        }

        AddWindboards(dark, halfL + 0.01f, halfW, wallH, ridgeY);
        AddWindboards(dark, -halfL - 0.01f, halfW, wallH, ridgeY);
        AddLog(dark, new Vector3(0f, ridgeY + 0.01f, -halfL - eave), new Vector3(0f, ridgeY + 0.01f, halfL + eave), 0.01f, 6);

        AddDoor(dark, doorX, doorWidth, doorHeight, doorOnPlusX);

        if (firewood)
            AddFirewood(wood, doorOnPlusX ? -halfW - 0.04f : halfW + 0.04f, halfL * 0.15f, logR);

        var go = new GameObject("Dom");
        AttachMesh(go, "Bale", wood.ToMesh("Bale"), logWood);
        AttachMesh(go, "Strzecha", roof.ToMesh("Strzecha"), thatch);
        AttachMesh(go, "Detal", dark.ToMesh("Detal"), darkWood);
        return go;
    }

    static GameObject BuildGatehouse(float width, float depth, float wallH, float opening, float logR, Material logWood, Material thatch, Material darkWood)
    {
        var wood = new Acc();
        var roof = new Acc();
        var dark = new Acc();

        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;
        float halfOpen = opening * 0.5f;
        float overlap = logR * 1.55f;
        int courses = Mathf.Max(9, Mathf.RoundToInt(wallH / (logR * 2f)));
        float step = wallH / courses;

        for (int c = 0; c < courses; c++)
        {
            float y = logR + c * step;
            bool longCourse = (c % 2 == 0);
            if (longCourse)
            {
                AddLog(wood, new Vector3(-halfW, y, -halfD - overlap), new Vector3(-halfW, y, halfD + overlap), logR);
                AddLog(wood, new Vector3(halfW, y, -halfD - overlap), new Vector3(halfW, y, halfD + overlap), logR);
                AddLog(wood, new Vector3(-halfW, y, -halfOpen), new Vector3(-halfOpen - 0.02f, y, -halfOpen), logR);
                AddLog(wood, new Vector3(halfOpen + 0.02f, y, -halfOpen), new Vector3(halfW, y, -halfOpen), logR);
                AddLog(wood, new Vector3(-halfW, y, halfOpen), new Vector3(-halfOpen - 0.02f, y, halfOpen), logR);
                AddLog(wood, new Vector3(halfOpen + 0.02f, y, halfOpen), new Vector3(halfW, y, halfOpen), logR);
            }
            else
            {
                AddLog(wood, new Vector3(-halfW - overlap, y, -halfD), new Vector3(-halfOpen, y, -halfD), logR);
                AddLog(wood, new Vector3(halfOpen, y, -halfD), new Vector3(halfW + overlap, y, -halfD), logR);
                AddLog(wood, new Vector3(-halfW - overlap, y, halfD), new Vector3(-halfOpen, y, halfD), logR);
                AddLog(wood, new Vector3(halfOpen, y, halfD), new Vector3(halfW + overlap, y, halfD), logR);
            }
        }

        float ridgeY = wallH + width * 0.42f;
        float eave = 0.08f;
        AddGableLogs(wood, halfD, halfW, wallH, ridgeY, logR);
        AddGableLogs(wood, -halfD, halfW, wallH, ridgeY, logR);
        AddThatchSide(roof, halfD + eave, -halfW - eave * 0.3f, wallH, ridgeY, 0.03f);
        AddThatchSide(roof, halfD + eave, halfW + eave * 0.3f, wallH, ridgeY, 0.03f);

        for (int i = 0; i < 5; i++)
        {
            float t = (i + 0.5f) / 5f;
            AddRoofPole(dark, halfD + eave * 0.7f, halfW, wallH, ridgeY, t, 0.007f);
            AddRoofPole(dark, halfD + eave * 0.7f, -halfW, wallH, ridgeY, t, 0.007f);
        }

        AddWindboards(dark, halfD + 0.01f, halfW, wallH, ridgeY);
        AddWindboards(dark, -halfD - 0.01f, halfW, wallH, ridgeY);
        AddLog(dark, new Vector3(0f, ridgeY + 0.01f, -halfD - eave), new Vector3(0f, ridgeY + 0.01f, halfD + eave), 0.01f, 6);

        AddGateLeaf(dark, -halfOpen, halfD * 0.15f, wallH * 0.86f, true);
        AddGateLeaf(dark, halfOpen, halfD * 0.15f, wallH * 0.86f, false);

        var go = new GameObject("Brama");
        AttachMesh(go, "Bale", wood.ToMesh("Bale"), logWood);
        AttachMesh(go, "Strzecha", roof.ToMesh("Strzecha"), thatch);
        AttachMesh(go, "Detal", dark.ToMesh("Detal"), darkWood);

        AddPierCollider(go, new Vector3(-(halfW + halfOpen) * 0.5f, wallH * 0.5f, 0f), new Vector3(halfW - halfOpen, wallH, depth + 0.04f));
        AddPierCollider(go, new Vector3((halfW + halfOpen) * 0.5f, wallH * 0.5f, 0f), new Vector3(halfW - halfOpen, wallH, depth + 0.04f));
        return go;
    }

    static void AddPierCollider(GameObject go, Vector3 center, Vector3 size)
    {
        var box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    static void AddLog(Acc acc, Vector3 a, Vector3 b, float radius, int seg = 8)
    {
        AddCylinder(acc, a, b, radius, seg);
    }

    static void AddLogWithDoorGap(Acc acc, Vector3 a, Vector3 b, float radius, bool doorOnThisWall, float y, float doorHeight, float doorWidth)
    {
        if (!doorOnThisWall || y > doorHeight - radius * 0.2f)
        {
            AddCylinder(acc, a, b, radius);
            return;
        }

        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = (b - a).normalized;
        float gap = doorWidth * 0.5f + radius;
        AddCylinder(acc, a, mid - dir * gap, radius);
        AddCylinder(acc, mid + dir * gap, b, radius);
    }

    static void AddGableLogs(Acc wood, float z, float halfW, float wallH, float ridgeY, float logR)
    {
        int rows = 5;
        for (int i = 0; i < rows; i++)
        {
            float t = (i + 0.6f) / (rows + 0.4f);
            float y = Mathf.Lerp(wallH + logR, ridgeY - logR * 1.2f, t);
            float roofHalf = Mathf.Lerp(halfW, 0.02f, Mathf.InverseLerp(wallH, ridgeY, y));
            float len = roofHalf * 0.92f;
            if (len < logR * 1.5f)
                continue;
            AddLog(wood, new Vector3(-len, y, z), new Vector3(len, y, z), logR * 0.92f);
        }
    }

    static void AddThatchSide(Acc roof, float halfL, float xEave, float wallH, float ridgeY, float thick)
    {
        Vector3 p0 = new Vector3(xEave, wallH - 0.01f, -halfL);
        Vector3 p1 = new Vector3(xEave, wallH - 0.01f, halfL);
        Vector3 p2 = new Vector3(0f, ridgeY, halfL);
        Vector3 p3 = new Vector3(0f, ridgeY, -halfL);
        Vector3 n = Vector3.Cross(p1 - p0, p3 - p0).normalized;
        if (Vector3.Dot(n, Vector3.up) < 0f)
            n = -n;
        AddThickQuad(roof, p0, p1, p2, p3, n * thick);
    }

    static void AddRoofPole(Acc dark, float halfL, float xSignW, float wallH, float ridgeY, float t, float r)
    {
        float x = Mathf.Lerp(xSignW, 0f, t);
        float y = Mathf.Lerp(wallH + 0.02f, ridgeY + 0.015f, t);
        AddCylinder(dark, new Vector3(x, y, -halfL), new Vector3(x, y, halfL), r, 6);
    }

    static void AddWindboards(Acc dark, float z, float halfW, float wallH, float ridgeY)
    {
        Vector3 peak = new Vector3(0f, ridgeY + 0.05f, z);
        Vector3 left = new Vector3(-halfW * 0.22f, ridgeY - 0.08f, z);
        Vector3 right = new Vector3(halfW * 0.22f, ridgeY - 0.08f, z);
        AddPlank(dark, left, peak, 0.018f, 0.008f);
        AddPlank(dark, right, peak, 0.018f, 0.008f);
    }

    static void AddDoor(Acc dark, float x, float width, float height, bool plusX)
    {
        float nx = plusX ? 1f : -1f;
        float z0 = -width * 0.5f;
        float z1 = width * 0.5f;
        Vector3 a = new Vector3(x + nx * 0.012f, 0.01f, z0);
        Vector3 b = new Vector3(x + nx * 0.012f, height, z0);
        Vector3 c = new Vector3(x + nx * 0.012f, height, z1);
        Vector3 d = new Vector3(x + nx * 0.012f, 0.01f, z1);
        AddThickQuad(dark, a, d, c, b, Vector3.right * nx * 0.012f);
        AddCylinder(dark, new Vector3(x + nx * 0.02f, height * 0.48f, z0 + 0.02f), new Vector3(x + nx * 0.02f, height * 0.48f, z0 + 0.045f), 0.008f, 6);
    }

    static void AddGateLeaf(Acc dark, float xHinge, float z, float height, bool left)
    {
        float swing = left ? -0.35f : 0.35f;
        float x0 = xHinge;
        float x1 = xHinge + swing;
        Vector3 a = new Vector3(x0, 0.01f, z);
        Vector3 b = new Vector3(x0, height, z);
        Vector3 c = new Vector3(x1, height, z + 0.03f);
        Vector3 d = new Vector3(x1, 0.01f, z + 0.03f);
        AddThickQuad(dark, a, d, c, b, Vector3.forward * 0.012f);
    }

    static void AddFirewood(Acc wood, float x, float zCenter, float logR)
    {
        float r = logR * 0.72f;
        for (int row = 0; row < 4; row++)
        {
            int count = 5 - row / 2;
            for (int i = 0; i < count; i++)
            {
                float z = zCenter + (i - (count - 1) * 0.5f) * r * 2.05f;
                float y = r + row * r * 1.75f;
                AddCylinder(wood, new Vector3(x - 0.07f, y, z), new Vector3(x + 0.07f, y, z), r, 6);
            }
        }
    }

    static void AddPlank(Acc acc, Vector3 a, Vector3 b, float width, float thick)
    {
        Vector3 dir = (b - a).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.forward);
        if (side.sqrMagnitude < 0.001f)
            side = Vector3.Cross(dir, Vector3.right);
        side.Normalize();
        Vector3 n = Vector3.Cross(dir, side).normalized;
        Vector3 w = side * (width * 0.5f);
        Vector3 t = n * (thick * 0.5f);
        Vector3 p0 = a - w - t;
        Vector3 p1 = a + w - t;
        Vector3 p2 = b + w - t;
        Vector3 p3 = b - w - t;
        Vector3 p4 = a - w + t;
        Vector3 p5 = a + w + t;
        Vector3 p6 = b + w + t;
        Vector3 p7 = b - w + t;
        AddQuad(acc, p0, p1, p2, p3);
        AddQuad(acc, p5, p4, p7, p6);
        AddQuad(acc, p4, p5, p1, p0);
        AddQuad(acc, p6, p7, p3, p2);
        AddQuad(acc, p4, p0, p3, p7);
        AddQuad(acc, p1, p5, p6, p2);
    }

    static void AddThickQuad(Acc acc, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 extrude)
    {
        AddQuad(acc, p0, p1, p2, p3);
        AddQuad(acc, p0 + extrude, p3 + extrude, p2 + extrude, p1 + extrude);
        AddQuad(acc, p0, p3, p3 + extrude, p0 + extrude);
        AddQuad(acc, p1, p1 + extrude, p2 + extrude, p2);
        AddQuad(acc, p0, p0 + extrude, p1 + extrude, p1);
        AddQuad(acc, p3 + extrude, p3, p2, p2 + extrude);
    }

    static void AddQuad(Acc acc, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 n = Vector3.Cross(p1 - p0, p3 - p0);
        if (n.sqrMagnitude < 1e-10f)
            return;
        n.Normalize();
        int i = acc.v.Count;
        acc.v.Add(p0);
        acc.v.Add(p1);
        acc.v.Add(p2);
        acc.v.Add(p3);
        acc.n.Add(n);
        acc.n.Add(n);
        acc.n.Add(n);
        acc.n.Add(n);
        acc.uv.Add(new Vector2(p0.z * 3f, p0.y * 3f + p0.x * 3f));
        acc.uv.Add(new Vector2(p1.z * 3f, p1.y * 3f + p1.x * 3f));
        acc.uv.Add(new Vector2(p2.z * 3f, p2.y * 3f + p2.x * 3f));
        acc.uv.Add(new Vector2(p3.z * 3f, p3.y * 3f + p3.x * 3f));
        acc.t.Add(i);
        acc.t.Add(i + 1);
        acc.t.Add(i + 2);
        acc.t.Add(i);
        acc.t.Add(i + 2);
        acc.t.Add(i + 3);
    }

    static void AddCylinder(Acc acc, Vector3 a, Vector3 b, float radius, int seg = 8)
    {
        Vector3 axis = b - a;
        float len = axis.magnitude;
        if (len < 1e-5f)
            return;
        axis /= len;
        Vector3 n1 = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.92f ? Vector3.up : Vector3.right).normalized;
        Vector3 n2 = Vector3.Cross(axis, n1);
        int start = acc.v.Count;
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg;
            float ang = u * Mathf.PI * 2f;
            Vector3 r = (n1 * Mathf.Cos(ang) + n2 * Mathf.Sin(ang)) * radius;
            acc.v.Add(a + r);
            acc.v.Add(b + r);
            acc.n.Add(r.normalized);
            acc.n.Add(r.normalized);
            acc.uv.Add(new Vector2(u, 0f));
            acc.uv.Add(new Vector2(u, len / (radius * 5f)));
        }

        for (int i = 0; i < seg; i++)
        {
            int i0 = start + i * 2;
            acc.t.Add(i0);
            acc.t.Add(i0 + 2);
            acc.t.Add(i0 + 1);
            acc.t.Add(i0 + 1);
            acc.t.Add(i0 + 2);
            acc.t.Add(i0 + 3);
        }

        int c0 = acc.v.Count;
        acc.v.Add(a);
        acc.n.Add(-axis);
        acc.uv.Add(new Vector2(0.5f, 0.5f));
        acc.v.Add(b);
        acc.n.Add(axis);
        acc.uv.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i < seg; i++)
        {
            int i0 = start + i * 2;
            acc.t.Add(c0);
            acc.t.Add(i0);
            acc.t.Add(i0 + 2);
            acc.t.Add(c0 + 1);
            acc.t.Add(i0 + 3);
            acc.t.Add(i0 + 1);
        }
    }

    static void AttachMesh(GameObject parent, string name, Mesh mesh, Material mat)
    {
        if (mesh == null || mesh.vertexCount == 0)
            return;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    class Acc
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();

        public Mesh ToMesh(string name)
        {
            if (v.Count == 0)
                return null;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
