using UnityEngine;

// 타이틀 씬에 미리 배치되어 데이터를 운반하는 역할 (Network X)
public class GlobalWeaponConfig : MonoBehaviour
{
    public static GlobalWeaponConfig Instance { get; private set; }

    [Header("Data")]
    public SOWeapon[] allWeapons; // 여기에 무기 데이터 할당
    public int maxPlayers = 4;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}