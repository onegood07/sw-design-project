using UnityEngine;

public class BulletMove : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 velocity;
    private float bulletPower;
    [SerializeField]private float destroyTime = 3f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    /*
    - Start : 생성 후 destroyTime 경과 후 제거
    */
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
    /*
    - setVelocity
    - 인자 : hero 시야 방향
    - 반환 값 : 없음.
    */
    public void bulletSetting(Vector2 viewDirection, float bulletSpeed, float power)
    {
        velocity = viewDirection * bulletSpeed;
        bulletPower = power;
    }
    /*
    - shoot : velocity 를 가지고 이동
    */
    public void shoot()
    {
        rb.linearVelocity = velocity;
    }
    /*
    - OnTriggerEnter2D : 좀비와 충돌 시 탄 제거
    - 인수 : 충돌 객체
    - 반환 값 : 없음
    */
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Zombie"))
        {
            other.gameObject.GetComponent<ZombieStat>().takeDamage(bulletPower);
            Destroy(gameObject);
        }
    }
}
