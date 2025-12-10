using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    public float targetAspectWidth = 16.0f;
    public float targetAspectHeight = 9.0f;

    void Start()
    {
        UpdateCameraRect();
    }
    
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
            
            Rect rect = camera.rect;
            rect.width = 1.0f;
            rect.height = 1.0f;
            rect.x = 0;
            rect.y = 0;

            camera.rect = rect;
        }
    }
}