using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq; 

/// <summary>
/// 조합 UI 목록의 개별 항목입니다. (드래그 앤 드롭 납입 기반)
/// 이 항목은 레시피 정보 표시, 슬롯 상태에 기반한 가용성 확인, 그리고 납입된 재료 소모를 담당합니다.
/// </summary>
public class CraftingRecipeItem : MonoBehaviour
{
    [Header("Display Elements (Result)")]
    public Text rewardText; 
    public Image rewardIcon; 

    // 재료 요구사항 슬롯 컨테이너 리스트 (인스펙터 연결 필수)
    [Header("Ingredient Slots - Connected Manually")]
    [Tooltip("슬롯 역할을 하는 상위 오브젝트들을 순서대로 인스펙터에 연결해주세요. 이 오브젝트에 CraftingIngredientSlot 컴포넌트가 있어야 합니다.")]
    public List<GameObject> ingredientSlotsContainer = new List<GameObject>(); 
    
    // ⭐ 슬롯 컴포넌트 자체를 캐싱할 리스트 (데이터 주입 및 상태 확인용)
    private List<CraftingIngredientSlot> cachedIngredientSlots = new List<CraftingIngredientSlot>();

    [Header("Selection UI")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Craft/Submit Button")]
    public Button submitButton;

    public RecipeData Recipe { get; private set; } 
    public Action<CraftingRecipeItem> onSelected;

    // ==========================================================
    // ⭐ Awake: 인스펙터에 연결된 컨테이너에서 'CraftingIngredientSlot' 컴포넌트를 찾습니다.
    // ==========================================================
    private void Awake()
    {
        cachedIngredientSlots.Clear();
        
        foreach (GameObject slotContainer in ingredientSlotsContainer)
        {
            if (slotContainer == null) continue;
            
            // 슬롯 컨테이너 오브젝트에서 CraftingIngredientSlot 컴포넌트를 찾습니다.
            CraftingIngredientSlot slot = slotContainer.GetComponent<CraftingIngredientSlot>();
            
            if (slot != null)
            {
                cachedIngredientSlots.Add(slot);
                // ⭐ 슬롯의 상태 변경 이벤트를 구독하여 제작 가능 여부를 즉시 갱신합니다.
                slot.OnSlotUpdated += OnIngredientSlotUpdated;
            }
            else
            {
                Debug.LogError($"[CraftingRecipeItem] {slotContainer.name}에 CraftingIngredientSlot 컴포넌트가 없습니다! 드래그 앤 드롭 로직을 위해 필수입니다.");
            }
        }
    }
    
    private void OnDestroy()
    {
        // 구독 해제 (누수 방지)
        foreach (var slot in cachedIngredientSlots)
        {
            if (slot != null)
            {
                slot.OnSlotUpdated -= OnIngredientSlotUpdated;
            }
        }
    }
    
    private void OnIngredientSlotUpdated(CraftingIngredientSlot updatedSlot)
    {
        // 슬롯에 아이템이 드롭되거나 수량이 변경될 때마다 호출되어 버튼 상태를 갱신합니다.
        CheckAvailability();
    }
    // ==========================================================

    public void Setup(RecipeData recipe) 
    {
        Recipe = recipe;
        if (recipe == null)
        {
            Debug.LogError("[CraftingRecipeItem] Setup()에 null recipe 전달됨");
            rewardIcon.enabled = false;
            rewardText.text = "레시피 없음";
            cachedIngredientSlots.ForEach(s => s.ClearRequiredData());
            return;
        }

        // 1. 결과물 UI 세팅 (기존과 동일)
        string rewardItemName = recipe.resultItem != null ? recipe.resultItem.itemName : "N/A";
        Sprite rewardItemIcon = recipe.resultItem != null ? recipe.resultItem.itemImage : null;

        rewardText.text = $"{rewardItemName} x{recipe.resultCount}";
        
        if (rewardIcon != null)
        {
            rewardIcon.sprite = rewardItemIcon;
            rewardIcon.enabled = rewardItemIcon != null; 
        }
        
        // 2. 재료 UI 세팅: 슬롯 컴포넌트에 직접 데이터를 주입하여 UI를 설정
        for(int i = 0; i < cachedIngredientSlots.Count; i++)
        {
            CraftingIngredientSlot slot = cachedIngredientSlots[i];
            bool hasRequirement = i < recipe.requiredMaterials.Count;

            if (hasRequirement)
            {
                RequiredItem req = recipe.requiredMaterials[i];
                if (req.item != null)
                {
                    // ⭐ CraftingIngredientSlot의 SetRequiredData 호출
                    slot.SetRequiredData(req.item.itemName, req.amount, req.item.itemImage);
                    slot.gameObject.SetActive(true); 
                }
            }
            else 
            {
                // 요구 사항이 없으면 슬롯 비활성화 및 데이터 초기화
                slot.ClearRequiredData();
                slot.gameObject.SetActive(false); 
            }
        }

        // 3. 버튼 클릭 리스너 연결 (기존과 동일)
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
        
        // 4. 초기 가용성 체크
        CheckAvailability();
    }

    /// <summary>
    /// ⭐ 납입된 슬롯 상태에 기반하여 버튼 활성화/비활성화
    /// </summary>
    public void CheckAvailability()
    {
        if (submitButton == null || Recipe == null) return;
        
        bool allSlotsReady = true;

        // 모든 필수 재료 슬롯이 충분한 재료를 가지고 있는지 확인
        for(int i = 0; i < Recipe.requiredMaterials.Count; i++)
        {
            if (i < cachedIngredientSlots.Count)
            {
                // ⭐ 슬롯 자체의 HasEnoughMaterial() 로직을 사용
                if (!cachedIngredientSlots[i].HasEnoughMaterial()) 
                {
                    allSlotsReady = false;
                    break;
                }
            }
        }
        
        // 제작 버튼 활성화/비활성화
        submitButton.interactable = allSlotsReady;
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
            
        if (selected)
        {
            CheckAvailability();
        }
    }

    // 목록 항목의 배경 등을 클릭했을 때 (선택 하이라이트용)
    public void OnClick()
    {
        SetSelected(true);
        onSelected?.Invoke(this); 
    }
    
    /// <summary>
    /// ⭐ 제작 버튼 클릭 시 실행될 최종 로직 (납입된 재료를 기반으로 소모)
    /// </summary>
    private void OnSubmitClicked()
    {
        if (Recipe == null || CraftingManager.Instance == null) return;
        
        // 1. 슬롯 상태 최종 확인
        if (!submitButton.interactable)
        {
            Debug.LogWarning("[CraftingRecipeItem] 납입된 재료 부족으로 제작 실패.");
            return;
        }
        
        // 2. CraftingManager에게 제작 요청
        // (CraftingManager는 이제 납입된 아이템의 위치(temporaryItemIndex)를 기반으로 인벤토리에서 소모해야 함)
        // 현재는 편의상 TryCraft(Recipe)가 인벤토리 전체를 확인한다고 가정합니다.
        if (CraftingManager.Instance.TryCraft(Recipe))
        {
            Debug.Log($"[CraftingRecipeItem] '{Recipe.recipeName}' 제작 성공! 납입된 재료 소모 완료.");
            
            // 3. ⭐ 제작 성공 후 모든 슬롯 초기화 (납입 상태 해제)
            foreach(var slot in cachedIngredientSlots)
            {
                slot.ResetSlot();
            }
            
            CraftingManager.Instance.NotifyCraftSuccess(); // UI 갱신을 위해 매니저에 알림
        }
        else
        {
            Debug.LogWarning("[CraftingRecipeItem] 인벤토리에서 재료 소모 실패.");
        }
    }
}