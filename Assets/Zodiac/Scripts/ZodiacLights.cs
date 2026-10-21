using UnityEngine;

/// <summary>Tạo đèn 3D (URP Universal Renderer) — dùng cả lúc dựng scene lẫn lúc chạy (đạn lửa, ma trơi, đèn lồng).</summary>
public static class ZodiacLights
{
    /// <summary>Đèn điểm gắn vào object (đèn lồng, cầu lửa, ma trơi…) — hắt sáng lên mặt đất và nhân vật xung quanh.</summary>
    public static Light AddPoint(GameObject go, Color color, float radius, float intensity)
    {
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = radius;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return light;
    }

    /// <summary>Mặt trời / trăng của màn: đèn định hướng có bóng mềm.</summary>
    public static Light AddSun(GameObject go, Color color, float intensity, Vector3 euler, float shadowStrength = 0.8f)
    {
        go.transform.rotation = Quaternion.Euler(euler);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = shadowStrength > 0f ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = shadowStrength;
        return light;
    }
}
