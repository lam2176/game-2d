using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ánh sáng môi trường của màn (màu trời hắt xuống mọi vật, kể cả sprite nhân vật): gán thẳng probe SH khi vào màn
/// thay vì chờ Unity tính từ Lighting Settings — scene dựng bằng code không bake ánh sáng.
/// </summary>
public class ZodiacAmbient : MonoBehaviour
{
    public Color ambient = new Color(0.6f, 0.6f, 0.65f);

    void Awake()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(ambient);
        RenderSettings.ambientProbe = sh;
    }
}
