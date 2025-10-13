using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine Virtual Cameras")]
    public CinemachineCamera VCam_Overview;
    public CinemachineCamera VCam_PlayerTurn;
    public CinemachineCamera VCam_FollowMove;
    public CinemachineCamera VCam_Attack;
    public CinemachineCamera VCam_Parry;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SwitchToOverview();
    }

    // Fungsi utama ganti kamera
    private void SetActiveCamera(CinemachineCamera activeCam)
    {
        CinemachineCamera[] cams =
        { VCam_Overview, VCam_PlayerTurn, VCam_FollowMove, VCam_Attack, VCam_Parry };

        foreach (var cam in cams)
        {
#if CINEMACHINE_3_0_OR_NEWER
            cam.Priority.Value = (cam == activeCam) ? 10 : 0;
#else
            cam.Priority = (cam == activeCam) ? 10 : 0;
#endif
        }

        Debug.Log("Camera switched to: " + activeCam.name);
    }

    // Fungsi panggilan publik
    public void SwitchToOverview() => SetActiveCamera(VCam_Overview);
    public void SwitchToPlayerTurn() => SetActiveCamera(VCam_PlayerTurn);
    public void SwitchToFollowMove() => SetActiveCamera(VCam_FollowMove);
    public void SwitchToAttack() => SetActiveCamera(VCam_Attack);
    public void SwitchToParry() => SetActiveCamera(VCam_Parry);

    void Update()
{
    if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToOverview();
    if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToPlayerTurn();
    if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToFollowMove();
    if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToAttack();
    if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToParry();
}

}