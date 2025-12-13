using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text; 

// 🚨 주의: 이 스크립트는 ItemType 기반의 Dictionary를 사용하고 있으나,
// GameManager는 이미 Dictionary<Item, int>로 변경되었으므로,
// ItemType 대신 Item을 처리하도록 타입을 변경해야 합니다.

public class ItemSubmitManager : MonoBehaviour
{
    public static ItemSubmitManager Instance;

    [Header("UI Reference")]
    public GameObject exchangeUI; // 납입품 리스트 UI 패널

    [Header("Required Item Display")]
    // 납입 요구 목록 표시
    public Text requiredItemsText; 

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
        // GameManager 인스턴스 유효성 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ItemSubmitManager] GameManager.Instance is null. Cannot load required items.");
            requiredItemsText.text = "Error: GameManager not initialized."; // 에러 표시
            return;
        }

        // 납입 요구 목록 가져오기 (타입 변경 필요)
        // 기존: Dictionary<ItemType, int> requiredItems = GameManager.Instance.CurrentRequiredItems;
        // 수정: Dictionary<Item, int>로 타입을 바꾸고, 변수 이름을 CurrentRequiredItemsData로 변경합니다.
        Dictionary<Item, int> requiredItemsData = GameManager.Instance.CurrentRequiredItemsData;

        // UI Text 컴포넌트 연결 확인
        if (requiredItemsText == null)
        {
            Debug.LogError("[ItemSubmitManager] requiredItemsText is not assigned in the Inspector.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        
        // 현재 일차 정보
        int currentDay = (int)GameManager.Instance.CurrentDay + 1;
    
        // MARK: [디버그 로그] 가져온 아이템 개수 로그
        Debug.Log($"[ItemSubmitManager] Attempting to display {requiredItemsData.Count} required items.");


        if (requiredItemsData.Count == 0)
        {
            sb.AppendLine("\n오늘 요구되는 납입품이 없습니다.\n");
        }
        else
        {
            // Dictionary 키가 ItemType에서 Item으로 변경되었으므로 Key.itemName을 사용합니다.
            foreach (var item in requiredItemsData)
            {
                // Key.itemName을 사용하여 아이템 이름을 표시합니다.
                sb.AppendLine($"[ {item.Key.itemName} ] : {item.Value} 개 필요"); 
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
    
    // TODO: 여기에 아이템 납입 로직 추가하기
}