using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAmmoType", menuName = "Weapons/Ammo Type")]
public class AmmoType : ScriptableObject
{
    [Header("기본 정보")]
    public string ammoName = "New Ammo";

    [Header("능력치")]
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f; // 총알 속도
    public float penetration = 0f; // 관통력 (방탄/장갑 관통 계산용)

    [Header("사용 제한")]
    public List<GunType> compatibleGuns = new List<GunType>(); // 사용 가능한 총기 종류
    public int pelletsPerShot = 1; // 격발 탄환 수 (산탄총용)
}
