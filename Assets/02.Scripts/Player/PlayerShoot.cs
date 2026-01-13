using Unity.Netcode;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    const int RIFLEINDEX = 1;
    const int PISTOLINDEX = 2;
    private string[] ATK_POSE = { "TakeRifle", "TakePistol" };

    [SerializeField] private Transform[] pistol = new Transform[2]; 
    [SerializeField] private Transform[] rifle = new Transform[2];  
    //[SerializeField] private GameObject[] bulletprefab;

    private NetworkVariable<int> netCurrentWeaponIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    Animator anim;

    [SerializeField] SOWeapon[] weaponData;
    SOWeapon currentWeapon;

    [SerializeField] Transform[] firePoints;
    private PlayerInputsManager input;
    private float lastFireTime = 0f;
    private PlayerAimManager aimManager;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInputsManager>();
        aimManager = GetComponent<PlayerAimManager>();
    }

    public override void OnNetworkSpawn()
    {
        var gmInit = GameManager.Pool.transform;
        var parent = gmInit.Find("Ammo_Pool");
        if (parent == null)
        {
            parent = new GameObject("Ammo_Pool").transform;
            parent.SetParent(gmInit, false);
        }
        foreach (var weapon in weaponData)
        {
            GameManager.Pool.CreatePool(weapon.bulletPrefab.GetComponent<NetworkObject>(), weapon.maxAmmo, parent);
        }
        netCurrentWeaponIndex.OnValueChanged += OnWeaponStateChanged;
        UpdateWeaponVisuals(netCurrentWeaponIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        netCurrentWeaponIndex.OnValueChanged -= OnWeaponStateChanged;
    }

    private void OnWeaponStateChanged(int previousValue, int newValue)
    {
        UpdateWeaponVisuals(newValue);
    }

    private void Update()
    {
        if (!IsOwner) return;

        int currentState = netCurrentWeaponIndex.Value;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            int nextState = (currentState == RIFLEINDEX) ? 0 : RIFLEINDEX;

            netCurrentWeaponIndex.Value = nextState;

            if (nextState == RIFLEINDEX)
            {
                anim.SetTrigger("RifleDraw");
            }
            else if (currentState == RIFLEINDEX && nextState == 0)
            {
                anim.SetTrigger("RifleHolster");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            int nextState = (currentState == PISTOLINDEX) ? 0 : PISTOLINDEX;

            netCurrentWeaponIndex.Value = nextState;

            if (nextState == PISTOLINDEX)
            {
                anim.SetTrigger("PistolDraw");
            }
            else if (currentState == PISTOLINDEX && nextState == 0)
            {
                anim.SetTrigger("PistolHolster");
            }
        }

        UpdateCurrentWeaponData();

        if (Input.GetMouseButton(0) && currentWeapon != null)
        {
            if (Time.time >= lastFireTime + currentWeapon.cooltime)
            {
                Shoot();
                lastFireTime = Time.time;
            }
        }
    }
    private void UpdateWeaponVisuals(int weaponIndex)
    {

        bool isRifleActive = (weaponIndex == RIFLEINDEX);
        bool isPistolActive = (weaponIndex == PISTOLINDEX);

        rifle[0].gameObject.SetActive(!isRifleActive);
        rifle[1].gameObject.SetActive(isRifleActive);

        pistol[0].gameObject.SetActive(!isPistolActive);
        pistol[1].gameObject.SetActive(isPistolActive);

        anim.SetBool(ATK_POSE[0], isRifleActive);
        anim.SetBool(ATK_POSE[1], isPistolActive);
    }

    private void UpdateCurrentWeaponData()
    {
        int currentIdx = netCurrentWeaponIndex.Value;
        if (currentIdx == RIFLEINDEX)
        {
            currentWeapon = weaponData[0];
            input.bullet_damage = currentWeapon.damage;
        }
        else if (currentIdx == PISTOLINDEX)
        {
            currentWeapon = weaponData[1];
            input.bullet_damage = currentWeapon.damage;
        }
        else
        {
            currentWeapon = null;
        }
    }

    private bool previousStateIsWeapon(int weaponIdx)
    {
        return netCurrentWeaponIndex.Value == weaponIdx;
    }

    private void Shoot()
    {
        if (currentWeapon == null) return;

        anim.SetTrigger(currentWeapon.animationName);
        Transform currentFirePoint = (netCurrentWeaponIndex.Value == RIFLEINDEX) ? firePoints[0] : firePoints[1];
        Vector3 targetPoint = aimManager.CurrentAimPoint;
        Vector3 dir = (targetPoint - currentFirePoint.position).normalized;

        Quaternion bulletRotation = Quaternion.LookRotation(dir);

        //기존 버전
        //GameObject bullet = Instantiate(currentWeapon.bulletPrefab, currentFirePoint.position, bulletRotation);
        //var bulletScript = bullet.GetComponent<Bullet>();
        //if (bulletScript != null) bulletScript.SetDaamage(currentWeapon.damage);
        //var netObj = bullet.GetComponent<NetworkObject>();
        //if (netObj != null) netObj.Spawn();

        //풀링 버전
        NetworkObject netObj = currentWeapon.bulletPrefab.GetComponent<NetworkObject>();
        NetworkObject poolObj = PoolManager.instance.GetFromPool(netObj);

        poolObj.transform.SetPositionAndRotation(currentFirePoint.position, bulletRotation);

        poolObj.Spawn();

        var bullet = poolObj.GetComponent<Bullet>();
        if(bullet != null) bullet.SetDamage(currentWeapon.damage);

        if (currentWeapon.muzzleFX != null && currentFirePoint != null)
        {
            GameObject FX = Instantiate(currentWeapon.muzzleFX, currentFirePoint.position, currentFirePoint.rotation);
            Destroy(FX, 0.2f);
        }
    }
}

