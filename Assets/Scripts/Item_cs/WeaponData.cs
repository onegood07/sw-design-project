using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "ItemData/WeaponData")]
public class WeaponData : ItemData
{
    // 무기 공격력
    public float power;
    // 공격 지연 시간
    public float coolTime;
    // 발사 프리팹
    public GameObject bulletPrefab;
    // 범위
    public float range;
}