using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Voxel : MonoBehaviour
{
    public Material atlasMaterial;
    public BlockTypeData blockType; // Assigned block type

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private const int atlasWidth = 4; // Assuming a 4x4 texture atlas

    // Example 2D array of texture coordinates
    private Vector2Int[,] textureCoordinates = new Vector2Int[atlasWidth, atlasWidth];

    // Static array for face directions
    public static readonly Vector3[] faceChecks = new Vector3[6] {
        new Vector3(0.0f, 0.0f, -1.0f), // Back face
        new Vector3(0.0f, 0.0f, 1.0f),  // Front face
        new Vector3(0.0f, 1.0f, 0.0f),  // Top face
        new Vector3(0.0f, -1.0f, 0.0f), // Bottom face
        new Vector3(-1.0f, 0.0f, 0.0f), // Left face
        new Vector3(1.0f, 0.0f, 0.0f)   // Right face
    };

    void Start()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        // Setup texture coordinates (example)
        SetupTextureCoordinates();
        GenerateVoxel();
    }

    void SetupTextureCoordinates()
    {
        // Example: Assigning texture coordinates in a 4x4 atlas
        for (int x = 0; x < atlasWidth; x++)
        {
            for (int y = 0; y < atlasWidth; y++)
            {
                textureCoordinates[x, y] = new Vector2Int(x, y);
            }
        }
    }

    public void GenerateVoxel()
    {
        Vector3[] vertices = {
            // Bottom face
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1),
            // Top face
            new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
            // Front face
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
            // Back face
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0),
            // Left face
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0),
            // Right face
            new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0)
        };

        int[] triangles = {
            // Bottom face
            0, 1, 2, 0, 2, 3,
            // Top face
            4, 6, 5, 4, 7, 6,
            // Front face
            8, 9, 10, 8, 10, 11,
            // Back face
            12, 15, 14, 12, 14, 13,
            // Left face
            16, 17, 18, 16, 18, 19,
            // Right face
            20, 23, 22, 20, 22, 21
        };

        Vector2[] uvs = new Vector2[24];

        // Set UVs for each face
        SetUVs(uvs, 0, blockType.bottomTextureAtlasCoord);
        SetUVs(uvs, 4, blockType.topTextureAtlasCoord);
        SetUVs(uvs, 8, blockType.frontTextureAtlasCoord);
        SetUVs(uvs, 12, blockType.backTextureAtlasCoord);
        SetUVs(uvs, 16, blockType.leftTextureAtlasCoord);
        SetUVs(uvs, 20, blockType.rightTextureAtlasCoord);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = atlasMaterial;
        meshCollider.sharedMesh = mesh;

        ApplyFaceCulling();
    }

    private void SetUVs(Vector2[] uvs, int startIndex, Vector2Int atlasCoord)
    {
        float tileSize = 1.0f / atlasWidth; // Calculate tile size based on atlas width

        // Calculate UV coordinates for each vertex of the quad (face)
        uvs[startIndex + 0] = new Vector2(atlasCoord.x * tileSize, atlasCoord.y * tileSize);
        uvs[startIndex + 1] = new Vector2((atlasCoord.x + 1) * tileSize, atlasCoord.y * tileSize);
        uvs[startIndex + 2] = new Vector2((atlasCoord.x + 1) * tileSize, (atlasCoord.y + 1) * tileSize);
        uvs[startIndex + 3] = new Vector2(atlasCoord.x * tileSize, (atlasCoord.y + 1) * tileSize);
    }

    private void ApplyFaceCulling()
    {
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        List<int> culledTriangles = new List<int>();

        // Iterate through each face (6 faces in total)
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 vertexA = vertices[triangles[i]];
            Vector3 vertexB = vertices[triangles[i + 1]];
            Vector3 vertexC = vertices[triangles[i + 2]];

            Vector3 faceNormal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

            // Calculate the center of the face
            Vector3 faceCenter = (vertexA + vertexB + vertexC) / 3f;

            // Check if the face is exposed to air (not covered by another voxel)
            bool isVisible = IsFaceVisible(faceCenter, faceNormal);

            if (isVisible)
            {
                culledTriangles.Add(triangles[i]);
                culledTriangles.Add(triangles[i + 1]);
                culledTriangles.Add(triangles[i + 2]);
            }
        }

        // Update the mesh with the culled triangles
        mesh.triangles = culledTriangles.ToArray();
        mesh.RecalculateNormals();
    }

    private bool IsFaceVisible(Vector3 faceCenter, Vector3 faceNormal)
    {
        // Cast a ray from the face center outward to check for obstructions
        float rayLength = 0.51f; // Slightly more than half the voxel size to avoid floating point precision issues
        RaycastHit hit;

        if (Physics.Raycast(faceCenter, faceNormal, out hit, rayLength))
        {
            // Check if the hit point is within the same voxel's bounds
            if (hit.collider.gameObject == gameObject)
            {
                return false; // Face is obstructed by another part of the same voxel
            }
            else
            {
                return true; // Face is exposed to air
            }
        }

        return true; // No obstruction found, face is exposed to air
    }

    // Method to retrieve texture coordinates for a specific face
    public Vector2Int GetTextureCoordinates(VoxelFace face)
    {
        switch (face)
        {
            case VoxelFace.Bottom:
                return blockType.bottomTextureAtlasCoord;
            case VoxelFace.Top:
                return blockType.topTextureAtlasCoord;
            case VoxelFace.Front:
                return blockType.frontTextureAtlasCoord;
            case VoxelFace.Back:
                return blockType.backTextureAtlasCoord;
            case VoxelFace.Left:
                return blockType.leftTextureAtlasCoord;
            case VoxelFace.Right:
                return blockType.rightTextureAtlasCoord;
            default:
                return Vector2Int.zero;
        }
    }

    // Method to retrieve the block type
    public BlockTypeData GetBlockType()
    {
        return blockType;
    }
}

public enum VoxelFace
{
    Bottom,
    Top,
    Front,
    Back,
    Left,
    Right
}
