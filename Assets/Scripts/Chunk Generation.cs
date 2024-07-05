using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chunk : MonoBehaviour
{
    public Vector3Int chunkSize = new Vector3Int(16, 50, 16); // Increased height to 50 blocks
    public Vector3Int chunkPosition; // Position of the chunk in world space
    public BlockTypeData[] blockTypes; // Array of block types to use
    public Material atlasMaterial; // Atlas material to apply to all voxels
    public int atlasTextureSize = 4; // Assuming 4x4 texture atlas for this example

    // Perlin noise parameters (exposed to Unity Inspector)
    public float noiseScale = 0.1f; // Scale of the noise
    public float heightMultiplier = 10f; // Multiplier for height adjustment
    public int numOctaves = 4; // Number of octaves for Perlin noise
    public float persistence = 0.5f; // Persistence parameter for Perlin noise
    public float lacunarity = 2f; // Lacunarity parameter for Perlin noise

    private BlockTypeData[,,] voxelData; // 3D array to store voxel types
    private MeshFilter meshFilter; // Reference to the MeshFilter component

    public void Initialize(Vector3Int chunkPosition, BlockTypeData[] blockTypes, Material atlasMaterial)
    {
        this.chunkPosition = chunkPosition;
        this.blockTypes = blockTypes;
        this.atlasMaterial = atlasMaterial;
        voxelData = new BlockTypeData[chunkSize.x, chunkSize.y, chunkSize.z];
        GenerateVoxelData();
        GenerateChunkMesh();
    }

    void GenerateVoxelData()
    {
        // Adjust parameters based on Inspector values
        float scale = noiseScale;

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int z = 0; z < chunkSize.z; z++)
            {
                float height = GenerateHeight(x, z);

                for (int y = 0; y < chunkSize.y; y++)
                {
                    int blockTypeIndex = Mathf.Clamp(y, 0, blockTypes.Length - 1);
                    BlockTypeData selectedBlockType = blockTypes[blockTypeIndex];

                    if (y <= height)
                    {
                        if (y == 0)
                        {
                            selectedBlockType = blockTypes[0]; // Bedrock
                        }
                        else if (y > 0 && y < 10)
                        {
                            selectedBlockType = blockTypes[1]; // Stone
                        }
                        else if (y > 9 && y < 11)
                        {
                            selectedBlockType = blockTypes[2]; // Dirt near surface
                        }
                        else if (y > 12)
                        {
                            selectedBlockType = blockTypes[3]; // Grass Block
                        }
                    }
                    else
                    {
                        selectedBlockType = null; // Empty space above terrain
                    }

                    voxelData[x, y, z] = selectedBlockType;
                }
            }
        }
    }

    float GenerateHeight(int x, int z)
    {
        float height = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // Adjust scale based on noise scale
        float scaledX = (chunkPosition.x * chunkSize.x + x) * noiseScale;
        float scaledZ = (chunkPosition.z * chunkSize.z + z) * noiseScale;

        for (int i = 0; i < numOctaves; i++)
        {
            height += Mathf.PerlinNoise(scaledX * frequency, scaledZ * frequency) * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        height *= heightMultiplier;

        return height;
    }

    // Inside Chunk.cs

    void GenerateChunkMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int z = 0; z < chunkSize.z; z++)
            {
                for (int y = 0; y < chunkSize.y; y++)
                {
                    BlockTypeData currentBlock = voxelData[x, y, z];
                    if (currentBlock == null) continue; // Skip empty voxels

                    Vector3 voxelPosition = new Vector3(x, y, z);

                    // Check each face direction to determine if it should be rendered
                    foreach (Vector3Int direction in ChunkManager.faceDirections)
                    {
                        Vector3Int neighborPosition = new Vector3Int(x, y, z) + direction;
                        if (IsPositionInBounds(neighborPosition))
                        {
                            if (voxelData[neighborPosition.x, neighborPosition.y, neighborPosition.z] == null)
                            {
                                AddFace(vertices, triangles, uvs, voxelPosition, direction, currentBlock);
                            }
                        }
                        else
                        {
                            // Render face if neighbor is out of chunk bounds
                            AddFace(vertices, triangles, uvs, voxelPosition, direction, currentBlock);
                        }
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>(); // Get existing or add if necessary
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = atlasMaterial;

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>(); // Get existing or add if necessary
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = mesh;
        gameObject.layer = LayerMask.NameToLayer("Voxellayer");
    }


    bool IsPositionInBounds(Vector3Int position)
    {
        return position.x >= 0 && position.x < chunkSize.x &&
               position.y >= 0 && position.y < chunkSize.y &&
               position.z >= 0 && position.z < chunkSize.z;
    }

    void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 position, Vector3Int direction, BlockTypeData blockType)
    {
        int vertexIndex = vertices.Count;
        Vector3[] faceVertices = GetFaceVertices(position, direction);
        vertices.AddRange(faceVertices);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 1);

        Vector2[] faceUVs = GetFaceUVs(blockType, direction);
        uvs.AddRange(faceUVs);
    }

    Vector3[] GetFaceVertices(Vector3 position, Vector3Int direction)
    {
        Vector3[] faceVertices = new Vector3[4];

        if (direction == new Vector3Int(0, 0, -1)) // Back face
        {
            faceVertices[0] = position + new Vector3(0, 0, 0);
            faceVertices[1] = position + new Vector3(1, 0, 0);
            faceVertices[2] = position + new Vector3(0, 1, 0);
            faceVertices[3] = position + new Vector3(1, 1, 0);
        }
        else if (direction == new Vector3Int(0, 0, 1)) // Front face
        {
            faceVertices[0] = position + new Vector3(1, 0, 1);
            faceVertices[1] = position + new Vector3(0, 0, 1);
            faceVertices[2] = position + new Vector3(1, 1, 1);
            faceVertices[3] = position + new Vector3(0, 1, 1);
        }
        else if (direction == new Vector3Int(0, 1, 0)) // Top face
        {
            faceVertices[0] = position + new Vector3(0, 1, 0);
            faceVertices[1] = position + new Vector3(1, 1, 0);
            faceVertices[2] = position + new Vector3(0, 1, 1);
            faceVertices[3] = position + new Vector3(1, 1, 1);
        }
        else if (direction == new Vector3Int(0, -1, 0)) // Bottom face
        {
            faceVertices[0] = position + new Vector3(0, 0, 1);
            faceVertices[1] = position + new Vector3(1, 0, 1);
            faceVertices[2] = position + new Vector3(0, 0, 0);
            faceVertices[3] = position + new Vector3(1, 0, 0);
        }
        else if (direction == new Vector3Int(-1, 0, 0)) // Left face
        {
            faceVertices[0] = position + new Vector3(0, 0, 1);
            faceVertices[1] = position + new Vector3(0, 0, 0);
            faceVertices[2] = position + new Vector3(0, 1, 1);
            faceVertices[3] = position + new Vector3(0, 1, 0);
        }
        else if (direction == new Vector3Int(1, 0, 0)) // Right face
        {
            faceVertices[0] = position + new Vector3(1, 0, 0);
            faceVertices[1] = position + new Vector3(1, 0, 1);
            faceVertices[2] = position + new Vector3(1, 1, 0);
            faceVertices[3] = position + new Vector3(1, 1, 1);
        }

        return faceVertices;
    }


    Vector2[] GetFaceUVs(BlockTypeData blockType, Vector3Int direction)
    {
        Vector2[] uv = new Vector2[4];

        // Calculate the size of each tile in the atlas
        float tileSize = 1f / atlasTextureSize;

        Vector2 uvOffset = Vector2.zero;

        // Select the UV offset based on the direction
        if (direction == new Vector3Int(0, 0, -1)) // Back face
        {
            uvOffset = new Vector2(blockType.backTextureAtlasCoord.x * tileSize, blockType.backTextureAtlasCoord.y * tileSize);
        }
        else if (direction == new Vector3Int(0, 0, 1)) // Front face
        {
            uvOffset = new Vector2(blockType.frontTextureAtlasCoord.x * tileSize, blockType.frontTextureAtlasCoord.y * tileSize);
        }
        else if (direction == new Vector3Int(0, 1, 0)) // Top face
        {
            uvOffset = new Vector2(blockType.topTextureAtlasCoord.x * tileSize, blockType.topTextureAtlasCoord.y * tileSize);
        }
        else if (direction == new Vector3Int(0, -1, 0)) // Bottom face
        {
            uvOffset = new Vector2(blockType.bottomTextureAtlasCoord.x * tileSize, blockType.bottomTextureAtlasCoord.y * tileSize);
        }
        else if (direction == new Vector3Int(-1, 0, 0)) // Left face
        {
            uvOffset = new Vector2(blockType.leftTextureAtlasCoord.x * tileSize, blockType.leftTextureAtlasCoord.y * tileSize);
        }
        else if (direction == new Vector3Int(1, 0, 0)) // Right face
        {
            uvOffset = new Vector2(blockType.rightTextureAtlasCoord.x * tileSize, blockType.rightTextureAtlasCoord.y * tileSize);
        }

        // Apply the UV offset to each vertex
        uv[0] = new Vector2(0, 0) * tileSize + uvOffset;
        uv[1] = new Vector2(1, 0) * tileSize + uvOffset;
        uv[2] = new Vector2(0, 1) * tileSize + uvOffset;
        uv[3] = new Vector2(1, 1) * tileSize + uvOffset;

        return uv;
    }

    public void DestroyVoxel(Vector3Int position)
    {
        if (IsPositionInBounds(position))
        {
            voxelData[position.x, position.y, position.z] = null;
            GenerateChunkMesh(); // Regenerate the mesh to reflect the change
        }
    }


    public void PlaceBlock(Vector3Int voxelPosition, BlockTypeData blockType)
    {
        // Check if the voxel position is within bounds of the chunk
        if (IsPositionInBounds(voxelPosition))
        {
            // Assign the block type at the specified voxel position
            voxelData[voxelPosition.x, voxelPosition.y, voxelPosition.z] = blockType;

            // Regenerate the chunk mesh to reflect the updated voxel data
            GenerateChunkMesh();
        }
        else
        {
            Debug.LogWarning("Attempted to place block outside chunk bounds.");
        }
    }
}
