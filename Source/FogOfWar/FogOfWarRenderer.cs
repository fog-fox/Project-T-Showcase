using UnityEngine;
using UnityEngine.Tilemaps;

public class FogOfWarRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FogOfWarGrid fogGrid;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap unexploredTilemap;
    [SerializeField] private Tilemap exploredTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase unexploredTile;
    [SerializeField] private TileBase exploredTile;

    private bool initialized;

    private void Start()
    {
        InitializeAllUnexplored();
    }

    [ContextMenu("Initialize All Unexplored")]
    public void InitializeAllUnexplored()
    {
        if (fogGrid == null || unexploredTilemap == null || exploredTilemap == null)
        {
            Debug.LogWarning("FogOfWarRenderer: 참조가 비어 있습니다.");
            return;
        }

        unexploredTilemap.ClearAllTiles();
        exploredTilemap.ClearAllTiles();

        for (int x = 0; x < fogGrid.Width; x++)
        {
            for (int y = 0; y < fogGrid.Height; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                unexploredTilemap.SetTile(tilePos, unexploredTile);
            }
        }

        initialized = true;
    }

    public void ApplyChanges(VisibilityChangeSet changes)
    {
        if (!initialized)
        {
            InitializeAllUnexplored();
        }

        if (changes == null || !changes.HasAnyChange)
            return;

        // 새로 Visible 된 셀: 양쪽 타일맵에서 제거
        foreach (Vector2Int cell in changes.BecameVisible)
        {
            Vector3Int tilePos = ToTilePos(cell);
            unexploredTilemap.SetTile(tilePos, null);
            exploredTilemap.SetTile(tilePos, null);
        }

        // Explored 로 바뀐 셀: unexplored 제거, explored 배치
        foreach (Vector2Int cell in changes.BecameExplored)
        {
            Vector3Int tilePos = ToTilePos(cell);
            unexploredTilemap.SetTile(tilePos, null);
            exploredTilemap.SetTile(tilePos, exploredTile);
        }

        // 현재 구조에서는 거의 쓰지 않지만 확장 대비
        foreach (Vector2Int cell in changes.BecameUnexplored)
        {
            Vector3Int tilePos = ToTilePos(cell);
            exploredTilemap.SetTile(tilePos, null);
            unexploredTilemap.SetTile(tilePos, unexploredTile);
        }
    }

    private Vector3Int ToTilePos(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }
}