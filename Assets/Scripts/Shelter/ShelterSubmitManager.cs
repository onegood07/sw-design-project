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
            // ShelterSubmitUI는 내부에서 GameManager 데이터를 로드합니다.
            submitUI.Show(); 
            Debug.Log("[ShelterSubmitManager] 납입 UI 활성화");
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