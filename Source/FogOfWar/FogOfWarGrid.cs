using System;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWarGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2 worldOrigin = new Vector2(-20f, -20f);
    [SerializeField] private int width = 40;
    [SerializeField] private int height = 40;
    [SerializeField] private float cellSize = 1f;

    private VisibilityState[,] states;
    private bool isInitialized;

    private readonly HashSet<Vector2Int> previousVisibleCells = new();
    private readonly HashSet<Vector2Int> tempRemovedCells = new();
    private readonly HashSet<Vector2Int> tempAddedCells = new();

    public event Action OnVisibilityUpdated;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public Vector2 WorldOrigin => worldOrigin;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (isInitialized && states != null)
            return;

        states = new VisibilityState[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                states[x, y] = VisibilityState.Unexplored;
            }
        }

        previousVisibleCells.Clear();
        tempRemovedCells.Clear();
        tempAddedCells.Clear();

        isInitialized = true;
    }

    public bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width &&
               cell.y >= 0 && cell.y < height;
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - worldOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - worldOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        return new Vector3(
            worldOrigin.x + (cell.x + 0.5f) * cellSize,
            worldOrigin.y + (cell.y + 0.5f) * cellSize,
            0f
        );
    }

    public VisibilityState GetCellState(Vector2Int cell)
    {
        EnsureInitialized();

        if (!IsInBounds(cell))
            return VisibilityState.Unexplored;

        return states[cell.x, cell.y];
    }

    public bool IsWorldPositionVisible(Vector3 worldPos)
    {
        EnsureInitialized();

        Vector2Int cell = WorldToCell(worldPos);
        return IsInBounds(cell) && states[cell.x, cell.y] == VisibilityState.Visible;
    }

    public bool IsWorldPositionExplored(Vector3 worldPos)
    {
        EnsureInitialized();

        Vector2Int cell = WorldToCell(worldPos);

        if (!IsInBounds(cell))
            return false;

        VisibilityState state = states[cell.x, cell.y];
        return state == VisibilityState.Visible || state == VisibilityState.Explored;
    }

    public VisibilityChangeSet UpdateVisibleCells(HashSet<Vector2Int> newVisibleCells)
    {
        EnsureInitialized();

        VisibilityChangeSet changes = new VisibilityChangeSet();

        tempRemovedCells.Clear();
        tempAddedCells.Clear();

        // previous - current
        tempRemovedCells.UnionWith(previousVisibleCells);
        tempRemovedCells.ExceptWith(newVisibleCells);

        // current - previous
        tempAddedCells.UnionWith(newVisibleCells);
        tempAddedCells.ExceptWith(previousVisibleCells);

        // Visible -> Explored
        foreach (Vector2Int cell in tempRemovedCells)
        {
            if (!IsInBounds(cell))
                continue;

            if (states[cell.x, cell.y] == VisibilityState.Visible)
            {
                states[cell.x, cell.y] = VisibilityState.Explored;
                changes.BecameExplored.Add(cell);
            }
        }

        // New Visible
        foreach (Vector2Int cell in tempAddedCells)
        {
            if (!IsInBounds(cell))
                continue;

            states[cell.x, cell.y] = VisibilityState.Visible;
            changes.BecameVisible.Add(cell);
        }

        // 유지되는 Visible 셀 보정
        foreach (Vector2Int cell in newVisibleCells)
        {
            if (!IsInBounds(cell))
                continue;

            if (states[cell.x, cell.y] != VisibilityState.Visible)
            {
                states[cell.x, cell.y] = VisibilityState.Visible;
            }
        }

        previousVisibleCells.Clear();
        previousVisibleCells.UnionWith(newVisibleCells);

        if (changes.HasAnyChange)
        {
            OnVisibilityUpdated?.Invoke();
        }

        return changes;
    }

    public void ForceVisibilityRefreshEvent()
    {
        EnsureInitialized();
        OnVisibilityUpdated?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;

        Vector3 size = new Vector3(width * cellSize, height * cellSize, 0f);
        Vector3 center = new Vector3(
            worldOrigin.x + size.x * 0.5f,
            worldOrigin.y + size.y * 0.5f,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
#endif
}