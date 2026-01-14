using UnityEngine;

public class PlayerUserData : MonoBehaviour
{
    public string userId;
    public Transform ammoMagazine;

    private void Awake()
    {
        userId = PlayerSettingManager.instance.playerId;
        ammoMagazine = PlayerSettingManager.instance.own_Ammo_Magazine;
    }
}
