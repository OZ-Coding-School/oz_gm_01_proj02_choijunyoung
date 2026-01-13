using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] float speed = 20f;

    Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, 3f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == enemyLayer)
        {
            Damage();
        }
        if(collision.gameObject.layer == enemyLayer || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }

    public void Damage()
    {
        Debug.Log("Рћ ИэСп!");
    }
}
