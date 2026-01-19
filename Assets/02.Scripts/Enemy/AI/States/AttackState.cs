using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AttackState : IEnemyState
{
    NavMeshAgent agent;
    List<ParticleSystem> muzzleFlash = new List<ParticleSystem>();

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("[Attack State] : State Entered");
        agent = enemy.GetComponent<NavMeshAgent>();

        foreach (ParticleSystem p in enemy.GetComponent<EnemyDataManager>().muzzleFlashVFX)
        {
            muzzleFlash.Add(p);
        }
        
        foreach (var muzzle in muzzleFlash) 
        {
            muzzle.gameObject.SetActive(true);
            muzzle.Stop();
            muzzle.Play();
        }
        
        // 공격 중에는 이동 완전 정지
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        enemy.GetComponent<Animator>().SetBool("IsMove", false);
        enemy.GetComponent<Animator>().SetBool("IsAttack", true);
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("[Attack State] : State Exited");
        foreach (var muzzle in muzzleFlash)
        {
            muzzle.gameObject.SetActive(false);
            muzzle.Stop();
        }
        
        enemy.GetComponent<Animator>().SetBool("IsAttack", false);
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        EnemySight sight = enemy.GetComponent<EnemySight>();
        Transform target = sight.CurrentTarget;

        // 타겟이 존재할 때만 로직 수행
        if (target != null)
        {
           
            Vector3 dir = (target.position - enemy.transform.position).normalized;
            dir.y = 0; // 높이 차이 무시
            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRot, Time.deltaTime * 10f);
            }

            float dist = Vector3.Distance(enemy.transform.position, target.position);
            if (dist >= 30f)
            {
                enemy.TransitionToState(new ChaseState());
                return;
            }
        }
        else
        {
            // 타겟이 없어졌으면 Idle로 복귀
            enemy.TransitionToState(new IdleState());
            return;
        }

        if (!sight.IsPlayerInRange())
        {
            enemy.TransitionToState(new ChaseState());
        }
    }
}