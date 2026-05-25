using UnityEngine;

public static class DamageCalculator
{
    static bool missingStatsWarningLogged;

    public static PlayerStats ResolvePlayerStats()
    {
        if (PlayerStats.Instance != null)
            return PlayerStats.Instance;

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            PlayerStats onPlayer = GameManager.instance.player.GetComponent<PlayerStats>();
            if (onPlayer != null)
                return onPlayer;
        }

        return Object.FindFirstObjectByType<PlayerStats>();
    }

    /// <summary>
    /// 무기 고정 데미지, 플레이어 스탯(공격력, 치명타), 룬의 특수 배율을 종합하여 최종 데미지를 계산합니다.
    /// </summary>
    public static float CalculateBaseDamage(WeaponInstance weapon, RuneData runeData = null)
    {
        if (weapon == null) return 0f;

        float baseWeaponDamage = weapon.damage;
        float damageAfterPlayerStats = baseWeaponDamage;
        bool isCrit = false;

        PlayerStats stats = ResolvePlayerStats();
        if (stats != null)
        {
            damageAfterPlayerStats = stats.CalculateAttackDamage(baseWeaponDamage, out isCrit);
        }
        else if (!missingStatsWarningLogged)
        {
            missingStatsWarningLogged = true;
            Debug.LogWarning("[DamageCalculator] PlayerStats가 없습니다. Player 오브젝트에 PlayerStats 컴포넌트를 추가하세요.");
        }

        float runeMultiplier = 1f;
        if (runeData != null && runeData.power > 0f)
            runeMultiplier = runeData.power;

        return damageAfterPlayerStats * runeMultiplier;
    }
}
