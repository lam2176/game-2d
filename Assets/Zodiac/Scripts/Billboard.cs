using UnityEngine;

/// <summary>
/// Ảnh phẳng quay về camera. Mặc định chỉ ngả theo góc cúi của camera (trục X) — nhân vật vẫn đứng thẳng trên mặt đất
/// nhưng nhìn từ camera không bị dẹt; <see cref="full"/> = quay hẳn theo camera (thanh máu, khung chữ, dấu "!").
/// Đặt trên object riêng nằm giữa phần vật lý và phần hình (Facing → Tilt → Visual) để không xoay hitbox.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Tooltip("Quay theo cả hướng camera thay vì chỉ ngả theo góc cúi.")]
    public bool full;

    static Camera cachedCamera;

    static Camera Cam
    {
        get
        {
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled) cachedCamera = Camera.main;
            return cachedCamera;
        }
    }

    void LateUpdate()
    {
        var cam = Cam;
        if (cam == null) return;
        transform.rotation = full ? cam.transform.rotation : Quaternion.Euler(cam.transform.eulerAngles.x, 0f, 0f);
    }
}
