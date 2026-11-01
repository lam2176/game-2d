using UnityEngine;
using UnityEngine.Rendering;
using static ZodiacLevelData;

/// <summary>
/// Cảnh Hồi 4 — Núi Tản Viên và Thiên Đình:
///   • Núi Tản Viên: bậc đá rêu lên núi, rừng thông sương mù, vách đá dựng, thác trắng, đền Thượng thờ Sơn Tinh với
///     cột hương; đá lở từ vách rơi xuống đường (dùng lại bẫy rơi của hang).
///   • Thiên Đình: nền mây trắng, lối ngọc viền vàng, cột rồng vàng, cổng Nam Thiên Môn, cầu vồng, sao lấp lánh,
///     đèn trời. Bẫy: mây sấm.
/// </summary>
public static partial class ZodiacLevelBuilder
{
    // ───────────────────────────── Núi Tản Viên ─────────────────────────────

    static void BuildMountain()
    {
        var t = ctx.textures;
        var cliff = Mat("Cliff", t.cobble.texture, Color.Lerp(def.stone, def.far, 0.25f));
        var moss = Mat("Moss", t.turf.texture, Color.Lerp(def.grass, new Color(0.2f, 0.35f, 0.2f), 0.4f));
        var pineTrunk = Plain("PineTrunk", new Color(0.35f, 0.25f, 0.18f));
        var pine = Plain("PineNeedles", Color.Lerp(def.grass, new Color(0.1f, 0.28f, 0.2f), 0.55f));
        var falls = Mat("Waterfall", null, new Color(0.9f, 0.95f, 1f), 0.9f, new Color(0.25f, 0.3f, 0.35f));
        var temple = Plain("TempleWall", new Color(0.85f, 0.78f, 0.65f));
        var templeRoof = Mat("TempleRoof", t.roof.texture, new Color(0.45f, 0.25f, 0.2f));
        var incense = Plain("TempleIncense", new Color(1f, 0.5f, 0.2f), new Color(1f, 0.4f, 0.1f) * 2f);

        // Vách núi dựng phía sau, mép trên lởm chởm
        float cz = def.laneMax + 9f;
        for (float x = -24f; x < def.Length + 26f; x += R(5f, 8f))
        {
            float h = R(7f, 13f), w = R(6f, 9f);
            Box("CliffFace", scenery, new Vector3(x, h / 2f, cz + R(-1f, 1.5f)), new Vector3(w, h, 3f), cliff, 3f, euler: new Vector3(0f, R(-6f, 6f), 0f));
            Box("CliffMoss", scenery, new Vector3(x, h + 0.15f, cz), new Vector3(w * 0.9f, 0.3f, 3.1f), moss, 2f);
        }
        // Thác trắng đổ từ vách
        for (float x = R(12f, 20f); x < def.Length; x += R(38f, 50f))
        {
            Box("Waterfall", scenery, new Vector3(x, 6f, cz - 1.6f), new Vector3(2.2f, 12f, 0.1f), falls, 1f);
            Box("FallPool", scenery, new Vector3(x, 0.02f, cz - 2.6f), new Vector3(4f, 0.03f, 2.2f), WaterMat("FallPool", new Color(0.3f, 0.45f, 0.5f), 0.1f), 1f);
        }
        // Rừng thông giữa vách và đường, sương phủ
        for (float x = R(-3f, 2f); x < def.Length + 6f; x += R(3f, 5.5f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(2f, 7f));
            float h = R(3.5f, 6f);
            Primitive(PrimitiveType.Cylinder, "PineTrunk", scenery, p + Vector3.up * h * 0.3f, new Vector3(0.3f, h * 0.3f, 0.3f), pineTrunk);
            for (int k = 0; k < 3; k++)
            {
                float s = (1.9f - k * 0.5f) * h / 4.5f;
                Primitive(PrimitiveType.Cylinder, "PineTier", scenery, p + Vector3.up * (h * 0.45f + k * h * 0.2f), new Vector3(s, h * 0.12f, s), pine);
            }
        }
        // Đền Thượng thờ Đức Thánh Tản trên bậc đá, cột hương nghi ngút
        for (float x = R(20f, 30f); x < def.Length; x += R(55f, 70f))
        {
            var p = new Vector3(x, 0f, def.laneMax + 5.5f);
            for (int k = 0; k < 4; k++)
                Box("TempleStep", scenery, p + new Vector3(0f, 0.15f + k * 0.3f, -1.6f + k * 0.4f), new Vector3(5f - k * 0.4f, 0.3f, 0.8f), cliff, 1f);
            Box("Temple", scenery, p + new Vector3(0f, 2.4f, 0.6f), new Vector3(4.6f, 2.2f, 2.4f), temple, 1f);
            Box("TempleRoof", scenery, p + new Vector3(0f, 3.75f, 0.5f), new Vector3(5.8f, 0.5f, 3.2f), templeRoof, 1f, euler: new Vector3(-5f, 0f, 0f));
            foreach (float s in new[] { -1f, 1f })
                Box("TempleRoofCurl", scenery, p + new Vector3(s * 3f, 4.05f, -0.9f), new Vector3(0.5f, 0.18f, 0.18f), templeRoof, 1f, euler: new Vector3(0f, 0f, s * 25f));
            var urn = Primitive(PrimitiveType.Sphere, "IncenseUrn", scenery, p + new Vector3(0f, 1.6f, -1.2f), new Vector3(0.7f, 0.5f, 0.7f), incense);
            ZodiacLights.AddPoint(urn, new Color(1f, 0.6f, 0.3f), 6f, 1.6f);
        }
        // Tiền cảnh: tảng đá phủ rêu và cây thông nhỏ
        for (float x = R(2f, 5f); x < def.Length; x += R(5f, 9f))
        {
            var p = new Vector3(x, 0f, def.laneMin - R(1.6f, 3f));
            float s = R(0.7f, 1.3f);
            Primitive(PrimitiveType.Sphere, "MossRock", scenery, p + Vector3.up * s * 0.3f, new Vector3(s * 1.4f, s * 0.8f, s), rnd.NextDouble() < 0.5 ? cliff : moss);
        }
    }

    // ───────────────────────────── Thiên Đình ─────────────────────────────

    static void BuildHeaven()
    {
        var t = ctx.textures;
        var cloud = Plain("Cloud", new Color(1f, 0.98f, 0.95f), new Color(0.25f, 0.24f, 0.22f));
        var cloudShade = Plain("CloudShade", new Color(0.88f, 0.86f, 0.95f), new Color(0.12f, 0.12f, 0.16f));
        var gold = Plain("HeavenGold", new Color(1f, 0.82f, 0.3f), new Color(0.45f, 0.32f, 0.05f));
        var red = Plain("HeavenRed", new Color(0.8f, 0.15f, 0.12f), new Color(0.2f, 0.03f, 0.02f));
        var jade = Mat("HeavenJade", t.roof.texture, new Color(0.35f, 0.75f, 0.6f));
        var lantern = Plain("SkyLantern", new Color(1f, 0.7f, 0.3f), new Color(1f, 0.55f, 0.15f) * 2.4f);
        var star = Plain("Star", Color.white, Color.white * 3f);
        Color[] rainbow =
        {
            new Color(1f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.9f, 0.3f),
            new Color(0.4f, 0.85f, 0.4f), new Color(0.35f, 0.6f, 1f), new Color(0.6f, 0.4f, 0.9f),
        };

        // Biển mây trải mãi phía sau và hai bên đường
        for (float x = -20f; x < def.Length + 20f; x += R(3f, 5f))
            for (int row = 0; row < 4; row++)
            {
                float s = R(2.5f, 4.5f) + row;
                Primitive(PrimitiveType.Sphere, "CloudPuff", scenery, new Vector3(x + R(-1f, 1f), R(-0.6f, 0.2f), def.laneMax + 2f + row * 5f + R(-1f, 1f)),
                          new Vector3(s * 1.4f, s * 0.5f, s), row % 2 == 0 ? cloud : cloudShade);
            }
        for (float x = R(0f, 3f); x < def.Length; x += R(3f, 5f))
            Primitive(PrimitiveType.Sphere, "CloudFront", scenery, new Vector3(x, -0.25f, def.laneMin - R(1.6f, 2.6f)), new Vector3(R(2f, 3.2f), 0.7f, 1.4f), cloud);
        // Hàng cột rồng vàng sau con đường
        for (float x = 0f; x < def.Length; x += 6f)
        {
            var p = new Vector3(x, 0f, def.laneMax + 1.6f);
            Primitive(PrimitiveType.Cylinder, "DragonPillar", scenery, p + Vector3.up * 2.4f, new Vector3(0.55f, 2.4f, 0.55f), red);
            for (int k = 0; k < 4; k++)   // thân rồng vàng quấn quanh cột
                Box("PillarDragon", scenery, p + new Vector3(0f, 0.8f + k * 1.1f, 0f), new Vector3(0.7f, 0.18f, 0.7f), gold, 1f, euler: new Vector3(0f, k * 45f, 12f));
            Primitive(PrimitiveType.Cylinder, "PillarCap", scenery, p + Vector3.up * 4.9f, new Vector3(0.9f, 0.12f, 0.9f), gold);
        }
        // Cổng Nam Thiên Môn: hai trụ, mái ngọc ba tầng, biển vàng
        for (float x = R(12f, 18f); x < def.Length; x += R(40f, 50f))
        {
            var p = new Vector3(x, 0f, def.laneMax + 6f);
            foreach (float dx in new[] { -3f, 3f })
                Primitive(PrimitiveType.Cylinder, "GatePillar", scenery, p + new Vector3(dx, 3f, 0f), new Vector3(0.9f, 3f, 0.9f), red);
            float y = 6f;
            for (int k = 0; k < 3; k++)
            {
                float w = 9f - k * 2.2f;
                Box("GateRoof", scenery, p + Vector3.up * (y + 0.3f), new Vector3(w, 0.5f, 3f), jade, 1f, euler: new Vector3(-5f, 0f, 0f));
                foreach (float s in new[] { -1f, 1f })
                    Box("GateRoofCurl", scenery, p + new Vector3(s * (w / 2f + 0.3f), y + 0.6f, -1.2f), new Vector3(0.6f, 0.2f, 0.2f), gold, 1f, euler: new Vector3(0f, 0f, s * 28f));
                Box("GateBeam", scenery, p + Vector3.up * (y - 0.3f), new Vector3(w - 1.2f, 0.5f, 0.8f), red, 1f);
                y += 1.3f;
            }
            Box("GatePlaque", scenery, p + new Vector3(0f, 5.2f, -0.5f), new Vector3(2.4f, 0.8f, 0.1f), gold, 1f);
        }
        // Cầu vồng xa
        for (int i = 0; i < rainbow.Length; i++)
            for (int k = 0; k <= 16; k++)
            {
                float a = Mathf.PI * k / 16f, r = 40f - i * 1.2f;
                Box("Rainbow", scenery, new Vector3(def.Length * 0.5f + Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.6f - 4f, def.laneMax + 70f),
                    new Vector3(8f, 1.1f, 0.2f), Plain("Rainbow" + i, rainbow[i], rainbow[i] * 0.6f), 1f, euler: new Vector3(0f, 0f, Mathf.Rad2Deg * a - 90f));
            }
        // Đèn trời và sao
        for (float x = R(2f, 6f); x < def.Length; x += R(6f, 10f))
        {
            var l = Primitive(PrimitiveType.Cylinder, "SkyLantern", scenery, new Vector3(x, R(4f, 8f), def.laneMax + R(3f, 12f)), new Vector3(0.45f, 0.35f, 0.45f), lantern);
            l.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (rnd.NextDouble() < 0.4) ZodiacLights.AddPoint(l, new Color(1f, 0.65f, 0.3f), 5f, 1.2f);
        }
        for (int k = 0; k < 60; k++)
        {
            var s = Primitive(PrimitiveType.Sphere, "Star", scenery, new Vector3(R(-30f, def.Length + 30f), R(14f, 40f), def.laneMax + R(60f, 110f)), Vector3.one * R(0.2f, 0.5f), star);
            s.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    /// <summary>Bẫy theo cảnh Hồi 4: đá vụn sắc (núi), mây sấm loé điện (Thiên Đình).</summary>
    static bool SkyHazardLook(GameObject go, float x, float zMin, float zMax)
    {
        var center = go.transform.position;
        float depth = zMax - zMin;
        if (def.backdrop == Backdrop.Heaven)
        {
            var storm = Plain("StormCloud", new Color(0.35f, 0.33f, 0.45f), new Color(0.08f, 0.08f, 0.15f));
            var bolt = Plain("Lightning", new Color(0.8f, 0.9f, 1f), new Color(0.6f, 0.8f, 1f) * 3f);
            for (float sx = -1f; sx <= 1f; sx += 0.5f)
                Primitive(PrimitiveType.Sphere, "StormPuff", go.transform, new Vector3(x + sx, 0.15f, center.z + R(-0.3f, 0.3f)), new Vector3(0.9f, 0.35f, depth * 0.6f), storm);
            for (int k = 0; k < 5; k++)
                Box("Bolt", go.transform, new Vector3(x + R(-1f, 1f), 0.35f, R(zMin + 0.2f, zMax - 0.2f)), new Vector3(0.05f, 0.5f, 0.05f), bolt, 1f,
                    euler: new Vector3(0f, 0f, R(-30f, 30f)));
            ZodiacLights.AddPoint(go, new Color(0.6f, 0.75f, 1f), 3.5f, 1.8f);
            return true;
        }
        var bed = Plain("Scree", Color.Lerp(def.stone, Color.black, 0.3f));
        var shard = Plain("SharpRock", Color.Lerp(def.stone, Color.white, 0.1f));
        Box("Bed", go.transform, center + Vector3.up * 0.03f, new Vector3(2.5f, 0.06f, depth), bed, 1f);
        for (float sx = -1f; sx <= 1f; sx += 0.45f)
            for (float sz = zMin + 0.3f; sz < zMax - 0.1f; sz += 0.55f)
                Box("SharpRock", go.transform, new Vector3(x + sx + R(-0.1f, 0.1f), 0.12f, sz), new Vector3(0.22f, 0.32f, 0.22f), shard, 1f,
                    euler: new Vector3(R(-20f, 20f), R(0f, 90f), R(-20f, 20f)));
        return true;
    }
}
