using System.Collections.Generic;
using UnityEngine;

public class VisibilityChangeSet
{
    public readonly List<Vector2Int> BecameVisible = new();
    public readonly List<Vector2Int> BecameExplored = new();
    public readonly List<Vector2Int> BecameUnexplored = new();

    public bool HasAnyChange =>
        BecameVisible.Count > 0 ||
        BecameExplored.Count > 0 ||
        BecameUnexplored.Count > 0;

    public void Clear()
    {
        BecameVisible.Clear();
        BecameExplored.Clear();
        BecameUnexplored.Clear();
    }
}