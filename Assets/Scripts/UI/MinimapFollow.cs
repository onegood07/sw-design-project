using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    public Transform target;   // 플레이어
    public float height = 50f; // 위에서 얼마나 떨어질지

    void LateUpdate()
    {
        if (target == null) return;

        // 2D 탑뷰라면 z축만 고정하고 x,y만 따라가기
        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );
    }
}
