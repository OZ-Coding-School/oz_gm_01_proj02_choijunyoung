using UnityEngine;
using Unity.Netcode;

public class TestPlayerAttack_Net : NetworkBehaviour
{
    const int RIFLEINDEX = 1;
    const int PISTOLINDEX = 2;
    private string[] ATK_POSE = { "TakeRifle", "TakePistol" };

    [SerializeField] private Transform[] pistol = new Transform[2]; // 0: Socket, 1: Hand
    [SerializeField] private Transform[] rifle = new Transform[2];  // 0: Socket, 1: Hand
    [SerializeField] private GameObject[] bulletprefab;

    // [변경 1] 로컬 bool 변수 대신 네트워크 변수 선언
    // 0: 홀스터(무기 없음), 1: 라이플, 2: 권총
    // WritePermission.Owner: 오너(클라이언트)가 직접 값을 바꿀 수 있음 (Distributed Authority 필수)
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

    // [변경 2] 네트워크 스폰 시 동기화 연결
    public override void OnNetworkSpawn()
    {
        // 값이 변경될 때 실행될 함수(이벤트) 연결
        netCurrentWeaponIndex.OnValueChanged += OnWeaponStateChanged;

        // 접속하자마자 현재 상태에 맞춰 무기 셋팅 (늦게 들어온 사람을 위해)
        UpdateWeaponVisuals(netCurrentWeaponIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        // 연결 해제 (메모리 누수 방지)
        netCurrentWeaponIndex.OnValueChanged -= OnWeaponStateChanged;
    }

    // [변경 3] 값이 바뀌면 모든 클라이언트에서 실행되는 함수
    private void OnWeaponStateChanged(int previousValue, int newValue)
    {
        UpdateWeaponVisuals(newValue);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // [수정 1] 현재 상태를 미리 '지역 변수'에 저장해둡니다. (값을 바꾸기 전에!)
        int currentState = netCurrentWeaponIndex.Value;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 현재 라이플(1)이면 -> 0(해제), 아니면 -> 1(라이플)
            int nextState = (currentState == RIFLEINDEX) ? 0 : RIFLEINDEX;

            // 값을 변경합니다.
            netCurrentWeaponIndex.Value = nextState;

            // [수정 2] 애니메이션 처리는 'currentState'(이전 값)와 'nextState'(다음 값)를 비교해서 처리
            if (nextState == RIFLEINDEX)
            {
                anim.SetTrigger("RifleDraw");
            }
            // "방금 전까지 라이플이었는데(currentState), 이제 0이 됐다면" -> 집어넣기
            else if (currentState == RIFLEINDEX && nextState == 0)
            {
                anim.SetTrigger("RifleHolster");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 현재 권총(2)이면 -> 0(해제), 아니면 -> 2(권총)
            int nextState = (currentState == PISTOLINDEX) ? 0 : PISTOLINDEX;

            netCurrentWeaponIndex.Value = nextState;

            if (nextState == PISTOLINDEX)
            {
                anim.SetTrigger("PistolDraw");
            }
            // "방금 전까지 권총이었는데(currentState), 이제 0이 됐다면" -> 집어넣기
            else if (currentState == PISTOLINDEX && nextState == 0)
            {
                anim.SetTrigger("PistolHolster");
            }
        }

        UpdateCurrentWeaponData();

        // 발사 로직 (동일)
        if (Input.GetMouseButton(0) && currentWeapon != null)
        {
            if (Time.time >= lastFireTime + currentWeapon.cooltime)
            {
                Shoot();
                lastFireTime = Time.time;
            }
        }
    }

    // [변경 4] 실제 눈에 보이는 무기(SetActive) 처리 로직 분리
    // 이 함수는 NetworkVariable 덕분에 모든 클라이언트에서 실행됨
    private void UpdateWeaponVisuals(int weaponIndex)
    {
        // 상태 0: 모두 등/허리에 매기 (Socket Active, Hand Inactive)
        // 상태 1: 라이플 들기
        // 상태 2: 권총 들기

        bool isRifleActive = (weaponIndex == RIFLEINDEX);
        bool isPistolActive = (weaponIndex == PISTOLINDEX);

        // 라이플 처리
        // 0번(Socket): 안 들고 있을 때 켜짐
        // 1번(Hand): 들고 있을 때 켜짐
        rifle[0].gameObject.SetActive(!isRifleActive);
        rifle[1].gameObject.SetActive(isRifleActive);

        // 권총 처리
        pistol[0].gameObject.SetActive(!isPistolActive);
        pistol[1].gameObject.SetActive(isPistolActive);

        // 애니메이션 상태값 동기화 (레이어 마스크 등용)
        anim.SetBool(ATK_POSE[0], isRifleActive);
        anim.SetBool(ATK_POSE[1], isPistolActive);
    }

    // Update문 정리를 위한 헬퍼 함수
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

    // 애니메이션 트리거 처리를 위한 헬퍼 (이전 상태 확인용)
    private bool previousStateIsWeapon(int weaponIdx)
    {
        // 트리거 로직은 간단하게 처리하거나, OnValueChanged에서 oldVal을 활용해도 됨
        // 여기서는 간단히 현재 상태만 보고 판단하도록 로직 수정 권장
        return netCurrentWeaponIndex.Value == weaponIdx;
    }

    private void Shoot()
    {
        // Shoot 코드는 기존과 동일하게 유지
        // ... (생략) ...
        if (currentWeapon == null) return;

        anim.SetTrigger(currentWeapon.animationName);
        Transform currentFirePoint = (netCurrentWeaponIndex.Value == RIFLEINDEX) ? firePoints[0] : firePoints[1];
        Vector3 targetPoint = aimManager.CurrentAimPoint;
        Vector3 dir = (targetPoint - currentFirePoint.position).normalized;

        Quaternion bulletRotation = Quaternion.LookRotation(dir);

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, currentFirePoint.position, bulletRotation);
        // 총알에 데미지 주입 (안전장치 추가)
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null) bulletScript.SetDamage(currentWeapon.damage);

        if (currentWeapon.muzzleFX != null && currentFirePoint != null)
        {
            GameObject FX = Instantiate(currentWeapon.muzzleFX, currentFirePoint.position, currentFirePoint.rotation);
            Destroy(FX, 0.2f);
        }
    }
}