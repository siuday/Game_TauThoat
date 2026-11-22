// Tẩu Thoát Turbo - vật lý xe kiểu arcade (tự chạy ga, lái trái/phải, phanh trượt)
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TTCar : MonoBehaviour
{
    public CarDef def;
    public bool cop, dead, braking;
    public float hp, steer, maxMul = 1, accMul = 1, rev, stuck, hitT, lat;
    /// 0..1: tốc độ mong muốn (1 = hết ga, 0 = dừng). Dùng cho lái bằng chuột.
    public float throttle = 1;
    public bool hb, reverse;
    public CarParts parts;
    public Rigidbody rb;
    public System.Action<TTCar, Collision, float> onHit;
    float yaw, av, seed;

    public float Speed { get { return rb ? new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude : 0; } }
    public Vector3 Vel { get { return rb && !rb.isKinematic ? rb.linearVelocity : Vector3.zero; } }

    public void Init(CarDef d, bool isCop, CarParts p)
    {
        def = d; cop = isCop; parts = p; hp = d.hp;
        rb = GetComponent<Rigidbody>();
        yaw = transform.eulerAngles.y; seed = Random.value * 10f;
    }

    void FixedUpdate()
    {
        if (dead || rb == null) return;
        float dt = Time.fixedDeltaTime;
        var q = Quaternion.Euler(0, yaw, 0);
        Vector3 fw = q * Vector3.forward, rt = q * Vector3.right;
        Vector3 v = rb.linearVelocity; v.y = 0;
        float f = Vector3.Dot(v, fw), l = Vector3.Dot(v, rt);
        float st = steer; bool h = hb;
        float acc = def.acc * accMul, mx = def.max * maxMul;
        braking = h;
        if (reverse && rev <= 0)
        {
            // Đang tiến: phanh gấp. Đã dừng: chạy lùi (tối đa ~65 km/h).
            if (f > 1f) f -= acc * 1.8f * dt;
            else { f -= acc * 0.7f * dt; if (f < -18f) f = -18f; }
            h = false; braking = true;
        }
        else if (rev > 0) { rev -= dt; f -= acc * dt; if (f < -19f) f = -19f; st = -st; h = false; braking = true; }
        else if (h) f *= Mathf.Exp(-1.3f * dt);
        else if (throttle <= 0.01f) { f *= Mathf.Exp(-3.5f * dt); braking = f > 1f; }
        else
        {
            mx *= throttle;
            if (f < mx) f = Mathf.Min(mx, f + acc * dt);
            else { f += (mx - f) * Mathf.Min(1, 3 * dt); braking = f > mx + 3f; }
        }
        float tgt = st * def.turn * Mathf.Clamp01(Mathf.Abs(f) / 17f) * (h ? 1.3f : 1f) * (f < 0 ? -1f : 1f);
        av += (tgt - av) * Mathf.Min(1, (h ? 2.2f : 10f) * dt);
        yaw += av * Mathf.Rad2Deg * dt;
        l *= Mathf.Exp(-(h ? 1.5f : 7.5f) * dt);
        lat = l;
        rb.MoveRotation(Quaternion.Euler(0, yaw, 0));
        rb.linearVelocity = fw * f + rt * l;
    }

    void Update()
    {
        if (parts == null) return;
        if (cop && !dead && parts.barA != null)
        {
            bool on = Mathf.Repeat((Time.time + seed) * 3.5f, 1f) < 0.5f;
            parts.barA.sharedMaterial = on ? TTCarBuilder.RedOn : TTCarBuilder.RedOff;
            parts.barB.sharedMaterial = on ? TTCarBuilder.BlueOff : TTCarBuilder.BlueOn;
            if (parts.flash) parts.flash.color = on ? new Color(1f, 0.15f, 0.25f) : new Color(0.2f, 0.4f, 1f);
        }
        if (parts.tail != null && !dead)
            parts.tail.SetColor("_EmissionColor", braking ? new Color(1f, 0.08f, 0.08f) * 4f : new Color(0.7f, 0.05f, 0.05f));
        if (parts.trails != null)
            foreach (var t in parts.trails) if (t) t.emitting = !dead && Mathf.Abs(lat) > 11f;
    }

    void OnCollisionEnter(Collision c)
    {
        if (dead || c.contactCount == 0) return;
        float imp = Mathf.Abs(Vector3.Dot(c.relativeVelocity, c.GetContact(0).normal));
        if (onHit != null) onHit(this, c, imp);
    }

    public void Wreck()
    {
        if (dead) return;
        dead = true; steer = 0; hb = false;
        if (rb) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        var burnt = TTMat.Get(TT.H("#2a2422"), 0, 0.1f);
        var burnt2 = TTMat.Get(TT.H("#14100f"), 0, 0.1f);
        if (parts != null && parts.model)
        {
            int i = 0;
            foreach (var r in parts.model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = (i++ % 3 == 0) ? burnt2 : burnt;
        }
        if (parts != null)
        {
            if (parts.flash) parts.flash.enabled = false;
            if (parts.trails != null) foreach (var t in parts.trails) if (t) t.emitting = false;
        }
        foreach (var lgt in GetComponentsInChildren<Light>()) lgt.enabled = false;
    }
}
