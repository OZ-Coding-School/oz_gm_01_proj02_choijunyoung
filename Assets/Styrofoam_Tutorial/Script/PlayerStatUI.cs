using UnityEngine;
using UnityEngine.UI;

public class PlayerStatUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] Image _hpFillImage;
    [SerializeField] Image _skillFillImage;

    PlayerStat _playerStat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerStat = FindAnyObjectByType<PlayerStat>();
        if(_playerStat == null)
        {
            Debug.LogWarning("Can't find PlayerStat Component");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) _playerStat.TakeDamage(10);
        if (Input.GetKeyDown(KeyCode.H)) _playerStat.Heal(10);

        SyncStatUI();
    }

    void SyncStatUI()
    {
        if (_playerStat == null) return;

        float _targetHPFill = _playerStat._currentHP / _playerStat._maxHP;
        float _targetSkillFill = _playerStat._currentSkillGauge / _playerStat._maxSkillGauge;

        _hpFillImage.fillAmount = Mathf.Lerp(_hpFillImage.fillAmount, _targetHPFill, Time.deltaTime * 5);
        _skillFillImage.fillAmount = Mathf.Lerp(_skillFillImage.fillAmount, _targetSkillFill, Time.deltaTime * 5); 
    }
}
