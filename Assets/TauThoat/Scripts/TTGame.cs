// Tẩu Thoát Turbo - quản lý game: menu, gara, 3 chế độ, cảnh sát AI, vật phẩm, camera, HUD.
// Không cần dựng scene: bấm Play là game tự tạo toàn bộ.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TTGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<TTGame>() == null) new GameObject("TauThoatGame").AddComponent<TTGame>();
    }

    enum St { Menu, Play, Dying, Over }
    St state = St.Menu; bool paused; string mode = "survival";

    [System.Serializable]
    class SaveData
    {
        public int coins; public List<int> owned = new List<int> { 0 }; public int car; public int bestS, bestG, bestZ;
        public bool mute; public int theme; public int ctrl; public List<string> paint = new List<string>();
    }
    SaveData save;

    TTCity city; Camera cam; Light sun; Transform world; Transform showCar;
    TTCar player; readonly List<TTCar> cops = new List<TTCar>();
    readonly List<Transform> coins = new List<Transform>();
    class Pup { public Transform t; public int k; }
    readonly List<Pup> pups = new List<Pup>();
    class Wreck { public Transform t; public float time; }
    readonly List<Wreck> wrecks = new List<Wreck>();
    class Floater { public Vector3 p; public string txt; public float t; }
    readonly List<Floater> floaters = new List<Floater>();

    Transform targetT, zoneFill, beacon, shieldRing; Vector3 targetP; int targetKind; float targetR, zoneP;
    float T, shake, spawnT, overT, shieldT, nitroT, hintT, toastT, lampT; int score, earned, wrecked; string toast = "";
    Vector3 focus; ParticleSystem psFire, psSmoke, psNitro, snowPs;
    AudioSource engA, sirA, sfxA; AudioClip crashC, blipC, boomC;
    readonly List<Light> lampLights = new List<Light>();
    PhysicsMaterial slick;
    bool overNew; int overScore;
    static readonly string[] PupName = { "shield", "nitro", "fix" };
    static readonly Color[] PupCol = { TT.H("#35b8ff"), TT.H("#9b6bff"), TT.H("#3fdc7a") };

    // ---------------- khởi tạo ----------------
    void Start()
    {
        Application.targetFrameRate = 60;
        Load();
        cam = Camera.main;
        if (!cam) { var g = new GameObject("Main Camera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>(); g.AddComponent<AudioListener>(); }
        cam.fieldOfView = 40; cam.nearClipPlane = 0.5f; cam.farClipPlane = 700; cam.clearFlags = CameraClearFlags.SolidColor;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
        if (!sun) { var g = new GameObject("Sun"); sun = g.AddComponent<Light>(); sun.type = LightType.Directional; }
        sun.shadows = LightShadows.Soft;
        QualitySettings.shadowDistance = 160;
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null)
        {
            var pi = rp.GetType().GetProperty("shadowDistance");
            if (pi != null && pi.CanWrite) pi.SetValue(rp, 160f);
        }

        slick = new PhysicsMaterial("TT_slick") { dynamicFriction = 0, staticFriction = 0, bounciness = 0.2f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Average };

        engA = gameObject.AddComponent<AudioSource>(); engA.clip = TTAudio.Engine(); engA.loop = true; engA.volume = 0; engA.Play();
        sirA = gameObject.AddComponent<AudioSource>(); sirA.clip = TTAudio.Siren(); sirA.loop = true; sirA.volume = 0; sirA.Play();
        sfxA = gameObject.AddComponent<AudioSource>();
        crashC = TTAudio.Noise(0.35f, 1800); boomC = TTAudio.Noise(0.9f, 900); blipC = TTAudio.Blip();

        psFire = MakePS("Fire", TT.H("#ffd23c"), TT.H("#ff3d2e"), 0.8f, 1.6f, 0.4f);
        psSmoke = MakePS("Smoke", new Color(0.25f, 0.25f, 0.28f, 0.8f), new Color(0.45f, 0.45f, 0.5f, 0.6f), 1.8f, 3f, -0.05f);
        psNitro = MakePS("Nitro", TT.H("#b08cff"), TT.H("#5fd0ff"), 0.35f, 0.9f, 0f);

        BuildWorld();
        ShowRoom();
    }

    ParticleSystem MakePS(string n, Color c0, Color c1, float life, float size, float grav)
    {
        var g = new GameObject(n); g.transform.SetParent(transform);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.loop = true; m.playOnAwake = false; m.duration = 1;
        m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
        m.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        m.startSpeed = 0; m.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
        m.gravityModifier = grav; m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 4000;
        var em = ps.emission; em.rateOverTime = 0;
        var sh = ps.shape; sh.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        col.color = gr;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.5f));
        var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = TTMat.Particle(); r.shadowCastingMode = ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    void Emit(ParticleSystem ps, Vector3 p, int n, float spd, float up = 0.5f)
    {
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < n; i++)
        {
            ep.position = p + Random.insideUnitSphere * 0.4f;
            ep.velocity = Random.insideUnitSphere * spd + Vector3.up * spd * up;
            ps.Emit(ep, 1);
        }
    }

    void Load()
    {
        try { save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("TT_save", "")); } catch { save = null; }
        if (save == null) save = new SaveData();
        if (save.owned == null || save.owned.Count == 0) save.owned = new List<int> { 0 };
        if (save.paint == null) save.paint = new List<string>();
        while (save.paint.Count < TT.Cars.Length) save.paint.Add("");
        if (save.car < 0 || save.car >= TT.Cars.Length || !save.owned.Contains(save.car)) save.car = 0;
        if (save.theme < 0 || save.theme >= TT.Themes.Length) save.theme = 0;
    }
    void Save() { PlayerPrefs.SetString("TT_save", JsonUtility.ToJson(save)); PlayerPrefs.Save(); }
    Color PaintOf(int i) { return string.IsNullOrEmpty(save.paint[i]) ? TT.Cars[i].col : TT.H(save.paint[i]); }
    ThemeDef Th { get { return TT.Themes[save.theme]; } }

    void BuildWorld()
    {
        if (city != null && city.root) Destroy(city.root);
        if (world) Destroy(world.gameObject);
        foreach (var l in lampLights) if (l) Destroy(l.gameObject);
        lampLights.Clear(); cops.Clear(); coins.Clear(); pups.Clear(); wrecks.Clear(); floaters.Clear();
        player = null; targetT = null; showCar = null;

        var th = Th;
        city = new TTCity(); city.Build(th);
        world = new GameObject("Dynamic").transform;

        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = th.amb;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = th.sky;
        RenderSettings.fogStartDistance = 110; RenderSettings.fogEndDistance = 280;
        cam.backgroundColor = th.sky;
        sun.color = th.sun; sun.intensity = th.sunI; sun.transform.rotation = Quaternion.Euler(th.night ? 35 : 52, -35, 0);

        if (th.night)
            for (int i = 0; i < 14; i++)
            {
                var g = new GameObject("LampLight"); g.transform.SetParent(world);
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 15; l.intensity = 5; l.color = TT.H("#ffc477"); l.shadows = LightShadows.None;
                lampLights.Add(l);
            }

        if (snowPs) Destroy(snowPs.gameObject);
        if (th.snow)
        {
            snowPs = MakePS("Snow", Color.white, new Color(0.9f, 0.95f, 1f), 9f, 0.35f, 0.04f);
            var em = snowPs.emission; em.rateOverTime = 500;
            var sh = snowPs.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(160, 1, 160);
            var m = snowPs.main; m.startSpeed = 0.5f;
            var sz = snowPs.sizeOverLifetime; sz.enabled = false;
        }
    }

    void ShowRoom()
    {
        if (showCar) Destroy(showCar.gameObject);
        CarParts p;
        var go = TTCarBuilder.Build(TT.Cars[save.car], PaintOf(save.car), world, out p, false);
        float c = 6 * TTCity.CELL;
        go.transform.position = new Vector3(c, 0.05f, c);
        showCar = go.transform;
        if (Th.night) HeadLight(go.transform, TT.Cars[save.car]);
    }

    void HeadLight(Transform car, CarDef d)
    {
        var g = new GameObject("HeadLight"); g.transform.SetParent(car, false);
        g.transform.localPosition = new Vector3(0, 1f, d.len / 2); g.transform.localEulerAngles = new Vector3(14, 0, 0);
        var l = g.AddComponent<Light>(); l.type = LightType.Spot; l.range = 45; l.spotAngle = 75; l.intensity = 12; l.color = TT.H("#fff2cf"); l.shadows = LightShadows.None;
    }

    // ---------------- vòng chơi ----------------
    void StartGame(string m)
    {
        mode = m;
        BuildWorld();
        float c = 6 * TTCity.CELL;
        player = SpawnCar(TT.Cars[save.car], PaintOf(save.car), new Vector3(c, 0.05f, c), 0, false);
        player.onHit = OnPlayerHit;
        if (Th.night) HeadLight(player.transform, player.def);
        var ring = TTCarBuilder.Part(PrimitiveType.Cylinder, world, Vector3.zero, new Vector3(6f, 0.02f, 6f), TTMat.Get(TT.H("#35b8ff"), 3f));
        shieldRing = ring.transform; ring.SetActive(false);
        for (int i = 0; i < 45; i++) coins.Add(MakeCoin(RoadPointAt(25, 9999)));
        for (int i = 0; i < 4; i++) pups.Add(MakePup(RoadPointAt(40, 9999), i % 3));
        T = 0; score = 0; earned = 0; wrecked = 0; shake = 0; spawnT = 1.5f; shieldT = 0; nitroT = 0; hintT = 5; paused = false;
        NewTarget();
        focus = player.transform.position;
        state = St.Play;
        Toast(m == "getaway" ? "Đi đón đồng bọn!" : m == "zone" ? "Chiếm vùng vàng!" : "Chạy đi!");
    }

    TTCar SpawnCar(CarDef d, Color col, Vector3 pos, float yaw, bool isCop)
    {
        CarParts parts;
        var go = TTCarBuilder.Build(d, col, world, out parts, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = d.sty == CarStyle.Swat ? 2600 : d.sty == CarStyle.CopSuv ? 1700 : 1200;
        rb.linearDamping = 0; rb.angularDamping = 0; rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(d.wid, 1.4f, d.len); bc.center = new Vector3(0, 0.8f, 0); bc.sharedMaterial = slick;
        var car = go.AddComponent<TTCar>();
        car.Init(d, isCop, parts);
        parts.trails = new[] { Trail(go.transform, new Vector3(-(d.wid / 2 - 0.15f), 0.08f, -d.len * 0.31f)), Trail(go.transform, new Vector3(d.wid / 2 - 0.15f, 0.08f, -d.len * 0.31f)) };
        if (parts.flash) parts.flash.intensity = Th.night ? 7 : 2.5f;
        return car;
    }

    TrailRenderer Trail(Transform parent, Vector3 lp)
    {
        var g = new GameObject("Skid"); g.transform.SetParent(parent, false);
        g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var t = g.AddComponent<TrailRenderer>();
        t.time = 7; t.minVertexDistance = 0.3f; t.widthMultiplier = 0.32f; t.alignment = LineAlignment.TransformZ;
        t.sharedMaterial = TTMat.Plain(); t.shadowCastingMode = ShadowCastingMode.Off; t.receiveShadows = false;
        Color sc = Th.snow ? new Color(0.25f, 0.3f, 0.4f, 0.35f) : new Color(0.06f, 0.06f, 0.08f, 0.5f);
        t.startColor = sc; t.endColor = sc; t.emitting = false;
        return t;
    }

    Transform MakeCoin(Vector3 p)
    {
        var g = TTCarBuilder.Part(PrimitiveType.Cylinder, world, p + Vector3.up * 1.3f, new Vector3(1.5f, 0.1f, 1.5f), TTMat.Get(TT.H("#ffc21a"), 0.6f, 0.8f, 0.8f), new Vector3(90, 0, 0));
        return g.transform;
    }

    Pup MakePup(Vector3 p, int k)
    {
        var g = TTCarBuilder.Part(PrimitiveType.Cube, world, p + Vector3.up * 1.5f, Vector3.one * 1.7f, TTMat.Get(PupCol[k], 1.5f));
        TTCarBuilder.Part(PrimitiveType.Sphere, g.transform, Vector3.zero, Vector3.one * 0.6f, TTMat.Get(Color.white, 1.5f));
        return new Pup { t = g.transform, k = k };
    }

    Vector3 RoadPoint()
    {
        int k = Random.Range(0, TTCity.N + 1); float t = Random.Range(6f, TTCity.W - 6f), o = Random.Range(-4f, 4f);
        return Random.value < 0.5f ? new Vector3(k * TTCity.CELL + o, 0.05f, t) : new Vector3(t, 0.05f, k * TTCity.CELL + o);
    }
    Vector3 RoadPointAt(float minD, float maxD)
    {
        Vector3 c = player ? player.transform.position : new Vector3(TTCity.W / 2, 0, TTCity.W / 2), p = c;
        for (int i = 0; i < 80; i++) { p = RoadPoint(); float d = DXZ(p, c); if (d >= minD && d <= maxD) return p; }
        return p;
    }
    static float DXZ(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

    void NewTarget()
    {
        if (mode == "getaway") { targetKind = 1; targetP = RoadPointAt(90, 170); targetR = 7; }
        else if (mode == "zone") { targetKind = 3; targetP = RoadPointAt(90, 180); targetR = 15; zoneP = 0; }
        else { targetKind = 0; if (targetT) Destroy(targetT.gameObject); return; }
        BuildTarget();
    }

    void BuildTarget()
    {
        if (targetT) Destroy(targetT.gameObject);
        Color c = targetKind == 1 ? TT.H("#3fdc7a") : targetKind == 2 ? TT.H("#ff7a1a") : TT.H("#ffd23f");
        var root = new GameObject("Target").transform; root.SetParent(world); root.position = new Vector3(targetP.x, 0, targetP.z);
        TTCarBuilder.Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.3f, 0), new Vector3(targetR * 2 + 1f, 0.02f, targetR * 2 + 1f), TTMat.Get(c, 2.5f));
        TTCarBuilder.Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.32f, 0), new Vector3(targetR * 2, 0.02f, targetR * 2), TTMat.Get(TT.Shade(c, 0.45f), 0.5f));
        beacon = TTCarBuilder.Part(PrimitiveType.Cylinder, root, new Vector3(0, 25, 0), new Vector3(0.9f, 25, 0.9f), TTMat.Get(c, 3f)).transform;
        zoneFill = null;
        if (targetKind == 3) zoneFill = TTCarBuilder.Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.34f, 0), new Vector3(0.01f, 0.02f, 0.01f), TTMat.Get(TT.H("#ff7a1a"), 1.5f)).transform;
        if (targetKind == 1)
            for (int i = -1; i <= 1; i++)
            {
                TTCarBuilder.Part(PrimitiveType.Capsule, root, new Vector3(i * 1.6f, 1.1f, 0), new Vector3(0.7f, 0.75f, 0.7f), TTMat.Get(TT.H("#22252c")));
                TTCarBuilder.Part(PrimitiveType.Sphere, root, new Vector3(i * 1.6f, 2.2f, 0), Vector3.one * 0.55f, TTMat.Get(TT.H("#f3c9a0")));
            }
        if (targetKind == 2)
        {
            TTCarBuilder.Part(PrimitiveType.Cube, root, new Vector3(0, 0.4f, 0), new Vector3(targetR * 1.4f, 0.04f, 0.8f), TTMat.Get(Color.white, 1f), new Vector3(0, 45, 0));
            TTCarBuilder.Part(PrimitiveType.Cube, root, new Vector3(0, 0.4f, 0), new Vector3(targetR * 1.4f, 0.04f, 0.8f), TTMat.Get(Color.white, 1f), new Vector3(0, -45, 0));
        }
        targetT = root;
    }

    void OnPlayerHit(TTCar me, Collision c, float imp)
    {
        if (state != St.Play) return;
        TTCar other = c.rigidbody ? c.rigidbody.GetComponent<TTCar>() : null;
        if (other != null && other.cop && !other.dead)
        {
            if (shieldT > 0) { WreckCop(other); return; }
            if (other.hitT <= 0 && imp > 6)
            { Hurt(Mathf.Min(18f, imp * 0.35f * other.def.dmg)); other.hitT = 0.6f; shake = Mathf.Max(shake, imp / 6f); Sfx(crashC, 0.5f); }
        }
        else if (imp > 15) { shake = Mathf.Max(shake, imp / 8f); Sfx(crashC, 0.35f); } // đâm nhà/tường: không mất máu
    }

    void OnCopHit(TTCar me, Collision c, float imp)
    {
        if (me.dead || state != St.Play) return;
        TTCar other = c.rigidbody ? c.rigidbody.GetComponent<TTCar>() : null;
        if (other == player) return;
        if (other != null && other.cop && !other.dead) { if (imp > me.def.brk * 0.7f) WreckCop(me); }
        else if (imp > me.def.brk) WreckCop(me);
    }

    void WreckCop(TTCar c)
    {
        if (c.dead) return;
        c.Wreck(); cops.Remove(c);
        Explode(c.transform.position, c.def.sty == CarStyle.Swat);
        wrecked++; earned += c.def.reward;
        floaters.Add(new Floater { p = c.transform.position + Vector3.up * 2, txt = "+" + c.def.reward, t = 1.2f });
        wrecks.Add(new Wreck { t = c.transform, time = 9 });
        Destroy(c.gameObject, 9f);
    }

    void Explode(Vector3 p, bool big)
    {
        Emit(psFire, p + Vector3.up, big ? 60 : 36, big ? 16 : 11, 0.6f);
        Emit(psSmoke, p + Vector3.up, 16, 3f, 0.8f);
        var g = new GameObject("Flash"); g.transform.position = p + Vector3.up * 2;
        var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = TT.H("#ff9a3c"); l.range = 25; l.intensity = 12;
        Destroy(g, 0.18f);
        shake = Mathf.Max(shake, big ? 2.4f : 1.4f);
        Sfx(boomC, big ? 1f : 0.7f);
    }

    void Hurt(float d)
    {
        if (shieldT > 0 || state != St.Play) return;
        player.hp -= d;
        if (player.hp <= 0)
        {
            player.hp = 0; Explode(player.transform.position, true); player.Wreck();
            state = St.Dying; overT = 1.6f;
        }
    }

    int FinalScore() { return mode == "survival" ? Mathf.FloorToInt(T) + wrecked * 5 : score; }

    void EndGame()
    {
        state = St.Over;
        int s = FinalScore(), best = mode == "survival" ? save.bestS : mode == "getaway" ? save.bestG : save.bestZ;
        overNew = s > best; overScore = s;
        if (overNew) { if (mode == "survival") save.bestS = s; else if (mode == "getaway") save.bestG = s; else save.bestZ = s; }
        save.coins += earned; Save();
    }

    CarDef CopType()
    {
        float h = T + (mode == "survival" ? 0 : score * 25); float r = Random.value;
        if (h > 90 && r < 0.18f) return TT.Cops[2];
        if (h > 40 && r < 0.4f) return TT.Cops[1];
        return TT.Cops[0];
    }

    bool Blocked(Vector3 o, Vector3 dir, float dist)
    {
        RaycastHit h;
        if (Physics.Raycast(o + Vector3.up * 0.8f, dir, out h, dist))
        {
            var rb = h.collider.attachedRigidbody;
            if (rb == null) return true;
            var car = rb.GetComponent<TTCar>();
            return car == null || car.dead;
        }
        return false;
    }

    void CopAI(TTCar c, float dt)
    {
        Vector3 pos = c.transform.position, pp = player.transform.position;
        float d = DXZ(pp, pos), lead = Mathf.Clamp(d / 70f, 0, 0.7f);
        Vector3 tp = pp + player.Vel * lead, to = tp - pos;
        float yaw = c.transform.eulerAngles.y;
        float da = Mathf.DeltaAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float s = Mathf.Clamp(da * 2.6f, -1, 1);
        foreach (float o in new[] { -0.5f, 0.5f })
            if (Blocked(pos, Quaternion.Euler(0, yaw + o * Mathf.Rad2Deg, 0) * Vector3.forward, 9f)) s += o < 0 ? 1.1f : -1.1f;
        if (Blocked(pos, c.transform.forward, 6.5f)) s += da >= 0 ? 1 : -1;
        c.steer = Mathf.Clamp(s, -1, 1);
        c.hb = Mathf.Abs(da) > 2.3f && d < 50;
        if (c.Speed < 4 && c.rev <= 0) { c.stuck += dt; if (c.stuck > 1) { c.rev = 0.9f; c.stuck = 0; } } else c.stuck = 0;
        c.hitT -= dt;
    }

    void PlayUpdate(float dt, bool L, bool R, bool HB, bool BK)
    {
        T += dt; hintT -= dt; shieldT -= dt; nitroT -= dt;
        bool both = L && R;
        float steer = both ? 0 : (R ? 1 : 0) - (L ? 1 : 0); bool hbOn = HB || both, backOn = BK;
        player.throttle = 1;
        if (save.ctrl == 1) MouseDrive(ref steer, ref hbOn, ref backOn);
        player.reverse = backOn; player.hb = hbOn && !backOn; player.steer = steer;
        bool nb = nitroT > 0;
        player.maxMul = (nb ? 1.38f : 1f) * (player.hp < player.def.hp * 0.25f ? 0.88f : 1f);
        player.accMul = nb ? 1.7f : 1f;
        Vector3 pp = player.transform.position;
        if (nb) Emit(psNitro, pp - player.transform.forward * player.def.len * 0.55f + Vector3.up * 0.5f, 2, 2f, 0.2f);
        if (player.hp < player.def.hp * 0.35f && Random.value < 0.3f) Emit(psSmoke, pp + Vector3.up * 1.2f + player.transform.forward * 1.5f, 1, 1.5f, 1.2f);

        float cf = 0.9f + Mathf.Min(0.2f, T / 500f) + (mode == "survival" ? 0 : score * 0.012f);
        for (int i = cops.Count - 1; i >= 0; i--)
        {
            var c = cops[i];
            if (c == null || c.dead) { cops.RemoveAt(i); continue; }
            if (DXZ(c.transform.position, pp) > 260) { Destroy(c.gameObject); cops.RemoveAt(i); continue; }
            c.maxMul = cf; c.accMul = cf;
            CopAI(c, dt);
        }

        foreach (var cn in coins)
        {
            cn.Rotate(0, 0, 180 * dt, Space.Self);
            if (DXZ(cn.position, pp) < 3.4f) { earned++; Sfx(blipC, 0.35f); cn.position = RoadPointAt(50, 300) + Vector3.up * 1.3f; }
        }
        foreach (var p in pups)
        {
            p.t.Rotate(40 * dt, 90 * dt, 0);
            p.t.position = new Vector3(p.t.position.x, 1.5f + Mathf.Sin(T * 3f) * 0.3f, p.t.position.z);
            if (DXZ(p.t.position, pp) < 3.8f)
            {
                if (p.k == 0) { shieldT = 7; Toast("Khiên bảo vệ! Tông cảnh sát đi!"); }
                else if (p.k == 1) { nitroT = 4; Toast("NITRO!"); }
                else { player.hp = Mathf.Min(player.def.hp, player.hp + player.def.hp * 0.45f); Toast("Sửa xe +45%"); }
                Sfx(blipC, 0.6f);
                int nk = Random.Range(0, 3);
                p.t.GetComponent<Renderer>().sharedMaterial = TTMat.Get(PupCol[nk], 1.5f); p.k = nk;
                p.t.position = RoadPointAt(70, 300) + Vector3.up * 1.5f;
            }
        }

        if (targetKind != 0)
        {
            float d = DXZ(targetP, pp);
            if (beacon) beacon.localScale = new Vector3(0.9f + Mathf.Sin(T * 5) * 0.25f, 25, 0.9f + Mathf.Sin(T * 5) * 0.25f);
            if (targetKind == 1 && d < targetR) { targetKind = 2; targetP = RoadPointAt(140, 260); targetR = 7; BuildTarget(); Toast("Đã đón! Tới điểm hẹn!"); Sfx(blipC, 0.8f); }
            else if (targetKind == 2 && d < targetR) { score++; earned += 5; floaters.Add(new Floater { p = pp + Vector3.up * 2, txt = "+5", t = 1.2f }); Toast("Chuyến " + score + " xong! Cảnh sát đông hơn..."); Sfx(blipC, 0.8f); NewTarget(); }
            else if (targetKind == 3)
            {
                if (d < targetR) { zoneP += dt / 4.5f; if (zoneP >= 1) { score++; earned += 4; floaters.Add(new Floater { p = pp + Vector3.up * 2, txt = "+4", t = 1.2f }); Toast("Chiếm được vùng " + score + "!"); Sfx(blipC, 0.8f); NewTarget(); } }
                else zoneP = Mathf.Max(0, zoneP - dt * 0.05f);
                if (zoneFill) { float w = targetR * 2 * Mathf.Clamp01(zoneP); zoneFill.localScale = new Vector3(w, 0.02f, w); }
            }
        }

        spawnT -= dt;
        int maxC = Mathf.Min(16, 2 + Mathf.FloorToInt(T / 13f) + (mode == "survival" ? 0 : score));
        if (spawnT <= 0 && cops.Count < maxC)
        {
            spawnT = Random.Range(1.6f, 2.8f);
            var p = RoadPointAt(75, 140);
            var vp = cam.WorldToViewportPoint(p);
            if (!(vp.z > 0 && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f))
            {
                var t = CopType();
                Vector3 to = pp - p;
                var c = SpawnCar(t, t.col, p, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, true);
                c.onHit = OnCopHit; cops.Add(c);
                if (t.sty == CarStyle.Swat) Toast("Xe SWAT xuất hiện!");
            }
        }

        if (shieldRing)
        {
            shieldRing.gameObject.SetActive(shieldT > 0);
            float sc = 6f + Mathf.Sin(T * 10) * 0.4f;
            shieldRing.position = pp + Vector3.up * 0.35f; shieldRing.localScale = new Vector3(sc, 0.02f, sc);
        }
    }

    /// Lái bằng chuột: di chuột tới đâu xe chạy tới đó, không cần nhấn.
    /// Xa thì chạy nhanh, gần thì chậm lại, tới nơi thì dừng; con trỏ ở ngay phía sau thì tự lùi.
    void MouseDrive(ref float steer, ref bool hb, ref bool back)
    {
        Vector2 mp; bool lm, rm;
        if (!TTInput.Mouse(out mp, out lm, out rm)) return;
        var ray = cam.ScreenPointToRay(mp);
        var ground = new Plane(Vector3.up, Vector3.zero);
        float e;
        if (!ground.Raycast(ray, out e)) return;
        Vector3 to = ray.GetPoint(e) - player.transform.position; to.y = 0;
        float d = to.magnitude;
        if (d < 3.5f) { player.throttle = 0; steer = 0; return; }
        float da = Mathf.DeltaAngle(player.transform.eulerAngles.y, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
        if (Mathf.Abs(da) > 125f && d < 16f)
        {
            // điểm đến ở ngay sau xe: lùi lại, đuôi xe hướng về con trỏ
            back = true;
            float dr = Mathf.DeltaAngle(player.transform.eulerAngles.y, Mathf.Atan2(-to.x, -to.z) * Mathf.Rad2Deg);
            steer = -Mathf.Clamp(dr / 25f, -1f, 1f);
            player.throttle = 1;
            return;
        }
        steer = Mathf.Clamp(da / 25f, -1f, 1f);
        player.throttle = Mathf.Clamp(0.2f + (d - 3.5f) / 30f, 0.2f, 1f);
    }

    void FollowCam(float dt)
    {
        if (!player) return;
        Vector3 tgt = player.transform.position + player.Vel * 0.35f;
        focus = Vector3.Lerp(focus, tgt, 1 - Mathf.Exp(-4 * dt));
        var rot = Quaternion.Euler(72, 45, 0);
        Vector3 pos = focus - rot * Vector3.forward * 60f + Random.insideUnitSphere * shake * 0.25f;
        cam.transform.SetPositionAndRotation(pos, rot);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool L, R, HB, BK, P;
        TTInput.Keys(out L, out R, out HB, out BK, out P);
        if (P && state == St.Play) paused = !paused;
        Time.timeScale = state == St.Play && paused ? 0 : 1;

        if (state == St.Menu && showCar)
        {
            showCar.Rotate(0, 25 * dt, 0);
            var rot = Quaternion.Euler(20, Time.time * 10f, 0);
            Vector3 f = showCar.position + Vector3.up * 1.1f;
            cam.transform.SetPositionAndRotation(f - rot * Vector3.forward * 13f - rot * Vector3.right * 4.2f, rot);
            focus = showCar.position;
        }
        else if (state == St.Play && !paused) { PlayUpdate(dt, L, R, HB, BK); FollowCam(dt); }
        else if (state == St.Dying) { T += dt; overT -= dt; if (overT <= 0) EndGame(); FollowCam(dt); }
        else if (state == St.Over) FollowCam(dt);

        for (int i = wrecks.Count - 1; i >= 0; i--)
        {
            var w = wrecks[i]; w.time -= dt;
            if (w.t == null || w.time <= 0) { wrecks.RemoveAt(i); continue; }
            if (w.time > 2 && Random.value < 0.5f) Emit(psFire, w.t.position + Vector3.up * 1.2f, 1, 1.2f, 2f);
        }
        if (player && player.dead && state != St.Play && Random.value < 0.5f) Emit(psFire, player.transform.position + Vector3.up * 1.2f, 1, 1.2f, 2f);

        for (int i = floaters.Count - 1; i >= 0; i--) { floaters[i].t -= dt; floaters[i].p += Vector3.up * 3 * dt; if (floaters[i].t <= 0) floaters.RemoveAt(i); }
        shake = Mathf.Max(0, shake - 6 * dt); toastT -= dt;
        if (snowPs) snowPs.transform.position = focus + Vector3.up * 35;

        lampT -= dt;
        if (lampLights.Count > 0 && lampT <= 0 && city != null)
        {
            lampT = 0.3f;
            var sorted = new List<Vector3>(city.lamps);
            Vector3 f = focus;
            sorted.Sort((a, b) => DXZ(a, f).CompareTo(DXZ(b, f)));
            for (int i = 0; i < lampLights.Count && i < sorted.Count; i++) lampLights[i].transform.position = sorted[i];
        }

        bool on = state == St.Play && !paused && !save.mute;
        float spd = player && !player.dead ? player.Speed : 0;
        engA.volume = Mathf.MoveTowards(engA.volume, on ? 0.22f : 0, dt * 2); engA.pitch = 0.6f + spd / 45f * 1.4f;
        float nd = 1e9f; if (player) foreach (var c in cops) if (c) nd = Mathf.Min(nd, DXZ(c.transform.position, player.transform.position));
        sirA.volume = Mathf.MoveTowards(sirA.volume, on ? Mathf.Clamp01(1 - nd / 90f) * 0.3f : 0, dt * 2);
    }

    void Sfx(AudioClip c, float v) { if (!save.mute && c) sfxA.PlayOneShot(c, v); }
    void Toast(string s) { toast = s; toastT = 1.8f; }

    // ---------------- giao diện (IMGUI) ----------------
    GUIStyle sTitle, sH2, sBtn, sBtnSel, sLbl, sSmall, sBig, sCenter, sChip, sOr;
    readonly Dictionary<Color, Texture2D> texs = new Dictionary<Color, Texture2D>();
    Texture2D Tex(Color c)
    {
        Texture2D t;
        if (texs.TryGetValue(c, out t) && t) return t;
        t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); texs[c] = t; return t;
    }

    void Styles()
    {
        if (sBtn != null) return;
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = true, padding = new RectOffset(14, 10, 6, 6) };
        sBtn.normal.background = Tex(TT.H("#2b3042")); sBtn.hover.background = Tex(TT.H("#3a4058")); sBtn.active.background = Tex(TT.H("#ff7a1a"));
        sBtn.focused.background = sBtn.normal.background;
        sBtn.normal.textColor = sBtn.hover.textColor = sBtn.active.textColor = sBtn.focused.textColor = Color.white;
        sBtnSel = new GUIStyle(sBtn); sBtnSel.normal.background = Tex(TT.H("#7a3a10")); sBtnSel.hover.background = Tex(TT.H("#8a4414")); sBtnSel.focused.background = sBtnSel.normal.background;
        sOr = new GUIStyle(sBtn) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sOr.normal.background = Tex(TT.H("#ff7a1a")); sOr.hover.background = Tex(TT.H("#ff8f3d")); sOr.focused.background = sOr.normal.background;
        sTitle = new GUIStyle(GUI.skin.label) { fontSize = 50, fontStyle = FontStyle.Bold, richText = true }; sTitle.normal.textColor = TT.H("#ff7a1a");
        sH2 = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold }; sH2.normal.textColor = TT.H("#ffd9b8");
        sLbl = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, richText = true }; sLbl.normal.textColor = Color.white;
        sSmall = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, richText = true }; sSmall.normal.textColor = TT.H("#c8ccd8");
        sBig = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true }; sBig.normal.textColor = Color.white;
        sCenter = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true }; sCenter.normal.textColor = Color.white;
        sChip = new GUIStyle(GUI.skin.box) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
        sChip.normal.background = Tex(new Color(0.06f, 0.07f, 0.1f, 0.8f)); sChip.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        Styles();
        float s = Screen.height / 900f, vw = Screen.width / s;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        if (state == St.Menu) MenuGUI(vw);
        else
        {
            if (player && state != St.Over) HudGUI(vw, s);
            if (state == St.Play && paused) PauseGUI(vw);
            if (state == St.Over) OverGUI(vw);
        }
        if (toastT > 0)
        {
            var c = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(toastT * 2));
            Shadowed(new Rect(0, 190, vw, 50), "<size=34>" + toast + "</size>");
            GUI.color = c;
        }
    }

    void Shadowed(Rect r, string txt)
    {
        var c = GUI.color; GUI.color = new Color(0, 0, 0, c.a);
        GUI.Label(new Rect(r.x + 2, r.y + 3, r.width, r.height), StripColor(txt), sCenter);
        GUI.color = c; GUI.Label(r, txt, sCenter);
    }
    static string StripColor(string s) { return System.Text.RegularExpressions.Regex.Replace(s, "<color=[^>]*>|</color>", ""); }

    void MenuGUI(float vw)
    {
        GUI.DrawTexture(new Rect(0, 0, 600, 900), Tex(new Color(0.06f, 0.07f, 0.1f, 0.9f)));
        float x = 28, y = 18, w = 544;
        GUI.Label(new Rect(x, y, w + 40, 62), "TẨU THOÁT <color=#ffffff>TURBO</color>", sTitle); y += 64;
        GUI.Label(new Rect(x, y, w, 28), "Xu: <color=#ffc21a><b>" + save.coins + "</b></color>     Lái xe trốn cảnh sát, dụ chúng đâm vào nhà và vào nhau.", sSmall); y += 34;

        GUI.Label(new Rect(x, y, w, 28), "Chế độ chơi", sH2); y += 30;
        string[] mk = { "getaway", "survival", "zone" };
        string[] nm = { "Tài xế tẩu thoát", "Sinh tồn", "Chiếm vùng" };
        string[] ds = { "Đón đồng bọn rồi chở tới điểm hẹn.", "Trốn càng lâu càng tốt, hạ xe cảnh sát +5 điểm.", "Đứng trong vòng vàng đủ lâu để chiếm vùng." };
        string[] bs = { save.bestG + " chuyến", save.bestS + " điểm", save.bestZ + " vùng" };
        for (int i = 0; i < 3; i++)
        {
            if (GUI.Button(new Rect(x, y, w, 56), "<b><color=#ff9a4a>" + nm[i] + "</color></b>   <size=14><color=#9fd0ff>Kỷ lục: " + bs[i] + "</color></size>\n<size=14>" + ds[i] + "</size>", sBtn)) StartGame(mk[i]);
            y += 58;
        }

        y += 4; GUI.Label(new Rect(x, y, w, 28), "Bản đồ", sH2); y += 30;
        float mw = (w - 24) / 4f;
        for (int i = 0; i < TT.Themes.Length; i++)
        {
            var th = TT.Themes[i]; var r = new Rect(x + i * (mw + 8), y, mw, 50);
            if (GUI.Button(r, "", save.theme == i ? sBtnSel : sBtn)) { save.theme = i; Save(); BuildWorld(); ShowRoom(); }
            GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, 14, 34), Tex(th.outC)); GUI.DrawTexture(new Rect(r.x + 22, r.y + 8, 14, 34), Tex(th.pal[0]));
            GUI.Label(new Rect(r.x + 42, r.y, r.width - 44, r.height), "<b>" + th.name + "</b>", sSmall);
        }
        y += 58;

        GUI.Label(new Rect(x, y + 8, 120, 28), "Điều khiển", sH2);
        string[] cn = { "Bàn phím", "Chuột" };
        for (int i = 0; i < 2; i++)
            if (GUI.Button(new Rect(x + 130 + i * 210, y, 200, 44), "<b>" + cn[i] + "</b>" + (save.ctrl == i ? "  <color=#3fdc7a>✓</color>" : ""), save.ctrl == i ? sBtnSel : sBtn))
            { save.ctrl = i; Save(); }
        y += 52;

        GUI.Label(new Rect(x, y, w, 28), "Gara", sH2); y += 30;
        float cw = (w - 10) / 2f;
        for (int i = 0; i < TT.Cars.Length; i++)
        {
            var c = TT.Cars[i]; bool own = save.owned.Contains(i);
            var r = new Rect(x + (i % 2) * (cw + 10), y + (i / 2) * 56, cw, 52);
            string st = save.car == i ? "<color=#3fdc7a>Đang dùng</color>" : own ? "Chọn" : "<color=#ffc21a>" + c.price + " xu</color>";
            string txt = "<b>" + c.name + "</b>  <size=14>" + st + "</size>\n<size=13>" + Mathf.RoundToInt(c.max * 3.6f) + " km/h · lái " + c.turn.ToString("0.0") + " · bền " + c.hp + "</size>";
            if (GUI.Button(r, txt, save.car == i ? sBtnSel : sBtn))
            {
                if (own) save.car = i;
                else if (save.coins >= c.price) { save.coins -= c.price; save.owned.Add(i); save.car = i; Toast("Đã mua " + c.name + "!"); }
                else Toast("Chưa đủ xu");
                Save(); ShowRoom();
            }
        }
        y += 4 * 56 + 4;

        GUI.Label(new Rect(x, y, w, 28), "Sơn xe: " + TT.Cars[save.car].name, sH2); y += 30;
        Color cur = PaintOf(save.car);
        for (int i = 0; i < TT.Paints.Length; i++)
        {
            var pc = TT.H(TT.Paints[i]); var r = new Rect(x + i * 54, y, 40, 40);
            if (ColorUtility.ToHtmlStringRGB(pc) == ColorUtility.ToHtmlStringRGB(cur)) GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, 46, 46), Tex(Color.white));
            GUI.DrawTexture(r, Tex(pc));
            if (GUI.Button(r, "", GUIStyle.none)) { save.paint[save.car] = TT.Paints[i]; Save(); ShowRoom(); }
        }
        y += 50;
        GUI.Label(new Rect(x, y - 4, w, 50), save.ctrl == 1
            ? "Chuột: di chuột tới đâu xe chạy tới đó, không cần nhấn. Chuột xa chạy nhanh, gần thì chậm, tới nơi xe dừng; chuột ngay sau xe thì xe lùi. P / Esc: tạm dừng."
            : "Bàn phím: ← / A rẽ trái, → / D rẽ phải, Space (hoặc giữ cả hai) phanh trượt, ↓ / S phanh và lùi. P / Esc: tạm dừng.", sSmall);

        var cd = TT.Cars[save.car];
        Shadowed(new Rect(600, 780, vw - 600, 50), "<size=36>" + cd.name + "</size>");
        if (GUI.Button(new Rect(vw - 210, 20, 190, 44), save.mute ? "Âm thanh: Tắt" : "Âm thanh: Bật", sBtn)) { save.mute = !save.mute; Save(); }
    }

    void HudGUI(float vw, float s)
    {
        float pct = Mathf.Clamp01(player.hp / player.def.hp);
        GUI.Box(new Rect(16, 16, 250, 52), "", sChip);
        GUI.Label(new Rect(28, 18, 200, 24), "<b>Máu</b>  <size=14>" + Mathf.RoundToInt(player.Speed * 3.6f) + " km/h</size>", sLbl);
        GUI.DrawTexture(new Rect(28, 46, 226, 12), Tex(TT.H("#3a3f4f")));
        GUI.DrawTexture(new Rect(28, 46, 226 * pct, 12), Tex(Color.Lerp(TT.H("#ff3d6e"), TT.H("#3fdc7a"), pct)));

        string sc = mode == "survival" ? Fmt(T) + " · " + FinalScore() + " điểm" : mode == "getaway" ? score + " chuyến" : score + " vùng";
        string ob = mode == "survival" ? "Cảnh sát: " + cops.Count + " · Hạ " + wrecked :
                    mode == "getaway" ? (targetKind == 1 ? "Đón đồng bọn (cột xanh lá)" : "Chở tới điểm hẹn (cột cam)") :
                    "Chiếm vùng: " + Mathf.FloorToInt(zoneP * 100) + "%";
        if (shieldT > 0) ob += "  · Khiên " + Mathf.CeilToInt(shieldT) + "s";
        if (nitroT > 0) ob += "  · Nitro";
        GUI.Box(new Rect(vw / 2 - 170, 14, 340, 40), "<size=24>" + sc + "</size>", sChip);
        GUI.Box(new Rect(vw / 2 - 210, 58, 420, 32), "<size=16>" + ob + "</size>", sChip);
        GUI.Box(new Rect(vw - 200, 16, 120, 44), "<color=#ffc21a>●</color> " + earned, sChip);
        if (state == St.Play && GUI.Button(new Rect(vw - 70, 16, 54, 44), "II", sOr)) paused = true;

        if (targetKind != 0 && state == St.Play)
        {
            Vector3 sp = cam.WorldToScreenPoint(targetP + Vector3.up);
            bool behind = sp.z < 0; if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
            float gx = sp.x / s, gy = (Screen.height - sp.y) / s;
            if (behind || gx < 50 || gx > vw - 50 || gy < 110 || gy > 850)
            {
                Vector2 c = new Vector2(vw / 2, 470), d = new Vector2(gx, gy) - c;
                if (d.sqrMagnitude < 1) d = Vector2.up;
                float k = Mathf.Min((vw / 2 - 50) / Mathf.Abs(d.x + 0.001f), (360f) / Mathf.Abs(d.y + 0.001f));
                Vector2 e = c + d * k;
                float a = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;
                string[] arr = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
                int idx = Mathf.RoundToInt(Mathf.Repeat(a, 360) / 45f) % 8;
                Color col = targetKind == 1 ? TT.H("#3fdc7a") : targetKind == 2 ? TT.H("#ff7a1a") : TT.H("#ffd23f");
                GUI.Box(new Rect(e.x - 34, e.y - 30, 68, 60), "<size=30><color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + arr[idx] + "</color></size>\n<size=13>" + Mathf.RoundToInt(DXZ(targetP, player.transform.position)) + " m</size>", sChip);
            }
        }

        foreach (var f in floaters)
        {
            Vector3 sp = cam.WorldToScreenPoint(f.p); if (sp.z < 0) continue;
            Shadowed(new Rect(sp.x / s - 60, (Screen.height - sp.y) / s - 20, 120, 40), "<size=30><color=#ffd23f>" + f.txt + "</color></size>");
        }

        if (state == St.Play && hintT <= 0 && !player.reverse && player.throttle > 0.01f && player.Speed < 2f && T > 2f)
            GUI.Box(new Rect(vw / 2 - 220, 800, 440, 50), (save.ctrl == 1 ? "<size=18>Bị kẹt? Đưa chuột ra ngay <b>phía sau xe</b> để lùi</size>" : "<size=18>Bị kẹt? Giữ <b>↓ / S</b> để lùi xe</size>"), sChip);
        if (state == St.Play && hintT > 0)
            GUI.Box(new Rect(vw / 2 - 360, 800, 720, 50), (save.ctrl == 1 ? "<size=17>Di chuột tới đâu xe chạy tới đó · không cần nhấn · chuột ngay sau xe để lùi</size>" : "<size=17>← → / A D để lái · Space phanh trượt · ↓ / S để lùi</size>"), sChip);
    }

    void PauseGUI(float vw)
    {
        GUI.DrawTexture(new Rect(0, 0, vw, 900), Tex(new Color(0, 0, 0, 0.55f)));
        var r = new Rect(vw / 2 - 260, 260, 520, 330);
        GUI.DrawTexture(r, Tex(new Color(0.08f, 0.09f, 0.13f, 0.95f)));
        GUI.Label(new Rect(r.x, r.y + 20, r.width, 60), "<size=46><color=#ff7a1a>TẠM DỪNG</color></size>", sCenter);
        if (GUI.Button(new Rect(r.x + 60, r.y + 100, 400, 54), "Tiếp tục", sOr)) paused = false;
        if (GUI.Button(new Rect(r.x + 60, r.y + 166, 400, 50), save.mute ? "Âm thanh: Tắt" : "Âm thanh: Bật", sBtn)) { save.mute = !save.mute; Save(); }
        if (GUI.Button(new Rect(r.x + 60, r.y + 228, 400, 50), "Bỏ cuộc (về menu)", sBtn)) ToMenu();
    }

    void OverGUI(float vw)
    {
        GUI.DrawTexture(new Rect(0, 0, vw, 900), Tex(new Color(0, 0, 0, 0.45f)));
        var r = new Rect(vw / 2 - 300, 200, 600, 430);
        GUI.DrawTexture(r, Tex(new Color(0.08f, 0.09f, 0.13f, 0.95f)));
        GUI.Label(new Rect(r.x, r.y + 24, r.width, 70), overNew ? "<size=50><color=#ff7a1a>KỶ LỤC MỚI!</color></size>" : "<size=50><color=#ff7a1a>BỊ BẮT!</color></size>", sCenter);
        string unit = mode == "survival" ? " điểm" : mode == "getaway" ? " chuyến" : " vùng";
        GUI.Label(new Rect(r.x, r.y + 100, r.width, 90), overScore + "<size=30>" + unit + "</size>", sBig);
        GUI.Label(new Rect(r.x + 30, r.y + 200, r.width - 60, 60), "<size=18>Thời gian " + Fmt(T) + " · Hạ " + wrecked + " xe cảnh sát · Nhận " + earned + " xu</size>", sCenter);
        if (GUI.Button(new Rect(r.x + 60, r.y + 290, 230, 60), "Chơi lại", sOr)) StartGame(mode);
        if (GUI.Button(new Rect(r.x + 310, r.y + 290, 230, 60), "Về menu", sBtn)) ToMenu();
    }

    void ToMenu()
    {
        paused = false; Time.timeScale = 1; state = St.Menu;
        BuildWorld(); ShowRoom();
    }

    static string Fmt(float t) { return Mathf.FloorToInt(t / 60) + ":" + Mathf.FloorToInt(t % 60).ToString("00"); }
}
