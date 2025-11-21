using UnityEngine;

public abstract class WeaponData : ItemData
{
    // 무기 공격력
    public float power;
    // 발사 프리팹
    public GameObject bulletPrefab;
    // 범위
    public float range;
}