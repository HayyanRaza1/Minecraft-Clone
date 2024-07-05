using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public GameObject chunkPrefab; // Prefab for the chunk
    public int renderDistance = 2; // Distance in chunks to render around the player
    public Transform player; // Reference to the player
    public BlockTypeData[] blockTypes; // Array of block types to use
    public Material atlasMaterial; // Atlas material to apply to all voxels
    public int seed; // Seed for random generation
    public int maxChunksToRender = 4; // Maximum number of chunks to render simultaneously

    private Vector3Int currentChunkPosition; // Current chunk position of the player
    private Queue<Vector3Int> chunksToCreate = new Queue<Vector3Int>(); // Queue of chunks to create
    private HashSet<Vector3Int> activeChunks = new HashSet<Vector3Int>(); // Set of active chunks
    private Dictionary<Vector3Int, GameObject> chunkObjects = new Dictionary<Vector3Int, GameObject>(); // Dictionary to keep track of chunk GameObjects

    public static readonly Vector3Int[] faceDirections = new Vector3Int[]
    {
        new Vector3Int(0, 0, -1), // Back
        new Vector3Int(0, 0, 1),  // Front
        new Vector3Int(0, 1, 0),  // Top
        new Vector3Int(0, -1, 0), // Bottom
        new Vector3Int(-1, 0, 0), // Left
        new Vector3Int(1, 0, 0)   // Right
    };

    private int chunkSize = 16; // Ensure chunk size is consistent

    void Start()
    {
        currentChunkPosition = GetChunkPosition(player.position);
        UpdateChunks();
        StartCoroutine(GenerateChunks());
    }

    void Update()
    {
        Vector3Int newChunkPosition = GetChunkPosition(player.position);
        if (newChunkPosition != currentChunkPosition)
        {
            currentChunkPosition = newChunkPosition;
            UpdateChunks();
        }
    }

    Vector3Int GetChunkPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = 0; // Assuming flat terrain for simplicity
        int z = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector3Int(x, y, z);
    }

    void UpdateChunks()
    {
        HashSet<Vector3Int> newChunks = new HashSet<Vector3Int>();

        // Add new chunks within the render distance to the set
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector3Int chunkPos = new Vector3Int(currentChunkPosition.x + x, 0, currentChunkPosition.z + z);
                if (!activeChunks.Contains(chunkPos) && !chunksToCreate.Contains(chunkPos))
                {
                    chunksToCreate.Enqueue(chunkPos);
                    newChunks.Add(chunkPos);
                    Debug.Log("Adding chunk to create: " + chunkPos);
                }
            }
        }

        // Remove chunks that are no longer within the render distance
        List<Vector3Int> chunksToRemove = new List<Vector3Int>();
        foreach (Vector3Int chunkPos in activeChunks)
        {
            if (Vector3Int.Distance(chunkPos, currentChunkPosition) > renderDistance)
            {
                chunksToRemove.Add(chunkPos);
                if (chunkObjects.ContainsKey(chunkPos))
                {
                    Destroy(chunkObjects[chunkPos]);
                    chunkObjects.Remove(chunkPos);
                    Debug.Log("Removing chunk: " + chunkPos);
                }
            }
        }

        foreach (Vector3Int chunkToRemove in chunksToRemove)
        {
            activeChunks.Remove(chunkToRemove);
        }

        activeChunks.UnionWith(newChunks); // Update activeChunks set with newChunks
    }

    IEnumerator GenerateChunks()
    {
        while (true)
        {
            // Limit the number of chunks to create per frame
            int chunksCreatedThisFrame = 0;
            while (chunksToCreate.Count > 0 && chunksCreatedThisFrame < maxChunksToRender)
            {
                Vector3Int chunkPosition = chunksToCreate.Dequeue();
                CreateChunk(chunkPosition);
                activeChunks.Add(chunkPosition);
                chunksCreatedThisFrame++;
            }

            yield return null; // Wait for the next frame
        }
    }

    void CreateChunk(Vector3Int chunkPosition)
    {
        Vector3 worldPosition = new Vector3(chunkPosition.x * chunkSize, 0, chunkPosition.z * chunkSize); // Multiply by chunk size to get world position
        Debug.Log("Creating chunk at position: " + worldPosition);
        GameObject chunkGO = Instantiate(chunkPrefab, worldPosition, Quaternion.identity);
        chunkGO.name = chunkPosition.ToString(); // Name the chunk for easy identification
        Chunk chunk = chunkGO.GetComponent<Chunk>();
        chunk.Initialize(chunkPosition, blockTypes, atlasMaterial);
        chunkObjects[chunkPosition] = chunkGO; // Add to the dictionary
    }
}
