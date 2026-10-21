using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hạt hiệu ứng bằng Particle System, tạo một lần lúc chạy rồi phát bằng Emit (không tạo/huỷ object mỗi lần):
/// bụi chân (chạy, nhảy, đáp, đổi hướng), tia lửa khi trúng đòn, mảnh vỡ khi quái chết, lấp lánh khi nhặt xu.
/// Hạt bay trong không gian 3D (toả cả theo chiều sâu), mảnh vỡ nảy trên mặt đất (layer Ground).
/// </summary>
public static class ZodiacParticles
{
    static readonly Dictionary<string, ParticleSystem> systems = new Dictionary<string, ParticleSystem>();

    /// <summary>Bụi chân. dir = hướng bay chính (thường là ngược hướng chạy hoặc toả hai bên khi đáp).</summary>
    public static void Dust(Vector3 position, Vector3 dir, int count, Color? color = null)
    {
        var ps = Get("Dust", lit: true, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.gravityModifier = -0.05f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1, 0f)));
            FadeOut(ps);
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.15f;
            limit.limit = 0.5f;
        });
        var p = new ParticleSystem.EmitParams { startColor = color ?? new Color(0.85f, 0.78f, 0.65f, 0.75f) };
        for (int i = 0; i < count; i++)
        {
            p.position = position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.1f), Random.Range(-0.2f, 0.2f));
            Vector3 v = dir * Random.Range(1f, 3f) + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.3f, 1.2f), Random.Range(-0.5f, 0.5f));
            p.velocity = v;
            ps.Emit(p, 1);
        }
    }

    /// <summary>Tia lửa toả tròn khi trúng đòn (tự phát sáng, không bị bóng tối che).</summary>
    public static void Sparks(Vector3 position, Color color, int count)
    {
        var ps = Get("Sparks", lit: false, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.gravityModifier = 1.5f;
            FadeOut(ps);
            var stretch = ps.GetComponent<ParticleSystemRenderer>();
            stretch.renderMode = ParticleSystemRenderMode.Stretch;
            stretch.velocityScale = 0.05f;
            stretch.lengthScale = 1.5f;
        });
        var p = new ParticleSystem.EmitParams { position = position };
        for (int i = 0; i < count; i++)
        {
            p.velocity = Random.onUnitSphere * Random.Range(4f, 11f);
            p.startColor = Color.Lerp(color, Color.white, Random.Range(0.2f, 0.7f));
            ps.Emit(p, 1);
        }
    }

    /// <summary>Mảnh vỡ văng ra rồi rơi (quái chết, tượng đá vỡ).</summary>
    public static void Debris(Vector3 position, Color color, int count, float spread = 0.5f)
    {
        var ps = Get("Debris", lit: true, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.5f;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            FadeOut(ps, 0.7f);
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0.35f;
            collision.dampen = 0.3f;
            collision.collidesWith = LayerMask.GetMask("Ground");
        }, square: true);
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            p.position = position + Random.insideUnitSphere * spread;
            p.velocity = new Vector3(Random.Range(-4f, 4f), Random.Range(3f, 9f), Random.Range(-2.5f, 2.5f));
            p.startColor = Color.Lerp(color, Color.black, Random.Range(0f, 0.35f));
            ps.Emit(p, 1);
        }
    }

    /// <summary>Lấp lánh bay lên (nhặt xu, thắp đèn lồng, hồi máu).</summary>
    public static void Sparkle(Vector3 position, Color color, int count, float radius = 0.4f)
    {
        var ps = Get("Sparkle", lit: false, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = -0.3f;
            FadeOut(ps);
        });
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            p.position = position + Random.insideUnitSphere * radius;
            p.velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 2.5f), Random.Range(-0.6f, 0.6f));
            p.startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.5f));
            ps.Emit(p, 1);
        }
    }

    // ───────── Tạo hệ hạt ─────────

    static ParticleSystem Get(string name, bool lit, System.Action<ParticleSystem> setup, bool square = false)
    {
        if (systems.TryGetValue(name, out var existing) && existing != null) return existing;

        var go = new GameObject("Particles " + name);
        Object.DontDestroyOnLoad(go);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.startSpeed = 0f;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var res = ZodiacResources.Get;
        Material baseMat = res != null ? (lit ? res.spriteLit : res.spriteUnlit) : null;
        var mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = (square ? ZodiacFx.Square : ZodiacFx.Circle).texture;
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = lit ? 13 : 18;

        setup(ps);
        ps.Play();
        systems[name] = ps;
        return ps;
    }

    static void FadeOut(ParticleSystem ps, float holdUntil = 0.3f)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }
}
