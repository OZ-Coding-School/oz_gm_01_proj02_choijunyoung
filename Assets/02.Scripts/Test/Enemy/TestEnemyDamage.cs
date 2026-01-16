using UnityEngine;

public class TestEnemyDamage : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Enemy took " + damage + " damage. Remaining health: " + health);
        if (health <= 0f)
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
