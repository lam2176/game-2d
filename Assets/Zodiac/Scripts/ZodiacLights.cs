using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Tạo đèn 2D (URP 2D Renderer) — dùng cả lúc dựng scene lẫn lúc chạy (đạn lửa, ma trơi).</summary>
public static class ZodiacLights
{
    /// <summary>Đèn điểm gắn vào object (đèn lồng, cầu lửa, ma trơi…).</summary>
    public static Light2D AddPoint(GameObject go, Color color, float radius, float intensity, float falloff = 0.6f)
    {
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.15f;
        light.falloffIntensity = falloff;
        light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        return light;
    }

    /// <summary>Ánh sáng toàn cục của màn (trời chiều, rừng sương, hang tối).</summary>
    public static Light2D AddGlobal(GameObject go, Color color, float intensity)
    {
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = color;
        light.intensity = intensity;
        light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        return light;
    }
}
