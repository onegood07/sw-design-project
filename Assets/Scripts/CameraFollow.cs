using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 따라갈 객체(요소 설정)
    public float smoothSpeed = 0.1f; // 자연스러운 화면 이동을 위한 이동 지연 속도 정의

    // offset 지원 (플레이어 기준 카메라 위치)
    // 2D 탑뷰 -> 카메라 z 좌표를 -10으로 초기화
    public Vector3 offset = new Vector3(0, 0, -10f);
 
    void LateUpdate()
    {
        if (target == null) return;

        // offset을 적용한 위치 계산
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}