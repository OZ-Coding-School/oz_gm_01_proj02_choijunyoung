using UnityEngine;

public class TestPlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform[] pistol = new Transform[2];
    [SerializeField] private Transform[] rifle = new Transform[2];
    bool isActive;

    private void Awake()
    {
        isActive = true;
        pistol[0].gameObject.SetActive(isActive);
        pistol[1].gameObject.SetActive(!isActive);
        rifle[0].gameObject.SetActive(isActive);
        rifle[1].gameObject.SetActive(!isActive);
    }

    private void Update()
    {
        // 1 누름 -> 라이플 장착 -> isActive = false
        // 
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (pistol[1].gameObject.activeSelf)
            {
                isActive = !isActive;
                pistol[0].gameObject.SetActive(isActive);
                pistol[1].gameObject.SetActive(!isActive);
            }
            isActive = !isActive;
            rifle[0].gameObject.SetActive(isActive);
            rifle[1].gameObject.SetActive(!isActive);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (rifle[1].gameObject.activeSelf)
            {
                isActive = !isActive;
                rifle[0].gameObject.SetActive(isActive);
                rifle[1].gameObject.SetActive(!isActive);
            }
            isActive = !isActive;
            pistol[0].gameObject.SetActive(isActive);
            pistol[1].gameObject.SetActive(!isActive);
        }
    }
}
