using UnityEngine;

[CreateAssetMenu(fileName = "NewPistol", menuName = "ItemData/WeaponData/PistolData")]
public class PistolData : WeaponData,IUsable
{
    private GameObject bulletObject;
    /*
    - Use
    - 인자 : hero 위치, 시야 방향
    - 리턴값 : 없음
    */
    public void Use(Transform HeroTransform, Vector2 currentViewDirection)
    {
        // bulletPrefab 생성
        bulletObject = GameObject.Instantiate(
        bulletPrefab,
        HeroTransform.position,
        Quaternion.identity
        );

        BulletMove bullet = bulletObject.GetComponent<BulletMove>();
        bullet.bulletSetting(currentViewDirection,bulletSpeed,power);
        bullet.shoot();

        // 이펙트 생성
        float angle = Mathf.Atan2(currentViewDirection.y, currentViewDirection.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle + 90);
        Instantiate(effect, HeroTransform.position + (Vector3)currentViewDirection*0.8f, rotation);
    }
}