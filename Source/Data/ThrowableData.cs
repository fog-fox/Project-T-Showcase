using UnityEngine;

public enum ThrowableEffectType
{
    Explosive,
    Smoke
}

[CreateAssetMenu(fileName = "NewThrowableData", menuName = "Weapons/Throwable Data")]
public class ThrowableData : ScriptableObject
{
    [Header("Basic Info")]
    public string throwableName;
    public Sprite icon;
    public ThrowableEffectType effectType = ThrowableEffectType.Explosive;

    [Header("Throw Settings")]
    public float minChargeTime = 0.15f;
    public float maxChargeTime = 1.2f;
    public float minThrowDistance = 1.5f;
    public float maxThrowDistance = 7f;

    [Header("Throw Speed Settings")]
    public float minThrowSpeed = 5f;
    public float maxThrowSpeed = 14f;

    [Header("Common Fuse Settings")]
    public float fuseTime = 1.5f;

    [Header("Explosion Settings")]
    public float explosionRadius = 2.5f;
    public float damage = 40f;

    [Header("Smoke Logic Settings")]
    [Tooltip("연막 작용 반경이 최대까지 퍼지는 시간")]
    public float smokeBuildUpTime = 1.0f;

    [Tooltip("연막이 최대 반경 상태로 유지되는 시간")]
    public float smokeSustainTime = 4.0f;

    [Tooltip("연막이 점차 옅어지는 시간")]
    public float smokeFadeOutTime = 2.0f;

    [Tooltip("연막 생성 가능 최대 반경")]
    public float smokeMaxRadius = 3.5f;

    [Tooltip("연막 범위가 관통하지 못하는 장애물 레이어")]
    public LayerMask smokeObstacleMask;

    [Header("Smoke Spawn Settings")]
    [Tooltip("연막 생성 제어용 필드 프리팹")]
    public GameObject smokeFieldPrefab;

    [Tooltip("개별 연기 퍼프 프리팹")]
    public GameObject smokePuffPrefab;

    [Tooltip("퍼프 생성 간격")]
    public float smokeEmitInterval = 0.12f;

    [Tooltip("한 번 생성 시 최소 퍼프 수")]
    public int smokePuffsPerBurstMin = 2;

    [Tooltip("한 번 생성 시 최대 퍼프 수")]
    public int smokePuffsPerBurstMax = 5;

    [Tooltip("퍼프 하나의 생존 시간")]
    public float smokePuffLifetime = 2.8f;

    [Tooltip("퍼프 시작 스케일")]
    public float smokePuffStartScale = 0.5f;

    [Tooltip("퍼프 종료 스케일")]
    public float smokePuffEndScale = 1.4f;

    [Tooltip("기존 스프라이트를 건드리지 않고 퍼프 표시 크기를 키우는 배율")]
    public float smokePuffVisualScaleMultiplier = 2.5f;

    [Tooltip("퍼프 최대 알파")]
    [Range(0f, 1f)]
    public float smokePuffMaxAlpha = 0.45f;

    [Tooltip("퍼프가 위로 떠오르는 거리")]
    public float smokePuffRiseDistance = 0.15f;

    [Tooltip("퍼프 생성 위치 랜덤 오프셋")]
    public float smokePuffPositionJitter = 0.25f;

    [Tooltip("중앙부 밀도 보정")]
    public float smokeCenterDensityMultiplier = 1.25f;

    [Header("Smoke Puff Blocking")]
    [Tooltip("퍼프 하나가 실제로 시야를 차단하는 반경")]
    public float smokePuffBlockRadius = 0.45f;

    [Tooltip("플레이어가 연막 내부로 판정되기 위한 최소 겹침 퍼프 수")]
    public int smokeInsideRequiredPuffs = 2;

    [Header("Smoke Puff Motion")]
    [Tooltip("퍼프의 랜덤 드리프트 속도")]
    public float smokePuffDriftSpeed = 0.35f;

    [Tooltip("퍼프가 중심에서 너무 멀리 벗어나지 않도록 잡아주는 강도")]
    public float smokePuffReturnForce = 0.6f;

    [Tooltip("퍼프가 생성 중심에서 벗어날 수 있는 최대 거리 비율")]
    public float smokePuffMaxDriftRadiusMultiplier = 1.15f;

    [Header("Prefab")]
    [Tooltip("실제 투척되어 생성될 프리팹")]
    public GameObject projectilePrefab;
}