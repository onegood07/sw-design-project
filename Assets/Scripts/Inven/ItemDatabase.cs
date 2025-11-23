using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase instance;   // DB를 어디서든 접근하기 위한 싱글톤

    private void Awake()
    {
        instance = this;                   // 싱글톤 인스턴스 설정
    }

    [Header("아이템 프리팹 리스트")]
    public GameObject[] itemPrefabs;       // 미리 등록해둔 아이템 프리팹 배열

    [HideInInspector]
    public List<Item> itemDB = new List<Item>(); // 실제 아이템 데이터가 저장되는 리스트

    private void Start()
    {
        LoadItemsFromPrefabs();            // 프리팹에서 아이템 데이터 자동 로드
    }

    // 프리팹에서 Item 컴포넌트 정보를 가져와 DB에 등록하는 함수
    void LoadItemsFromPrefabs()
    {
        itemDB.Clear();                    // 기존 DB 초기화

        foreach (var prefab in itemPrefabs)
        {
            Item item = prefab.GetComponent<Item>(); // 프리팹에서 Item 스크립트 검색

            if (item != null)
            {
                itemDB.Add(item);          // 유효한 아이템이면 DB에 추가
            }
            else
            {
                Debug.LogWarning($"{prefab.name} 프리팹에 Item 컴포넌트가 없음!");
            }
        }

        Debug.Log($"DB에 {itemDB.Count}개 아이템 등록됨.");  // 로딩된 아이템 개수 출력
    }
}
