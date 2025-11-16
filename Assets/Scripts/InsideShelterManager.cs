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

    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap collisionTilemap;


    IEnumerator Start()
    {
        while (GameManager.Instance == null)
            yield return null;

        Debug.Log("GameManager 발견! SurvivorScore: " + GameManager.Instance.SurvivorScore);

    }
}