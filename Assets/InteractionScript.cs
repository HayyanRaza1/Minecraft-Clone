using UnityEngine;

public class VoxelInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionRange = 5.0f;
    public LayerMask voxelLayer; // Layer mask to filter voxels
    public BlockTypeData blockToPlace; // Assign this in the inspector

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button for destruction
        {
            DestroyVoxel();
        }
        else if (Input.GetMouseButtonDown(1)) // Right mouse button for placement
        {
            PlaceBlock();
        }
    }

    void DestroyVoxel()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, voxelLayer))
        {
            // Check if the ray hit a voxel (chunk) game object
            Chunk chunk = hit.transform.GetComponent<Chunk>();
            if (chunk != null)
            {
                // Convert hit point from world space to local voxel coordinates
                Vector3 localHitPoint = hit.point - chunk.transform.position;
                Vector3Int voxelPosition = new Vector3Int(
                    Mathf.FloorToInt(localHitPoint.x),
                    Mathf.FloorToInt(localHitPoint.y),
                    Mathf.FloorToInt(localHitPoint.z)
                );

                // Now, voxelPosition contains the local coordinates of the voxel within the chunk
                // You can modify the voxel data or destroy the voxel here
                chunk.DestroyVoxel(voxelPosition);
            }
        }
    }

    void PlaceBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            // Check if the ray hit a voxel (chunk) game object
            Chunk chunk = hit.transform.GetComponent<Chunk>();
            if (chunk != null)
            {
                // Convert hit point from world space to local voxel coordinates
                Vector3 localHitPoint = hit.point - chunk.transform.position;
                Vector3Int voxelPosition = new Vector3Int(
                    Mathf.FloorToInt(localHitPoint.x),
                    Mathf.FloorToInt(localHitPoint.y),
                    Mathf.FloorToInt(localHitPoint.z)
                );

                // Now, voxelPosition contains the local coordinates of the voxel within the chunk
                // You can modify the voxel data or place a new block here
                chunk.PlaceBlock(voxelPosition, blockToPlace);
            }
        }
    }
}
