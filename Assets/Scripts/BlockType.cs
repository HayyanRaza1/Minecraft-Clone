using UnityEngine;

[CreateAssetMenu(fileName = "BlockTypeData", menuName = "Block Type Data", order = 1)]
public class BlockTypeData : ScriptableObject
{
    public static BlockTypeData[] blockTypes; // Static array to hold block types

    public string blockName;
    public Vector2Int topTextureAtlasCoord = new Vector2Int(0, 0);
    public Vector2Int bottomTextureAtlasCoord = new Vector2Int(0, 0);
    public Vector2Int leftTextureAtlasCoord = new Vector2Int(0, 0);
    public Vector2Int rightTextureAtlasCoord = new Vector2Int(0, 0);
    public Vector2Int frontTextureAtlasCoord = new Vector2Int(0, 0); // Define front face texture coordinates
    public Vector2Int backTextureAtlasCoord = new Vector2Int(0, 0);  // Define back face texture coordinates

    // Example method to initialize or access block types
    public static BlockTypeData[] GetBlockTypes()
    {
        return blockTypes;
    }
}
