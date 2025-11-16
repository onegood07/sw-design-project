using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class InsideShelterManager : MonoBehaviour
{
    [Header("Hero Spawn Settings")]
    public Vector3 heroDefaultSpawn = new Vector3(0f, -3.0f, 0f); 
    
    [Header("NPC Prefab")]
    public GameObject survivorPrefab;

    [Header("Night Skip UI")]
    public GameObject skipCanvasObject; 

    IEnumerator Start()
    {
        while (GameManager.Instance == null)
            yield return null;

        Debug.Log("GameManager 발견! SurvivorScore: " + GameManager.Instance.SurvivorScore);

        
        StartCoroutine(NightSkipUILoop());
    }

    IEnumerator NightSkipUILoop()
    {
        bool skipUIShown = false;

        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == Phase.Night)
            {
                if (!skipUIShown)
                {
                    // 쉘터 씬에 있고 밤 페이즈일 때 Skip UI 활성화
                    if (skipCanvasObject != null)
                    {
                        skipCanvasObject.SetActive(true);
                        Debug.Log("밤 스킵 UI 활성화 (쉘터 내부)");
                    }
                    else
                    {
                        Debug.LogWarning("SkipCanvas Object가 할당되지 않았습니다. UI를 활성화할 수 없습니다.");
                    }
                    skipUIShown = true;
                }
            }
            else
            {
                // 낮 페이즈일 때 UI 비활성화
                if (skipUIShown && skipCanvasObject != null)
                {
                    skipCanvasObject.SetActive(false);
                    Debug.Log("밤 스킵 UI 비활성화 (낮 페이즈)");
                    skipUIShown = false;
                }
            }
            yield return null;
        }
    }

    public void SkipNightDeclined()
    {
        if (skipCanvasObject != null)
        {
            skipCanvasObject.SetActive(false);
            Debug.Log("밤 스킵 거절. 밤 페이즈 지속!");
        }
    }

    public void SkipNightConfirmedAndHideUI()
    {
        GameManager.Instance?.SkipNightConfirmed(); 
        
        if (skipCanvasObject != null)
        {
            skipCanvasObject.SetActive(false);
        }
    }
}