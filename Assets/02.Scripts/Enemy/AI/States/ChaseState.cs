using UnityEngine;
using UnityEngine.AI;

public class ChaseState : IEnemyState
{
    float _chaseTime = 2f;
    float _chaseTimer = 0f;

    NavMeshAgent agent;
    EnemySight sight;

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("[Chase State] : State Entered");

        // Agent 컴포넌트 가져오기 및 초기화
        agent = enemy.GetComponent<NavMeshAgent>();
        sight = enemy.GetComponent<EnemySight>();

        // 적 데이터에서 추격 속도 가져와서 적용
        float chaseSpeed = enemy.GetComponent<EnemyDataManager>()._enemyData.ChaseSpeed;
        agent.speed = chaseSpeed;
        agent.isStopped = false; // 이동 시작 허용
        agent.updateRotation = true;

        enemy.GetComponent<Animator>().SetBool("IsMove", true);

        _chaseTimer = 0f;

        if (sight.CurrentTarget != null)
        {
            agent.SetDestination(sight.CurrentTarget.position);
        }
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("[Chase State] : State Exited");
        enemy.GetComponent<Animator>().SetBool("IsMove", false);
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 시야 체크
        bool _playerVisible = sight.IsPlayerInRange();

        if (_playerVisible) _chaseTimer = 0f;
        else _chaseTimer += Time.deltaTime;

        // 놓쳤을 때 (추격 시간 초과) 처리
        if (!_playerVisible && _chaseTimer > _chaseTime)
        {
            if (Random.value < 0.5f)
                enemy.TransitionToState(new IdleState());
            else
                enemy.TransitionToState(new PatrolState());
            return;
        }

        // 가장 가까운 플레이어 추격
        Transform target = sight.CurrentTarget;

        if (target != null)
        {
            float attackRange = enemy.GetComponent<EnemyDataManager>()._enemyData.AttackRange;
            float dist = Vector3.Distance(enemy.transform.position, target.position);

            if (dist <= attackRange)
            {
                // 공격 상태로 전환
                enemy.TransitionToState(new AttackState());
                return;
            }

            agent.SetDestination(target.position);

            enemy.GetComponent<EnemyRotation>().LookAtTarget(target.position);
        }
    }

}
