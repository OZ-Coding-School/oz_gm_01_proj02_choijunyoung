using UnityEngine;

[CreateAssetMenu]
public class SOWeapon : ScriptableObject
{
    public float damage;
    public float cooltime;
    public int maxAmmo;
    public float range;

    public string animationName;
    public GameObject bulletPrefab;
    public GameObject muzzleFX;
}
