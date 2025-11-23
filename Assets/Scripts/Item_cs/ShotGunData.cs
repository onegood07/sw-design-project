using UnityEngine;

[CreateAssetMenu(fileName = "NewShotGun", menuName = "ItemData/WeaponData/ShotGunData")]
public class ShotGunData : WeaponData,IUsable
{
    private GameObject[] bulletObjects = new GameObject[5];

    // 탄 퍼짐 관리용 배열 및 벡터
    public float[] wides = new float[5]
    {
        // 조정하여 각도를 바꿀 수 있음
        -0.4f,-0.2f,0f,0.2f,0.4f
    };
    private Vector2 wide;


    /*
    - Use
    - 인자 : hero 위치, 시야 방향
    - 리턴값 : 없음
    */

    public void Use(Transform HeroTransform, Vector2 currentViewDirection)
    {
        for(int i = 0; i < 5; i++)
        {
            // bullet 생성
            bulletObjects[i] = GameObject.Instantiate(
            bulletPrefab,
            HeroTransform.position,
            Quaternion.identity
            );
            // current view direction 반영하여 각도를 조정한다
            if(currentViewDirection.x == 0)wide = Vector2.right * wides[i];
            else wide = Vector2.up * wides[i];

            // bullet 의 세부 사항을 설정하고 쏜다.
            BulletMove bullet = bulletObjects[i].GetComponent<BulletMove>();
            bullet.bulletSetting(currentViewDirection + wide,bulletSpeed,power);
            bullet.shoot();
        }

    }
}