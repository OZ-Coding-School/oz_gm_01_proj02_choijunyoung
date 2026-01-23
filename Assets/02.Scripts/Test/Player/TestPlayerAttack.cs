using UnityEngine;

public class TestPlayerAttack : MonoBehaviour
{
    const int RIFLEINDEX = 1;
    const int PISTOLINDEX = 2;
    private string[] ATK_POSE = {"TakeRifle", "TakePistol"};

    [SerializeField] private Transform[] pistol = new Transform[2];
    [SerializeField] private Transform[] rifle = new Transform[2];
    [SerializeField] private GameObject[] bulletprefab;
    bool isActive;
    bool isRifle, isPistol;
    Animator anim;

    [SerializeField] SOWeapon[] weaponData;
    SOWeapon currentWeapon;

    [SerializeField] Transform[] firePoints;

    private TestPlayerInputs input;

    private float lastFireTime = 0f; // 발사 쿨 타임 관리 변수
    private TestPlayerAimManager aimManager;


    private void Awake()
    {
        isActive = true;
        pistol[0].gameObject.SetActive(isActive);
        pistol[1].gameObject.SetActive(!isActive);
        rifle[0].gameObject.SetActive(isActive);
        rifle[1].gameObject.SetActive(!isActive);
        anim = GetComponent<Animator>();
        isRifle = false;
        isPistol = false;
        input = GetComponent<TestPlayerInputs>();
        aimManager = GetComponent<TestPlayerAimManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isPistol = false;
            isRifle = !isRifle;
            WeaponSwap(RIFLEINDEX);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            isRifle = false;
            isPistol = !isPistol;
            WeaponSwap(PISTOLINDEX);
        }
        if (isRifle) 
        {
            currentWeapon = weaponData[0];
            input.bullet_damage = currentWeapon.damage;
        }
        else if (isPistol) 
        {
            currentWeapon = weaponData[1];
            input.bullet_damage = currentWeapon.damage;
        }

        if (Input.GetMouseButton(0) && currentWeapon != null)
        {
            if (Time.time >= lastFireTime + currentWeapon.cooltime)
            {
                Shoot();
                lastFireTime = Time.time; // 마지막 발사 시간 갱신
            }
        }
    }

    // 무기 스왑(1: 라이플, 2: 권총) 메서드
    private void WeaponSwap(int num)
    {
        if (num == 1) 
        {
            if (pistol[1].gameObject.activeSelf)
            {
                isActive = !isActive;
                pistol[0].gameObject.SetActive(isActive);
                pistol[1].gameObject.SetActive(!isActive);
            }
            isActive = !isActive;
            if (isActive == false)
            {
                anim.SetTrigger("RifleDraw");
            }
            else
            {
                anim.SetTrigger("RifleHolster");
            }
            rifle[0].gameObject.SetActive(isActive);
            rifle[1].gameObject.SetActive(!isActive);
        }
        else if (num == 2)
        {
            if (rifle[1].gameObject.activeSelf)
            {
                isActive = !isActive;
                rifle[0].gameObject.SetActive(isActive);
                rifle[1].gameObject.SetActive(!isActive);
            }
            isActive = !isActive;
            if (isActive == false)
            {
                anim.SetTrigger("PistolDraw");
            }
            else
            {
                anim.SetTrigger("PistolHolster");
            }
            pistol[0].gameObject.SetActive(isActive);
            pistol[1].gameObject.SetActive(!isActive);
        }

        anim.SetBool(ATK_POSE[0], rifle[1].gameObject.activeSelf);
        anim.SetBool(ATK_POSE[1], pistol[1].gameObject.activeSelf);

        Debug.Log("라이플 : " + rifle[1].gameObject.activeSelf);
        Debug.Log("피스톨 : " + pistol[1].gameObject.activeSelf);
    }

    private void Shoot()
    {
        if(currentWeapon == null) return;
        if (!isRifle && !isPistol) return;

        anim.SetTrigger(currentWeapon.animationName);
        Transform currentFirePoint = isRifle ? firePoints[0] : firePoints[1];
        Vector3 targetPoint = aimManager.CurrentAimPoint;
        Vector3 dir = (targetPoint - currentFirePoint.position).normalized;

        Quaternion bulletRotation = Quaternion.LookRotation(dir);

        //GameObject bullet = Instantiate(currentWeapon.bulletPrefab,isRifle ? firePoints[0].position : firePoints[1].position,(isRifle ? firePoints[0].rotation : firePoints[1].rotation) * Quaternion.Euler(0, 90, 0));
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, currentFirePoint.position, bulletRotation);
        bullet.GetComponent<Bullet>().SetDamage(currentWeapon.damage);

        if (currentWeapon.muzzleFX != null&& currentFirePoint != null)
        {
            GameObject FX = Instantiate(currentWeapon.muzzleFX, currentFirePoint.position, currentFirePoint.rotation);

            Destroy(FX, 0.2f); // 풀 구현후 리턴풀로 변경
        }
    }

}
