using UnityEngine;


public enum GunType
{
    Pistol,
    SMG,
    AssaultRifle,
    Shotgun,
    DMR,
    LMG,
    Throwable
}

public enum ReloadType
{
    Magazine, // 탄창 삽입식
    SingleLoad // 수동 삽탄식
}

[CreateAssetMenu(fileName = "NewGunStats", menuName = "Weapons/Gun Stats")]
public class GunStats : ScriptableObject
{
    [Header("기본 정보")]
    public string gunName = "New Gun";
    public GunType gunType = GunType.Pistol;
    public Sprite gunSprite; // UI나 인게임 표시용
    public GameObject weaponPrefab;

    [Header("능력치")]
    public float baseDamage = 5f;
    public float range = 20f;
    [Range(0f, 1f)] public float accuracy = 0.9f; // 명중률
    public float fireRate = 0.5f; // 발사 간격(초)
    public float weight = 1f; // 이동 속도 감소에 영향
    public bool isAutomatic = false; // 연사 가능 여부
    public float weaponSwitchSpeed = 0.5f; // 무기 전환 속도
    public float noiseLevel = 1f; // 총기 소음 (스텔스 관련)

    [Header("탄약 관련")]
    public int magazineSize = 12; // 장탄수
    public float reloadTime = 2f; // 재장전 속도
    public ReloadType reloadType = ReloadType.Magazine;

    [Header("총기 반동")]
    [Tooltip("기본 수직 반동 크기")]
    public float verticalRecoil = 0.35f;

    [Tooltip("기본 수평 반동 크기")]
    public float horizontalRecoil = 0.2f;
}
