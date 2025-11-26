using UnityEngine;

[CreateAssetMenu(fileName = "NewShotGun", menuName = "ItemData/WeaponData/ShotGunData")]
public class ShotGunData : WeaponData,IUsable
{
    private GameObject[] bulletObjects = new GameObject[5];

    // 탄 퍼짐 관리용 배열 및 벡터
    // 인스펙터에서 지정할 것
    public float[] wides = new float[5];



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

            Quaternion rotation = Quaternion.Euler(0, 0, wides[i]);
            // bullet 의 세부 사항을 설정하고 쏜다.
            BulletMove bullet = bulletObjects[i].GetComponent<BulletMove>();

            // 각도를 조정하여 속도 세팅. quarternion 값이 먼저 와야 Vector3와 곱 연산이 가능함
            bullet.bulletSetting((Vector2)(rotation * (Vector3)currentViewDirection),bulletSpeed,power);
            bullet.shoot();
        }

    }
}