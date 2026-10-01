using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("총기 데이터")]
    public GunStats gunStats;
    public AmmoType currentAmmo;

    public float CurrentDamage => gunStats.baseDamage * (currentAmmo != null ? currentAmmo.damageMultiplier : 1f);
    public float CurrentRange => gunStats.range;
    public float CurrentAccuracy => gunStats.accuracy;
    public float CurrentFireRate => gunStats.fireRate;
    public float CurrentWeight => gunStats.weight;

    private float lastFireTime = 0f;

    public bool CanShoot()
    {
        return Time.time >= lastFireTime + CurrentFireRate;
    }

    public void Shoot()
    {
        if (!CanShoot()) return;

        lastFireTime = Time.time;

        float damage = CurrentDamage;
        Debug.Log($"{gunStats.gunName} 발사! 데미지: {damage}, 탄종: {currentAmmo?.ammoName ?? "기본"}");

        // TODO: 총알 생성 및 발사 로직 (PlayerShooting과 연동)
    }
}
