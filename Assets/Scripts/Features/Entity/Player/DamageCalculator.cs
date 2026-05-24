using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// 무기 고정 데미지, 플레이어 스탯(공격력, 치명타), 룬의 특수 배율을 종합하여 최종 데미지를 계산합니다.
    /// </summary>
    public static float CalculateBaseDamage(WeaponInstance weapon, RuneData runeData = null)
    {
        if (weapon == null) return 0f;

        // 1. 이미 생성 시 확정되어 들어온 무기 고정 데미지 고르기
        float baseWeaponDamage = weapon.damage; 

        // 2. 플레이어 스탯 시스템(PlayerStats) 연동하여 데미지 및 치명타 계산
        float damageAfterPlayerStats = baseWeaponDamage;
        bool isCrit = false;

        if (PlayerStats.Instance != null)
        {
            // PlayerStats 내부에 구현된 스탯 계산기를 거쳐 (무기데미지 * 공격력배율), 치명타 여부까지 한 번에 처리합니다.
            damageAfterPlayerStats = PlayerStats.Instance.CalculateAttackDamage(baseWeaponDamage, out isCrit);
            
            if (isCrit)
            {
                Debug.Log("🔥 PlayerStats 시스템에 의해 치명타 발생!");
            }
        }
        else
        {
            // 만약 씬에 PlayerStats가 없다면 기본 무기 데미지를 유지합니다.
            Debug.LogWarning("[DamageCalculator] PlayerStats 인스턴스를 찾을 수 없어 기본 무기 데미지로 계산합니다.");
        }

        // 3. 룬의 관여 여부 체크 (룬 자체의 개별 데미지 배율 반영)
        float runeMultiplier = 1f;
        if (runeData != null && runeData.power > 0f)
        {
            // 예: 분열(Split) 룬 등에서 자식 데미지 배율(0.5f 등) 또는 특정 트리거의 파워 배율로 사용
            runeMultiplier = runeData.power;
        }

        // 4. 최종 데미지 산출 (플레이어 스탯과 치명타가 반영된 데미지 × 룬 특수 배율)
        float finalDamage = damageAfterPlayerStats * runeMultiplier;

        return finalDamage;
    }
}