using Unity.Netcode;
using UnityEngine;

public class EnemyStateManager : NetworkBehaviour
{
    public IEnemyState CurrentState;

    public Transform Avatar;
    [HideInInspector]
    public Transform Player;
    [HideInInspector]
    public Rigidbody _rb;

    float _maxHP, _currentHP;

    [SerializeField] public AudioClip shootClip;

    private void Start()
    {
        if (IsSpawned)
        {
            Debug.Log($"[Client {NetworkManager.LocalClientId}] Enemy {gameObject.name} Owner = {NetworkObject.OwnerClientId}, IsOwner = {NetworkObject.IsOwner}");
        }
        Debug.Log("적 상태 매니저 실행");
        TransitionToState(new IdleState());
        AllocateComponents();
    }

    void AllocateComponents()
    {
        _rb = GetComponent<Rigidbody>();
        Player = FindAnyObjectByType<PlayerMove>()?.transform;
    }

    private void Update()
    {
        if (!IsSpawned || !NetworkObject.IsOwner) return;
        CurrentState?.UpdateState(this);
    }

    private void FixedUpdate()
    {
        _rb.AddForce(Vector3.down * 30);
    }

    public void TransitionToState(IEnemyState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
        
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    public void TransferOwnershipToHost()
    {
        if (NetworkObject.IsSpawned && NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId); // Host에게 양도
        }
    }

    
    public void PlaySoundtoDis(Vector3 enemyPosition, AudioClip currentClip, float maxDis)
    {
        GameObject tempEmitter = new GameObject("TempReloadSound");
        tempEmitter.transform.position = enemyPosition;

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
