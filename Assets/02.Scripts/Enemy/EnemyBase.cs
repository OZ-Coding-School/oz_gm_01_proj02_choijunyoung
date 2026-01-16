using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    SOEnemy enemyData;
    public float maxHealth;
    private float curhealth;
    public Transform target;

    Rigidbody rb;
    BoxCollider boxCollider;
    NavMeshAgent agent;
    Animator anim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //boxCollider = GetComponent<BoxCollider>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        curhealth = maxHealth;
    }

    private void Update()
    {
        agent.SetDestination(target.position);
        
        if (agent.velocity.magnitude > 0.1f)
        {
            anim.SetFloat("Move", 1);
            
        }
        else { anim.SetFloat("Move", 0); }
    }

    public void TakeDamage(float damage)
    {
        curhealth -= damage;
        Debug.Log("Enemy took " + damage + " damage. Remaining health: " + curhealth);
        if (curhealth <= 0f)
        {
            Die();
        }
    }
    public void Die()
    {
        Debug.Log("Enemy died.");
        Destroy(gameObject);
    }
}
