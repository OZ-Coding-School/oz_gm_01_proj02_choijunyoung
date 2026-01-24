using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCurrentWeaponInfo : MonoBehaviour
{
    [SerializeField] RawImage currentWeaponImage;
    [SerializeField] TextMeshProUGUI ammoCount;
    [SerializeField] Texture2D[] weaponImages;
    [SerializeField] private SOWeapon[] currentWeapon;
    private NetworkObject playerNetObj;
    private PlayerShoot playerShoot;
    private NetworkVariable<int> netCurrentWeaponIndex;
    public int magazinCount = 0;
    private int currentWeaponMaxAmmo;
    private int[] currentAmmoCount = new int[2];
    

    private void Start()
    {
        playerNetObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerNetObj != null)
        {
            playerShoot = playerNetObj.GetComponent<PlayerShoot>();
            if (playerShoot != null)
            {
                netCurrentWeaponIndex = playerShoot.netCurrentWeaponIndex;
                netCurrentWeaponIndex.OnValueChanged += UpdateWeaponGUI;
                UpdateWeaponGUI(0, netCurrentWeaponIndex.Value);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (netCurrentWeaponIndex != null)
        {
            netCurrentWeaponIndex.OnValueChanged -= UpdateWeaponGUI;
        }
    }
    private void UpdateWeaponGUI(int previous, int current)
    {
        Debug.Log("[WeaponInfoUI] 현재 총기 인덱스 : " +current);

        if (currentWeaponImage != null)
        {
            currentWeaponImage.texture = weaponImages[current];
            currentWeaponMaxAmmo = (currentWeapon[current] != null)? currentWeapon[current].maxAmmo:0;
        }
        // 만약 현재 총알 갯수가 0이라면? -> 최대 총알을 넣어줄것
        for (int i = 0; i < currentAmmoCount.Length; i++)
        {
            if (currentAmmoCount[i] == 0)
            {
                if(current == 1) currentAmmoCount[0] = currentWeaponMaxAmmo;
                else if(current == 2) currentAmmoCount[1] = currentWeaponMaxAmmo;
            }
            else continue;
        }
        foreach(var item in currentAmmoCount)
        {
            Debug.Log($"[WeaponInfo] 총기들의 총알 갯수 {item}");
        }

        if (ammoCount != null)
        {
            if (current != 1&&current != 2)
            { 
                ammoCount.text = "";
                return;
            }
            // 만약 현재총의 인덱스가 1, 2번이라면 그에 맞게 텍스트에 현재 남아있는 총알의 갯수를 남겨줄것
            if(current == 1) ammoCount.text = $"{currentAmmoCount[0]}/{currentWeaponMaxAmmo}";
            if(current == 2) ammoCount.text = $"{currentAmmoCount[1]}/{currentWeaponMaxAmmo}";

        }
    }

    public void UpdateCurrentAmmo(int ammo)
    {
        magazinCount = ammo;
        //현재 총의 쏘고 남은 총알을 currentAmmoCount 배열의 0, 1 인덱스에 저장해야함
        if (netCurrentWeaponIndex.Value == 1) currentAmmoCount[0] = magazinCount;
        else if (netCurrentWeaponIndex.Value == 2) currentAmmoCount[1] = magazinCount;

        if (ammoCount != null)
        {
            ammoCount.text = $"{(magazinCount < 0? 0 : magazinCount)}/{currentWeaponMaxAmmo}";
        }
    }
}
