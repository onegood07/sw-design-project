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
        bulletObject.GetComponent<bulletMove>().setVelocity(currentViewDirection);
        bulletObject.GetComponent<bulletMove>().shoot();
    }
}