using UnityEngine;
using UnityEngine.Rendering;
using static ZodiacLevelData;

/// <summary>
/// Cảnh riêng của các màn mới — mỗi màn một bộ cảnh và một nét chơi riêng thay vì chỉ đổi màu:
///   • Cánh Đồng Lúa: ruộng bậc vàng, ruộng nước loáng, đống rơm, bù nhìn, cây đa và miếu nhỏ, cò trắng; bẫy rơm cháy.
///   • Đầm Sen: cầu ván tre bắc qua mặt nước đêm, lá sen, hoa sen, lau sậy, thuỷ đình xa, trăng; bẫy ván gãy nước xoáy.
///   • Chợ Phiên Ma: dãy sạp mái vải sọc, hàng hoá, nhà ống, dây đèn lồng đỏ; nhiều thúng, hũ đập vỡ; bẫy bếp than hàng nướng.
///   • Hang Đá Vôi: nhũ đá rủ từ trần, cột sáng lọt qua lỗ trần, suối ngầm, ruộng bậc thạch nhũ; nhũ đá rơi (<see cref="FallingStalactite"/>).
///   • Bờ Hồ Tây: kè đá lan can, mặt hồ mênh mông, vệt trăng, bảo tháp trên đảo, thuyền đèn, liễu rủ, đèn đá.
/// Các phần chung (nền, ánh sáng gốc, nhạc) theo kiểu cảnh gốc <see cref="Base"/>.
/// </summary>
public static partial class ZodiacLevelBuilder
{
    /// <summary>Kiểu cảnh gốc để dùng chung nhạc, bước chân, nền xa…</summary>
    static Backdrop Base(Backdrop b) => b switch
    {
        Backdrop.Field or Backdrop.Market or Backdrop.Lakeshore => Backdrop.Village,
        Backdrop.Pond => Backdrop.Bamboo,
        Backdrop.Limestone => Backdrop.Cave,
        _ => b,
    };

    static bool IsNight => def.backdrop == Backdrop.Pond || def.backdrop == Backdrop.Lakeshore;

    // ───────────────────────────── Cánh Đồng Lúa ─────────────────────────────

    static void BuildField()
    {
        var t = ctx.textures;
        var ripe = Mat("RipeRice", t.turf.texture, new Color(0.92f, 0.76f, 0.32f));
        var green = Mat("YoungRice", t.turf.texture, new Color(0.62f, 0.74f, 0.3f));
        var paddyWater = Mat("PaddyWater", null, new Color(0.55f, 0.66f, 0.72f), 0.92f, new Color(0.08f, 0.08f, 0.06f));
        var dike = Mat("Dike", t.soil.texture, def.soil);
        var hay = Mat("Hay", t.wood.texture, new Color(0.93f, 0.78f, 0.42f));
        var stick = Plain("Stick", new Color(0.45f, 0.32f, 0.2f));
        var cloth = Plain("ScarecrowCloth", new Color(0.55f, 0.22f, 0.15f));
        var hat = Plain("ConicalHat", new Color(0.9f, 0.82f, 0.6f));
        var trunk = Plain("BanyanTrunk", new Color(0.38f, 0.3f, 0.22f));
        var canopy = Mat("BanyanLeaves", t.turf.texture, new Color(0.3f, 0.48f, 0.22f));
        var stork = Plain("Stork", new Color(0.97f, 0.97f, 0.95f));
        var shrine = Plain("ShrineWall", new Color(0.88f, 0.82f, 0.7f));
        var shrineRoof = Mat("ShrineRoof", t.roof.texture, new Color(0.6f, 0.3f, 0.2f));

        // Ruộng phía sau: các thửa xen lúa chín / lúa non / ruộng nước loáng nắng, ngăn bởi bờ ruộng
        float depth0 = def.laneMax + 1.6f;
        for (int row = 0; row < 6; row++)
        {
            float z0 = depth0 + row * 5f;
            for (float x = -14f; x < def.Length + 14f;)
            {
                float w = R(7f, 13f);
                double roll = rnd.NextDouble();
                var mat = roll < 0.55 ? ripe : roll < 0.8 ? green : paddyWater;
                float h = mat == paddyWater ? 0.03f : R(0.45f, 0.7f) - row * 0.04f;
                Box("Paddy", scenery, new Vector3(x + w / 2f, h / 2f, z0 + 2.5f), new Vector3(w - 0.4f, h, 4.6f), mat, 2f);
                Box("DikeX", scenery, new Vector3(x + w, 0.08f, z0 + 2.5f), new Vector3(0.4f, 0.16f, 5f), dike, 1f);
                if (mat == paddyWater && rnd.NextDouble() < 0.6) Stork(new Vector3(x + w / 2f + R(-2f, 2f), 0f, z0 + R(1f, 4f)), stork, stick);
                x += w;
            }
            Box("DikeZ", scenery, new Vector3(def.Length / 2f, 0.08f, z0), new Vector3(def.Length + 40f, 0.16f, 0.4f), dike, 2f);
        }
        // Đống rơm và bù nhìn rải trên ruộng
        for (float x = R(-4f, 4f); x < def.Length + 8f; x += R(6f, 11f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(2.5f, 14f));
            if (rnd.NextDouble() < 0.6) Haystack(p, hay, stick);
            else Scarecrow(p, stick, cloth, hat);
        }
        // Cây đa cổ thụ với miếu nhỏ dưới gốc, cách quãng
        for (float x = R(10f, 20f); x < def.Length; x += R(30f, 45f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(5f, 8f));
            Primitive(PrimitiveType.Cylinder, "BanyanTrunk", scenery, p + Vector3.up * 2f, new Vector3(1.4f, 2f, 1.4f), trunk);
            for (int k = 0; k < 6; k++)
            {
                float s = R(3.2f, 4.6f);
                Primitive(PrimitiveType.Sphere, "BanyanCanopy", scenery, p + new Vector3(R(-3f, 3f), R(4.2f, 5.6f), R(-2f, 2f)), new Vector3(s, s * 0.7f, s), canopy);
            }
            for (int k = 0; k < 5; k++)   // rễ phụ thõng xuống
                Box("AerialRoot", scenery, p + new Vector3(R(-2.6f, 2.6f), 2.3f, R(-1.5f, 1.5f)), new Vector3(0.07f, 3.4f, 0.07f), trunk, 1f);
            var s0 = p + new Vector3(2.2f, 0f, -1.2f);
            Box("Shrine", scenery, s0 + Vector3.up * 0.5f, new Vector3(0.9f, 1f, 0.8f), shrine, 1f);
            Box("ShrineRoof", scenery, s0 + Vector3.up * 1.12f, new Vector3(1.2f, 0.18f, 1.1f), shrineRoof, 1f);
            var incense = Primitive(PrimitiveType.Sphere, "Incense", scenery, s0 + new Vector3(0f, 0.55f, -0.42f), Vector3.one * 0.12f,
                                    Plain("IncenseGlow", new Color(1f, 0.5f, 0.2f), new Color(1f, 0.4f, 0.1f) * 2f));
            ZodiacLights.AddPoint(incense, new Color(1f, 0.6f, 0.3f), 2.5f, 1.2f);
        }
        // Tiền cảnh: khóm lúa thấp ven đường (không che nhân vật)
        for (float x = R(1f, 4f); x < def.Length; x += R(3f, 6f))
        {
            var p = new Vector3(x, 0f, def.laneMin - R(1.4f, 3f));
            for (int k = 0; k < 7; k++)
                Box("RiceStalk", scenery, p + new Vector3(R(-0.4f, 0.4f), 0.35f, R(-0.3f, 0.3f)), new Vector3(0.05f, 0.7f, 0.05f), ripe, 1f,
                    euler: new Vector3(R(-12f, 12f), 0f, R(-15f, 15f)));
        }
    }

    static void Haystack(Vector3 p, Material hay, Material stick)
    {
        Primitive(PrimitiveType.Cylinder, "Haystack", scenery, p + Vector3.up * 0.7f, new Vector3(1.5f, 0.7f, 1.5f), hay);
        Primitive(PrimitiveType.Sphere, "HayTop", scenery, p + Vector3.up * 1.45f, new Vector3(1.5f, 1.1f, 1.5f), hay);
        Box("HayPole", scenery, p + Vector3.up * 1.9f, new Vector3(0.08f, 1f, 0.08f), stick, 1f);
    }

    static void Scarecrow(Vector3 p, Material stick, Material cloth, Material hat)
    {
        Box("Pole", scenery, p + Vector3.up * 1f, new Vector3(0.1f, 2f, 0.1f), stick, 1f);
        Box("Arms", scenery, p + Vector3.up * 1.45f, new Vector3(1.4f, 0.08f, 0.08f), stick, 1f, euler: new Vector3(0f, 0f, R(-8f, 8f)));
        Box("Shirt", scenery, p + Vector3.up * 1.2f, new Vector3(0.6f, 0.7f, 0.25f), cloth, 1f);
        Primitive(PrimitiveType.Sphere, "Head", scenery, p + Vector3.up * 1.8f, Vector3.one * 0.32f, Plain("StrawHead", new Color(0.9f, 0.78f, 0.45f)));
        Primitive(PrimitiveType.Cylinder, "Hat", scenery, p + Vector3.up * 1.98f, new Vector3(0.75f, 0.06f, 0.75f), hat);
        Primitive(PrimitiveType.Sphere, "HatTip", scenery, p + Vector3.up * 2.06f, new Vector3(0.4f, 0.22f, 0.4f), hat);
    }

    static void Stork(Vector3 p, Material white, Material leg)
    {
        Primitive(PrimitiveType.Sphere, "StorkBody", scenery, p + Vector3.up * 0.75f, new Vector3(0.5f, 0.32f, 0.28f), white);
        Box("StorkNeck", scenery, p + new Vector3(0.22f, 1.0f, 0f), new Vector3(0.06f, 0.4f, 0.06f), white, 1f, euler: new Vector3(0f, 0f, -20f));
        Box("StorkLeg", scenery, p + Vector3.up * 0.3f, new Vector3(0.03f, 0.6f, 0.03f), leg, 1f);
    }

    // ───────────────────────────── Đầm Sen ─────────────────────────────

    static Material WaterMat(string key, Color color, float glow = 0.05f) =>
        Mat(key, null, color, 0.95f, color * glow);

    static void BuildPond()
    {
        var t = ctx.textures;
        var water = WaterMat("PondWater", new Color(0.1f, 0.2f, 0.26f), 0.12f);
        var pad = Plain("LilyPad", new Color(0.2f, 0.45f, 0.25f));
        var petal = Plain("Lotus", new Color(1f, 0.6f, 0.75f), new Color(0.35f, 0.12f, 0.2f));
        var reed = Plain("Reed", new Color(0.45f, 0.5f, 0.3f));
        var post = Plain("BridgePost", new Color(0.35f, 0.26f, 0.18f));
        var rope = Plain("Rope", new Color(0.6f, 0.5f, 0.35f));
        var lantern = Plain("PondLantern", new Color(1f, 0.6f, 0.3f), new Color(1f, 0.5f, 0.2f) * 2.2f);
        var wall = Mat("PavilionWall", t.wood.texture, new Color(0.55f, 0.4f, 0.28f));
        var roof = Mat("PavilionRoof", t.roof.texture, new Color(0.35f, 0.22f, 0.2f));

        // Mặt nước phủ kín hai bên cầu (nhô trên nền một chút)
        float backZ = def.laneMax + 0.7f, frontZ = def.laneMin - 0.7f;
        Box("PondBack", scenery, new Vector3(def.Length / 2f, 0.01f, (backZ + 40f) / 2f), new Vector3(def.Length + 80f, 0.02f, 40f - backZ), water, 4f);
        Box("PondFront", scenery, new Vector3(def.Length / 2f, 0.01f, (frontZ - 12f) / 2f), new Vector3(def.Length + 80f, 0.02f, frontZ + 12f), water, 4f);
        // Cột cầu và dây chão hai bên mép cầu, đèn lồng treo trên cột
        for (float x = 0f; x < def.Length; x += 3f)
            foreach (float z in new[] { def.laneMin - 0.8f, def.laneMax + 0.8f })
            {
                Box("BridgePost", scenery, new Vector3(x, 0.3f, z), new Vector3(0.18f, 1.2f, 0.18f), post, 1f);
                if (Mathf.Repeat(x, 9f) < 0.1f && z > LaneCenter)
                {
                    var l = Primitive(PrimitiveType.Sphere, "Lantern", scenery, new Vector3(x, 1.15f, z), new Vector3(0.32f, 0.4f, 0.32f), lantern);
                    l.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    ZodiacLights.AddPoint(l, new Color(1f, 0.65f, 0.35f), 6f, 2f);
                }
            }
        foreach (float z in new[] { def.laneMin - 0.8f, def.laneMax + 0.8f })
            Box("Rope", scenery, new Vector3(def.Length / 2f, 0.7f, z), new Vector3(def.Length, 0.04f, 0.04f), rope, 1f);
        // Lá sen, hoa sen trước và sau cầu
        for (float x = -8f; x < def.Length + 8f; x += R(0.9f, 2f))
            foreach (bool back in new[] { true, false })
            {
                if (!back && rnd.NextDouble() < 0.5) continue;
                float z = back ? def.laneMax + R(1.6f, 12f) : def.laneMin - R(1.5f, 3.5f);
                float s = R(0.6f, 1.2f);
                Primitive(PrimitiveType.Cylinder, "LilyPad", scenery, new Vector3(x, 0.04f, z), new Vector3(s, 0.01f, s), pad);
                if (rnd.NextDouble() < 0.3)
                {
                    float h = R(0.4f, 1.1f);
                    Box("LotusStem", scenery, new Vector3(x, h / 2f, z), new Vector3(0.04f, h, 0.04f), pad, 1f);
                    Primitive(PrimitiveType.Sphere, "LotusFlower", scenery, new Vector3(x, h + 0.1f, z), new Vector3(0.34f, 0.26f, 0.34f), petal);
                }
            }
        // Lau sậy thành từng khóm
        for (float x = R(-6f, 0f); x < def.Length + 8f; x += R(4f, 8f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(3f, 9f));
            for (int k = 0; k < 9; k++)
            {
                float h = R(1.4f, 2.6f);
                Box("Reed", scenery, p + new Vector3(R(-0.6f, 0.6f), h / 2f, R(-0.4f, 0.4f)), new Vector3(0.05f, h, 0.05f), reed, 1f,
                    euler: new Vector3(R(-8f, 8f), 0f, R(-10f, 10f)));
            }
        }
        // Thuỷ đình trên cọc ở xa, đèn hắt xuống mặt nước
        for (float x = R(12f, 25f); x < def.Length; x += R(40f, 55f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(16f, 22f));
            for (int k = 0; k < 4; k++)
                Box("Stilt", scenery, p + new Vector3(k % 2 == 0 ? -2f : 2f, 0.6f, k < 2 ? -1.5f : 1.5f), new Vector3(0.25f, 1.2f, 0.25f), post, 1f);
            Box("Deck", scenery, p + Vector3.up * 1.25f, new Vector3(5f, 0.15f, 3.6f), wall, 1f);
            Box("PavilionWall", scenery, p + Vector3.up * 2.1f, new Vector3(3.6f, 1.6f, 2.6f), wall, 1f);
            Box("PavilionRoof", scenery, p + Vector3.up * 3.15f, new Vector3(5.4f, 0.3f, 4f), roof, 1f);
            Box("PavilionRoofTop", scenery, p + Vector3.up * 3.55f, new Vector3(3.4f, 0.5f, 2.4f), roof, 1f);
            var glow = Box("PavilionWindow", scenery, p + new Vector3(0f, 2.1f, -1.32f), new Vector3(1.4f, 0.8f, 0.05f), lantern, 1f);
            ZodiacLights.AddPoint(glow, new Color(1f, 0.7f, 0.4f), 10f, 2.4f);
        }
    }

    // ───────────────────────────── Chợ Phiên Ma ─────────────────────────────

    static void BuildMarket()
    {
        var t = ctx.textures;
        var post = Plain("StallPost", new Color(0.4f, 0.28f, 0.18f));
        var table = Mat("StallTable", t.wood.texture, new Color(0.62f, 0.45f, 0.3f));
        Color[] awnings = { new Color(0.75f, 0.15f, 0.12f), new Color(0.2f, 0.35f, 0.65f), new Color(0.9f, 0.7f, 0.2f), new Color(0.25f, 0.55f, 0.35f) };
        var cream = Plain("AwningStripe", new Color(0.95f, 0.9f, 0.8f));
        var basket = Mat("Basket", t.wood.texture, new Color(0.7f, 0.55f, 0.32f));
        var clay = Plain("Pot", new Color(0.55f, 0.32f, 0.2f));
        Color[] fruitColors = { new Color(1f, 0.55f, 0.15f), new Color(0.4f, 0.65f, 0.2f), new Color(0.85f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.3f) };
        var plaster = Mat("ShopPlaster", t.brick.texture, new Color(0.85f, 0.78f, 0.68f));
        var roof = Mat("ShopRoof", t.roof.texture, new Color(0.5f, 0.28f, 0.22f));
        var dark = Plain("ShopDoor", new Color(0.14f, 0.09f, 0.07f));
        var lanternRed = Plain("RedLantern", new Color(0.95f, 0.2f, 0.12f), new Color(0.9f, 0.15f, 0.05f) * 2.2f);
        var wire = Plain("Wire", new Color(0.2f, 0.15f, 0.12f));
        var banner = Plain("Banner", new Color(0.7f, 0.1f, 0.08f));

        // Dãy sạp ngay sau mép đường: 4 cột, mái vải sọc nghiêng, mặt bàn bày hàng
        for (float x = -4f; x < def.Length + 6f; x += R(4.2f, 5.5f))
        {
            var p = new Vector3(x, 0f, def.laneMax + 2.4f);
            var awning = Plain("Awning" + rnd.Next(awnings.Length), awnings[rnd.Next(awnings.Length)]);
            foreach (var o in new[] { new Vector3(-1.6f, 0f, -0.9f), new Vector3(1.6f, 0f, -0.9f), new Vector3(-1.6f, 0f, 0.9f), new Vector3(1.6f, 0f, 0.9f) })
                Box("StallPost", scenery, p + o + Vector3.up * (o.z < 0 ? 1.1f : 1.4f), new Vector3(0.1f, o.z < 0 ? 2.2f : 2.8f, 0.1f), post, 1f);
            for (int k = 0; k < 4; k++)
                Box("Awning", scenery, p + new Vector3(-1.35f + k * 0.9f, 2.5f, 0f), new Vector3(0.9f, 0.06f, 2.3f), k % 2 == 0 ? awning : cream, 1f,
                    euler: new Vector3(-14f, 0f, 0f));
            Box("Table", scenery, p + new Vector3(0f, 0.75f, -0.2f), new Vector3(3.2f, 0.1f, 1.3f), table, 1f);
            Box("TableFront", scenery, p + new Vector3(0f, 0.38f, -0.84f), new Vector3(3.2f, 0.74f, 0.05f), table, 1f);
            // Hàng hoá: thúng trái cây, chum vại, cuộn vải
            for (int k = 0; k < 3; k++)
            {
                var g = p + new Vector3(-1.05f + k * 1.05f, 0.8f, -0.25f);
                switch (rnd.Next(3))
                {
                    case 0:
                        Primitive(PrimitiveType.Cylinder, "Basket", scenery, g + Vector3.up * 0.12f, new Vector3(0.75f, 0.12f, 0.75f), basket);
                        var fruit = Plain("Fruit" + k, fruitColors[rnd.Next(fruitColors.Length)]);
                        for (int f = 0; f < 5; f++)
                            Primitive(PrimitiveType.Sphere, "Fruit", scenery, g + new Vector3(R(-0.22f, 0.22f), 0.3f, R(-0.22f, 0.22f)), Vector3.one * 0.2f, fruit);
                        break;
                    case 1:
                        Primitive(PrimitiveType.Sphere, "Jar", scenery, g + Vector3.up * 0.28f, new Vector3(0.5f, 0.56f, 0.5f), clay);
                        break;
                    default:
                        Primitive(PrimitiveType.Cylinder, "ClothRoll", scenery, g + Vector3.up * 0.15f, new Vector3(0.28f, 0.4f, 0.28f),
                                  Plain("Cloth" + k, awnings[rnd.Next(awnings.Length)]), new Vector3(0f, 0f, 90f));
                        break;
                }
            }
        }
        // Nhà ống hai tầng phía sau dãy sạp
        for (float x = -6f; x < def.Length + 8f;)
        {
            float w = R(3.6f, 5f), h = R(4.5f, 6.5f);
            var p = new Vector3(x + w / 2f, 0f, def.laneMax + R(6f, 7f));
            Box("ShopHouse", scenery, p + Vector3.up * h / 2f, new Vector3(w - 0.2f, h, 4f), plaster, 1.5f);
            Box("ShopRoof", scenery, p + Vector3.up * (h + 0.2f), new Vector3(w + 0.2f, 0.4f, 4.4f), roof, 1f);
            Box("ShopDoor", scenery, p + new Vector3(0f, 1.1f, -2.02f), new Vector3(w * 0.5f, 2.2f, 0.05f), dark, 1f);
            Box("ShopWindow", scenery, p + new Vector3(0f, h * 0.7f, -2.02f), new Vector3(w * 0.45f, 0.9f, 0.05f), dark, 1f);
            if (rnd.NextDouble() < 0.5)
                Box("Banner", scenery, p + new Vector3(w * 0.38f, h * 0.55f, -2.1f), new Vector3(0.45f, 1.8f, 0.04f), banner, 1f);
            x += w;
        }
        // Dây đèn lồng đỏ giăng ngang phía trên dãy sạp
        for (float x = -2f; x < def.Length + 4f; x += 12f)
        {
            Box("LanternWire", scenery, new Vector3(x + 6f, 3.6f, def.laneMax + 3.6f), new Vector3(12f, 0.03f, 0.03f), wire, 1f);
            for (float lx = x + 1f; lx < x + 12f; lx += 1.5f)
            {
                var l = Primitive(PrimitiveType.Sphere, "RedLantern", scenery, new Vector3(lx, 3.3f, def.laneMax + 3.6f), new Vector3(0.38f, 0.48f, 0.38f), lanternRed);
                l.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                if (Mathf.Repeat(lx - x, 4.5f) < 0.1f) ZodiacLights.AddPoint(l, new Color(1f, 0.35f, 0.2f), 7f, 1.8f);
            }
        }
        // Tiền cảnh: thúng, ghế đẩu đổ, chum vỡ
        for (float x = R(2f, 5f); x < def.Length; x += R(5f, 9f))
        {
            var p = new Vector3(x, 0f, def.laneMin - R(1.4f, 2.6f));
            if (rnd.NextDouble() < 0.5) Primitive(PrimitiveType.Cylinder, "Basket", scenery, p + Vector3.up * 0.2f, new Vector3(0.8f, 0.2f, 0.8f), basket);
            else Box("Stool", scenery, p + Vector3.up * 0.25f, new Vector3(0.5f, 0.5f, 0.5f), table, 1f, euler: new Vector3(0f, R(0f, 90f), 70f));
        }
    }

    // ───────────────────────────── Hang Đá Vôi ─────────────────────────────

    static void BuildLimestone()
    {
        var t = ctx.textures;
        var rock = Mat("Limestone", t.brick.texture, def.stone);
        var rockDark = Mat("LimestoneDark", t.soil.texture, Color.Lerp(def.stone, def.far, 0.5f));
        var drip = Plain("Dripstone", Color.Lerp(def.stone, Color.white, 0.25f));
        var stream = WaterMat("Stream", new Color(0.2f, 0.55f, 0.6f), 0.35f);
        var pool = WaterMat("RimPool", new Color(0.35f, 0.7f, 0.7f), 0.3f);

        // Vách sau và trần hang (trần cao — thấy nhũ đá rủ xuống)
        Box("CaveBack", scenery, new Vector3(def.Length / 2f, 8f, def.laneMax + 16f), new Vector3(def.Length + 60f, 20f, 2f), rockDark, 4f);
        Box("CaveCeiling", scenery, new Vector3(def.Length / 2f, 12.5f, 8f), new Vector3(def.Length + 60f, 1.5f, 24f), rockDark, 4f);
        // Suối ngầm chảy dọc phía sau con đường, bờ đá
        Box("Stream", scenery, new Vector3(def.Length / 2f, 0.01f, def.laneMax + 4f), new Vector3(def.Length + 60f, 0.02f, 3.2f), stream, 3f);
        for (float x = -8f; x < def.Length + 8f; x += R(1.5f, 3f))
            Primitive(PrimitiveType.Sphere, "Bank", scenery, new Vector3(x, 0.15f, def.laneMax + R(2.2f, 2.5f)), new Vector3(R(0.8f, 1.4f), 0.45f, 0.7f), rock);
        // Ruộng bậc thạch nhũ (bể nước bậc thang) bên kia suối
        for (float x = R(0f, 6f); x < def.Length + 6f; x += R(10f, 16f))
            for (int k = 0; k < 3; k++)
            {
                var p = new Vector3(x + k * 0.8f, 0.25f + k * 0.45f, def.laneMax + 7f + k * 1.4f);
                Primitive(PrimitiveType.Cylinder, "RimTerrace", scenery, p, new Vector3(3f - k * 0.5f, 0.25f, 1.8f), drip);
                Primitive(PrimitiveType.Cylinder, "RimPool", scenery, p + Vector3.up * 0.26f, new Vector3(2.6f - k * 0.5f, 0.01f, 1.5f), pool);
            }
        // Cột đá lớn và măng đá
        for (float x = -6f; x < def.Length + 8f; x += R(5f, 9f))
        {
            float h = R(7f, 11f);
            Box("Column", scenery, new Vector3(x, h / 2f, def.laneMax + R(9f, 13f)), new Vector3(R(1.2f, 2.2f), h, R(1.2f, 2f)), rock, 2f,
                euler: new Vector3(0f, R(0f, 45f), R(-3f, 3f)));
        }
        // Nhũ đá rủ từ trần (trang trí, cao hẳn trên đầu)
        for (float x = -6f; x < def.Length + 6f; x += R(0.8f, 1.8f))
        {
            float len = R(0.8f, 3f), z = R(def.laneMin - 2f, def.laneMax + 10f);
            Box("Stalactite", scenery, new Vector3(x, 11.8f - len / 2f, z), new Vector3(R(0.2f, 0.5f), len, R(0.2f, 0.5f)), drip, 1f,
                euler: new Vector3(0f, 45f, 0f));
        }
        for (float x = R(2f, 6f); x < def.Length; x += R(5f, 10f))
        {
            float s = R(0.5f, 1f);
            Box("Stalagmite", scenery, new Vector3(x, s, def.laneMin - R(1.6f, 3f)), new Vector3(s, s * 2f, s), drip, 1f, euler: new Vector3(0f, 45f, R(-6f, 6f)));
        }
        // Cột sáng lọt qua lỗ trần — vệt sương ấm đứng thẳng, có đèn hắt xuống mặt đường
        for (float x = R(4f, 10f); x < def.Length; x += R(14f, 20f))
        {
            var shaft = Card("LightShaft", scenery, ctx.textures.fog, new Vector3(x, 6f, def.laneMax + 1.2f), new Vector2(2.4f, 12f),
                             new Color(1f, 0.92f, 0.7f, 0.35f), tiled: false);
            shaft.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
            shaft.transform.rotation = Quaternion.Euler(0f, 0f, R(-8f, 8f));
            var spot = new GameObject("ShaftLight");
            spot.transform.SetParent(scenery, false);
            spot.transform.position = new Vector3(x, 3f, LaneCenter);
            ZodiacLights.AddPoint(spot, new Color(1f, 0.9f, 0.7f), 8f, 2.6f);
        }
    }

    // ───────────────────────────── Bờ Hồ Tây ─────────────────────────────

    static void BuildLakeshore()
    {
        var t = ctx.textures;
        var lake = WaterMat("Lake", new Color(0.08f, 0.14f, 0.24f), 0.1f);
        var moonlight = Mat("MoonStreak", null, new Color(0.95f, 0.92f, 0.75f), 0.9f, new Color(0.9f, 0.85f, 0.6f) * 1.4f);
        var stone = Mat("Balustrade", t.cobble.texture, Color.Lerp(def.stone, Color.white, 0.15f));
        var willowTrunk = Plain("WillowTrunk", new Color(0.3f, 0.25f, 0.2f));
        var willow = Plain("Willow", new Color(0.32f, 0.5f, 0.3f));
        var tower = Mat("PagodaWall", t.brick.texture, new Color(0.62f, 0.3f, 0.22f));
        var towerRoof = Mat("PagodaRoof", t.roof.texture, new Color(0.3f, 0.22f, 0.22f));
        var lamp = Plain("LakeLamp", new Color(1f, 0.75f, 0.4f), new Color(1f, 0.6f, 0.25f) * 2.2f);
        var boat = Mat("Boat", t.wood.texture, new Color(0.4f, 0.28f, 0.2f));
        var island = Mat("Island", t.turf.texture, new Color(0.18f, 0.28f, 0.22f));

        // Lan can đá sau con đường, mặt hồ mênh mông phía sau
        for (float x = 0f; x < def.Length; x += 2f)
            Box("BalusterPost", scenery, new Vector3(x, 0.45f, def.laneMax + 1.1f), new Vector3(0.22f, 0.9f, 0.22f), stone, 1f);
        Box("BalusterRail", scenery, new Vector3(def.Length / 2f, 0.85f, def.laneMax + 1.1f), new Vector3(def.Length, 0.12f, 0.26f), stone, 1f);
        Box("LakeSurface", scenery, new Vector3(def.Length / 2f, 0.015f, def.laneMax + 40f), new Vector3(def.Length + 140f, 0.02f, 76f), lake, 6f);
        Box("Embankment", scenery, new Vector3(def.Length / 2f, 0.1f, def.laneMax + 1.6f), new Vector3(def.Length + 40f, 0.2f, 0.6f), stone, 2f);
        // Vệt trăng trên mặt hồ (đi theo nửa màn, đủ thấy từ mọi khu)
        for (float x = 10f; x < def.Length; x += 36f)
            for (int k = 0; k < 8; k++)
                Box("MoonStreak", scenery, new Vector3(x + R(-0.6f, 0.6f), 0.03f, def.laneMax + 8f + k * 4f), new Vector3(R(1.5f, 3f) - k * 0.1f, 0.01f, 0.4f), moonlight, 1f);
        // Đảo nhỏ có bảo tháp nhiều tầng (gợi chùa cổ ven hồ), đèn sáng từng tầng
        for (float x = R(15f, 30f); x < def.Length + 10f; x += R(55f, 75f))
        {
            var p = new Vector3(x, 0f, def.laneMax + R(28f, 34f));
            Primitive(PrimitiveType.Cylinder, "Island", scenery, p + Vector3.up * 0.2f, new Vector3(14f, 0.4f, 7f), island);
            float y = 0.4f;
            for (int k = 0; k < 7; k++)
            {
                float w = 2.6f - k * 0.25f, h = k == 0 ? 2f : 1.3f;
                Box("PagodaTier", scenery, p + Vector3.up * (y + h / 2f), new Vector3(w, h, w), tower, 1f);
                Box("PagodaEave", scenery, p + Vector3.up * (y + h), new Vector3(w + 0.8f, 0.18f, w + 0.8f), towerRoof, 1f);
                if (k % 2 == 1)
                {
                    var win = Box("PagodaLight", scenery, p + new Vector3(0f, y + h * 0.45f, -w / 2f - 0.02f), new Vector3(0.4f, 0.5f, 0.04f), lamp, 1f);
                    if (k == 3) ZodiacLights.AddPoint(win, new Color(1f, 0.7f, 0.4f), 12f, 2f);
                }
                y += h;
            }
            Box("PagodaSpire", scenery, p + Vector3.up * (y + 0.6f), new Vector3(0.18f, 1.2f, 0.18f), towerRoof, 1f);
            for (int k = 0; k < 6; k++)
            {
                float s = R(2f, 3.2f);
                Primitive(PrimitiveType.Sphere, "IslandTree", scenery, p + new Vector3(R(-6f, 6f), R(1f, 2f), R(-2f, 2f)), new Vector3(s, s * 0.8f, s), willow);
            }
        }
        // Thuyền thả đèn trôi trên hồ
        for (float x = R(5f, 15f); x < def.Length; x += R(20f, 30f))
        {
            var p = new Vector3(x, 0.15f, def.laneMax + R(8f, 18f));
            Box("Boat", scenery, p, new Vector3(2.6f, 0.3f, 0.8f), boat, 1f);
            Box("BoatBow", scenery, p + new Vector3(1.45f, 0.12f, 0f), new Vector3(0.6f, 0.2f, 0.5f), boat, 1f, euler: new Vector3(0f, 0f, 20f));
            var l = Primitive(PrimitiveType.Sphere, "BoatLantern", scenery, p + new Vector3(-0.6f, 0.8f, 0f), new Vector3(0.3f, 0.36f, 0.3f), lamp);
            ZodiacLights.AddPoint(l, new Color(1f, 0.7f, 0.4f), 5f, 1.6f);
        }
        // Liễu rủ: thân + nhiều cành dài thõng
        for (float x = R(-2f, 4f); x < def.Length + 6f; x += R(9f, 14f))
        {
            var p = new Vector3(x, 0f, rnd.NextDouble() < 0.7 ? def.laneMax + 2.2f : def.laneMin - 2.6f);
            Primitive(PrimitiveType.Cylinder, "WillowTrunk", scenery, p + Vector3.up * 1.8f, new Vector3(0.4f, 1.8f, 0.4f), willowTrunk, new Vector3(0f, 0f, R(-8f, 8f)));
            Primitive(PrimitiveType.Sphere, "WillowCrown", scenery, p + Vector3.up * 3.9f, new Vector3(3f, 1.4f, 2.4f), willow);
            for (int k = 0; k < 14; k++)
            {
                float len = R(1.6f, 2.8f);
                Box("WillowStrand", scenery, p + new Vector3(R(-1.5f, 1.5f), 3.8f - len / 2f, R(-1.1f, 1.1f)), new Vector3(0.06f, len, 0.06f), willow, 1f);
            }
        }
        // Đèn đá tiền cảnh trên bãi cỏ
        for (float x = R(4f, 8f); x < def.Length; x += R(12f, 16f))
        {
            var p = new Vector3(x, 0f, def.laneMin - 2f);
            Box("StoneLanternBase", scenery, p + Vector3.up * 0.4f, new Vector3(0.4f, 0.8f, 0.4f), stone, 1f);
            var head = Box("StoneLanternHead", scenery, p + Vector3.up * 1f, new Vector3(0.55f, 0.4f, 0.55f), lamp, 1f);
            Box("StoneLanternCap", scenery, p + Vector3.up * 1.3f, new Vector3(0.8f, 0.15f, 0.8f), stone, 1f);
            ZodiacLights.AddPoint(head, new Color(1f, 0.75f, 0.45f), 5f, 1.8f);
        }
    }

    /// <summary>Trăng tròn treo trên trời các màn đêm (Đầm Sen, Bờ Hồ Tây).</summary>
    static void BuildMoon()
    {
        var moon = Primitive(PrimitiveType.Sphere, "Moon", scenery, new Vector3(def.Length * 0.5f, 34f, 150f), Vector3.one * 12f,
                             Mat("Moon", null, new Color(1f, 0.97f, 0.85f), 0.1f, new Color(1f, 0.95f, 0.8f) * 1.6f));
        moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // ───────────────────────────── Nét chơi riêng từng khu ─────────────────────────────

    /// <summary>
    /// Thêm vào mỗi khu theo màn: nhũ đá rơi (Hang Đá Vôi — khu có bẫy thì 2 cái), đống rơm đập ra xu (Cánh Đồng),
    /// thúng và chum hàng chợ đập vỡ (Chợ Phiên Ma).
    /// </summary>
    static void BuildRoomGimmick(Transform room, int index, Stage stage, float xMin, float xMax)
    {
        switch (def.backdrop)
        {
            case Backdrop.Limestone:
                int traps = stage.hazard ? 2 : 1;
                for (int k = 0; k < traps; k++) CreateStalactiteTrap(room, xMin, xMax, k);
                break;
            case Backdrop.Field:
                CreateHayBale(room, new Vector3(xMin + 3.8f, 0f, def.laneMin + 0.5f));
                CreateHayBale(room, new Vector3(xMin + 10.5f, 0f, def.laneMax - 0.55f));
                break;
            case Backdrop.Market:
                CreateJar(room, new Vector3(xMin + 6.6f, 0f, def.laneMin + 0.45f));
                CreateJar(room, new Vector3(xMin + 7.3f, 0f, def.laneMin + 0.5f));
                CreateBasket(room, new Vector3(xMin + 12.2f, 0f, def.laneMax - 0.5f));
                CreateBasket(room, new Vector3(xMin + 2.6f, 0f, def.laneMax - 0.5f));
                break;
        }
    }

    static void CreateStalactiteTrap(Transform room, float xMin, float xMax, int k)
    {
        var go = new GameObject("FallingStalactite");
        go.transform.SetParent(room, false);
        go.transform.position = new Vector3((xMin + xMax) / 2f, 0f, LaneCenter);
        var trap = go.AddComponent<FallingStalactite>();
        trap.xMin = xMin;
        trap.xMax = xMax;
        trap.interval = new Vector2(3.5f + k * 1.3f, 6.5f + k * 1.5f);
        trap.debrisColor = def.stone;
        var rock = new GameObject("Rock").transform;
        rock.SetParent(go.transform, false);
        var drip = Plain("Dripstone", Color.Lerp(def.stone, Color.white, 0.25f));
        // Mũi nhọn quay xuống: tâm (chân mũi) ở dưới cùng
        Box("Tip", rock, rock.position + Vector3.up * 0.45f, new Vector3(0.28f, 0.9f, 0.28f), drip, 1f, euler: new Vector3(0f, 45f, 0f));
        Box("Body", rock, rock.position + Vector3.up * 1.4f, new Vector3(0.5f, 1.2f, 0.5f), drip, 1f, euler: new Vector3(0f, 45f, 0f));
        trap.rock = rock;
    }

    /// <summary>Đống rơm nhỏ ven đường: 2 đòn là tung, rơi vài xu.</summary>
    static void CreateHayBale(Transform parent, Vector3 position)
    {
        var b = BreakableRoot("Đống rơm", parent, position, new Vector3(0.9f, 0.9f, 0.8f), ZodiacBalance.Unit + 5);
        var hay = Mat("Hay", ctx.textures.wood.texture, new Color(0.93f, 0.78f, 0.42f));
        Primitive(PrimitiveType.Cylinder, "Bale", b.visual, position + Vector3.up * 0.35f, new Vector3(0.9f, 0.35f, 0.8f), hay);
        Primitive(PrimitiveType.Sphere, "BaleTop", b.visual, position + Vector3.up * 0.72f, new Vector3(0.9f, 0.5f, 0.8f), hay);
        b.debrisColor = new Color(0.93f, 0.78f, 0.42f);
        b.coinDrop = new Vector2Int(2, 4);
        b.loot = new[] { L("binh_mau_nho", 0.1f) };
    }

    /// <summary>Thúng hàng chợ: đập ra xu và đôi khi bình Khí.</summary>
    static void CreateBasket(Transform parent, Vector3 position)
    {
        var b = BreakableRoot("Thúng hàng", parent, position, new Vector3(0.8f, 0.6f, 0.8f), ZodiacBalance.Unit + 5);
        var basket = Mat("Basket", ctx.textures.wood.texture, new Color(0.7f, 0.55f, 0.32f));
        Primitive(PrimitiveType.Cylinder, "Basket", b.visual, position + Vector3.up * 0.25f, new Vector3(0.8f, 0.25f, 0.8f), basket);
        var fruit = Plain("BasketFruit", new Color(1f, 0.55f, 0.15f));
        for (int k = 0; k < 4; k++)
            Primitive(PrimitiveType.Sphere, "Fruit", b.visual, position + new Vector3(R(-0.2f, 0.2f), 0.55f, R(-0.2f, 0.2f)), Vector3.one * 0.22f, fruit);
        b.debrisColor = new Color(0.7f, 0.55f, 0.32f);
        b.coinDrop = new Vector2Int(2, 5);
        b.loot = new[] { L("binh_khi", 0.15f), L("binh_mau_nho", 0.1f) };
    }

    // ───────────────────────────── Bẫy theo cảnh ─────────────────────────────

    /// <summary>Hình bẫy theo màn (bãi chông mặc định). Trả về false nếu màn dùng hình mặc định.</summary>
    static bool ThemedHazardLook(GameObject go, float x, float zMin, float zMax)
    {
        var center = go.transform.position;
        float depth = zMax - zMin;
        switch (def.backdrop)
        {
            case Backdrop.Field:   // rơm cháy
            case Backdrop.Market:  // bếp than hàng nướng
            {
                bool market = def.backdrop == Backdrop.Market;
                var bed = market ? Plain("GrillBed", new Color(0.18f, 0.12f, 0.1f), new Color(0.6f, 0.15f, 0.03f))
                                 : Plain("BurntStraw", new Color(0.35f, 0.22f, 0.1f), new Color(0.5f, 0.15f, 0.02f));
                var flame = Plain("StrawFlame", new Color(1f, 0.6f, 0.2f), new Color(1f, 0.45f, 0.1f) * 2.5f);
                Box("Bed", go.transform, center + Vector3.up * 0.03f, new Vector3(2.5f, 0.06f, depth), bed, 1f);
                for (float sx = -1f; sx <= 1f; sx += 0.5f)
                    for (float sz = zMin + 0.35f; sz < zMax - 0.1f; sz += 0.6f)
                    {
                        float h = R(0.2f, 0.45f);
                        Box("Flame", go.transform, new Vector3(x + sx + R(-0.1f, 0.1f), h / 2f, sz), new Vector3(0.22f, h, 0.22f), flame, 1f,
                            euler: new Vector3(0f, 45f, R(-10f, 10f)));
                    }
                if (market)   // vỉ nướng
                    for (float sx = -1.1f; sx <= 1.1f; sx += 0.3f)
                        Box("GrillBar", go.transform, new Vector3(x + sx, 0.5f, center.z), new Vector3(0.04f, 0.04f, depth), Plain("Iron", new Color(0.2f, 0.2f, 0.22f)), 1f);
                ZodiacLights.AddPoint(go, new Color(1f, 0.5f, 0.2f), 4.5f, 2.2f);
                return true;
            }
            case Backdrop.Pond:    // ván gãy, nước xoáy
            {
                var hole = WaterMat("Whirlpool", new Color(0.05f, 0.12f, 0.16f), 0.05f);
                var foam = Plain("Foam", new Color(0.8f, 0.9f, 0.95f), new Color(0.2f, 0.3f, 0.35f));
                var plank = Plain("BrokenPlank", new Color(0.45f, 0.34f, 0.22f));
                Box("Hole", go.transform, center + Vector3.up * 0.02f, new Vector3(2.5f, 0.03f, depth), hole, 1f);
                for (int k = 0; k < 3; k++)
                    Primitive(PrimitiveType.Cylinder, "FoamRing", go.transform, center + Vector3.up * 0.04f,
                              new Vector3(0.6f + k * 0.6f, 0.005f, Mathf.Min(depth, 0.6f + k * 0.6f)), foam);
                Box("PlankL", go.transform, center + new Vector3(-1.3f, 0.06f, 0f), new Vector3(0.3f, 0.08f, depth), plank, 1f, euler: new Vector3(0f, 0f, 12f));
                Box("PlankR", go.transform, center + new Vector3(1.3f, 0.06f, 0f), new Vector3(0.3f, 0.08f, depth), plank, 1f, euler: new Vector3(0f, 0f, -14f));
                return true;
            }
            default:
                return false;
        }
    }
}
