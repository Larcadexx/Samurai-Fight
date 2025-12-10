using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    // Target rasio aspek (16:9 = 1.7777)
    public float targetAspectWidth = 16.0f;
    public float targetAspectHeight = 9.0f;

    void Start()
    {
        UpdateCameraRect();
    }
    
    // Gunakan Update agar bisa merespon perubahan window secara real-time
    void Update() 
    {
        UpdateCameraRect();
    }

    void UpdateCameraRect()
    {
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        // KONDISI 1: Layar Terlalu Tinggi / Kurus
        // Kita TETAP beri bar hitam di atas & bawah (Letterbox)
        if (scaleHeight < 1.0f)
        {
            Rect rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            camera.rect = rect;
        }
        else 
        {
            // MODIFIKASI DI SINI:
            // Jika layar melebar (lebih lebar dari 16:9), JANGAN beri bar hitam di samping.
            // Biarkan kamera memenuhi layar.
            
            Rect rect = camera.rect;
            rect.width = 1.0f;
            rect.height = 1.0f;
            rect.x = 0;
            rect.y = 0;

            camera.rect = rect;
        }
    }
}