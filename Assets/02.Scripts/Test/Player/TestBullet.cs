using UnityEngine;

public class TestBullet : MonoBehaviour
{
    [SerializeField] GameObject hit_Env_FX_Prefab;
    [SerializeField] float speed = 20f;
    float damage;

    Rigidbody rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, 3f);
    }
  

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Damage(collision, damage);
            Destroy(gameObject);
        }
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            ContactPoint contact = collision.GetContact(0);
            GameObject fx = Instantiate(hit_Env_FX_Prefab, contact.point, Quaternion.LookRotation(contact.normal));

            Destroy(fx, 5f);
            Destroy(gameObject);
        }
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void Damage(Collision collision, float damage)
    {
        Debug.Log("적 명중! 데미지 :" + damage);
        collision.gameObject.GetComponent<TestEnemyDamage>().TakeDamage(damage);

    }
}
