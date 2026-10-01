using UnityEngine;

public class VisibilityObjectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FogOfWarGrid fogGrid;

    [Header("Render Targets")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private Canvas[] worldCanvases;

    [Header("Optional Toggle Targets")]
    [Tooltip("시야 밖일 때 함께 꺼야 하는 추가 Behaviour들")]
    [SerializeField] private Behaviour[] extraBehavioursToToggle;

    [Tooltip("시야 밖일 때 통째로 숨길 UI 루트 오브젝트들")]
    [SerializeField] private GameObject[] extraGameObjectsToToggle;

    [Header("Auto Collect")]
    [Tooltip("비어 있을 경우 자식 SpriteRenderer를 자동 수집")]
    [SerializeField] private bool autoCollectSpriteRenderers = true;

    [Tooltip("비어 있을 경우 자식 Canvas를 자동 수집")]
    [SerializeField] private bool autoCollectCanvases = true;

    [Header("Options")]
    [SerializeField] private bool hideOnStartUntilEvaluated = true;

    private bool currentVisibleState;

    private void Awake()
    {
        if (autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (autoCollectCanvases && (worldCanvases == null || worldCanvases.Length == 0))
        {
            worldCanvases = GetComponentsInChildren<Canvas>(true);
        }

        if (hideOnStartUntilEvaluated)
        {
            ApplyVisibleState(false);
            currentVisibleState = false;
        }
    }

    private void OnEnable()
    {
        if (fogGrid != null)
        {
            fogGrid.OnVisibilityUpdated += HandleVisibilityUpdated;
        }

        RefreshNow();
    }

    private void OnDisable()
    {
        if (fogGrid != null)
        {
            fogGrid.OnVisibilityUpdated -= HandleVisibilityUpdated;
        }
    }

    public void RefreshNow()
    {
        if (fogGrid == null)
            return;

        bool isVisible = fogGrid.IsWorldPositionVisible(transform.position);

        if (currentVisibleState == isVisible)
            return;

        currentVisibleState = isVisible;
        ApplyVisibleState(isVisible);
    }

    private void HandleVisibilityUpdated()
    {
        RefreshNow();
    }

    private void ApplyVisibleState(bool visible)
    {
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null)
                    sr.enabled = visible;
            }
        }

        if (worldCanvases != null)
        {
            foreach (Canvas canvas in worldCanvases)
            {
                if (canvas != null)
                    canvas.enabled = visible;
            }
        }

        if (extraBehavioursToToggle != null)
        {
            foreach (Behaviour behaviour in extraBehavioursToToggle)
            {
                if (behaviour != null)
                    behaviour.enabled = visible;
            }
        }

        if (extraGameObjectsToToggle != null)
        {
            foreach (GameObject go in extraGameObjectsToToggle)
            {
                if (go != null)
                    go.SetActive(visible);
            }
        }
    }
}