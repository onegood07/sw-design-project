using UnityEngine;

[CreateAssetMenu(fileName = "NewPistolData", menuName = "ItemData/WeaponData/PistolData")]
public class PistolData : WeaponData,IUsable
{
    private GameObject bulletObject;
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