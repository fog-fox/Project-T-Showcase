using UnityEngine;

public enum VisionBlockerType
{
    Wall,
    Smoke
}

public class VisionBlocker : MonoBehaviour
{
    [SerializeField] private VisionBlockerType blockerType = VisionBlockerType.Wall;

    public VisionBlockerType BlockerType => blockerType;

    public void SetBlockerType(VisionBlockerType newType)
    {
        blockerType = newType;
    }
}