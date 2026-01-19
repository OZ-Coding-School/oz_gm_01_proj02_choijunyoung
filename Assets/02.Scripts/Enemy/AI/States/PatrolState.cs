using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IEnemyState
{
    Animator anim;
    NavMeshAgent agent;
    bool _hasSetDestination = false; // 목적지 설정 성공 여부
    float _patrolRadius = 25f;

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("[Patrol State] : State Entered");
        agent = enemy.GetComponent<NavMeshAgent>();
        anim = enemy.GetComponent<Animator>();

        agent.acceleration = 999f;
        anim.SetBool("IsMove", true);

        agent.speed = enemy.GetComponent<EnemyDataManager>()._enemyData.PatrolSpeed;
        agent.isStopped = false;

        _hasSetDestination = SetRandomDestination(enemy.transform.position, _patrolRadius);

        if (!_hasSetDestination) enemy.TransitionToState(new IdleState());
    }

    public void ExitState(EnemyStateManager enemy)
    {
        anim.SetBool("IsMove", false);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        Debug.Log("[Patrol State] : State Exited");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        anim.SetBool("IsMove", true);
        Debug.Log("감지 시도 중..." + enemy.GetComponent<EnemySight>().IsPlayerInRange());
        if (enemy.GetComponent<EnemySight>().IsPlayerInRange())
        {
            enemy.TransitionToState(new ChaseState());
            return;
        }

        if (!agent.pathPending)
        {
            // 남은 거리가 정지 거리보다 작거나 같으면 도착한 것으로 간주
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // 도착했으나 경로가 없거나 속도가 0인 경우
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    enemy.TransitionToState(new IdleState());
                }
            }
        }
    }

    bool SetRandomDestination(Vector3 origin, float distance)
    {
        // 원점을 기준으로 반경 내의 랜덤한 점 생성
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, distance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return true; // 성공
        }

        return false; // 이동 가능한 위치를 못 찾음
    }
}
