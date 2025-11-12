using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine Virtual Cameras")]
    public CinemachineCamera Cam_Default;
    public CinemachineCamera Cam_Melangkah;
    public CinemachineCamera Cam_Bergerak;
    public CinemachineCamera Cam_Tangkis;
    public CinemachineCamera Cam_Attack;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SwitchToDefault();
    }

    private void SetActiveCamera(CinemachineCamera activeCam)
    {
        CinemachineCamera[] cams =
        { Cam_Default, Cam_Melangkah, Cam_Bergerak, Cam_Tangkis, Cam_Attack };

        foreach (var cam in cams)
        {
            #if CINEMACHINE_3_0_OR_NEWER
            cam.Priority.Value = (cam == activeCam) ? 10 : 0;
            #else
            cam.Priority = (cam == activeCam) ? 10 : 0;
            #endif
        }
    }

    public void SwitchToDefault() => SetActiveCamera(Cam_Default);
    public void SwitchToMelangkah() => SetActiveCamera(Cam_Melangkah);
    public void SwitchToBergerak() => SetActiveCamera(Cam_Bergerak);
    public void SwitchToTangkis() => SetActiveCamera(Cam_Tangkis);
    public void SwitchToAttack() => SetActiveCamera(Cam_Attack);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToDefault();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToMelangkah();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToBergerak();
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToTangkis();
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToAttack();
    }
}