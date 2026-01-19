using UnityEngine;

public class PlayerSettingManager : MonoBehaviour
{
    public static PlayerSettingManager instance {  get; private set; }
    public Transform own_Ammo_Magazine;
    public string playerId;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetAmmoPool(string userId)
    {
        var gmInit = GameManager.Pool.transform;
        var parent = gmInit.Find("Ammo_Pool");
        if (parent == null)
        {
            parent = new GameObject("Ammo_Pool").transform;
            parent.SetParent(gmInit, false);
        }
        var parent2 = parent.Find(userId);
        if (parent2 == null)
        {
            parent2 = new GameObject(userId).transform;
            parent2.SetParent(parent, false);
        }
        own_Ammo_Magazine = parent2;
    }

    public void SetUserData(string userId)
    {
        playerId = userId;
    }
}
