using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 레거시 UI Text를 사용하기 위해 이 네임스페이스를 사용합니다.
using System.Text; 

// ItemType Enum은 ItemManager.cs 파일에서 정의됩니다. (필수 전제 조건)

public class ItemSubmitManager : MonoBehaviour
{
    public static ItemSubmitManager Instance;

    [Header("UI Reference")]
    public GameObject exchangeUI; // 납입품 리스트 UI 패널

    [Header("Required Item Display")]
    // 납입 요구 목록을 표시할 레거시 Text 컴포넌트
    public Text requiredItemsText; 
    
    // 납입 항목을 동적으로 표시할 부모 Transform 
    public Transform requiredItemsContainer; 
    // 납입 항목 프리팹 (동적 생성 시 필요하지만, 현재는 간단히 텍스트로 대체)
    // public GameObject requiredItemPrefab; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (exchangeUI != null) exchangeUI.SetActive(false); // 시작 시 숨김
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 납입품 UI를 띄우는 함수
    public void OpenExchangeUI()
    {
        if (exchangeUI != null)
        {
            exchangeUI.SetActive(true);
            
            // 납입 요구 목록을 로드하고 UI에 표시
            DisplayRequiredItems(); 

            Debug.Log("납입품 리스트 창이 열렸습니다.");
        }
    }

    // MARK: 납입 요구 목록을 UI에 표시하는 로직
    private void DisplayRequiredItems()
    {
        // 1. GameManager 인스턴스 유효성 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ItemSubmitManager] GameManager.Instance is null. Cannot load required items. Make sure GameManager exists and is initialized.");
            requiredItemsText.text = "Error: GameManager not initialized."; // 에러 표시
            return;
        }

        // 납입 요구 목록 가져오기
        Dictionary<ItemType, int> requiredItems = GameManager.Instance.CurrentRequiredItems;

        // 2. UI Text 컴포넌트 연결 확인
        if (requiredItemsText == null)
        {
            Debug.LogError("[ItemSubmitManager] requiredItemsText is not assigned in the Inspector. Please link a UI Text (Legacy) component.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        
        // 현재 일차 정보
        int currentDay = (int)GameManager.Instance.CurrentDay + 1;
    
        // MARK: [디버그 로그] 가져온 아이템 개수 로그
        Debug.Log($"[ItemSubmitManager] Attempting to display {requiredItems.Count} required items.");


        if (requiredItems.Count == 0)
        {
            sb.AppendLine("\n오늘 요구되는 납입품이 없습니다.\n");
        }
        else
        {
            foreach (var item in requiredItems)
            {
                // ItemType 이름과 요구 수량을 포맷 및 추가
                sb.AppendLine($"[ {item.Key} ] : {item.Value} 개 필요");
            }
        }
        
        // 최종 텍스트를 UI에 반영하기
        requiredItemsText.text = sb.ToString();
        
        // MARK: [디버그 로그] 최종 반영된 텍스트 확인
        Debug.Log($"[ItemSubmitManager] Text set to UI: \n{requiredItemsText.text}");
    }


    // 납입품 UI를 닫는 함수
    public void CloseExchangeUI()
    {
        if (exchangeUI != null)
        {
            exchangeUI.SetActive(false);
            Debug.Log("납입품 리스트 창이 닫혔습니다.");
        }
    }
    
    // TODO: 여기에 아이템 납입 로직 (SubmitItem)을 추가해야 합니다.
}