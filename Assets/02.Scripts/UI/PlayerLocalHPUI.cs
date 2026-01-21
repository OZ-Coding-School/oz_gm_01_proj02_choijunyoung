using DG.Tweening;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLocalHPUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private float maxHealth = 100f;

    private NetworkObject playerNetObj;
    private PlayerDamage playerDamage;
    private NetworkVariable<float> currentHealth;

    private void Start()
    {
        
        playerNetObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerNetObj != null)
        {
            playerDamage = playerNetObj.GetComponent<PlayerDamage>();
            if (playerDamage != null)
            {
                currentHealth = playerDamage.currentHealth;
                currentHealth.OnValueChanged += UpdateHPUI;
                UpdateHPUI(0f, currentHealth.Value);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (currentHealth != null)
        {
            currentHealth.OnValueChanged -= UpdateHPUI;
        }
    }

    private void UpdateHPUI(float previous, float current)
    {
        if (hpBarFill != null)
        {
            float fill = current / maxHealth;
            hpBarFill.DOFillAmount(fill, 0.2f);
        }

        if (hpText != null)
        {
            hpText.text = $"{current:F0}/{maxHealth}";
        }
    }
}