using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraContoller : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
    private float targetSide = 1f;

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivityX = 100f;   // 좌우 회전 감도
    [SerializeField] private float mouseSensitivityY = 100f;   // 상하 회전 감도
    [SerializeField] private float minPitch = -30f;             // 최소 상하 각도
    [SerializeField] private float maxPitch = 60f;              // 최대 상하 각도

    private float currentPitch = 0f;       // 현재 카메라 상하 각도

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            targetSide = targetSide == 1f ? 0f : 1f;
        }
        thirdPersonFollow.CameraSide = Mathf.Lerp(thirdPersonFollow.CameraSide, targetSide, Time.deltaTime * 5f);

        //float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        //transform.Rotate(Vector3.up * mouseX);

        //float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
        //currentPitch -= mouseY;
        //currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)  // 로컬 플레이어만!
        {
            playerCamera.gameObject.SetActive(true);     // 활성화
            playerCamera.Priority = 20;                  // 최고 우선순위

            
            if (Camera.main != null)
                Camera.main.GetComponent<AudioListener>().enabled = false;
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener) listener.enabled = true;
        }
        else  // 다른 플레이어 카메라 완전 차단
        {
            playerCamera.gameObject.SetActive(false);
            playerCamera.Priority = -100;  // 최저 우선 (영향 0)
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);
    }
}