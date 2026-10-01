using System.Collections;
using UnityEngine;

public class SmokeField : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool drawRadiusGizmos = true;

    private ThrowableData smokeData;
    private float currentRadius;
    private float elapsed;
    private bool initialized;

    public float CurrentRadius => currentRadius;
    public Vector3 Center => transform.position;

    public void Initialize(ThrowableData data)
    {
        smokeData = data;
        initialized = true;
        elapsed = 0f;
        currentRadius = 0.01f;

        StartCoroutine(RunSmokeFieldRoutine());
    }

    private IEnumerator RunSmokeFieldRoutine()
    {
        if (!initialized || smokeData == null)
            yield break;

        float totalDuration =
            smokeData.smokeBuildUpTime +
            smokeData.smokeSustainTime +
            smokeData.smokeFadeOutTime;

        float nextEmitTime = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            float density = EvaluateDensity01(elapsed);
            currentRadius = Mathf.Lerp(0.01f, smokeData.smokeMaxRadius, density);

            if (Time.time >= nextEmitTime)
            {
                SpawnSmokeBurst(density);
                nextEmitTime = Time.time + smokeData.smokeEmitInterval;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private float EvaluateDensity01(float time)
    {
        float build = Mathf.Max(0.01f, smokeData.smokeBuildUpTime);
        float sustain = Mathf.Max(0f, smokeData.smokeSustainTime);
        float fade = Mathf.Max(0.01f, smokeData.smokeFadeOutTime);

        if (time < build)
            return Mathf.Clamp01(time / build);

        if (time < build + sustain)
            return 1f;

        float fadeElapsed = time - build - sustain;
        return 1f - Mathf.Clamp01(fadeElapsed / fade);
    }

    private void SpawnSmokeBurst(float density)
    {
        if (smokeData == null || smokeData.smokePuffPrefab == null || density <= 0.01f)
            return;

        int minCount = Mathf.Max(1, smokeData.smokePuffsPerBurstMin);
        int maxCount = Mathf.Max(minCount, smokeData.smokePuffsPerBurstMax);
        int spawnCount = Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, density));
        spawnCount = Mathf.Max(1, spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPointInCurrentRadius();

            float jitter = smokeData.smokePuffPositionJitter;
            if (jitter > 0f)
                spawnPos += (Vector3)(Random.insideUnitCircle * jitter);

            GameObject puffObj = Instantiate(smokeData.smokePuffPrefab, spawnPos, Quaternion.identity);

            SmokePuff puff = puffObj.GetComponent<SmokePuff>();
            if (puff == null)
            {
                Debug.LogWarning("SmokeField: smokePuffPrefab에 SmokePuff 컴포넌트가 없습니다.");
                Destroy(puffObj);
                continue;
            }

            float centerBias = 1f - Mathf.Clamp01(Vector2.Distance(spawnPos, transform.position) / Mathf.Max(0.01f, currentRadius));
            float alphaMultiplier = Mathf.Lerp(0.8f, smokeData.smokeCenterDensityMultiplier, centerBias);

            puff.Initialize(
                smokeData,
                this,
                smokeData.smokePuffLifetime,
                smokeData.smokePuffStartScale,
                smokeData.smokePuffEndScale,
                smokeData.smokePuffVisualScaleMultiplier,
                smokeData.smokePuffMaxAlpha * density * alphaMultiplier,
                smokeData.smokePuffRiseDistance
            );
        }
    }

    private Vector3 GetRandomSpawnPointInCurrentRadius()
    {
        Vector2 origin = transform.position;
        float radius = Mathf.Max(0.05f, currentRadius);

        for (int i = 0; i < 24; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * radius;

            if (!IsBlockedByObstacle(origin, candidate))
                return candidate;
        }

        return transform.position;
    }

    private bool IsBlockedByObstacle(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float distance = dir.magnitude;

        if (distance <= 0.001f)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            from,
            dir.normalized,
            distance,
            smokeData.smokeObstacleMask
        );

        return hit.collider != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawRadiusGizmos)
            return;

        Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
#endif
}