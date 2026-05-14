using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private AudioListener audioListener;

    public static CameraController Instance;

    private void Awake()
    {
        Instance = this;
        audioListener.enabled = false;
    }

    public void SetTarget(Transform target, bool isOwner)
    {
        if (!isOwner) return;

        cinemachineCamera.Target.TrackingTarget = target;
        audioListener.enabled = true;
    }
}
