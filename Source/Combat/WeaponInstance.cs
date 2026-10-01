using UnityEngine;

/// <summary>
/// 총기 프리팹에 부착되는 런타임 총기 정보 컴포넌트
/// - 총기의 고정 데이터(GunStats, 기본 탄종)
/// - 현재 사용 탄종, 현재 장탄수
/// - 장착 상태 / 드롭 상태 전환
/// 를 관리합니다.
/// </summary>
public class WeaponInstance : MonoBehaviour
{
    [Header("Static Data")]
    [Tooltip("총기의 고정 성능 데이터")]
    public GunStats gunStats;

    [Tooltip("총기의 기본 탄종")]
    public AmmoType defaultAmmoType;

    [Header("Runtime Data")]
    [Tooltip("현재 사용 중인 탄종")]
    public AmmoType currentAmmoType;

    [Tooltip("현재 탄창에 남아 있는 탄 수")]
    public int currentAmmo;

    [Tooltip("런타임 초기화 여부")]
    public bool initialized;

    [Header("References")]
    [Tooltip("바닥에 떨어졌을 때 줍기용 콜라이더")]
    [SerializeField] private Collider2D pickupCollider;

    [Tooltip("필요 시 물리 처리용 Rigidbody2D")]
    [SerializeField] private Rigidbody2D rb2D;

    [Tooltip("바닥에 떨어진 무기 상태를 나타내는 컴포넌트")]
    [SerializeField] private DroppedWeapon droppedWeapon;

    [Tooltip("바닥 무기 상호작용용 컴포넌트")]
    [SerializeField] private DroppedWeaponInteractable droppedWeaponInteractable;

    private Vector3 originalLocalScale;

    private void Awake()
    {
        originalLocalScale = transform.localScale;

        // 자동 참조 보정
        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();

        if (rb2D == null)
            rb2D = GetComponent<Rigidbody2D>();

        if (droppedWeapon == null)
            droppedWeapon = GetComponent<DroppedWeapon>();

        if (droppedWeaponInteractable == null)
            droppedWeaponInteractable = GetComponent<DroppedWeaponInteractable>();
    }

    public void Initialize()
    {
        if (gunStats == null)
        {
            Debug.LogError($"{name}: gunStats가 비어 있습니다.");
            return;
        }

        if (defaultAmmoType == null)
        {
            Debug.LogError($"{name}: defaultAmmoType이 비어 있습니다.");
            return;
        }

        if (currentAmmoType == null)
            currentAmmoType = defaultAmmoType;

        if (!initialized)
        {
            currentAmmo = gunStats.magazineSize;
            initialized = true;
        }
    }

    public void InitializeRuntime(int ammo, AmmoType ammoType)
    {
        if (gunStats == null)
        {
            Debug.LogError($"{name}: gunStats가 비어 있습니다.");
            return;
        }

        if (defaultAmmoType == null)
        {
            Debug.LogError($"{name}: defaultAmmoType이 비어 있습니다.");
            return;
        }

        currentAmmo = ammo;
        currentAmmoType = ammoType != null ? ammoType : defaultAmmoType;
        initialized = true;
    }

    /// <summary>
    /// 플레이어가 손에 들고 있는 상태
    /// - 충돌/상호작용 비활성화
    /// </summary>
    public void SetEquippedState()
    {
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (rb2D != null)
        {
            rb2D.simulated = false;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        if (droppedWeapon != null)
            droppedWeapon.enabled = false;

        if (droppedWeaponInteractable != null)
            droppedWeaponInteractable.enabled = false;

        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 바닥에 떨어진 상태
    /// - 충돌/상호작용 활성화
    /// </summary>
    public void SetDroppedState()
    {
        if (pickupCollider != null)
        {
            pickupCollider.enabled = true;
            pickupCollider.isTrigger = true;
        }

        if (rb2D != null)
        {
            rb2D.simulated = false;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        if (droppedWeapon != null)
            droppedWeapon.enabled = true;

        if (droppedWeaponInteractable != null)
            droppedWeaponInteractable.enabled = true;

        transform.rotation = Quaternion.identity;
        transform.localScale = originalLocalScale;
    }
}