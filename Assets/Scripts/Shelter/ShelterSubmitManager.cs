using UnityEngine;

public class ShelterSubmitManager : MonoBehaviour
{
    public static ShelterSubmitManager Instance;

    [Header("UI Reference")]
    public ShelterSubmitUI submitUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (submitUI == null)
        {
            Debug.LogError("[ShelterSubmitManager] SubmitUI 참조 누락!");
            return;
        }

        submitUI.Hide();
    }

    /// <summary>
    /// 납입 UI 열기 (인자 없이 호출)
    /// </summary>
    public void OpenSubmitUI()
    {
        if (submitUI != null)
        {
            // ShelterSubmitUI.Show()를 인자 없이 호출합니다.
            submitUI.Show(); 
            Debug.Log("[ShelterSubmitManager] 납입 UI 활성화");
            
            // ⭐⭐⭐ 핵심 수정: 인벤토리 UI도 함께 엽니다. ⭐⭐⭐
            if (InventoryUI.instance != null)
            {
                InventoryUI.instance.OpenInventory();
                Debug.Log("[ShelterSubmitManager] 인벤토리 UI도 함께 활성화");
            }
            else
            {
                Debug.LogWarning("[ShelterSubmitManager] InventoryUI 인스턴스를 찾을 수 없습니다. InventoryUI가 씬에 있는지 확인하세요.");
            }
        }
    }

    /// <summary>
    /// 납입 UI 닫기
    /// </summary>
    public void CloseSubmitUI()
    {
        if (submitUI != null)
        {
            submitUI.Hide();
            Debug.Log("[ShelterSubmitManager] 납입 UI 비활성화");
            
            // ⭐ 보너스 수정: 납입창을 닫을 때 인벤토리도 닫는 것이 일반적입니다.
            if (InventoryUI.instance != null)
            {
                InventoryUI.instance.CloseInventory();
            }
        }
    }

    /// <summary>
    /// 제출 성공 시 UI 갱신
    /// </summary>
    public void NotifySubmitSuccess()
    {
        if (submitUI != null)
        {
            submitUI.UpdateAllItemsAvailability();
        }
    }
}