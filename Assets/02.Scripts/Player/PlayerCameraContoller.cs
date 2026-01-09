using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraContoller : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
    private float targetSide = 1f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            targetSide = targetSide == 1f ? 0f : 1f;
        }
        thirdPersonFollow.CameraSide = Mathf.Lerp(thirdPersonFollow.CameraSide, targetSide, Time.deltaTime * 5f);
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