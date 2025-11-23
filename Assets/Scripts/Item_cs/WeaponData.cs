using UnityEngine;

public abstract class WeaponData : ItemData
{
    // 무기 공격력
    [Header("공격력")]
    public float power;
    // 발사 프리팹
    [Header("총알")]
    public GameObject bulletPrefab;
    // 탄속
    [Header("탄속")]
    public float bulletSpeed;
}