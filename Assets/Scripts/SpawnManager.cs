using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Tilemap groundTilemap;     
    public GameObject zombiePrefab;   

    private List<Vector3> spawnPositions = new List<Vector3>();
    private List<GameObject> spawnedZombies = new List<GameObject>();
   
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
