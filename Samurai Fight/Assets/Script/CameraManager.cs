using UnityEngine;
using Unity.Cinemachine; // Menggunakan namespace Cinemachine

public class CameraManager : MonoBehaviour
{
    // Singleton pattern: variabel statis untuk menyimpan satu-satunya instance CameraManager
    public static CameraManager Instance; 

    [Header("Cinemachine Virtual Cameras")]
    // Referensi ke semua Virtual Camera (VCam) yang akan digunakan
    public CinemachineCamera VCam_Overview;     // Kamera untuk tampilan umum
    public CinemachineCamera VCam_PlayerTurn;  // Kamera fokus ke pemain saat giliran
    public CinemachineCamera VCam_FollowMove;  // Kamera yang mengikuti pion bergerak
    public CinemachineCamera VCam_Attack;      // Kamera untuk adegan menyerang
    public CinemachineCamera VCam_Parry;       // Kamera untuk adegan menangkis

    private void Awake()
    {
        // Mengatur Singleton pattern
        Instance = this; 
    }

    private void Start()
    {
        // Saat game dimulai, aktifkan kamera overview
        SwitchToOverview(); 
    }

    // Fungsi utama ganti kamera
    private void SetActiveCamera(CinemachineCamera activeCam)
    {
        // Membuat array sementara dari semua VCam yang kita miliki
        CinemachineCamera[] cams = 
        { VCam_Overview, VCam_PlayerTurn, VCam_FollowMove, VCam_Attack, VCam_Parry };

        // Looping melalui setiap kamera di array
        foreach (var cam in cams)
        {
        // Cek versi Cinemachine. Versi 3.0+ menggunakan .Value
        #if CINEMACHINE_3_0_OR_NEWER
            // Jika kamera ini adalah kamera yang ingin kita aktifkan, set prioritasnya ke 10
            // Jika tidak, set prioritasnya ke 0
            // Cinemachine akan otomatis pindah ke kamera dengan prioritas tertinggi
            cam.Priority.Value = (cam == activeCam) ? 10 : 0;
        #else
            // Versi lama Cinemachine langsung mengakses .Priority
            cam.Priority = (cam == activeCam) ? 10 : 0;
        #endif
        }

        // Mencatat di konsol kamera mana yang sekarang aktif
        Debug.Log("Camera switched to: " + activeCam.name);
    }

    // Fungsi panggilan publik: Ini adalah "shortcut" yang lebih mudah dibaca
    // yang bisa dipanggil dari skrip lain (seperti GameManager)
    public void SwitchToOverview() => SetActiveCamera(VCam_Overview);
    public void SwitchToPlayerTurn() => SetActiveCamera(VCam_PlayerTurn);
    public void SwitchToFollowMove() => SetActiveCamera(VCam_FollowMove);
    public void SwitchToAttack() => SetActiveCamera(VCam_Attack);
    public void SwitchToParry() => SetActiveCamera(VCam_Parry);

    // Fungsi Update dipanggil setiap frame
    void Update()
    {
        // Ini adalah fungsi DEBUGGING. 
        // Memungkinkan kita mengganti kamera secara manual dengan menekan tombol angka 1-5.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToOverview();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToPlayerTurn();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToFollowMove();
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToAttack();
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToParry();
    }
}