using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(VisionBlocker))]
public class SmokePuff : MonoBehaviour
{
    private static readonly List<SmokePuff> ActivePuffs = new();

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D blockCollider;
    [SerializeField] private VisionBlocker visionBlocker;

    private ThrowableData smokeData;
    private SmokeField ownerField;

    private float lifetime;
    private float timer;
    private float startScale;
    private float endScale;
    private float visualScaleMultiplier = 1f;
    private float maxAlpha;
    private float riseDistance;

    private float driftSeedX;
    private float driftSeedY;

    private Vector3 logicalBasePosition;
    private Vector3 visualStartPosition;
    private bool initialized;

    public static IReadOnlyList<SmokePuff> ActiveSmokePuffs => ActivePuffs;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (blockCollider == null)
            blockCollider = GetComponent<CircleCollider2D>();

        if (visionBlocker == null)
            visionBlocker = GetComponent<VisionBlocker>();

        blockCollider.isTrigger = true;
        visionBlocker.SetBlockerType(VisionBlockerType.Smoke);
    }

    private void OnEnable()
    {
        if (!ActivePuffs.Contains(this))
            ActivePuffs.Add(this);
    }

    private void OnDisable()
    {
        ActivePuffs.Remove(this);
    }

    public void Initialize(
        ThrowableData data,
        SmokeField field,
        float puffLifetime,
        float puffStartScale,
        float puffEndScale,
        float puffVisualScaleMultiplier,
        float puffMaxAlpha,
        float puffRiseDistance)
    {
        smokeData = data;
        ownerField = field;
        lifetime = Mathf.Max(0.01f, puffLifetime);
        startScale = puffStartScale;
        endScale = puffEndScale;
        visualScaleMultiplier = Mathf.Max(0.01f, puffVisualScaleMultiplier);
        maxAlpha = puffMaxAlpha;
        riseDistance = puffRiseDistance;

        logicalBasePosition = transform.position;
        visualStartPosition = transform.position;

        driftSeedX = Random.Range(0f, 1000f);
        driftSeedY = Random.Range(0f, 1000f);

        blockCollider.radius = smokeData != null ? smokeData.smokePuffBlockRadius : 0.45f;

        transform.localScale = Vector3.one * (startScale * visualScaleMultiplier);
        SetAlpha(0f);

        initialized = true;
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        return blockCollider != null && blockCollider.OverlapPoint(worldPoint);
    }

    private void Update()
    {
        if (!initialized || smokeData == null || ownerField == null)
            return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        UpdateDriftMotion();
        UpdateVisual(t);

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateDriftMotion()
    {
        float time = Time.time;
        float noiseX = Mathf.PerlinNoise(driftSeedX, time * smokeData.smokePuffDriftSpeed) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(driftSeedY, time * smokeData.smokePuffDriftSpeed) * 2f - 1f;

        Vector3 randomDrift = new Vector3(noiseX, noiseY, 0f) * smokeData.smokePuffDriftSpeed * Time.deltaTime;

        logicalBasePosition += randomDrift;

        Vector3 toCenter = ownerField.Center - logicalBasePosition;
        float maxDriftRadius = Mathf.Max(0.05f, ownerField.CurrentRadius * smokeData.smokePuffMaxDriftRadiusMultiplier);

        if (toCenter.magnitude > maxDriftRadius)
        {
            logicalBasePosition += toCenter.normalized * smokeData.smokePuffReturnForce * Time.deltaTime;
        }

        // 장애물 안쪽으로 깊게 들어가는 이동 억제
        Vector2 moveDir = (logicalBasePosition - transform.position);
        float moveDistance = moveDir.magnitude;

        if (moveDistance > 0.001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                moveDir.normalized,
                moveDistance,
                smokeData.smokeObstacleMask
            );

            if (hit.collider == null)
            {
                transform.position = logicalBasePosition;
            }
        }
        else
        {
            transform.position = logicalBasePosition;
        }
    }

    private void UpdateVisual(float t)
    {
        float currentScale = Mathf.Lerp(startScale, endScale, t) * visualScaleMultiplier;
        transform.localScale = Vector3.one * currentScale;

        Vector3 lifted = transform.position + new Vector3(0f, Mathf.Lerp(0f, riseDistance, t), 0f);

        // 실제 충돌 위치와 시각 위치를 동일 transform으로 쓰므로,
        // 위로 뜨는 느낌은 스프라이트 렌더러 sorting / 알파로만 표현하고
        // 위치는 logicalBasePosition 유지
        // 필요 시 자식 visual 분리 가능
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        float alphaT;
        if (t < 0.2f)
            alphaT = Mathf.InverseLerp(0f, 0.2f, t);
        else if (t > 0.7f)
            alphaT = 1f - Mathf.InverseLerp(0.7f, 1f, t);
        else
            alphaT = 1f;

        SetAlpha(alphaT * maxAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }
}