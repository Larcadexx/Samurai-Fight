using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    // Set target rasio ke 16:9
    public float targetAspectWidth = 16.0f;
    public float targetAspectHeight = 9.0f;

    void Start()
    {
        UpdateCamera();
    }

    // Update dipanggil tiap frame agar responsif saat window ditarik
    void Update()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        // Logika Letterbox (Bar Hitam)
        if (scaleHeight < 1.0f) // Jika window terlalu tinggi/kotak
        {
            Rect rect = camera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            camera.rect = rect;
        }
        else // Jika window terlalu lebar
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = camera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            camera.rect = rect;
        }
    }
}