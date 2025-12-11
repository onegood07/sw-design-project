using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform minimapPanel;
    public RectTransform playerIcon;
    public Transform playerWorld;

    [Header("Minimap Bounds (World)")]
    public Vector2 worldMin;
    public Vector2 worldMax;

    Vector2 minimapSize;

    void Start()
    {
        minimapSize = minimapPanel.rect.size;
    }

    void Update()
    {
        UpdatePlayerIcon();
    }

    void UpdatePlayerIcon()
    {
        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, playerWorld.position.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, playerWorld.position.y);

        Vector2 iconPos = new Vector2(
            nx * minimapSize.x,
            ny * minimapSize.y
        );

        playerIcon.anchoredPosition = iconPos;
    }
}
