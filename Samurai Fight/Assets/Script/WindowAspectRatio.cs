using UnityEngine;

public class WindowAspectRatio : MonoBehaviour
{
    // Rasio target 16:9 (1.7777...)
    private float targetAspect = 16.0f / 9.0f;
    
    // Simpan resolusi terakhir untuk pengecekan
    private int lastWidth;
    private int lastHeight;

    void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        // Jika game sedang Fullscreen, jangan jalankan logika ini
        if (Screen.fullScreen) return;

        // Cek apakah user mengubah lebar atau tinggi jendela
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            // OPSI 1: Prioritaskan Lebar (Width)
            // Jika user menarik ke samping, tinggi akan ikut berubah otomatis
            if (Screen.width != lastWidth)
            {
                int newHeight = Mathf.RoundToInt(Screen.width / targetAspect);
                
                // Terapkan resolusi baru (memaksa tinggi mengikuti lebar)
                Screen.SetResolution(Screen.width, newHeight, false);
            }
            // OPSI 2: Prioritaskan Tinggi (Height)
            // Jika user menarik ke atas/bawah, lebar akan ikut berubah otomatis
            else if (Screen.height != lastHeight)
            {
                int newWidth = Mathf.RoundToInt(Screen.height * targetAspect);
                
                // Terapkan resolusi baru (memaksa lebar mengikuti tinggi)
                Screen.SetResolution(newWidth, Screen.height, false);
            }

            // Update nilai terakhir
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }
    }
}