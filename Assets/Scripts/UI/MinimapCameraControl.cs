using UnityEngine;

public class MinimapCameraControl : MonoBehaviour
{
    public Camera minimapCamera;   // 미니맵 카메라 연결
    public float zoomStep = 5f;    // 한 번에 확대/축소되는 크기
    public float minZoom = 15f;     // 최소 확대
    public float maxZoom = 40f;    // 최대 축소

    // 버튼 클릭용
    public void ZoomIn()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = Mathf.Max(minZoom, minimapCamera.orthographicSize - zoomStep);
        }
    }

    public void ZoomOut()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = Mathf.Min(maxZoom, minimapCamera.orthographicSize + zoomStep);
        }
    }
}
