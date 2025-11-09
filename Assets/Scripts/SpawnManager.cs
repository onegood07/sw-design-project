using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    // TileMap 및 좀비 Prefab
    public Tilemap groundTilemap;     
    public GameObject zombiePrefab;   

    // 소환할 좌표 
    private List<Vector3> spawnPositions = new List<Vector3>();
    // 소환한 Prefab
    private List<GameObject> spawnedZombies = new List<GameObject>();

    void Awake()
    {
        // 스폰 계산은 맨 처음에 한번만
        GetSpawnPositions();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
