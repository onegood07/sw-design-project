using UnityEngine;

public class bulletMove : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 velocity;
    private float speed = 10f;
    public float bulletDamage = 300f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        Destroy(gameObject, 3f);
    }
    public void setVelocity(Vector2 viewDirection)
    {
        velocity = viewDirection.normalized * speed;
    }
    public void shoot()
    {
        rb.linearVelocity = velocity;
    }
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Zombie"))
        {
            other.gameObject.GetComponent<ZombieStat>().takeDamage(bulletDamage);
        }
        Destroy(gameObject);
    }
}
