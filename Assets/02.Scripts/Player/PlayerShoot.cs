using Unity.Netcode;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    const int RIFLEINDEX = 1;
    const int PISTOLINDEX = 2;
    private string[] ATK_POSE = { "TakeRifle", "TakePistol" };

    [SerializeField] private Transform[] pistol = new Transform[2]; 
    [SerializeField] private Transform[] rifle = new Transform[2];  
    [SerializeField] private GameObject[] bulletprefab;

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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            int nextState = (netCurrentWeaponIndex.Value == RIFLEINDEX) ? 0 : RIFLEINDEX;
            netCurrentWeaponIndex.Value = nextState;

            if (nextState == RIFLEINDEX) anim.SetTrigger("RifleDraw");
            else if (previousStateIsWeapon(RIFLEINDEX)) anim.SetTrigger("RifleHolster");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            int nextState = (netCurrentWeaponIndex.Value == PISTOLINDEX) ? 0 : PISTOLINDEX;
            netCurrentWeaponIndex.Value = nextState;

            if (nextState == PISTOLINDEX) anim.SetTrigger("PistolDraw");
            else if (previousStateIsWeapon(PISTOLINDEX)) anim.SetTrigger("PistolHolster");
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

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, currentFirePoint.position, bulletRotation);
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null) bulletScript.SetDamage(currentWeapon.damage);
        var netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn();
        if (currentWeapon.muzzleFX != null && currentFirePoint != null)
        {
            GameObject FX = Instantiate(currentWeapon.muzzleFX, currentFirePoint.position, currentFirePoint.rotation);
            Destroy(FX, 0.2f);
        }
    }
}

