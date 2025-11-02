using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Tilemap groundTilemap;     
    public GameObject zombiePrefab;   

    private List<Vector3> spawnPositions = new List<Vector3>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
   
    void Awake()
    {
        GetSpawnPositions(); // 한 번만 계산
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Tilemap에서 소환 가능 좌표 수집
    void GetSpawnPositions()
    {
        BoundsInt bounds = groundTilemap.cellBounds;
        TileBase[] allTiles = groundTilemap.GetTilesBlock(bounds);

        spawnPositions.Clear();

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile != null) 
                {
                    Vector3Int cellPos = new Vector3Int(x + bounds.x, y + bounds.y, 0);
                    Vector3 worldPos = groundTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0); 
                    spawnPositions.Add(worldPos);
                }
            }
        }
    }

    // 기존 좀비 삭제
    public void ClearZombies()
    {
        foreach (var zombie in spawnedZombies)
        {
            if (zombie != null) Destroy(zombie);
        }
        spawnedZombies.Clear();
    }
    
    // 랜덤 소환
    public void SpawnZombies(int spawnCount)
    {
        ClearZombies(); // 기존 좀비 삭제

        List<Vector3> availablePositions = new List<Vector3>(spawnPositions);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availablePositions.Count == 0) break;

            int index = Random.Range(0, availablePositions.Count);
            Vector3 spawnPos = availablePositions[index];
            GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
            spawnedZombies.Add(zombie);

            availablePositions.RemoveAt(index);
        }
    }

}
