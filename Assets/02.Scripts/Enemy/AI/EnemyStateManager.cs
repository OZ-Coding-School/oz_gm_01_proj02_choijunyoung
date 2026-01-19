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

    private void Start()
    {
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
        if (!IsServer) return;
        CurrentState?.UpdateState(this); // 파라미터 뒤에 ?.null이 아닐때만 실행하란뜻
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
}
