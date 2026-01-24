using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    const int RIFLEINDEX = 1;
    const int PISTOLINDEX = 2;
    private string[] ATK_POSE = { "TakeRifle", "TakePistol" };

    private bool isReloading = false;
    private AudioListener myListener;

    public List<int> magazineCount = new List<int>();
    private List<NetworkObject> activeBullets = new List<NetworkObject>();

    [SerializeField] private Transform[] pistol = new Transform[2];
    [SerializeField] private Transform[] rifle = new Transform[2];

    [SerializeField] private Transform[] reloadMagazine = new Transform[2];

    public NetworkVariable<int> netCurrentWeaponIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    Animator anim;

    [SerializeField] SOWeapon[] weaponData;
    private SOWeapon currentWeapon;

    [SerializeField] Transform[] firePoints;
    private PlayerInputsManager input;
    private float lastFireTime = 0f;
    private PlayerAimManager aimManager;
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField] private ParticleSystem[] muzzleFlashParticles;
    [SerializeField] private AudioSource weaponAudioSource;
    [SerializeField] private AudioClip[] shotClips;
    [SerializeField] private AudioClip reloadClip;

    private ObjectPoolSystem currentPoolSystem;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        input = GetComponent<PlayerInputsManager>();
        aimManager = GetComponent<PlayerAimManager>();

        myListener = GetComponentInChildren<AudioListener>(true);
        if (myListener != null)
        {
            // 모든 Listener 비활성화 후 자기 것만 켜기 (다른 플레이어의 Listener는 꺼짐)
            AudioListener[] allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var listener in allListeners)
            {
                listener.enabled = (listener == myListener);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        //var username = gameObject.GetComponent<PlayerUserData>().userId;
        //var parent = gameObject.GetComponent<PlayerUserData>().ammoMagazine;
        foreach (var weapon in weaponData)
        {
            magazineCount.Add(weapon.maxAmmo);
            //    GameManager.Pool.CreatePool(weapon.bulletPrefab.GetComponent<NetworkObject>(), weapon.maxAmmo, parent, username);
            //    var poolHandler = new NetworkedObjectPooling(weapon.bulletPrefab, username); // 20260116->네트워크 풀링을 위해 추가됨.
            //    NetworkManager.Singleton.PrefabHandler.AddHandler(weapon.bulletPrefab, poolHandler); // 20260116->네트워크 풀링을 위해 추가됨.
        }
        netCurrentWeaponIndex.OnValueChanged += OnWeaponStateChanged;
        UpdateWeaponVisuals(netCurrentWeaponIndex.Value);
        UpdateCurrentWeaponData();
    }

    public override void OnNetworkDespawn()
    {
        netCurrentWeaponIndex.OnValueChanged -= OnWeaponStateChanged;
        // 20260116->네트워크 풀링을 위해 추가됨.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.PrefabHandler != null)
        {
            foreach (var weapon in weaponData)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(weapon.bulletPrefab);
            }
        }
        // 20260116->네트워크 풀링을 위해 추가됨.
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
                anim.SetLayerWeight(2, 0);
                anim.SetLayerWeight(1, 1);
                anim.SetTrigger("RifleDraw");
            }
            else if (currentState == RIFLEINDEX && nextState == 0)
            {
                anim.SetLayerWeight(1, 0);
                anim.SetTrigger("RifleHolster");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            int nextState = (currentState == PISTOLINDEX) ? 0 : PISTOLINDEX;

            netCurrentWeaponIndex.Value = nextState;

            if (nextState == PISTOLINDEX)
            {
                anim.SetLayerWeight(1, 0);
                anim.SetLayerWeight(2, 1);
                anim.SetTrigger("PistolDraw");
            }
            else if (currentState == PISTOLINDEX && nextState == 0)
            {
                anim.SetLayerWeight(2, 0);
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

        if (Input.GetKeyDown(KeyCode.R))
        {
            int num = currentWeapon.name == "Rifle" ? 0 : 1;
            Reload(num);
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
        if (currentWeapon != null && currentWeapon.bulletPrefab != null)
        {
            if (ObjectPoolSystem.ExistingPoolSystems.ContainsKey(currentWeapon.bulletPrefab))
            {
                currentPoolSystem = ObjectPoolSystem.ExistingPoolSystems[currentWeapon.bulletPrefab];
            }
            else
            {
                Debug.Log("총알 프리팹 X");
            }
        }

    }

    //private bool previousStateIsWeapon(int weaponIdx)
    //{
    //    return netCurrentWeaponIndex.Value == weaponIdx;
    //}

    private void Shoot()
    {
        if (currentWeapon == null || currentPoolSystem == null || isReloading) return;
        PlayerCurrentWeaponInfo currentWeaponInfo = FindAnyObjectByType<PlayerCurrentWeaponInfo>();
        if (magazineCount[0] <= 0)
        {
            Reload(0);
        }
        else if (magazineCount[1] <= 0)
        {
            Reload(1);
        }
        else if (currentWeapon.name == "Rifle")
        {
            magazineCount[0]--;
            currentWeaponInfo.UpdateCurrentAmmo(magazineCount[0]);
        }
        else if (currentWeapon.name == "Pistol")
        {
            magazineCount[1]--;
            currentWeaponInfo.UpdateCurrentAmmo(magazineCount[1]);
        }
        if (magazineCount[0] < 1 || magazineCount[1] < 1) return;

        anim.SetTrigger(currentWeapon.animationName);
        Transform currentFirePoint = (netCurrentWeaponIndex.Value == RIFLEINDEX) ? firePoints[0] : firePoints[1];
        Vector3 targetPoint = aimManager.CurrentAimPoint;
        Vector3 dir = (targetPoint - currentFirePoint.position).normalized;

        Quaternion bulletRotation = Quaternion.LookRotation(dir);

        // ObjectPoolSystem으로 구현한 버전
        var inst = currentPoolSystem.GetInstance(IsOwner);
        if (inst != null)
        {
            inst.transform.position = currentFirePoint.position;
            inst.transform.rotation = bulletRotation;

            var rb = inst.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();

                rb.linearVelocity = dir * bulletSpeed;
            }
            var bullet = inst.GetComponent<Bullet>();
            if (bullet != null) bullet.SetDamage(currentWeapon.damage);
            inst.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
            
            activeBullets.Add(inst);
        }


        PlayMuzzleFlash(netCurrentWeaponIndex.Value); // 내화면 출력
        PlayShotSound(netCurrentWeaponIndex.Value);

        PlayMuzzleFlashClientRpc(netCurrentWeaponIndex.Value); // 다른 사람 화면 출력
        PlayShotSoundClientRpc(netCurrentWeaponIndex.Value, transform.position);

    }

    private void PlayMuzzleFlash(int weaponIndex)
    {
        int particleIndex = (weaponIndex == 1) ? 0 : 1;
        if (muzzleFlashParticles != null && muzzleFlashParticles.Length > particleIndex)
        {
            var particle = muzzleFlashParticles[particleIndex];
            if (particle != null)
            {
                StartCoroutine(PlayParticleRoutine(particle));
            }
        }
    }

    private IEnumerator PlayParticleRoutine(ParticleSystem particle)
    {

        particle.gameObject.SetActive(true);
        particle.Stop();
        particle.Play();


        float duration = particle.main.duration;


        if (duration < 0.1f) duration = 0.1f;

        yield return new WaitForSeconds(duration);

        particle.gameObject.SetActive(false);
    }
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayMuzzleFlashClientRpc(int weaponIndex)
    {

        if (IsOwner) return;
        PlayMuzzleFlash(weaponIndex);
    }



    private void PlayShotSound(int weaponIndex)
    {
        AudioClip clipToPlay = null;

        if (weaponIndex == RIFLEINDEX)
        {
            clipToPlay = shotClips[0];  
        }
        else if (weaponIndex == PISTOLINDEX)
        {
            clipToPlay = shotClips[1];          
        }

        if (clipToPlay == null || weaponAudioSource == null) return;

        weaponAudioSource.pitch = Random.Range(0.95f, 1.05f);
        weaponAudioSource.PlayOneShot(clipToPlay, Random.Range(0.9f, 1.1f));

    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayShotSoundClientRpc(int weaponIndex, Vector3 shooterPosition)
    {
        AudioClip clipToPlay = (weaponIndex == RIFLEINDEX) ? shotClips[0] : shotClips[1];
        if (clipToPlay == null) return;

        //소리 재생 메서드
        PlaySoundtoDis(shooterPosition, clipToPlay, 100f);
    }


    private void Reload(int num)
    {
        if (isReloading) return;
        isReloading = true;
        
        PlayerCurrentWeaponInfo currentWeaponInfo = FindAnyObjectByType<PlayerCurrentWeaponInfo>();

        StartCoroutine(ReloadAnimSequence());
        StartCoroutine(Reloading());
        magazineCount[num] = currentWeapon.maxAmmo;
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            NetworkObject bullet = activeBullets[i];
            if (bullet != null && bullet.IsSpawned)
            {
                bullet.Despawn();
            }
        }
        activeBullets.Clear();

        currentWeaponInfo.UpdateCurrentAmmo(magazineCount[num]);

    }

    IEnumerator ReloadAnimSequence()
    {
        if (netCurrentWeaponIndex.Value == 0) yield break;
        if(netCurrentWeaponIndex.Value == 1)
        {
            reloadMagazine[0].gameObject.SetActive(true);
            anim.SetTrigger("RifleReload");
            PlayReloadSound(netCurrentWeaponIndex.Value);
            PlayReloadSoundClientRpc(netCurrentWeaponIndex.Value, transform.position);
            yield return new WaitForSeconds(2f);
            reloadMagazine[0].gameObject.SetActive(false);
        }
        else if (netCurrentWeaponIndex.Value == 2)
        {
            reloadMagazine[1].gameObject.SetActive(true);
            anim.SetTrigger("PistolReload");
            PlayReloadSound(netCurrentWeaponIndex.Value);
            PlayReloadSoundClientRpc(netCurrentWeaponIndex.Value, transform.position);
            yield return new WaitForSeconds(1.5f);
            reloadMagazine[0].gameObject.SetActive(false);
        }
    }

    private void PlayReloadSound(int weaponIndex)
    {
        weaponAudioSource.maxDistance = 10f;
        AudioClip clipToPlay = reloadClip;
        if (clipToPlay == null || weaponAudioSource == null) return;

        weaponAudioSource.PlayOneShot(clipToPlay);
        StartCoroutine(DelaySetMaxDistance());
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayReloadSoundClientRpc(int weaponIndex, Vector3 shooterPosition)
    {
        AudioClip clipToPlay = reloadClip;
        if (clipToPlay == null) return;

        //소리 재생 메서드
        PlaySoundtoDis(shooterPosition, clipToPlay, 10f);
    }
    IEnumerator DelaySetMaxDistance()
    {
        yield return new WaitForSeconds(2f);
        weaponAudioSource.maxDistance = 150f;
    }

    IEnumerator Reloading()
    {
        yield return new WaitForSecondsRealtime(2f);
        isReloading = false;
    }

    private void PlaySoundtoDis(Vector3 shooterPosition, AudioClip currentClip, float maxDis)
    {
        GameObject tempEmitter = new GameObject("TempReloadSound");
        tempEmitter.transform.position = shooterPosition;

        AudioSource tempSource = tempEmitter.AddComponent<AudioSource>();
        tempSource.spatialBlend = 1f;
        tempSource.rolloffMode = AudioRolloffMode.Logarithmic;
        tempSource.minDistance = 2f;
        tempSource.maxDistance = maxDis;
        tempSource.pitch = Random.Range(0.95f, 1.05f);

        tempSource.PlayOneShot(currentClip, 1f);

        // 클립 길이 후 자동 삭제
        Destroy(tempEmitter, currentClip.length + 0.5f);
    }
}

