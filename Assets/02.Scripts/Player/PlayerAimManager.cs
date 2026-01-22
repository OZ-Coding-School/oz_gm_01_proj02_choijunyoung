using System.Globalization;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerAimManager : NetworkBehaviour
{
    private PlayerInputsManager input;

    [Header("Aim Settings")]
    [SerializeField] private CinemachineCamera aimCam;
    [SerializeField] private CinemachineThirdPersonFollow mainCam;
    private CinemachineThirdPersonFollow aimCam3rdPF;
    [SerializeField] private GameObject aimImage;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float aimDistance = 10f;

    public Vector3 CurrentAimPoint { get ; private set; }

    private void Start()
    {
        input = GetComponent<PlayerInputsManager>();
        aimCam3rdPF = aimCam.GetComponent<CinemachineThirdPersonFollow>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        UpdateAimPoint();
        AimCheck();
    }

    private void UpdateAimPoint()
    {
        Transform camTransform = Camera.main.transform;
        RaycastHit hit;

        if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, Mathf.Infinity))
        {
            CurrentAimPoint = hit.point;
        }
        else
        {
            CurrentAimPoint = camTransform.position + (camTransform.forward * aimDistance);
        }

    }

    private void AimCheck()
    {
        if (input.aim)
        {
            aimCam.gameObject.SetActive(true);
            aimCam3rdPF.CameraSide = mainCam.CameraSide < 0.2f ? 0.25f : 0.75f;
            //aimImage.SetActive(true);

            Vector3 targetPosition = Vector3.zero;
            Transform camTransform = Camera.main.transform;
            RaycastHit hit;

            if (input.aim)
            {
                if (!aimCam.gameObject.activeSelf)
                {
                    Debug.Log(">> 조준 모드 진입! (Aim Cam 켜짐)");
                    aimCam.gameObject.SetActive(true);
                }

                aimCam3rdPF.CameraSide = mainCam.CameraSide < 0.2f ? 0.25f : 0.75f;

                // ... (기존 레이캐스트 로직) ...
                if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, Mathf.Infinity, targetLayer))
                {
                    targetPosition = hit.point;
                }
                else
                {
                    targetPosition = camTransform.position + (camTransform.forward * aimDistance);
                }
            }

        }
        else
        {
            if (aimCam.gameObject.activeSelf)
            {
                Debug.Log("<< 조준 해제! (Aim Cam 꺼짐)");
                aimCam.gameObject.SetActive(false);
            }
        }
    }
}
