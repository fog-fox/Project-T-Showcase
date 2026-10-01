using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject bulletLinePrefab;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image reloadBar;
    [SerializeField] private PlayerReloadUI reloadUI;

    [Header("Hit Settings")]
    [SerializeField] private LayerMask shootHitMask;

    private PlayerInputActions inputActions;

    private bool isAiming = false;
    private bool isReloading = false;
    private bool shootingHeld = false;

    private float nextFireTime = 0f;
    private Coroutine reloadCoroutine;

    [Header("Accuracy Multipliers")]
    [Tooltip("조준 중 명중률 배율")]
    [SerializeField] private float aimingAccuracyMultiplier = 1.1f;

    [Tooltip("일반 이동 중 명중률 배율")]
    [SerializeField] private float movingAccuracyMultiplier = 0.8f;

    [Tooltip("걷기 중 명중률 배율")]
    [SerializeField] private float walkingAccuracyMultiplier = 0.9f;

    [Tooltip("정지 상태 명중률 배율")]
    [SerializeField] private float standingAccuracyMultiplier = 1.0f;

    [Tooltip("탄 퍼짐 반동 최대 시 명중률 하한 배율")]
    [SerializeField] private float minRecoilAccuracyMultiplier = 0.5f;

    [Header("Spread / Recoil")]
    [Tooltip("한 발 쏠 때마다 증가하는 탄 퍼짐 반동")]
    [SerializeField] private float recoilIncreasePerShot = 0.05f;

    [Tooltip("탄 퍼짐 반동 회복 속도")]
    [SerializeField] private float recoilRecoverySpeed = 1.0f;

    private float currentRecoil = 0f;

    [Header("Aim Recoil Settings")]
    [Tooltip("조준점 반동 회복 속도")]
    [SerializeField] private float aimRecoilRecoverySpeed = 10f;

    [Tooltip("조준 중 조준점 반동 감소 배수")]
    [SerializeField] private float aimingRecoilMultiplier = 0.75f;

    [Tooltip("정지 사격 시 조준점 반동 감소 배수")]
    [SerializeField] private float standingRecoilMultiplier = 0.85f;

    [Tooltip("걷기 중 조준점 반동 배수")]
    [SerializeField] private float walkingRecoilMultiplier = 1.0f;

    [Tooltip("일반 이동 중 조준점 반동 배수")]
    [SerializeField] private float movingRecoilMultiplier = 1.2f;

    [Tooltip("수직 반동을 화면 픽셀 오프셋으로 변환하는 배수")]
    [SerializeField] private float verticalRecoilPixelMultiplier = 35f;

    [Tooltip("수평 반동을 화면 픽셀 오프셋으로 변환하는 배수")]
    [SerializeField] private float horizontalRecoilPixelMultiplier = 20f;

    [Tooltip("현재 누적된 조준점 스크린 반동 오프셋")]
    [SerializeField] private Vector2 currentAimRecoilScreenOffset;

    [Header("Spread Screen Settings")]
    [Tooltip("탄 퍼짐 각도를 화면 픽셀 반경으로 변환하는 배수")]
    [SerializeField] private float spreadToScreenPixelScale = 6f;

    [Tooltip("기본 최소 퍼짐 반경")]
    [SerializeField] private float baseSpreadScreenRadius = 12f;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;

    public WeaponInstance currentWeapon;

    public GunStats EquippedGun => currentWeapon != null ? currentWeapon.gunStats : null;
    public AmmoType EquippedAmmo => currentWeapon != null ? currentWeapon.currentAmmoType : null;
    public int CurrentAmmo => currentWeapon != null ? currentWeapon.currentAmmo : 0;
    public bool IsAiming => isAiming;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();

            inputActions.Player.Shoot.performed += _ => StartShooting();
            inputActions.Player.Shoot.canceled += _ => StopShooting();

            inputActions.Player.Aim.performed += _ => isAiming = true;
            inputActions.Player.Aim.canceled += _ => isAiming = false;

            inputActions.Player.Reload.performed += _ => TryReload();

            inputActions.Player.Run.performed += _ => CancelReload();
            inputActions.Player.Dash.performed += _ => CancelReload();
        }

        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
    }

    private void Update()
    {
        RecoverRecoil();
    }

    /// <summary>
    /// 외부(투척, 카메라 등)에서 조준 상태를 강제로 해제할 때 사용
    /// </summary>
    public void ForceCancelAim()
    {
        isAiming = false;
    }

    /// <summary>
    /// 인벤토리에서 새 무기를 장착할 때 호출
    /// </summary>
    public void SetWeapon(WeaponInstance weaponInstance)
    {
        if (isReloading)
            CancelReload();

        currentWeapon = weaponInstance;
        currentRecoil = 0f;
        currentAimRecoilScreenOffset = Vector2.zero;
        UpdateAmmoUI();
    }

    /// <summary>
    /// 실제 입력된 마우스 스크린 좌표
    /// </summary>
    public Vector2 GetRawMouseScreenPosition()
    {
        if (Mouse.current == null)
            return Vector2.zero;

        return Mouse.current.position.ReadValue();
    }

    /// <summary>
    /// 반동 적용 후, 사거리 제한까지 반영된 현재 조준점의 스크린 좌표
    /// 크로스헤어 UI가 이 좌표를 따라감
    /// </summary>
    public Vector2 GetCurrentAimScreenPosition()
    {
        if (mainCamera == null)
            return GetRawMouseScreenPosition();

        Vector3 clampedAimWorldPos = GetCurrentAimWorldPosition();
        Vector3 clampedAimScreenPos = mainCamera.WorldToScreenPoint(clampedAimWorldPos);

        return new Vector2(clampedAimScreenPos.x, clampedAimScreenPos.y);
    }

    /// <summary>
    /// 반동 적용 후, 총기 사거리 안으로 제한된 현재 조준점 월드 좌표
    /// </summary>
    public Vector3 GetCurrentAimWorldPosition()
    {
        if (mainCamera == null)
            return transform.position;

        Vector2 rawAimScreenPos = GetRawMouseScreenPosition() + currentAimRecoilScreenOffset;
        float zDistance = Mathf.Abs(mainCamera.transform.position.z - firePoint.position.z);

        Vector3 rawAimWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(rawAimScreenPos.x, rawAimScreenPos.y, zDistance)
        );

        rawAimWorldPos.z = 0f;

        // 총기 사거리 = 조준점이 이동 가능한 최대 거리
        if (EquippedGun != null && firePoint != null)
        {
            Vector2 fromFirePoint = rawAimWorldPos - firePoint.position;
            Vector2 clamped = Vector2.ClampMagnitude(fromFirePoint, EquippedGun.range);

            Vector3 clampedAimWorldPos = firePoint.position + (Vector3)clamped;
            clampedAimWorldPos.z = 0f;
            return clampedAimWorldPos;
        }

        return rawAimWorldPos;
    }

    /// <summary>
    /// 크로스헤어 벌어짐 계산에 사용하는 현재 퍼짐 각도
    /// </summary>
    public float GetCurrentSpreadAngle()
    {
        if (EquippedGun == null || EquippedAmmo == null)
            return 0f;

        float finalAccuracy = CalculateFinalAccuracy();
        float baseSpread = (1f - finalAccuracy) * 10f;
        float recoilSpread = currentRecoil * 5f;

        return baseSpread + recoilSpread;
    }

    private float GetMovementAccuracyMultiplier()
    {
        if (playerMovement == null)
            return 1f;

        if (playerMovement.IsMoving)
        {
            if (playerMovement.IsWalking)
                return walkingAccuracyMultiplier;

            return movingAccuracyMultiplier;
        }

        return standingAccuracyMultiplier;
    }

    private float GetAimAccuracyMultiplier()
    {
        return isAiming ? aimingAccuracyMultiplier : 1f;
    }

    private float GetRecoilAccuracyMultiplier()
    {
        return Mathf.Lerp(1f, minRecoilAccuracyMultiplier, currentRecoil);
    }

    private float GetCurrentAimRecoilMultiplier()
    {
        float multiplier = 1f;

        bool isMoving = playerMovement != null && playerMovement.IsMoving;
        bool isWalking = playerMovement != null && playerMovement.IsWalking;

        if (isMoving)
            multiplier *= isWalking ? walkingRecoilMultiplier : movingRecoilMultiplier;
        else
            multiplier *= standingRecoilMultiplier;

        if (isAiming)
            multiplier *= aimingRecoilMultiplier;

        return multiplier;
    }

    /// <summary>
    /// 조준점 자체를 밀어내는 반동 적용
    /// 수직: 현재 조준 방향 앞쪽
    /// 수평: 현재 조준 방향 기준 좌우 랜덤
    /// </summary>
    private void ApplyAimRecoil()
    {
        if (EquippedGun == null || mainCamera == null)
            return;

        Vector2 firePointScreen = mainCamera.WorldToScreenPoint(firePoint.position);
        Vector2 currentAimScreen = GetCurrentAimScreenPosition();

        Vector2 forward = currentAimScreen - firePointScreen;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector2.up;
        else
            forward.Normalize();

        Vector2 right = new Vector2(-forward.y, forward.x);

        float recoilMultiplier = GetCurrentAimRecoilMultiplier();

        float verticalKick = EquippedGun.verticalRecoil * verticalRecoilPixelMultiplier * recoilMultiplier;
        float horizontalKick = Random.Range(
            -EquippedGun.horizontalRecoil,
            EquippedGun.horizontalRecoil
        ) * horizontalRecoilPixelMultiplier * recoilMultiplier;

        Vector2 recoilKick = forward * verticalKick + right * horizontalKick;

        currentAimRecoilScreenOffset += recoilKick;
    }

    /// <summary>
    /// 현재 퍼짐 각도에 대응하는, 조준점 주변 원형 랜덤 오프셋
    /// 크로스헤어 크기와 같은 의미를 가짐
    /// </summary>
    private Vector2 GetRandomSpreadScreenOffset(float spreadAngle)
    {
        float spreadRadius = baseSpreadScreenRadius + (spreadAngle * spreadToScreenPixelScale);
        return Random.insideUnitCircle * spreadRadius;
    }

    private Vector3 ScreenToAimWorldPosition(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return transform.position;

        float zDistance = Mathf.Abs(mainCamera.transform.position.z - firePoint.position.z);

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, zDistance)
        );

        worldPos.z = 0f;
        return worldPos;
    }

    private void StartShooting()
    {
        if (EquippedGun == null || EquippedAmmo == null)
            return;

        if (EquippedGun.isAutomatic)
        {
            shootingHeld = true;
            StartCoroutine(AutoFire());
        }
        else
        {
            Shoot();
        }
    }

    private void StopShooting()
    {
        shootingHeld = false;
        CancelReload();
    }

    private IEnumerator AutoFire()
    {
        while (shootingHeld)
        {
            Shoot();
            yield return null;
        }
    }

    private void Shoot()
    {
        if (currentWeapon == null || EquippedGun == null || EquippedAmmo == null)
            return;

        bool isRunning = inputActions.Player.Run.IsPressed();
        bool isDashing = playerMovement != null && playerMovement.IsDashing;

        // 달리기 / 대쉬 중 사격 불가
        if (isRunning || isDashing)
            return;

        if (isReloading || CurrentAmmo <= 0 || Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + EquippedGun.fireRate;

        currentWeapon.currentAmmo--;
        UpdateAmmoUI();

        // 탄 퍼짐용 반동 누적
        currentRecoil += recoilIncreasePerShot;
        currentRecoil = Mathf.Clamp01(currentRecoil);

        // 현재 실제 조준점(사거리 제한 반영)
        Vector2 aimScreenPos = GetCurrentAimScreenPosition();

        // 조준점 자체 반동 누적
        ApplyAimRecoil();

        float finalAccuracy = CalculateFinalAccuracy();
        float baseSpread = (1f - finalAccuracy) * 10f;
        float recoilSpread = currentRecoil * 5f;
        float spreadAngle = baseSpread + recoilSpread;

        int pellets = EquippedAmmo.pelletsPerShot;

        for (int i = 0; i < pellets; i++)
        {
            // 조준점 주변 원형 범위 안 랜덤 퍼짐
            Vector2 spreadOffset = GetRandomSpreadScreenOffset(spreadAngle);

            // 최종 탄착 목표 스크린 위치
            Vector2 finalAimScreenPos = aimScreenPos + spreadOffset;
            Vector3 finalAimWorldPos = ScreenToAimWorldPosition(finalAimScreenPos);

            Vector2 finalDir = (finalAimWorldPos - firePoint.position).normalized;
            float castDistance = Vector2.Distance(firePoint.position, finalAimWorldPos);

            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                finalDir,
                castDistance,
                shootHitMask
            );

            Vector3 endPoint = hit.collider != null
                ? hit.point
                : finalAimWorldPos;

            StartCoroutine(ShowShot(endPoint));

            if (hit.collider != null && hit.collider.TryGetComponent(out Dummy dummy))
            {
                dummy.TakeDamage(EquippedAmmo.damageMultiplier, EquippedAmmo.penetration);
            }
        }
    }

    /// <summary>
    /// 최종 명중률 = 기본 명중률 × 조준 배율 × 이동 배율 × 반동 배율
    /// </summary>
    private float CalculateFinalAccuracy()
    {
        if (EquippedGun == null || EquippedAmmo == null)
            return 0f;

        float baseAccuracy = EquippedGun.accuracy * EquippedAmmo.speedMultiplier;

        float aimMultiplier = GetAimAccuracyMultiplier();
        float movementMultiplier = GetMovementAccuracyMultiplier();
        float recoilMultiplier = GetRecoilAccuracyMultiplier();

        float finalAccuracy = baseAccuracy
                            * aimMultiplier
                            * movementMultiplier
                            * recoilMultiplier;

        return Mathf.Clamp01(finalAccuracy);
    }

    private void RecoverRecoil()
    {
        // 탄 퍼짐 반동 회복
        if (currentRecoil > 0f)
        {
            currentRecoil -= recoilRecoverySpeed * Time.deltaTime;
            currentRecoil = Mathf.Max(currentRecoil, 0f);
        }

        // 조준점 반동 회복
        currentAimRecoilScreenOffset = Vector2.Lerp(
            currentAimRecoilScreenOffset,
            Vector2.zero,
            aimRecoilRecoverySpeed * Time.deltaTime
        );
    }

    private void TryReload()
    {
        if (currentWeapon == null || EquippedGun == null)
            return;

        if (isReloading)
            return;

        if (CurrentAmmo >= EquippedGun.magazineSize)
            return;

        reloadCoroutine = EquippedGun.reloadType == ReloadType.Magazine
            ? StartCoroutine(ReloadMagazine())
            : StartCoroutine(ReloadSingleLoad());
    }

    private IEnumerator ReloadMagazine()
    {
        if (currentWeapon == null || EquippedGun == null)
            yield break;

        isReloading = true;
        int ammoBeforeReload = CurrentAmmo;

        if (reloadBar != null)
        {
            reloadBar.fillAmount = 0f;
            reloadBar.gameObject.SetActive(true);
        }

        float reloadTime = EquippedGun.reloadTime;
        float elapsedTime = 0f;

        while (elapsedTime < reloadTime)
        {
            if (!isReloading)
            {
                if (reloadBar != null)
                    reloadBar.gameObject.SetActive(false);
                yield break;
            }

            elapsedTime += Time.deltaTime;

            if (reloadBar != null)
                reloadBar.fillAmount = elapsedTime / reloadTime;

            yield return null;
        }

        currentWeapon.currentAmmo = ammoBeforeReload > 0
            ? EquippedGun.magazineSize + 1
            : EquippedGun.magazineSize;

        isReloading = false;

        if (reloadBar != null)
            reloadBar.gameObject.SetActive(false);

        UpdateAmmoUI();
    }

    private IEnumerator ReloadSingleLoad()
    {
        if (currentWeapon == null || EquippedGun == null)
            yield break;

        isReloading = true;

        if (reloadBar != null)
        {
            reloadBar.fillAmount = 0f;
            reloadBar.gameObject.SetActive(true);
        }

        float reloadTime = EquippedGun.reloadTime;

        while (CurrentAmmo < EquippedGun.magazineSize)
        {
            float elapsedTime = 0f;

            while (elapsedTime < reloadTime)
            {
                if (!isReloading)
                {
                    if (reloadBar != null)
                        reloadBar.gameObject.SetActive(false);
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                if (reloadBar != null)
                    reloadBar.fillAmount = elapsedTime / reloadTime;

                yield return null;
            }

            currentWeapon.currentAmmo++;
            UpdateAmmoUI();

            if (reloadBar != null)
                reloadBar.fillAmount = 0f;
        }

        isReloading = false;

        if (reloadBar != null)
            reloadBar.gameObject.SetActive(false);
    }

    private void CancelReload()
    {
        if (!isReloading)
            return;

        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);

        isReloading = false;
        reloadUI?.CancelReloadUI();

        if (reloadBar != null)
            reloadBar.gameObject.SetActive(false);
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null)
            return;

        if (EquippedGun == null || currentWeapon == null)
        {
            ammoText.text = "- / -";
            return;
        }

        ammoText.text = $"{currentWeapon.currentAmmo} / {EquippedGun.magazineSize}";
    }

    private IEnumerator ShowShot(Vector3 endPoint)
    {
        GameObject lrObj = Instantiate(bulletLinePrefab);
        LineRenderer lr = lrObj.GetComponent<LineRenderer>();

        lr.SetPosition(0, firePoint.position);
        lr.SetPosition(1, endPoint);

        yield return new WaitForSeconds(0.05f);
        Destroy(lrObj);
    }
}