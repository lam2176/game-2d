using UnityEngine;

/// <summary>
/// Lớp nền parallax: dời theo camera với tỉ lệ factor (0 = đứng yên như cảnh gần, 1 = dính theo camera như bầu trời).
/// Đặt các lớp núi xa / rừng tre / mây với factor khác nhau để tạo chiều sâu (như Ori, Hollow Knight).
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float factorX = 0.5f;
    [Range(0f, 1f)] public float factorY = 0.3f;

    Transform cam;
    Vector3 startPosition, camStart;

    void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        startPosition = transform.position;
        if (cam != null) camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 d = cam.position - camStart;
        transform.position = startPosition + new Vector3(d.x * factorX, d.y * factorY, 0f);
    }
}
