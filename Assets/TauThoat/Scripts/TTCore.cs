// Tẩu Thoát Turbo - dữ liệu, vật liệu, gộp mesh, âm thanh, mô hình xe, sinh thành phố
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CarStyle { Bug, Taxi, Van, Pickup, Sport, Muscle, Super, Armor, Cop, CopSuv, Swat }

public class CarDef
{
    public string name; public int price; public float max, acc, turn, hp; public Color col;
    public float len, wid, hgt; public CarStyle sty; public float brk, dmg; public int reward;
}

public class ThemeDef
{
    public string name; public Color outC, road, walk, lotg, park, lot, pond, tree0, tree1, sky, amb, sun;
    public float sunI; public Color[] pal; public bool night, cactus, snow;
}

public static class TT
{
    public static Color H(string h) { Color c; ColorUtility.TryParseHtmlString(h.StartsWith("#") ? h : "#" + h, out c); return c; }
    public static Color Shade(Color c, float f) { return f < 1 ? new Color(c.r * f, c.g * f, c.b * f, 1) : Color.Lerp(c, Color.white, f - 1); }

    static CarDef C(string n, int p, float mx, float ac, float tu, float hp, string col, float l, float w, float ht, CarStyle s, float brk = 0, float dmg = 1, int rew = 0)
    { return new CarDef { name = n, price = p, max = mx, acc = ac, turn = tu, hp = hp, col = H(col), len = l, wid = w, hgt = ht, sty = s, brk = brk, dmg = dmg, reward = rew }; }

    public static readonly CarDef[] Cars = {
        C("Bọ Cam",     0,   43f,   42f, 3.1f,  100, "#ff7a1a", 4.2f, 2.1f,  1.5f, CarStyle.Bug),
        C("Taxi",       80,  44.5f, 41f, 3.0f,  110, "#f4c21a", 4.6f, 2.0f,  1.45f, CarStyle.Taxi),
        C("Xe Thùng",   150, 40.5f, 37f, 2.7f,  170, "#2fb36d", 5.4f, 2.3f,  2.1f, CarStyle.Van),
        C("Bán Tải",    220, 44.5f, 41f, 2.85f, 150, "#8a1f2b", 5.2f, 2.2f,  1.7f, CarStyle.Pickup),
        C("Mũi Tên",    320, 50f,   47f, 3.3f,  85,  "#e5326b", 4.7f, 2.0f,  1.15f, CarStyle.Sport),
        C("Cơ Bắp",     420, 52f,   50f, 2.9f,  130, "#2f9bff", 5.0f, 2.1f,  1.35f, CarStyle.Muscle),
        C("Bóng Ma",    600, 55f,   53f, 3.6f,  115, "#7b5cff", 4.9f, 2.05f, 1.1f, CarStyle.Super),
        C("Thiết Giáp", 900, 47f,   43f, 2.8f,  270, "#4b5d3a", 5.6f, 2.5f,  2.2f, CarStyle.Armor) };

    public static readonly CarDef[] Cops = {
        C("Cảnh sát", 0, 43f,   44f, 3.0f,  1, "#f4f6fb", 4.6f, 2.0f, 1.5f, CarStyle.Cop,    22f, 1f,   2),
        C("SUV",      0, 41.5f, 42f, 2.75f, 1, "#1c2a52", 5.2f, 2.3f, 1.9f, CarStyle.CopSuv, 30f, 1.4f, 3),
        C("SWAT",     0, 39.5f, 39f, 2.45f, 1, "#23262e", 6.2f, 2.6f, 2.4f, CarStyle.Swat,   40f, 2f,   5) };

    public static readonly string[] Paints = { "#ff7a1a", "#e5326b", "#f4c21a", "#2fb36d", "#2f9bff", "#7b5cff", "#eef0f5", "#2a2d36", "#8a1f2b", "#4b5d3a" };

    static ThemeDef Th(string n, string o, string r, string w, string lg, string pk, string lt, string pd, string t0, string t1, string sky, string amb, string sun, float si, string[] pal, bool night = false, bool cactus = false, bool snow = false)
    {
        var p = new Color[pal.Length]; for (int i = 0; i < pal.Length; i++) p[i] = H(pal[i]);
        return new ThemeDef { name = n, outC = H(o), road = H(r), walk = H(w), lotg = H(lg), park = H(pk), lot = H(lt), pond = H(pd), tree0 = H(t0), tree1 = H(t1), sky = H(sky), amb = H(amb), sun = H(sun), sunI = si, pal = p, night = night, cactus = cactus, snow = snow };
    }

    public static readonly ThemeDef[] Themes = {
        Th("Thành phố", "#3d6b3a", "#454a57", "#cdc4b0", "#b5ab95", "#79c066", "#555b69", "#4aa8d8", "#3f9a4a", "#62bd5e", "#9fcdee", "#aab6c8", "#fff4e0", 1.25f,
           new[] { "#f2a65a", "#e86a6a", "#6ab0e8", "#f4d35e", "#9b8fe0", "#5fc9a8", "#f08fb6", "#e8e3d3", "#7fa7c9" }),
        Th("Đêm neon", "#14201a", "#2c303b", "#77716a", "#625c52", "#2f5a35", "#343844", "#1f4d73", "#24502b", "#2f6b37", "#0a0f1e", "#1a2238", "#8aa0ff", 0.18f,
           new[] { "#4a4f86", "#76488a", "#357a8a", "#8a4a5e", "#4f5e7a", "#6a6a8a" }, night: true),
        Th("Sa mạc", "#e3b36f", "#5e554c", "#ecd3a3", "#d9b47a", "#cfa865", "#6a6058", "#3fb5c9", "#4f8a3c", "#7aae4a", "#f2d6a2", "#d8c29c", "#ffe2b0", 1.4f,
           new[] { "#e8a76a", "#d98c5f", "#f2d0a0", "#c97b54", "#e9c08a", "#b8d0d8" }, cactus: true),
        Th("Tuyết", "#e9f1f7", "#5d6470", "#f3f6f9", "#dfe8ef", "#f4f8fb", "#6a717c", "#a8d8ef", "#2f6b55", "#3f8a6b", "#dbe7f1", "#c4d0de", "#eef4ff", 1.1f,
           new[] { "#c94b4b", "#4b6bc9", "#e0a040", "#7a5cc9", "#4bc98a", "#b9b9c4" }, snow: true) };
}

public static class TTMat
{
    static Shader lit;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Material particle, plain;

    public static Shader Lit
    {
        get
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            return lit;
        }
    }

    public static Material Get(Color c, float emit = 0, float smooth = 0.25f, float metal = 0)
    {
        string k = ColorUtility.ToHtmlStringRGB(c) + "_" + emit.ToString("0.00") + "_" + smooth.ToString("0.00") + "_" + metal.ToString("0.00");
        Material m;
        if (cache.TryGetValue(k, out m) && m != null) return m;
        m = New(c, emit, smooth, metal); m.name = "TT_" + k; cache[k] = m; return m;
    }

    public static Material New(Color c, float emit = 0, float smooth = 0.25f, float metal = 0)
    {
        var m = new Material(Lit);
        c.a = 1;
        m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", smooth); m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal);
        if (emit > 0) SetEmit(m, c * emit);
        return m;
    }

    public static void SetEmit(Material m, Color e)
    {
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", e);
    }

    static Shader SpriteShader()
    {
        var s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        return s;
    }

    public static Material Particle()
    {
        if (particle != null) return particle;
        particle = new Material(SpriteShader());
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                float a = Mathf.Pow(Mathf.Clamp01(1 - d), 1.4f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        particle.mainTexture = tex;
        return particle;
    }

    public static Material Plain()
    {
        if (plain == null) plain = new Material(SpriteShader());
        return plain;
    }
}

public static class TTMesh
{
    static readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();
    public static Mesh Get(PrimitiveType t)
    {
        Mesh m;
        if (meshes.TryGetValue(t, out m) && m != null) return m;
        var g = GameObject.CreatePrimitive(t);
        m = g.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(g);
        meshes[t] = m;
        return m;
    }
}

/// Gộp hàng nghìn khối tĩnh thành vài mesh lớn để chạy mượt.
public class TTBatch
{
    readonly Dictionary<Material, List<CombineInstance>> lists = new Dictionary<Material, List<CombineInstance>>();

    public void Add(Mesh mesh, Matrix4x4 m, Material mat)
    {
        List<CombineInstance> l;
        if (!lists.TryGetValue(mat, out l)) { l = new List<CombineInstance>(); lists[mat] = l; }
        l.Add(new CombineInstance { mesh = mesh, transform = m });
    }

    public void Add(PrimitiveType t, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default(Vector3))
    {
        Add(TTMesh.Get(t), Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale), mat);
    }

    public void AddObject(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var r = mf.GetComponent<MeshRenderer>();
            if (r == null || mf.sharedMesh == null) continue;
            Add(mf.sharedMesh, mf.transform.localToWorldMatrix, r.sharedMaterial);
        }
    }

    public void Build(Transform parent, bool shadows)
    {
        foreach (var kv in lists)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i += 3000)
            {
                var arr = list.GetRange(i, Mathf.Min(3000, list.Count - i)).ToArray();
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(arr, true, true);
                mesh.RecalculateBounds();
                var g = new GameObject("Batch_" + kv.Key.name);
                g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key;
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
        lists.Clear();
    }
}

public static class TTAudio
{
    const int SR = 22050;
    static AudioClip Make(string n, float[] d) { var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); return c; }

    public static AudioClip Engine()
    {
        var d = new float[SR];
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SR;
            d[i] = (Mathf.Repeat(t * 55f, 1f) * 2 - 1) * 0.35f + Mathf.Sin(2 * Mathf.PI * 110f * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * 27.5f * t) * 0.25f;
        }
        return Make("engine", d);
    }

    public static AudioClip Siren()
    {
        int n = (int)(SR * 1.2f); var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR; float f = t < 0.6f ? 620f : 880f;
            float ph = Mathf.Repeat(t * f, 1f);
            d[i] = (Mathf.Abs(ph * 4 - 2) - 1) * 0.5f;
        }
        return Make("siren", d);
    }

    public static AudioClip Noise(float dur, float cutoff)
    {
        int n = (int)(SR * dur); var d = new float[n]; float y = 0, a = Mathf.Clamp01(cutoff / SR * 6.28f);
        for (int i = 0; i < n; i++)
        {
            float x = Random.Range(-1f, 1f) * Mathf.Pow(1 - i / (float)n, 2.2f);
            y += a * (x - y); d[i] = y * 2.2f;
        }
        return Make("noise", d);
    }

    public static AudioClip Blip()
    {
        int n = (int)(SR * 0.1f); var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR; float f = 880f + t * 5000f;
            d[i] = (Mathf.Repeat(t * f, 1f) < 0.5f ? 0.3f : -0.3f) * (1 - i / (float)n);
        }
        return Make("blip", d);
    }
}

public static class TTInput
{
    /// Bàn phím: trái/phải, phanh trượt (Space), lùi (↓/S), tạm dừng (P/Esc).
    public static void Keys(out bool l, out bool r, out bool hb, out bool back, out bool pause)
    {
        l = r = hb = back = pause = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        l = k.leftArrowKey.isPressed || k.aKey.isPressed;
        r = k.rightArrowKey.isPressed || k.dKey.isPressed;
        hb = k.spaceKey.isPressed;
        back = k.downArrowKey.isPressed || k.sKey.isPressed;
        pause = k.pKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame;
#else
        l = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        r = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        hb = Input.GetKey(KeyCode.Space);
        back = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        pause = Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// Chuột: vị trí con trỏ (gốc dưới-trái), nút trái, nút phải.
    public static bool Mouse(out Vector2 pos, out bool left, out bool right)
    {
        pos = Vector2.zero; left = right = false;
#if ENABLE_INPUT_SYSTEM
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null) return false;
        pos = m.position.ReadValue(); left = m.leftButton.isPressed; right = m.rightButton.isPressed;
        return true;
#else
        pos = Input.mousePosition; left = Input.GetMouseButton(0); right = Input.GetMouseButton(1);
        return true;
#endif
    }
}

public class CarParts
{
    public Material tail; public Renderer barA, barB; public Light flash; public Transform model; public TrailRenderer[] trails;
}

public static class TTCarBuilder
{
    static Material M(string h, float e = 0, float s = 0.3f, float m = 0) { return TTMat.Get(TT.H(h), e, s, m); }

    public static GameObject Part(PrimitiveType t, Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 rot = default(Vector3))
    {
        var g = GameObject.CreatePrimitive(t);
        var col = g.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localScale = sc; g.transform.localEulerAngles = rot;
        var r = g.GetComponent<Renderer>(); r.sharedMaterial = m;
        return g;
    }

    public static GameObject Build(CarDef d, Color col, Transform parent, out CarParts P, bool parked)
    {
        var root = new GameObject(d.name);
        root.transform.SetParent(parent, false);
        P = new CarParts();
        var mdl = new GameObject("Model").transform; mdl.SetParent(root.transform, false); P.model = mdl;
        float L = d.len, W = d.wid;
        const PrimitiveType Cu = PrimitiveType.Cube, Cy = PrimitiveType.Cylinder, Sp = PrimitiveType.Sphere;
        var body = TTMat.Get(col, 0, 0.75f, 0.3f);
        var bodyL = TTMat.Get(TT.Shade(col, 1.18f), 0, 0.75f, 0.3f);
        var bodyD = TTMat.Get(TT.Shade(col, 0.6f), 0, 0.5f, 0.2f);
        var glass = M("#1b2433", 0, 0.95f, 0.4f);
        var tire = M("#141414", 0, 0.1f);
        var rim = M("#a5abb5", 0, 0.7f, 0.7f);
        var black = M("#16181d", 0, 0.3f);
        var white = M("#eef1f6", 0, 0.6f);
        var navy = M("#1f3c9e", 0, 0.6f);
        var head = M("#fff6c8", 3f);
        P.tail = parked ? M("#b01010", 0.5f) : TTMat.New(TT.H("#b01010"), 1f);

        foreach (float z in new[] { L * 0.31f, -L * 0.31f })
            foreach (float x in new[] { -1f, 1f })
            {
                Part(Cy, mdl, new Vector3(x * (W / 2 - 0.15f), 0.36f, z), new Vector3(0.72f, 0.14f, 0.72f), tire, new Vector3(0, 0, 90));
                Part(Cy, mdl, new Vector3(x * (W / 2 - 0.01f), 0.36f, z), new Vector3(0.4f, 0.02f, 0.4f), rim, new Vector3(0, 0, 90));
            }

        float bb = 0.3f, roofY = 1.4f, lightY = 0.62f;
        bool bar = false, bigBar = false;
        switch (d.sty)
        {
            case CarStyle.Bug:
                Part(Cu, mdl, new Vector3(0, bb + 0.25f, 0), new Vector3(W * 0.92f, 0.5f, L * 0.95f), body);
                foreach (float z in new[] { L * 0.31f, -L * 0.31f })
                    foreach (float x in new[] { -1f, 1f })
                        Part(Sp, mdl, new Vector3(x * (W / 2 - 0.2f), 0.62f, z), new Vector3(0.62f, 0.55f, 1.05f), body);
                Part(Sp, mdl, new Vector3(0, bb + 0.55f, -L * 0.05f), new Vector3(W * 0.88f, 0.95f, L * 0.62f), glass);
                Part(Sp, mdl, new Vector3(0, bb + 0.68f, -L * 0.08f), new Vector3(W * 0.8f, 0.8f, L * 0.45f), bodyL);
                roofY = bb + 1.08f; lightY = bb + 0.35f; break;
            case CarStyle.Van:
                Part(Cu, mdl, new Vector3(0, bb + 0.8f, 0), new Vector3(W, 1.6f, L), body);
                Part(Cu, mdl, new Vector3(0, bb + 1.15f, L / 2 - 0.02f), new Vector3(W * 0.86f, 0.6f, 0.08f), glass);
                Part(Cu, mdl, new Vector3(0, bb + 1.15f, L * 0.28f), new Vector3(W + 0.02f, 0.5f, L * 0.3f), glass);
                for (int q = 0; q < 4; q++) Part(Cu, mdl, new Vector3(0, bb + 1.62f, -L * 0.3f + q * L * 0.18f), new Vector3(W * 0.9f, 0.06f, 0.2f), bodyL);
                roofY = bb + 1.65f; lightY = bb + 0.45f; break;
            case CarStyle.Pickup:
                Part(Cu, mdl, new Vector3(0, bb + 0.3f, 0), new Vector3(W, 0.6f, L), body);
                Part(Cu, mdl, new Vector3(0, bb + 0.85f, L * 0.06f), new Vector3(W * 0.88f, 0.55f, L * 0.32f), glass);
                Part(Cu, mdl, new Vector3(0, bb + 1.15f, L * 0.05f), new Vector3(W * 0.86f, 0.07f, L * 0.28f), body);
                Part(Cu, mdl, new Vector3(0, bb + 0.62f, -L * 0.27f), new Vector3(W * 0.84f, 0.05f, L * 0.4f), bodyD);
                Part(Cu, mdl, new Vector3(-(W / 2 - 0.06f), bb + 0.78f, -L * 0.27f), new Vector3(0.12f, 0.35f, L * 0.42f), body);
                Part(Cu, mdl, new Vector3(W / 2 - 0.06f, bb + 0.78f, -L * 0.27f), new Vector3(0.12f, 0.35f, L * 0.42f), body);
                Part(Cu, mdl, new Vector3(0, bb + 0.78f, -L / 2 + 0.06f), new Vector3(W, 0.35f, 0.12f), body);
                roofY = bb + 1.2f; lightY = bb + 0.38f; break;
            case CarStyle.Sport:
                Part(Cu, mdl, new Vector3(0, bb + 0.21f, 0), new Vector3(W, 0.42f, L), body);
                Part(Cu, mdl, new Vector3(0, bb + 0.43f, L * 0.28f), new Vector3(W * 0.9f, 0.04f, L * 0.36f), bodyL);
                Part(Cu, mdl, new Vector3(0, bb + 0.6f, -L * 0.08f), new Vector3(W * 0.74f, 0.38f, L * 0.36f), glass);
                Part(Cu, mdl, new Vector3(-0.35f, bb + 0.79f, -L * 0.08f), new Vector3(0.05f, 0.01f, L * 0.36f), white);
                Part(Cu, mdl, new Vector3(0, bb + 0.75f, -L / 2 + 0.25f), new Vector3(W * 0.95f, 0.06f, 0.42f), black);
                Part(Cu, mdl, new Vector3(-W * 0.3f, bb + 0.55f, -L / 2 + 0.3f), new Vector3(0.08f, 0.3f, 0.12f), black);
                Part(Cu, mdl, new Vector3(W * 0.3f, bb + 0.55f, -L / 2 + 0.3f), new Vector3(0.08f, 0.3f, 0.12f), black);
                Part(Cu, mdl, new Vector3(0, bb + 0.2f, 0), new Vector3(W + 0.02f, 0.06f, L * 0.7f), white);
                roofY = bb + 0.8f; lightY = bb + 0.25f; break;
            case CarStyle.Super:
                Part(Cu, mdl, new Vector3(0, bb + 0.18f, 0), new Vector3(W, 0.36f, L), body);
                Part(Cu, mdl, new Vector3(0, bb + 0.3f, L * 0.3f), new Vector3(W * 0.85f, 0.2f, L * 0.36f), bodyL, new Vector3(-6, 0, 0));
                Part(Sp, mdl, new Vector3(0, bb + 0.36f, -L * 0.06f), new Vector3(W * 0.74f, 0.62f, L * 0.5f), glass);
                Part(Cu, mdl, new Vector3(-(W / 2), bb + 0.22f, -L * 0.15f), new Vector3(0.05f, 0.2f, L * 0.25f), black);
                Part(Cu, mdl, new Vector3(W / 2, bb + 0.22f, -L * 0.15f), new Vector3(0.05f, 0.2f, L * 0.25f), black);
                Part(Cu, mdl, new Vector3(0, bb + 0.12f, -L / 2), new Vector3(W * 0.8f, 0.2f, 0.1f), black);
                roofY = bb + 0.7f; lightY = bb + 0.24f; break;
            case CarStyle.Armor:
            case CarStyle.Swat:
                Part(Cu, mdl, new Vector3(0, bb + 0.5f, 0), new Vector3(W, 1.0f, L * 0.98f), body);
                Part(Cu, mdl, new Vector3(0, bb + 1.35f, -L * 0.08f), new Vector3(W * 0.92f, 0.7f, L * 0.62f), bodyD);
                Part(Cu, mdl, new Vector3(0, bb + 1.4f, L * 0.23f + 0.01f), new Vector3(W * 0.8f, 0.25f, 0.06f), glass);
                Part(Cu, mdl, new Vector3(-(W / 2 + 0.01f), bb + 1.4f, -L * 0.08f), new Vector3(0.04f, 0.22f, L * 0.4f), glass);
                Part(Cu, mdl, new Vector3(W / 2 + 0.01f, bb + 1.4f, -L * 0.08f), new Vector3(0.04f, 0.22f, L * 0.4f), glass);
                Part(Cu, mdl, new Vector3(0, bb + 0.55f, L / 2 - 0.02f), new Vector3(W * 0.7f, 0.3f, 0.1f), black);
                if (d.sty == CarStyle.Swat) { Part(Cu, mdl, new Vector3(0, bb + 0.7f, 0), new Vector3(W + 0.02f, 0.16f, L * 0.9f), white); bar = true; bigBar = true; }
                roofY = bb + 1.7f; lightY = bb + 0.6f; break;
            default: // Taxi, Muscle, Cop, CopSuv
                {
                    bool suv = d.sty == CarStyle.CopSuv;
                    float bh = suv ? 0.8f : 0.55f, ch = suv ? 0.55f : 0.48f;
                    Part(Cu, mdl, new Vector3(0, bb + bh / 2, 0), new Vector3(W, bh, L), body);
                    Part(Cu, mdl, new Vector3(0, bb + bh + ch / 2, -L * 0.05f), new Vector3(W * 0.86f, ch, L * (suv ? 0.6f : 0.46f)), glass);
                    Material roofM = d.sty == CarStyle.Cop ? white : body;
                    Part(Cu, mdl, new Vector3(0, bb + bh + ch + 0.035f, -L * 0.06f), new Vector3(W * 0.84f, 0.07f, L * (suv ? 0.56f : 0.38f)), roofM);
                    roofY = bb + bh + ch + 0.07f; lightY = bb + bh * 0.6f;
                    if (d.sty == CarStyle.Cop)
                    {
                        Part(Cu, mdl, new Vector3(0, bb + bh / 2, L * 0.38f), new Vector3(W + 0.01f, bh + 0.01f, L * 0.24f), navy);
                        Part(Cu, mdl, new Vector3(0, bb + bh / 2, -L * 0.4f), new Vector3(W + 0.01f, bh + 0.01f, L * 0.2f), navy);
                        bar = true;
                    }
                    else if (suv) { Part(Cu, mdl, new Vector3(0, bb + bh * 0.55f, 0), new Vector3(W + 0.02f, bh * 0.4f, L * 0.4f), white); bar = true; bigBar = true; }
                    else if (d.sty == CarStyle.Taxi)
                    {
                        Part(Cu, mdl, new Vector3(0, roofY + 0.12f, -L * 0.06f), new Vector3(W * 0.42f, 0.22f, 0.4f), M("#ffffff", 0.8f));
                        for (int q = 0; q < 6; q++)
                            foreach (float x in new[] { -1f, 1f })
                                Part(Cu, mdl, new Vector3(x * (W / 2 + 0.005f), bb + bh * 0.7f, -L * 0.25f + q * 0.36f), new Vector3(0.02f, 0.14f, 0.18f), black);
                    }
                    else if (d.sty == CarStyle.Muscle)
                    {
                        foreach (float x in new[] { -0.22f, 0.22f })
                        {
                            Part(Cu, mdl, new Vector3(x, bb + bh + 0.005f, 0), new Vector3(0.2f, 0.02f, L), white);
                            Part(Cu, mdl, new Vector3(x, roofY + 0.005f, -L * 0.06f), new Vector3(0.2f, 0.02f, L * 0.38f), white);
                        }
                        Part(Cu, mdl, new Vector3(0, bb + bh + 0.08f, L * 0.25f), new Vector3(0.5f, 0.16f, 0.7f), black);
                    }
                }
                break;
        }

        foreach (float x in new[] { -1f, 1f })
        {
            Part(Cu, mdl, new Vector3(x * W * 0.32f, lightY, L / 2), new Vector3(0.42f, 0.16f, 0.06f), head);
            var t = Part(Cu, mdl, new Vector3(x * W * 0.34f, lightY, -L / 2), new Vector3(0.4f, 0.14f, 0.06f), P.tail);
            Part(Cu, mdl, new Vector3(x * (W / 2 + 0.08f), lightY + 0.35f, L * 0.12f), new Vector3(0.16f, 0.1f, 0.1f), bodyD);
        }

        if (bar)
        {
            float bw = bigBar ? W * 0.75f : W * 0.6f;
            Part(Cu, mdl, new Vector3(0, roofY + 0.05f, -L * 0.04f), new Vector3(bw, 0.1f, 0.38f), black);
            P.barA = Part(Cu, mdl, new Vector3(-bw * 0.26f, roofY + 0.14f, -L * 0.04f), new Vector3(bw * 0.46f, 0.14f, 0.32f), RedOn).GetComponent<Renderer>();
            P.barB = Part(Cu, mdl, new Vector3(bw * 0.26f, roofY + 0.14f, -L * 0.04f), new Vector3(bw * 0.46f, 0.14f, 0.32f), BlueOff).GetComponent<Renderer>();
            if (!parked)
            {
                var lg = new GameObject("Flash"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = new Vector3(0, roofY + 0.8f, 0);
                P.flash = lg.AddComponent<Light>(); P.flash.type = LightType.Point; P.flash.range = 11; P.flash.intensity = 3; P.flash.shadows = LightShadows.None;
            }
        }
        return root;
    }

    public static Material RedOn { get { return TTMat.Get(TT.H("#ff2d4a"), 5f); } }
    public static Material RedOff { get { return TTMat.Get(TT.H("#5a0d16"), 0.3f); } }
    public static Material BlueOn { get { return TTMat.Get(TT.H("#3a6bff"), 5f); } }
    public static Material BlueOff { get { return TTMat.Get(TT.H("#101d4a"), 0.3f); } }
}

/// Sinh thành phố dạng lưới: đường, vỉa hè, nhà, công viên, bãi đỗ xe, đèn đường.
public class TTCity
{
    public const float CELL = 44f, ROAD = 16f; public const int N = 11;
    public static float W { get { return CELL * N; } }
    public static float B0 { get { return -ROAD / 2 + 1.2f; } }
    public static float B1 { get { return W + ROAD / 2 - 1.2f; } }
    public GameObject root;
    public readonly List<Vector3> lamps = new List<Vector3>();
    Transform cols;
    ThemeDef th;
    const PrimitiveType Cu = PrimitiveType.Cube, Cy = PrimitiveType.Cylinder, Sp = PrimitiveType.Sphere, Ca = PrimitiveType.Capsule;

    static Material M(Color c, float e = 0, float s = 0.2f) { return TTMat.Get(c, e, s); }
    static float R(float a, float b) { return Random.Range(a, b); }

    /// Collider của mỗi tòa nhà -> các renderer của nó (để ẩn khi che xe).
    public class Occ { public Bounds b; public Renderer[] rs; }
    public readonly List<Occ> occ = new List<Occ>();

    BoxCollider Col(Vector3 pos, Vector3 size, float yaw = 0)
    {
        var g = new GameObject("C"); g.transform.SetParent(cols, false);
        g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var b = g.AddComponent<BoxCollider>(); b.size = size; g.isStatic = true;
        return b;
    }

    void Bld(TTBatch outer, float x, float z, float w, float d, bool small)
    {
        var bb = new TTBatch();
        BldInner(bb, x, z, w, d, small);
        var holder = new GameObject("Bld"); holder.transform.SetParent(root.transform, false);
        bb.Build(holder.transform, true);
        var rs = holder.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var bounds = rs[0].bounds;
        foreach (var r in rs) bounds.Encapsulate(r.bounds);
        occ.Add(new Occ { b = bounds, rs = rs });
    }

    public void Build(ThemeDef theme)
    {
        th = theme;
        root = new GameObject("City");
        cols = new GameObject("Colliders").transform; cols.SetParent(root.transform, false);
        var lit = new TTBatch(); var flat = new TTBatch();
        float b0 = B0, b1 = B1, span = b1 - b0, mid = (b0 + b1) / 2;
        flat.Add(Cu, new Vector3(mid, -0.6f, mid), new Vector3(span + 500, 1, span + 500), M(th.outC));
        flat.Add(Cu, new Vector3(mid, -0.05f, mid), new Vector3(span, 0.1f, span), M(th.road, 0, 0.35f));

        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                float x = i * CELL + ROAD / 2, z = j * CELL + ROAD / 2, s = CELL - ROAD;
                var c = new Vector3(x + s / 2, 0, z + s / 2);
                flat.Add(Cu, new Vector3(c.x, 0.1f, c.z), new Vector3(s + 0.6f, 0.2f, s + 0.6f), M(TT.Shade(th.walk, 0.8f)));
                flat.Add(Cu, new Vector3(c.x, 0.12f, c.z), new Vector3(s, 0.24f, s), M(th.walk));
                float r = Random.value; bool center = (i == 5 || i == 6) && (j == 5 || j == 6);
                string type = center || r > 0.86f ? "lot" : r < 0.70f ? "b" : "park";
                Color inner = type == "park" ? th.park : type == "lot" ? th.lot : th.lotg;
                flat.Add(Cu, new Vector3(c.x, 0.13f, c.z), new Vector3(s - 1.8f, 0.26f, s - 1.8f), M(inner));
                float m = 3.0f, g = 6.4f;
                if (type == "b")
                {
                    if (r < 0.42f) Bld(lit, x + m, z + m, s - 2 * m, s - 2 * m, false);
                    else if (Random.value < 0.5f)
                    { float w = (s - 2 * m - g) / 2; Bld(lit, x + m, z + m, w, s - 2 * m, true); Bld(lit, x + m + w + g, z + m, w, s - 2 * m, true); }
                    else
                    { float h = (s - 2 * m - g) / 2; Bld(lit, x + m, z + m, s - 2 * m, h, true); Bld(lit, x + m, z + m + h + g, s - 2 * m, h, true); }
                }
                else if (type == "park") Park(lit, flat, x, z, s, c);
                else Lot(lit, flat, x, z, s);

                for (int u = 0; u < 3; u++)
                    for (int v = 0; v < 3; v++)
                    {
                        if (u == 1 && v == 1) continue;
                        Lamp(lit, new Vector3(x + 0.4f + u * (s - 0.8f) / 2, 0, z + 0.4f + v * (s - 0.8f) / 2));
                    }
            }

        Markings(flat);
        Border(lit, b0, b1, span, mid);
        lit.Build(root.transform, true);
        flat.Build(root.transform, false);
    }

    BoxCollider BldInner(TTBatch lit, float x, float z, float w, float d, bool small)
    {
        Color c = th.pal[Random.Range(0, th.pal.Length)];
        bool house = small && Random.value < 0.5f;
        float h = house ? R(4f, 5.5f) : R(5f, 8.5f), y0 = 0.26f;
        bool office = !house && h > 7.3f;
        var ctr = new Vector3(x + w / 2, y0 + h / 2, z + d / 2);
        Color wall = office ? Color.Lerp(c, TT.H("#7fb4d9"), 0.55f) : c;
        lit.Add(Cu, ctr, new Vector3(w, h, d), M(wall, 0, office ? 0.8f : 0.15f));
        var bcol = Col(ctr, new Vector3(w, h, d));
        if (house)
        {
            Color rc = th.snow ? TT.H("#f4f8fb") : TT.Shade(c, 0.55f);
            // mái dốc thấp (cao ~1/4 bề ngang) để không che đường
            var flat = Matrix4x4.TRS(new Vector3(ctr.x, y0 + h, ctr.z), Quaternion.identity, new Vector3(1, 0.5f, 1));
            if (w >= d) lit.Add(TTMesh.Get(Cu), flat * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(45, 0, 0), new Vector3(w + 0.5f, d / 1.414f + 0.3f, d / 1.414f + 0.3f)), M(rc));
            else lit.Add(TTMesh.Get(Cu), flat * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 45), new Vector3(w / 1.414f + 0.3f, w / 1.414f + 0.3f, d + 0.5f)), M(rc));
            lit.Add(Cu, new Vector3(ctr.x + w * 0.25f, y0 + h + 0.8f, ctr.z + d * 0.15f), new Vector3(0.7f, 1.4f, 0.7f), M(TT.H("#7a4a3a")));
            Material wn = th.night ? M(TT.H("#ffd36e"), 1.6f) : M(TT.H("#26324a"), 0, 0.85f);
            lit.Add(Cu, new Vector3(ctr.x, y0 + h * 0.55f, ctr.z), new Vector3(w + 0.08f, 1.0f, d * 0.4f), wn);
            lit.Add(Cu, new Vector3(ctr.x, y0 + h * 0.55f, ctr.z), new Vector3(w * 0.4f, 1.0f, d + 0.08f), wn);
            return bcol;
        }
        Material win = th.night ? M(TT.H("#ffd36e"), 1.6f) : M(TT.H("#26324a"), 0, 0.85f);
        float step = office ? 2.2f : 2.8f;
        for (float yy = y0 + 1.8f; yy < y0 + h - 1.0f; yy += step)
            lit.Add(Cu, new Vector3(ctr.x, yy, ctr.z), new Vector3(w + 0.1f, office ? 1.3f : 1.0f, d + 0.1f), win);
        lit.Add(Cu, new Vector3(ctr.x, y0 + h + 0.2f, ctr.z), new Vector3(w, 0.4f, d), M(TT.Shade(c, 1.2f)));
        lit.Add(Cu, new Vector3(ctr.x, y0 + h + 0.12f, ctr.z), new Vector3(w - 0.8f, 0.42f, d - 0.8f), M(th.snow ? TT.H("#f4f8fb") : TT.Shade(c, 0.8f)));
        float rt = y0 + h + 0.33f, mn = Mathf.Min(w, d);
        int k = Random.Range(0, 5);
        if (k == 0)
        {
            lit.Add(Cu, new Vector3(ctr.x, rt + 0.05f, ctr.z), new Vector3(w * 0.62f, 0.1f, d * 0.48f), M(TT.H("#e9e3d4")));
            lit.Add(Cu, new Vector3(ctr.x, rt + 0.07f, ctr.z), new Vector3(w * 0.52f, 0.12f, d * 0.38f), M(th.snow ? TT.H("#bfe3f2") : TT.H("#3fb7e8"), 0.25f, 0.95f));
        }
        else if (k == 1)
        {
            lit.Add(Cy, new Vector3(ctr.x, rt + 0.03f, ctr.z), new Vector3(mn * 0.66f, 0.03f, mn * 0.66f), M(TT.H("#ffd23f")));
            lit.Add(Cy, new Vector3(ctr.x, rt + 0.06f, ctr.z), new Vector3(mn * 0.6f, 0.03f, mn * 0.6f), M(TT.H("#3a3f4b")));
            float hh = mn * 0.16f;
            lit.Add(Cu, new Vector3(ctr.x - hh * 0.8f, rt + 0.1f, ctr.z), new Vector3(hh * 0.35f, 0.03f, hh * 2f), M(Color.white));
            lit.Add(Cu, new Vector3(ctr.x + hh * 0.8f, rt + 0.1f, ctr.z), new Vector3(hh * 0.35f, 0.03f, hh * 2f), M(Color.white));
            lit.Add(Cu, new Vector3(ctr.x, rt + 0.1f, ctr.z), new Vector3(hh * 1.9f, 0.03f, hh * 0.35f), M(Color.white));
        }
        else if (k == 2)
        {
            // pin mặt trời thấp
            for (int q = -1; q <= 1; q++)
                lit.Add(Cu, new Vector3(ctr.x + q * w * 0.22f, rt + 0.25f, ctr.z), new Vector3(w * 0.18f, 0.12f, d * 0.55f), M(TT.H("#1f3a6b"), 0, 0.9f), new Vector3(0, 0, 12));
        }
        else
        {
            Color ac = th.snow ? TT.H("#e8eef4") : TT.H("#b9bec7");
            lit.Add(Cu, new Vector3(ctr.x - w * 0.18f, rt + 0.5f, ctr.z), new Vector3(1.8f, 1f, 1.4f), M(ac));
            lit.Add(Cu, new Vector3(ctr.x + w * 0.15f, rt + 0.4f, ctr.z + d * 0.12f), new Vector3(1.2f, 0.8f, 1.2f), M(ac));
            lit.Add(Cy, new Vector3(ctr.x - w * 0.18f, rt + 1.02f, ctr.z), new Vector3(0.9f, 0.02f, 0.9f), M(TT.H("#3b3f48")));
        }
        return bcol;
    }

    void Park(TTBatch lit, TTBatch flat, float x, float z, float s, Vector3 c)
    {
        Color path = TT.Shade(th.walk, 0.95f);
        flat.Add(Cu, new Vector3(c.x, 0.27f, c.z), new Vector3(2.4f, 0.03f, s - 1.8f), M(path));
        flat.Add(Cu, new Vector3(c.x, 0.27f, c.z), new Vector3(s - 1.8f, 0.03f, 2.4f), M(path));
        bool pond = Random.value < 0.65f; Vector3 pc = Vector3.zero; float rx = 0, rz = 0;
        if (pond)
        {
            pc = new Vector3(c.x + R(-3, 3), 0, c.z + R(-3, 3)); rx = R(5.5f, 8.5f); rz = R(4f, 6f);
            flat.Add(Cy, new Vector3(pc.x, 0.285f, pc.z), new Vector3(rx * 2 + 1.2f, 0.02f, rz * 2 + 1.2f), M(TT.Shade(th.pond, 0.75f)));
            flat.Add(Cy, new Vector3(pc.x, 0.3f, pc.z), new Vector3(rx * 2, 0.02f, rz * 2), M(th.pond, th.night ? 0.2f : 0, 0.95f));
        }
        int n = 0, tries = 0;
        while (n < 8 && tries++ < 60)
        {
            float tx = R(x + 3.6f, x + s - 3.6f), tz = R(z + 3.6f, z + s - 3.6f), tr = R(1.5f, 2.3f);
            if (pond && Mathf.Pow((tx - pc.x) / (rx + tr), 2) + Mathf.Pow((tz - pc.z) / (rz + tr), 2) < 1) continue;
            if (Mathf.Abs(tx - c.x) < 2.4f || Mathf.Abs(tz - c.z) < 2.4f) continue;
            Tree(lit, tx, tz, tr); n++;
        }
    }

    void Tree(TTBatch lit, float x, float z, float r)
    {
        if (th.cactus)
        {
            lit.Add(Ca, new Vector3(x, 1.8f, z), new Vector3(0.75f, 1.6f, 0.75f), M(th.tree0));
            lit.Add(Ca, new Vector3(x + 0.7f, 2.2f, z), new Vector3(0.42f, 0.6f, 0.42f), M(th.tree1));
            lit.Add(Ca, new Vector3(x - 0.6f, 1.7f, z + 0.1f), new Vector3(0.38f, 0.5f, 0.38f), M(th.tree1));
        }
        else
        {
            lit.Add(Cy, new Vector3(x, 1.3f, z), new Vector3(0.35f, 1.1f, 0.35f), M(TT.H("#6b4a2e")));
            lit.Add(Sp, new Vector3(x, 2.6f + r * 0.7f, z), new Vector3(r * 2, r * 1.8f, r * 2), M(th.tree0));
            lit.Add(Sp, new Vector3(x - r * 0.3f, 3f + r * 0.9f, z - r * 0.3f), new Vector3(r * 1.2f, r * 1.1f, r * 1.2f), M(th.tree1));
            if (th.snow) lit.Add(Sp, new Vector3(x, 3.1f + r * 1.25f, z), new Vector3(r * 1.3f, r * 0.6f, r * 1.3f), M(TT.H("#f6fafd")));
        }
        Col(new Vector3(x, 2f, z), new Vector3(1.3f, 4f, 1.3f));
    }

    void Lot(TTBatch lit, TTBatch flat, float x, float z, float s)
    {
        var lineM = M(new Color(1, 1, 1, 1) * 0.92f);
        for (int q = 1; q < 7; q++)
        {
            float xx = x + 0.9f + q * (s - 1.8f) / 7;
            flat.Add(Cu, new Vector3(xx, 0.275f, z + 5.2f), new Vector3(0.15f, 0.02f, 6.8f), lineM);
            flat.Add(Cu, new Vector3(xx, 0.275f, z + s - 5.2f), new Vector3(0.15f, 0.02f, 6.8f), lineM);
        }
        for (int q = 0; q < 7; q++)
            for (int top = 0; top < 2; top++)
            {
                if (Random.value > 0.45f) continue;
                var def = TT.Cars[Random.Range(0, TT.Cars.Length)];
                Color col = TT.H(TT.Paints[Random.Range(0, TT.Paints.Length)]);
                var pos = new Vector3(x + 0.9f + (q + 0.5f) * (s - 1.8f) / 7, 0.26f, top == 1 ? z + 5.2f : z + s - 5.2f);
                float yaw = (top == 1 ? 180f : 0f) + R(-5, 5);
                CarParts parts;
                var car = TTCarBuilder.Build(def, col, root.transform, out parts, true);
                car.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                lit.AddObject(car);
                Object.Destroy(car);
                Col(pos + Vector3.up * 0.8f, new Vector3(def.wid, 1.6f, def.len), yaw);
            }
    }

    void Lamp(TTBatch lit, Vector3 p)
    {
        lit.Add(Cy, new Vector3(p.x, 2.64f, p.z), new Vector3(0.16f, 2.4f, 0.16f), M(TT.H("#2b2d33"), 0, 0.5f));
        lit.Add(Cu, new Vector3(p.x, 5.08f, p.z), new Vector3(0.55f, 0.18f, 0.55f), M(TT.H("#2b2d33")));
        lit.Add(Cu, new Vector3(p.x, 4.98f, p.z), new Vector3(0.42f, 0.04f, 0.42f), M(TT.H("#fff1b8"), th.night ? 4f : 0.5f));
        lamps.Add(new Vector3(p.x, 4.6f, p.z));
    }

    void Markings(TTBatch flat)
    {
        var mm = M(th.snow ? TT.H("#f0f4f8") : TT.H("#f2f2ee"), th.night ? 0.25f : 0f);
        for (int q = 0; q <= N; q++)
            for (int s = 0; s < N; s++)
            {
                float v = q * CELL, a = s * CELL + ROAD / 2 + 3f, e = (s + 1) * CELL - ROAD / 2 - 3f;
                for (float t = a; t + 3 <= e; t += 5.8f)
                {
                    flat.Add(Cu, new Vector3(v, 0.01f, t + 1.5f), new Vector3(0.28f, 0.02f, 3f), mm);
                    flat.Add(Cu, new Vector3(t + 1.5f, 0.01f, v), new Vector3(3f, 0.02f, 0.28f), mm);
                }
            }
        for (int q = 0; q <= N; q++)
            for (int s = 0; s <= N; s++)
            {
                float X = q * CELL, Z = s * CELL;
                for (float o = -6.4f; o <= 5.3f; o += 1.8f)
                {
                    if (Z > 0) flat.Add(Cu, new Vector3(X + o + 0.55f, 0.012f, Z - ROAD / 2 - 1.5f), new Vector3(1.1f, 0.02f, 2.2f), mm);
                    if (Z < W) flat.Add(Cu, new Vector3(X + o + 0.55f, 0.012f, Z + ROAD / 2 + 1.5f), new Vector3(1.1f, 0.02f, 2.2f), mm);
                    if (X > 0) flat.Add(Cu, new Vector3(X - ROAD / 2 - 1.5f, 0.012f, Z + o + 0.55f), new Vector3(2.2f, 0.02f, 1.1f), mm);
                    if (X < W) flat.Add(Cu, new Vector3(X + ROAD / 2 + 1.5f, 0.012f, Z + o + 0.55f), new Vector3(2.2f, 0.02f, 1.1f), mm);
                }
            }
    }

    void Border(TTBatch lit, float b0, float b1, float span, float mid)
    {
        var red = M(TT.H("#d6283c")); var wh = M(TT.H("#f4f4f4"));
        int n = Mathf.CeilToInt(span / 4f);
        for (int i = 0; i < n; i++)
        {
            float t = b0 + i * 4f + 2f; var m = i % 2 == 0 ? red : wh;
            lit.Add(Cu, new Vector3(t, 0.6f, b0 - 0.4f), new Vector3(4f, 1.2f, 0.8f), m);
            lit.Add(Cu, new Vector3(t, 0.6f, b1 + 0.4f), new Vector3(4f, 1.2f, 0.8f), m);
            lit.Add(Cu, new Vector3(b0 - 0.4f, 0.6f, t), new Vector3(0.8f, 1.2f, 4f), m);
            lit.Add(Cu, new Vector3(b1 + 0.4f, 0.6f, t), new Vector3(0.8f, 1.2f, 4f), m);
        }
        Col(new Vector3(mid, 1.5f, b0 - 0.4f), new Vector3(span + 2, 3f, 0.8f));
        Col(new Vector3(mid, 1.5f, b1 + 0.4f), new Vector3(span + 2, 3f, 0.8f));
        Col(new Vector3(b0 - 0.4f, 1.5f, mid), new Vector3(0.8f, 3f, span + 2));
        Col(new Vector3(b1 + 0.4f, 1.5f, mid), new Vector3(0.8f, 3f, span + 2));
    }
}
