using UnityEngine;
using NavMeshPlus.Extensions;

public class ZombieNavAI : MonoBehaviour
{
    AgentOverride2d agent;
    Transform hero;
    Animator animator;

    void Awake()
    {
        agent = GetComponent<AgentOverride2d>();
        animator = GetComponent<Animator>();
        hero = GameObject.Find("Hero")?.transform;
    }

    void Update()
    {
        if (hero == null) return;

        agent.Agent.SetDestination(hero.position);

        Vector2 vel = agent.Agent.velocity;

        if (vel.sqrMagnitude < 0.01f) return;

        Vector2Int dir;
        if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
            dir = vel.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = vel.y > 0 ? Vector2Int.up : Vector2Int.down;

        animator.SetBool("RIGHT", dir == Vector2Int.right);
        animator.SetBool("LEFT",  dir == Vector2Int.left);
        animator.SetBool("UP",    dir == Vector2Int.up);
        animator.SetBool("DOWN",  dir == Vector2Int.down);
    }
}
