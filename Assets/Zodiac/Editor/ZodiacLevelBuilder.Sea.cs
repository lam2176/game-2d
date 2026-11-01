using UnityEngine;
using UnityEngine.Rendering;
using static ZodiacLevelData;

/// <summary>
/// Cảnh Hồi 3 — Biển Đông và Long Cung:
///   • Bãi biển (Cửa Biển, Biển Đông): bãi cát vàng, sóng bạc đầu vỗ bờ, thúng chai úp mạn, hàng dừa, cọc phơi lưới,
///     ghềnh đá; màn trời chiều có hải đăng tắt đèn trên mỏm đá. Bẫy: vũng nước có cầu gai.
///   • Long Cung: cung điện ngói ngọc dưới đáy biển, cột san hô đỏ, rừng rong, vỏ trai khổng lồ ôm ngọc phát sáng,
///     cột sáng rọi từ mặt biển xuống, bọt nước bay lên. Bẫy: bụi san hô gai.
/// </summary>
public static partial class ZodiacLevelBuilder
{
    // ───────────────────────────── Bãi biển ─────────────────────────────

    /// <summary>Màn hoàng hôn (trời ngả đỏ) thì có hải đăng trên mỏm đá.</summary>
    static bool Dusk => def.sky.r > def.sky.b;

    static void BuildBeach()
    {
        var t = ctx.textures;
        var sea = WaterMat("Sea", Color.Lerp(new Color(0.12f, 0.42f, 0.55f), def.sky, 0.2f), 0.12f);
        var foam = Plain("SeaFoam", new Color(0.95f, 0.97f, 1f), new Color(0.25f, 0.3f, 0.35f));
        var wetSand = Mat("WetSand", t.soil.texture, Color.Lerp(def.soil, new Color(0.45f, 0.42f, 0.38f), 0.35f), 0.5f);
        var basket = Mat("BasketBoat", t.wood.texture, new Color(0.45f, 0.32f, 0.2f));
        var tar = Plain("BoatTar", new Color(0.15f, 0.12f, 0.1f));
        var palmTrunk = Mat("PalmTrunk", t.wood.texture, new Color(0.55f, 0.42f, 0.3f));
        var palmLeaf = Plain("PalmLeaf", Color.Lerp(def.grass, new Color(0.25f, 0.5f, 0.2f), 0.5f));
        var coconut = Plain("Coconut", new Color(0.35f, 0.28f, 0.15f));
        var pole = Plain("NetPole", new Color(0.4f, 0.3f, 0.2f));
        var net = Plain("FishNet", new Color(0.35f, 0.32f, 0.28f));
        var rock = Mat("SeaRock", t.cobble.texture, Color.Lerp(def.stone, Color.black, 0.15f));

        // Mép nước ướt sau con đường, rồi biển mênh mông
        Box("WetSand", scenery, new Vector3(def.Length / 2f, 0.005f, def.laneMax + 3.2f), new Vector3(def.Length + 60f, 0.02f, 4f), wetSand, 4f);
        Box("SeaSurface", scenery, new Vector3(def.Length / 2f, 0.01f, def.laneMax + 44f), new Vector3(def.Length + 160f, 0.02f, 80f), sea, 6f);
        // Sóng bạc đầu: các dải bọt nối tiếp, càng xa càng mảnh
        for (int row = 0; row < 6; row++)
            for (float x = -20f; x < def.Length + 20f; x += R(4f, 8f))
                Box("WaveFoam", scenery, new Vector3(x, 0.03f + row * 0.01f, def.laneMax + 5.4f + row * 3.2f + R(-0.4f, 0.4f)),
                    new Vector3(R(2.5f, 5f) - row * 0.3f, 0.04f, 0.25f - row * 0.02f), foam, 1f);

        // Thúng chai úp trên cát và trôi ngoài khơi (thuyền thúng — đặc sản miền biển)
        for (float x = R(2f, 6f); x < def.Length; x += R(7f, 12f))
        {
            bool afloat = rnd.NextDouble() < 0.4;
            var p = new Vector3(x, afloat ? 0.05f : 0f, def.laneMax + (afloat ? R(9f, 20f) : R(1.8f, 3.6f)));
            var hull = Primitive(PrimitiveType.Sphere, "BasketBoat", scenery, p + Vector3.up * (afloat ? 0.2f : 0.35f),
                                 new Vector3(1.8f, afloat ? 0.6f : 0.75f, 1.8f), basket);
            if (!afloat) hull.transform.rotation = Quaternion.Euler(R(150f, 170f), R(0f, 360f), 0f);   // úp xuống phơi
            Primitive(PrimitiveType.Cylinder, "BoatRim", scenery, p + Vector3.up * (afloat ? 0.42f : 0.05f), new Vector3(1.85f, 0.03f, 1.85f), tar);
        }
        // Cọc phơi lưới
        for (float x = R(8f, 14f); x < def.Length; x += R(18f, 26f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(1.6f, 2.4f));
            Box("NetPoleL", scenery, p + new Vector3(-1.2f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.12f), pole, 1f);
            Box("NetPoleR", scenery, p + new Vector3(1.2f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.12f), pole, 1f);
            Box("Net", scenery, p + new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 1.4f, 0.02f), net, 1f, euler: new Vector3(0f, 0f, R(-3f, 3f)));
        }
        // Hàng dừa nghiêng ra biển (phía sau, và thưa ở tiền cảnh)
        for (float x = R(-2f, 4f); x < def.Length + 6f; x += R(6f, 11f))
        {
            bool front = rnd.NextDouble() < 0.25;
            Palm(new Vector3(x, 0f, front ? def.laneMin - R(2f, 3f) : def.laneMax + R(1.4f, 2.2f)), palmTrunk, palmLeaf, coconut);
        }
        // Ghềnh đá ven bờ
        for (float x = R(0f, 6f); x < def.Length + 10f; x += R(9f, 15f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(5f, 9f));
            for (int k = 0; k < 3; k++)
            {
                float s = R(0.8f, 1.8f);
                Primitive(PrimitiveType.Sphere, "ShoreRock", scenery, p + new Vector3(R(-1.2f, 1.2f), s * 0.25f, R(-0.6f, 0.6f)), new Vector3(s * 1.3f, s * 0.8f, s), rock);
            }
        }
        if (Dusk) Lighthouse(rock);
    }

    static void Palm(Vector3 p, Material trunk, Material leaf, Material coconut)
    {
        float lean = R(8f, 18f) * (rnd.NextDouble() < 0.5 ? -1f : 1f);
        // Thân cong: ba đốt nghiêng dần
        Vector3 at = p;
        for (int k = 0; k < 3; k++)
        {
            Vector3 dir = Quaternion.Euler(0f, 0f, lean * k * 0.5f) * Vector3.up;
            Primitive(PrimitiveType.Cylinder, "PalmTrunk", scenery, at + dir * 0.9f, new Vector3(0.28f - k * 0.04f, 0.9f, 0.28f - k * 0.04f), trunk,
                      new Vector3(0f, 0f, lean * k * 0.5f));
            at += dir * 1.8f;
        }
        for (int k = 0; k < 7; k++)
        {
            float a = k * 360f / 7f + R(-10f, 10f);
            Vector3 d = Quaternion.Euler(0f, a, 0f) * Vector3.right;
            Box("PalmFrond", scenery, at + d * 1f + Vector3.down * 0.25f, new Vector3(2.2f, 0.04f, 0.45f), leaf, 1f,
                euler: new Vector3(0f, -a, -R(18f, 30f)));
        }
        for (int k = 0; k < 3; k++)
            Primitive(PrimitiveType.Sphere, "Coconut", scenery, at + new Vector3(R(-0.25f, 0.25f), -0.25f, R(-0.25f, 0.25f)), Vector3.one * 0.26f, coconut);
    }

    /// <summary>Hải đăng trên mỏm đá ngoài khơi — đèn đã tắt, chỉ còn đốm đỏ le lói (Hồ Tinh dập lửa).</summary>
    static void Lighthouse(Material rock)
    {
        var white = Plain("LighthouseWhite", new Color(0.92f, 0.9f, 0.86f));
        var red = Plain("LighthouseRed", new Color(0.7f, 0.15f, 0.12f));
        var ember = Plain("LighthouseEmber", new Color(1f, 0.4f, 0.2f), new Color(1f, 0.3f, 0.1f) * 1.5f);
        for (float x = R(20f, 35f); x < def.Length + 10f; x += R(60f, 80f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(24f, 30f));
            Primitive(PrimitiveType.Sphere, "LighthouseRock", scenery, p + Vector3.up * 0.6f, new Vector3(9f, 3f, 6f), rock);
            for (int k = 0; k < 5; k++)
                Primitive(PrimitiveType.Cylinder, "LighthouseBand", scenery, p + Vector3.up * (2.2f + k * 1.6f), new Vector3(1.8f - k * 0.12f, 0.8f, 1.8f - k * 0.12f),
                          k % 2 == 0 ? white : red);
            var lamp = Primitive(PrimitiveType.Sphere, "LighthouseLamp", scenery, p + Vector3.up * 10.4f, Vector3.one * 1.1f, ember);
            ZodiacLights.AddPoint(lamp, new Color(1f, 0.45f, 0.25f), 10f, 1.6f);
            Primitive(PrimitiveType.Cylinder, "LighthouseCap", scenery, p + Vector3.up * 11.2f, new Vector3(1.6f, 0.25f, 1.6f), red);
        }
    }

    // ───────────────────────────── Long Cung ─────────────────────────────

    static void BuildDragonPalace()
    {
        var t = ctx.textures;
        var jade = Mat("JadeWall", t.brick.texture, new Color(0.3f, 0.6f, 0.58f));
        var jadeRoof = Mat("JadeRoof", t.roof.texture, new Color(0.2f, 0.55f, 0.6f));
        var gold = Plain("PalaceGold", new Color(0.95f, 0.78f, 0.3f), new Color(0.3f, 0.22f, 0.05f));
        var coralRed = Plain("CoralRed", new Color(0.9f, 0.3f, 0.3f), new Color(0.25f, 0.05f, 0.05f));
        Material[] coral =
        {
            coralRed,
            Plain("CoralPink", new Color(1f, 0.55f, 0.65f), new Color(0.25f, 0.08f, 0.12f)),
            Plain("CoralOrange", new Color(1f, 0.6f, 0.25f), new Color(0.25f, 0.12f, 0.02f)),
            Plain("CoralViolet", new Color(0.6f, 0.4f, 0.9f), new Color(0.12f, 0.06f, 0.2f)),
        };
        var kelp = Plain("Kelp", new Color(0.2f, 0.5f, 0.3f));
        var shell = Plain("ClamShell", new Color(0.85f, 0.8f, 0.9f));
        var pearl = Plain("Pearl", new Color(0.95f, 0.95f, 1f), new Color(0.6f, 0.75f, 0.9f) * 2f);
        var sand = Mat("SeaBedSand", t.soil.texture, def.soil);

        // Đáy cát nhấp nhô phía sau
        for (float x = -10f; x < def.Length + 10f; x += R(6f, 10f))
            Primitive(PrimitiveType.Sphere, "Dune", scenery, new Vector3(x, -0.3f, def.laneMax + R(3f, 12f)), new Vector3(R(6f, 10f), R(0.8f, 1.4f), R(3f, 5f)), sand);
        // Tường cung ngói ngọc, cột san hô đỏ, mái cong viền vàng
        float wz = def.laneMax + 7f, wallH = 3.6f;
        Box("PalaceWall", scenery, new Vector3(def.Length / 2f, wallH / 2f, wz), new Vector3(def.Length + 60f, wallH, 1.6f), jade, 2f);
        Box("PalaceEave", scenery, new Vector3(def.Length / 2f, wallH + 0.25f, wz - 0.4f), new Vector3(def.Length + 60f, 0.4f, 2.6f), jadeRoof, 2f, euler: new Vector3(-8f, 0f, 0f));
        Box("PalaceGoldTrim", scenery, new Vector3(def.Length / 2f, wallH + 0.02f, wz - 1.5f), new Vector3(def.Length + 60f, 0.12f, 0.12f), gold, 1f);
        for (float x = -26f; x < def.Length + 28f; x += 4f)
            Primitive(PrimitiveType.Cylinder, "CoralPillar", scenery, new Vector3(x, wallH / 2f, wz - 1.1f), new Vector3(0.45f, wallH / 2f, 0.45f), coralRed);
        // Điện chính nhô cao từng quãng: ba tầng mái, ngọc lớn trên nóc
        for (float x = R(10f, 18f); x < def.Length + 10f; x += R(32f, 42f))
        {
            var p = new Vector3(x, 0f, wz + 1.5f);
            float y = 0f;
            for (int k = 0; k < 3; k++)
            {
                float w = 8f - k * 2f, h = k == 0 ? 4.6f : 2f;
                Box("HallTier", scenery, p + Vector3.up * (y + h / 2f), new Vector3(w, h, 3f), jade, 1.5f);
                Box("HallRoof", scenery, p + Vector3.up * (y + h + 0.2f), new Vector3(w + 1.8f, 0.4f, 3.8f), jadeRoof, 1f, euler: new Vector3(-6f, 0f, 0f));
                foreach (float s in new[] { -1f, 1f })
                    Box("HallRoofCurl", scenery, p + new Vector3(s * (w / 2f + 1f), y + h + 0.45f, -1.6f), new Vector3(0.5f, 0.2f, 0.2f), gold, 1f,
                        euler: new Vector3(0f, 0f, s * 28f));
                y += h + 0.4f;
            }
            var orb = Primitive(PrimitiveType.Sphere, "DragonPearl", scenery, p + Vector3.up * (y + 0.7f), Vector3.one * 1.2f, pearl);
            orb.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            ZodiacLights.AddPoint(orb, new Color(0.6f, 0.85f, 1f), 14f, 2.2f);
        }
        // Rạn san hô giữa tường cung và con đường
        for (float x = R(-2f, 2f); x < def.Length + 4f; x += R(2.5f, 4.5f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(1.6f, 4.5f));
            var m = coral[rnd.Next(coral.Length)];
            int branches = rnd.Next(3, 6);
            for (int k = 0; k < branches; k++)
            {
                float h = R(0.6f, 1.6f);
                Box("CoralBranch", scenery, p + new Vector3(R(-0.4f, 0.4f), h / 2f, R(-0.3f, 0.3f)), new Vector3(0.12f, h, 0.12f), m, 1f,
                    euler: new Vector3(R(-20f, 20f), 0f, R(-25f, 25f)));
            }
            if (rnd.NextDouble() < 0.4)
                Primitive(PrimitiveType.Sphere, "BrainCoral", scenery, p + new Vector3(R(-0.8f, 0.8f), 0.25f, 0.4f), new Vector3(0.9f, 0.55f, 0.8f), coral[rnd.Next(coral.Length)]);
        }
        // Rừng rong đung đưa (đứng thẳng, hơi nghiêng) ở tiền cảnh và sau rạn
        for (float x = R(0f, 3f); x < def.Length; x += R(2f, 4f))
        {
            bool front = rnd.NextDouble() < 0.35;
            var p = new Vector3(x, 0f, front ? def.laneMin - R(1.6f, 3f) : def.laneMax + R(4.5f, 6.2f));
            for (int k = 0; k < 4; k++)
            {
                float h = R(1.5f, front ? 2.4f : 4f);
                Box("KelpBlade", scenery, p + new Vector3(R(-0.3f, 0.3f), h / 2f, R(-0.2f, 0.2f)), new Vector3(0.18f, h, 0.03f), kelp, 1f,
                    euler: new Vector3(0f, R(0f, 180f), R(-10f, 10f)));
            }
        }
        // Vỏ trai khổng lồ hé miệng, ngọc trai phát sáng
        for (float x = R(6f, 10f); x < def.Length; x += R(14f, 20f))
        {
            var p = new Vector3(x, 0f, def.laneMin - R(1.8f, 2.6f));
            Primitive(PrimitiveType.Sphere, "ClamBottom", scenery, p + Vector3.up * 0.2f, new Vector3(1.4f, 0.4f, 1.1f), shell);
            Primitive(PrimitiveType.Sphere, "ClamTop", scenery, p + new Vector3(0f, 0.55f, 0.25f), new Vector3(1.4f, 0.4f, 1.1f), shell, new Vector3(-35f, 0f, 0f));
            var pe = Primitive(PrimitiveType.Sphere, "ClamPearl", scenery, p + Vector3.up * 0.45f, Vector3.one * 0.38f, pearl);
            ZodiacLights.AddPoint(pe, new Color(0.7f, 0.9f, 1f), 4f, 1.8f);
        }
        // Cột sáng rọi từ mặt biển
        for (float x = R(3f, 8f); x < def.Length; x += R(10f, 16f))
        {
            var shaft = Card("SeaShaft", scenery, ctx.textures.fog, new Vector3(x, 6f, def.laneMax + 1.5f), new Vector2(2.6f, 14f),
                             new Color(0.7f, 0.95f, 1f, 0.3f), tiled: false);
            shaft.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
            shaft.transform.rotation = Quaternion.Euler(0f, 0f, R(-12f, 12f));
        }
    }

    /// <summary>Bẫy theo cảnh biển: vũng nước có cầu gai đen (bãi biển), bụi san hô gai (Long Cung).</summary>
    static bool SeaHazardLook(GameObject go, float x, float zMin, float zMax)
    {
        var center = go.transform.position;
        float depth = zMax - zMin;
        bool palace = def.backdrop == Backdrop.DragonPalace;
        var bed = palace ? Plain("CoralBed", new Color(0.5f, 0.2f, 0.25f)) : WaterMat("TidePool", new Color(0.15f, 0.35f, 0.42f), 0.1f);
        var spike = palace ? Plain("CoralSpike", new Color(1f, 0.45f, 0.55f), new Color(0.4f, 0.1f, 0.15f))
                           : Plain("UrchinSpike", new Color(0.12f, 0.08f, 0.15f));
        Box("Bed", go.transform, center + Vector3.up * 0.02f, new Vector3(2.5f, 0.04f, depth), bed, 1f);
        for (float sx = -1f; sx <= 1f; sx += 0.5f)
            for (float sz = zMin + 0.35f; sz < zMax - 0.1f; sz += 0.6f)
            {
                var p = new Vector3(x + sx + R(-0.1f, 0.1f), 0.1f, sz);
                if (palace)
                    for (int k = 0; k < 3; k++)
                        Box("CoralSpike", go.transform, p + Vector3.up * 0.15f, new Vector3(0.06f, 0.45f, 0.06f), spike, 1f,
                            euler: new Vector3(R(-30f, 30f), 0f, R(-30f, 30f)));
                else
                {
                    Primitive(PrimitiveType.Sphere, "Urchin", go.transform, p, Vector3.one * 0.22f, spike);
                    for (int k = 0; k < 4; k++)
                        Box("UrchinSpine", go.transform, p, new Vector3(0.03f, 0.42f, 0.03f), spike, 1f, euler: new Vector3(R(0f, 180f), R(0f, 180f), 0f));
                }
            }
        return true;
    }
}
