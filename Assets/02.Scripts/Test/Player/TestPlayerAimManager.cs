using Unity.Cinemachine;
using UnityEngine;

public class TestPlayerAimManager : MonoBehaviour
{
    private TestPlayerInputs input;

    [Header("Aim Settings")]
    [SerializeField] private CinemachineCamera aimCam;
    [SerializeField] private CinemachineThirdPersonFollow mainCam;
    private CinemachineThirdPersonFollow aimCam3rdPF;
    [SerializeField] private GameObject aimImage;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private GameObject aimobj;
    [SerializeField] private float aimDistance = 10f;

    public Vector3 CurrentAimPoint { get ; private set; }

    private void Start()
    {
        input = GetComponent<TestPlayerInputs>();
        aimCam3rdPF = aimCam.GetComponent<CinemachineThirdPersonFollow>();
    }

    private void Update()
    {
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

        if (aimobj != null) aimobj.transform.position = CurrentAimPoint;
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

            if (Physics.Raycast(camTransform.position, camTransform.forward, out hit, Mathf.Infinity, targetLayer))
            {
                targetPosition = hit.point;
                aimobj.transform.position = hit.point;
            }
            else
            {
                targetPosition = camTransform.position = camTransform.forward;
                aimobj.transform.position = camTransform.position = camTransform.forward*aimDistance;
            }
            
        }
        else
        {
            aimCam.gameObject.SetActive(false);
            //aimImage.SetActive(false);
        }
    }
}
