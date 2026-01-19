using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [Header("HP")]
    public float _maxHP = 100F;
    public float _currentHP;

    [Header("Skill Gauge")]
    public float _maxSkillGauge = 100f;
    public float _currentSkillGauge;

    private void Awake()
    {
        _currentHP = _maxHP;
        _currentSkillGauge = 0;
    }

    public void TakeDamage(float amount)
    {
        _currentHP = Mathf.Max(_currentHP - amount, 0f);
        if(_currentHP <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        _currentHP = Mathf.Min(_currentHP + amount, _maxHP);
    }

    void Die()
    {
        print("Die");
    }
}
