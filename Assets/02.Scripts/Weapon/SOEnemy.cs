using UnityEngine;

[CreateAssetMenu]
public class SOEnemy : ScriptableObject
{
    public enum Type
    {
        Drone,
        Sentry
    }
    public float maxHealth;
    public float attackDamage;
    public float attackSpeed;
    public float coolDown;
    public float moveSpeed;
    public float flySpeed;
}
