#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 상단 메뉴 Tools → Create All Accessories / Create All Relics 로 실행.
/// AccessoryData SO 118개 + RelicData SO 12개를 자동 생성한다.
/// 스프라이트는 자동 연결 불가 — 생성 후 인스펙터에서 직접 연결.
/// </summary>
public static class AccessoryCreator
{
    const string AccessoryOutputPath = "Assets/Data/Accessory";
    const string RelicOutputPath     = "Assets/Data/Relic";

    // ═══════════════════════════════════════════════════════
    //  메뉴 진입점
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/Create All Accessories")]
    public static void CreateAllAccessories()
    {
        EnsureDirectory(AccessoryOutputPath);

        int created = 0;
        foreach (var def in GetAllAccessoryDefs())
        {
            string path = $"{AccessoryOutputPath}/{def.fileName}.asset";
            if (File.Exists(Path.GetFullPath(path)))
            {
                Debug.Log($"[AccessoryCreator] 이미 존재 — 건너뜀: {def.fileName}");
                continue;
            }

            AccessoryData so = ScriptableObject.CreateInstance<AccessoryData>();
            so.displayName   = def.displayName;
            so.grade         = def.grade;
            so.accessoryType = def.accessoryType;
            so.description   = def.description;
            so.effectType    = def.effectType;

            foreach (var mod in def.modifiers)
                so.modifiers.Add(mod);

            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AccessoryCreator] 악세사리 SO 생성 완료: {created}개");
        EditorUtility.DisplayDialog("완료", $"악세사리 SO {created}개 생성!\n경로: {AccessoryOutputPath}", "확인");
    }

    [MenuItem("Tools/Create All Relics")]
    public static void CreateAllRelics()
    {
        EnsureDirectory(RelicOutputPath);

        int created = 0;
        foreach (var def in GetAllRelicDefs())
        {
            string path = $"{RelicOutputPath}/{def.fileName}.asset";
            if (File.Exists(Path.GetFullPath(path)))
            {
                Debug.Log($"[AccessoryCreator] 이미 존재 — 건너뜀: {def.fileName}");
                continue;
            }

            RelicData so = ScriptableObject.CreateInstance<RelicData>();
            so.relicName   = def.relicName;
            so.description = def.description;

            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AccessoryCreator] 성물 SO 생성 완료: {created}개");
        EditorUtility.DisplayDialog("완료", $"성물 SO {created}개 생성!\n경로: {RelicOutputPath}", "확인");
    }

    // ═══════════════════════════════════════════════════════
    //  헬퍼
    // ═══════════════════════════════════════════════════════

    static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    static StatModifier Flat(StatType type, float value)
        => new StatModifier(type, value, false);

    static StatModifier Multi(StatType type, float value)
        => new StatModifier(type, value, true);

    // ═══════════════════════════════════════════════════════
    //  악세사리 정의 구조체
    // ═══════════════════════════════════════════════════════

    struct AccessoryDef
    {
        public string               fileName;
        public string               displayName;
        public AccessoryGrade       grade;
        public string               accessoryType;
        public string               description;
        public AccessoryEffectType  effectType;
        public List<StatModifier>   modifiers;
    }

    struct RelicDef
    {
        public string fileName;
        public string relicName;
        public string description;
    }

    // ═══════════════════════════════════════════════════════
    //  ── 일반 등급 (46종) ──────────────────────────────────
    // ═══════════════════════════════════════════════════════

    static List<AccessoryDef> GetAllAccessoryDefs()
    {
        var list = new List<AccessoryDef>();

        // ── 일반 ─────────────────────────────────────────────
        list.Add(new AccessoryDef { fileName="ACC_C_001", displayName="연습용 목검 패치",   grade=AccessoryGrade.Common, accessoryType="공격", description="공격력 +5%",                          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.AttackPower, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_002", displayName="낡은 장갑",          grade=AccessoryGrade.Common, accessoryType="공격", description="공격 속도 +5%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.AttackSpeed, -0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_003", displayName="부러진 화살촉",      grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 속도 +10%",                     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileSpeed, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_004", displayName="무거운 동상",        grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 크기 +10%",                     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_005", displayName="가벼운 화살통",      grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 개수 +1, 공격력 -10%",          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.ProjectileCount, 1f), Multi(StatType.AttackPower, -0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_006", displayName="납구슬",             grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 개수 +2, 이동 속도 -5%",        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.ProjectileCount, 2f), Multi(StatType.MovementSpeed, -0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_007", displayName="바람의 호른",        grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 사거리 +10%, 공격 속도 +3%",    effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.1f), Multi(StatType.AttackSpeed, -0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_008", displayName="하얀 천",            grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 사거리 +15%, 투사체 속도 +5%",  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.15f), Multi(StatType.ProjectileSpeed, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_009", displayName="토끼 발",            grade=AccessoryGrade.Common, accessoryType="공격", description="치명타 확률 +3%",                      effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CritChance, 0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_010", displayName="낡은 청동거울",      grade=AccessoryGrade.Common, accessoryType="공격", description="치명타 대미지 +10%",                   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.CritDamage, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_011", displayName="가죽 팔찌",          grade=AccessoryGrade.Common, accessoryType="공격", description="치명타 확률 +1%, 치명타 대미지 +5%",   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CritChance, 0.01f), Multi(StatType.CritDamage, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_012", displayName="거친 숫돌",          grade=AccessoryGrade.Common, accessoryType="공격", description="방어 관통력 +5",                       effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.ArmorPenetration, 5f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_013", displayName="작은 숫돌",          grade=AccessoryGrade.Common, accessoryType="공격", description="공격 대미지 +3 (고정)",                effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.AttackPower, 3f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_014", displayName="조약돌 새총",        grade=AccessoryGrade.Common, accessoryType="공격", description="공격 대미지 +2 (고정)",                effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.AttackPower, 2f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_015", displayName="무딘 칼날",          grade=AccessoryGrade.Common, accessoryType="공격", description="근접 공격 범위 +10%",                  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MeleeRange, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_016", displayName="나뭇가지 반지",      grade=AccessoryGrade.Common, accessoryType="방어", description="최대 체력 +5%",                         effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MaxHP, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_017", displayName="말린 육포",          grade=AccessoryGrade.Common, accessoryType="방어", description="최대 체력 +10%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MaxHP, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_018", displayName="가죽 허리띠",        grade=AccessoryGrade.Common, accessoryType="방어", description="최대 체력 +15 (고정)",                  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.MaxHP, 15f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_019", displayName="철제 펜던트",        grade=AccessoryGrade.Common, accessoryType="방어", description="방어력 +5",                             effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.Defense, 5f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_020", displayName="구리 목걸이",        grade=AccessoryGrade.Common, accessoryType="방어", description="방어력 +2",                             effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.Defense, 2f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_021", displayName="나무 방패 조각",     grade=AccessoryGrade.Common, accessoryType="방어", description="받는 피해량 -3 (고정)",                effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.DamageReduction, 3f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_022", displayName="단단한 신발",        grade=AccessoryGrade.Common, accessoryType="방어", description="장판 피해 -5%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.DotDamageReduction, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_023", displayName="바늘 뭉치",          grade=AccessoryGrade.Common, accessoryType="방어", description="받은 대미지의 5% 반사",                effectType=AccessoryEffectType.DamageReflect, modifiers=new List<StatModifier>{ Flat(StatType.DamageReflect, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_024", displayName="약초",               grade=AccessoryGrade.Common, accessoryType="방어", description="치유 효율 +5%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.HealingBonus, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_025", displayName="암살자의 장화",      grade=AccessoryGrade.Common, accessoryType="유틸", description="이동 속도 +5%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_026", displayName="허름한 망토",        grade=AccessoryGrade.Common, accessoryType="유틸", description="이동 속도 +7%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.07f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_027", displayName="부드러운 깃털",      grade=AccessoryGrade.Common, accessoryType="유틸", description="이동 속도 +3%, 공격 속도 +3%",        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.03f), Multi(StatType.AttackSpeed, -0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_028", displayName="나뭇잎 망토",        grade=AccessoryGrade.Common, accessoryType="유틸", description="재사용 대기시간 감소 +5%",             effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CooldownReduction, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_029", displayName="작은 약병",          grade=AccessoryGrade.Common, accessoryType="유틸", description="포션 쿨타임 -10%",                    effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CooldownReduction, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_030", displayName="반짝이는 돌",        grade=AccessoryGrade.Common, accessoryType="유틸", description="시야 범위 +10%",                      effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.VisionRange, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_031", displayName="행운의 동전",        grade=AccessoryGrade.Common, accessoryType="유틸", description="골드 획득량 +5%",                     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.GoldGainBonus, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_032", displayName="찢어진 지도",        grade=AccessoryGrade.Common, accessoryType="유틸", description="골드 획득량 +5%",                     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.GoldGainBonus, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_033", displayName="신기한 화살",        grade=AccessoryGrade.Common, accessoryType="유틸", description="보스 방향 안내 화살표 활성화",        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_C_034", displayName="빨간 리본",          grade=AccessoryGrade.Common, accessoryType="특수", description="피격 시 2초간 이동 속도 +20%",        effectType=AccessoryEffectType.SpeedOnHit, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_C_035", displayName="찢어진 마법서",      grade=AccessoryGrade.Common, accessoryType="속성", description="모든 속성 대미지 +5%",                effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.05f), Multi(StatType.PoisonPower, 0.05f), Multi(StatType.FreezePower, 0.05f), Multi(StatType.WaterPower, 0.05f), Multi(StatType.LightningPower, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_036", displayName="구리 반지",          grade=AccessoryGrade.Common, accessoryType="속성", description="모든 속성 대미지 +3%",                effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.03f), Multi(StatType.PoisonPower, 0.03f), Multi(StatType.FreezePower, 0.03f), Multi(StatType.WaterPower, 0.03f), Multi(StatType.LightningPower, 0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_037", displayName="정령의 가루",        grade=AccessoryGrade.Common, accessoryType="특수", description="상태 이상 지속 시간 +1초",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_C_038", displayName="얼음 조각",          grade=AccessoryGrade.Common, accessoryType="특수", description="피격 시 적 이동 속도 -10% (3초)",   effectType=AccessoryEffectType.SlowOnHit, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_C_039", displayName="부싯돌",             grade=AccessoryGrade.Common, accessoryType="특수", description="공격 시 화상 대미지 +5 (고정)",      effectType=AccessoryEffectType.BurnOnAttack, modifiers=new List<StatModifier>{ Flat(StatType.FirePower, 5f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_040", displayName="모래 주머니",        grade=AccessoryGrade.Common, accessoryType="공격", description="모든 대미지 +10%, 이동 속도 -10%",   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.AttackPower, 0.1f), Multi(StatType.MovementSpeed, -0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_041", displayName="낡은 나침반",        grade=AccessoryGrade.Common, accessoryType="공격", description="투사체 사거리 +10 (고정)",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.ProjectileRange, 10f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_042", displayName="구리 톱니바퀴",      grade=AccessoryGrade.Common, accessoryType="유틸", description="재사용 대기시간 감소 +3%",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CooldownReduction, 0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_043", displayName="화염의 장화",        grade=AccessoryGrade.Common, accessoryType="속성", description="화염 데미지 +5%, 방어력 +1",          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.05f), Flat(StatType.Defense, 1f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_044", displayName="작은 유리병",        grade=AccessoryGrade.Common, accessoryType="속성", description="모든 속성 대미지 +2 (고정)",          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.FirePower, 2f), Flat(StatType.PoisonPower, 2f), Flat(StatType.FreezePower, 2f), Flat(StatType.WaterPower, 2f), Flat(StatType.LightningPower, 2f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_045", displayName="얼음 뭉치",          grade=AccessoryGrade.Common, accessoryType="특수", description="공격 시 동결 확률 3%",                effectType=AccessoryEffectType.FreezeChance, modifiers=new List<StatModifier>{ Flat(StatType.FreezePower, 0.03f) } });
        list.Add(new AccessoryDef { fileName="ACC_C_046", displayName="독사의 이빨",        grade=AccessoryGrade.Common, accessoryType="속성", description="독 데미지 증가 3% (고정)",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.PoisonPower, 3f) } });

        // ── 희귀 ─────────────────────────────────────────────
        list.Add(new AccessoryDef { fileName="ACC_R_001", displayName="번개 맞은 나뭇가지", grade=AccessoryGrade.Rare, accessoryType="특수", description="공격 시 10% 확률로 낙뢰 투하",           effectType=AccessoryEffectType.LightningStrike, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_002", displayName="메두사의 이빨",      grade=AccessoryGrade.Rare, accessoryType="특수", description="모든 공격에 독 대미지 부여 (3초 지속)", effectType=AccessoryEffectType.PoisonOnAttack, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_003", displayName="고대의 불꽃의 핵",  grade=AccessoryGrade.Rare, accessoryType="속성", description="화염 속성 대미지 +15%, 화상 지속 +1초",  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_004", displayName="에키드나의 목걸이", grade=AccessoryGrade.Rare, accessoryType="특수", description="공격 시 15% 확률로 연쇄 번개",           effectType=AccessoryEffectType.ChainLightning, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_005", displayName="물 마법사의 귀걸이",grade=AccessoryGrade.Rare, accessoryType="속성", description="물 속성 대미지 +15%, 투사체 크기 +5%",   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.WaterPower, 0.15f), Multi(StatType.ProjectileRange, 0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_006", displayName="눈꽃 송이",          grade=AccessoryGrade.Rare, accessoryType="특수", description="타격 시 동결 확률 +15%",               effectType=AccessoryEffectType.FreezeChance, modifiers=new List<StatModifier>{ Flat(StatType.FreezePower, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_007", displayName="사막의 전갈 꼬리",  grade=AccessoryGrade.Rare, accessoryType="특수", description="공격 시 10% 확률로 출혈",              effectType=AccessoryEffectType.BleedOnAttack, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_008", displayName="용암의 비늘",        grade=AccessoryGrade.Rare, accessoryType="속성", description="불 장판에서 데미지 +20%, 화상 면역",    effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.2f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_009", displayName="무거운 돌",          grade=AccessoryGrade.Rare, accessoryType="공격", description="투사체 속도 -15%, 최종 데미지 +30%",   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileSpeed, -0.15f), Multi(StatType.AttackPower, 0.3f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_010", displayName="학자의 책",          grade=AccessoryGrade.Rare, accessoryType="공격", description="치명타 대미지 +15%",                   effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.CritDamage, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_011", displayName="쌍둥이 보석",        grade=AccessoryGrade.Rare, accessoryType="특수", description="투사체 발사 시 10% 확률로 복제",       effectType=AccessoryEffectType.DuplicateBullet, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_012", displayName="거인의 발가락",      grade=AccessoryGrade.Rare, accessoryType="공격", description="근접 공격 범위 +15%",                  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MeleeRange, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_013", displayName="곰의 가죽",          grade=AccessoryGrade.Rare, accessoryType="공격", description="최종 데미지 +15%, 이동 속도 -5%",      effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.AttackPower, 0.15f), Multi(StatType.MovementSpeed, -0.05f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_014", displayName="강철의 심장",        grade=AccessoryGrade.Rare, accessoryType="특수", description="체력 10% 이하일 때 방어력 1.5배",      effectType=AccessoryEffectType.ShieldOnLowHP, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_015", displayName="흡혈귀의 송곳니",    grade=AccessoryGrade.Rare, accessoryType="특수", description="적 처치 시 10% 확률로 체력 +1",        effectType=AccessoryEffectType.LifeStealOnKill, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_016", displayName="신비한 약병",        grade=AccessoryGrade.Rare, accessoryType="특수", description="포션 복용 시 3초간 무적",              effectType=AccessoryEffectType.InvincibleOnPotion, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_017", displayName="단단한 껍질",        grade=AccessoryGrade.Rare, accessoryType="특수", description="큰 피해 20% 확률로 무효화",           effectType=AccessoryEffectType.BlockHeavyDamage, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_018", displayName="부활의 씨앗",        grade=AccessoryGrade.Rare, accessoryType="특수", description="사망 시 체력 20%로 즉시 부활 (1회)",  effectType=AccessoryEffectType.Revive, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_019", displayName="가시 목걸이",        grade=AccessoryGrade.Rare, accessoryType="특수", description="피격 시 8방향으로 복수 화살 발사",    effectType=AccessoryEffectType.RevengeArrow, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_020", displayName="약초 꾸러미",        grade=AccessoryGrade.Rare, accessoryType="특수", description="5초마다 체력 2 자동 회복",            effectType=AccessoryEffectType.AutoHeal, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_021", displayName="구미호 꼬리",        grade=AccessoryGrade.Rare, accessoryType="방어", description="회피율 +15%",                         effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.Evasion, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_022", displayName="신비로운 은",        grade=AccessoryGrade.Rare, accessoryType="유틸", description="재사용 대기시간 감소 -10%",           effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CooldownReduction, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_023", displayName="기사단의 문장",      grade=AccessoryGrade.Rare, accessoryType="공격", description="공격 속도 +15%, 공격 범위 +15%",      effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.AttackSpeed, -0.15f), Multi(StatType.MeleeRange, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_024", displayName="서리 구슬",          grade=AccessoryGrade.Rare, accessoryType="특수", description="적중 시 2초간 적 이동 속도 -20%",    effectType=AccessoryEffectType.SlowOnHit, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_025", displayName="용수철 장화",        grade=AccessoryGrade.Rare, accessoryType="유틸", description="이동 속도 +10%, 방어력 +10",          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.1f), Flat(StatType.Defense, 10f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_026", displayName="그림자 가면",        grade=AccessoryGrade.Rare, accessoryType="특수", description="그림자 분신 소환 (쿨타임 30초)",      effectType=AccessoryEffectType.ShadowClone, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_027", displayName="황금 손목 보호대",   grade=AccessoryGrade.Rare, accessoryType="특수", description="공격 적중 시 1% 확률로 1골드 드랍",  effectType=AccessoryEffectType.GoldOnHit, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_028", displayName="바람의 부적",        grade=AccessoryGrade.Rare, accessoryType="유틸", description="이동 속도 +10%, 공격 속도 +10%",     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.1f), Multi(StatType.AttackSpeed, -0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_029", displayName="고무 화살촉",        grade=AccessoryGrade.Rare, accessoryType="특수", description="모든 투사체에 벽 튕기기 1회 부여",   effectType=AccessoryEffectType.Ricochet, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_030", displayName="정찰병의 망원경",    grade=AccessoryGrade.Rare, accessoryType="유틸", description="시야 범위 +20%, 투사체 사거리 +10%", effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.VisionRange, 0.2f), Multi(StatType.ProjectileRange, 0.1f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_031", displayName="흑마법의 인장",      grade=AccessoryGrade.Rare, accessoryType="특수", description="처치 시 20% 확률로 해골 소환",       effectType=AccessoryEffectType.SkeletonOnKill, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_032", displayName="탐욕의 박쥐 날개",   grade=AccessoryGrade.Rare, accessoryType="유틸", description="아이템 습득 범위 +30%",              effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MagnetRange, 0.3f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_033", displayName="빛나는 이끼",        grade=AccessoryGrade.Rare, accessoryType="유틸", description="이동 속도 +15%",                     effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_034", displayName="광부의 곡괭이",      grade=AccessoryGrade.Rare, accessoryType="유틸", description="보물 상자 등장 확률 +5%",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_035", displayName="탐욕의 돈",          grade=AccessoryGrade.Rare, accessoryType="특수", description="무작위 스탯 +20% 또는 -10% (영구)", effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_036", displayName="심연의 고드름",      grade=AccessoryGrade.Rare, accessoryType="공격", description="빙결 적 타격 시 치명타 확률 +15%",  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CritChance, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_R_037", displayName="맹독성 확산기",      grade=AccessoryGrade.Rare, accessoryType="특수", description="독 처치 시 독 스택 주변 전이",       effectType=AccessoryEffectType.PoisonSpread, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_038", displayName="불투명한 프리즘",    grade=AccessoryGrade.Rare, accessoryType="특수", description="투사체에 무작위 속성 부여",           effectType=AccessoryEffectType.RandomElement, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_R_039", displayName="원소의 조율자",      grade=AccessoryGrade.Rare, accessoryType="특수", description="다른 속성 공격 시 5초간 속성 데미지 +5% (최대 5중첩)", effectType=AccessoryEffectType.ElementStack, modifiers=new List<StatModifier>() });

        // ── 유니크 ───────────────────────────────────────────
        list.Add(new AccessoryDef { fileName="ACC_U_001", displayName="대마법사의 부서진 지팡이", grade=AccessoryGrade.Unique, accessoryType="특수", description="룬 발동 확률 1.5배, 룬 효과 범위 +30%",          effectType=AccessoryEffectType.RuneMaxEffect, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_002", displayName="피의 계약서",             grade=AccessoryGrade.Unique, accessoryType="공격", description="공격력 +70%, 최대 체력 -50%, 처치 시 체력 1% 회복", effectType=AccessoryEffectType.BloodContract, modifiers=new List<StatModifier>{ Multi(StatType.AttackPower, 0.7f), Multi(StatType.MaxHP, -0.5f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_003", displayName="차원의 나침반",           grade=AccessoryGrade.Unique, accessoryType="특수", description="스테이지 클리어 후 선택지 새로고침 1회",           effectType=AccessoryEffectType.DimensionCompass, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_004", displayName="거인의 어깨 갑옷",        grade=AccessoryGrade.Unique, accessoryType="방어", description="방어력 +60%, 피격 시 2초간 피해 -50%",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.Defense, 0.6f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_005", displayName="번개 깃든 암령",          grade=AccessoryGrade.Unique, accessoryType="특수", description="적중 시 감전 스택 부여, 번개 데미지 +15%",         effectType=AccessoryEffectType.ElectricOnHit, modifiers=new List<StatModifier>{ Multi(StatType.LightningPower, 0.15f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_006", displayName="연금술사의 가방",         grade=AccessoryGrade.Unique, accessoryType="유틸", description="습득 범위 +50%, 소비 아이템 30% 확률 보존",        effectType=AccessoryEffectType.AlchemistBag, modifiers=new List<StatModifier>{ Multi(StatType.MagnetRange, 0.5f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_007", displayName="태풍의 눈",              grade=AccessoryGrade.Unique, accessoryType="공격", description="관통 및 투사체 크기 +20%, 사거리 +15%",            effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.2f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_008", displayName="그림자 추적자의 망토",   grade=AccessoryGrade.Unique, accessoryType="공격", description="투사체 갯수 +40%, 투사체 속도 +40%",              effectType=AccessoryEffectType.ShadowTracker, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileCount, 0.4f), Multi(StatType.ProjectileSpeed, 0.4f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_009", displayName="드래곤의 비늘",          grade=AccessoryGrade.Unique, accessoryType="속성", description="모든 지속 상태 효과 +40%",                        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 0.4f), Multi(StatType.PoisonPower, 0.4f), Multi(StatType.FreezePower, 0.4f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_010", displayName="집행자의 눈가리개",      grade=AccessoryGrade.Unique, accessoryType="특수", description="체력 20% 이하 적 10% 확률 처형, 처형 시 쿨타임 -1초", effectType=AccessoryEffectType.ExecutionEye, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_011", displayName="검은 구멍",              grade=AccessoryGrade.Unique, accessoryType="특수", description="10% 확률로 필드 내 모든 적 속박, 투사체 크기 +30%", effectType=AccessoryEffectType.BlackHolePull, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.3f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_012", displayName="영혼의 등불",            grade=AccessoryGrade.Unique, accessoryType="특수", description="주변 회전 영혼 탄환 3개 소환 (공격력 50%)",        effectType=AccessoryEffectType.SoulBullet, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_013", displayName="시간의 모래",            grade=AccessoryGrade.Unique, accessoryType="유틸", description="재사용 대기시간 -30%, 피격 무적 시간 +1초",        effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.CooldownReduction, 0.3f), Flat(StatType.InvincibilityFrames, 1f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_014", displayName="폭발성 물약",            grade=AccessoryGrade.Unique, accessoryType="특수", description="공격 시 20% 확률로 광역 폭발",                    effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_015", displayName="대지의 신발",            grade=AccessoryGrade.Unique, accessoryType="특수", description="이동 중 피해 +30%, 정지 2초 시 시야 +50%",        effectType=AccessoryEffectType.MovingDamage, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_016", displayName="메아리의 소라",          grade=AccessoryGrade.Unique, accessoryType="특수", description="룬 효과 25% 확률로 한 번 더 발생 (데미지 50%)",   effectType=AccessoryEffectType.RuneEcho, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_017", displayName="냉기의 서",              grade=AccessoryGrade.Unique, accessoryType="속성", description="화면 내 적 이동 속도 -15%, 빙결 적 치명타 +50%",  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.FreezePower, 0.5f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_018", displayName="화염의 외투",            grade=AccessoryGrade.Unique, accessoryType="특수", description="근거리 적 화상 부여, 화상 사망 시 폭발",           effectType=AccessoryEffectType.BurningAura, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_019", displayName="투명 망토",              grade=AccessoryGrade.Unique, accessoryType="방어", description="회피율 60%, 회피 성공 시 1초 은신",               effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Flat(StatType.Evasion, 0.6f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_020", displayName="황금 손가락",            grade=AccessoryGrade.Unique, accessoryType="특수", description="상점 성물 등장 +30%, 상자 개봉 시 골드 2배",      effectType=AccessoryEffectType.GoldenFinger, modifiers=new List<StatModifier>{ Multi(StatType.GoldGainBonus, 1.0f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_021", displayName="고대 문양",              grade=AccessoryGrade.Unique, accessoryType="특수", description="룬 슬롯 1칸 추가, 룬 발동률 +10%",               effectType=AccessoryEffectType.ExtraRuneSlot, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_U_022", displayName="심해의 진주",            grade=AccessoryGrade.Unique, accessoryType="속성", description="물 속성 대미지 2배, 투사체 사거리 +30%",          effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.WaterPower, 1.0f), Multi(StatType.ProjectileRange, 0.3f) } });
        list.Add(new AccessoryDef { fileName="ACC_U_023", displayName="태양의 펜던트",          grade=AccessoryGrade.Unique, accessoryType="특수", description="밝은 곳 모든 스탯 +30%, 어두운 곳 사거리 +30%",  effectType=AccessoryEffectType.None, modifiers=new List<StatModifier>{ Multi(StatType.ProjectileRange, 0.3f) } });

        // ── 전설 ─────────────────────────────────────────────
        list.Add(new AccessoryDef { fileName="ACC_L_001", displayName="제우스의 심판",          grade=AccessoryGrade.Legendary, accessoryType="특수", description="[천벌] 공격 시 20% 확률로 거대 낙뢰 투하, 연쇄 5명, 감전 0.5초",   effectType=AccessoryEffectType.ZeusJudgment, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_002", displayName="심연의 군주",            grade=AccessoryGrade.Legendary, accessoryType="특수", description="[공허의 손길] 그림자 촉수 4개 상시 소환, 적중 시 최대 체력 1% 흡혈", effectType=AccessoryEffectType.AbyssLord, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_003", displayName="불사조의 깃털",          grade=AccessoryGrade.Legendary, accessoryType="특수", description="[윤회] 사망 시 풀체력 부활 (1회), 부활 시 3000% 폭발 + 5초 무적",  effectType=AccessoryEffectType.PhoenixFeather, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_004", displayName="미네르바의 지혜",        grade=AccessoryGrade.Legendary, accessoryType="특수", description="[전지적 설계] 보유 아이템 1개당 최종 데미지 10% 복리 증가",         effectType=AccessoryEffectType.MinervaWisdom, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_005", displayName="미다스의 장갑",          grade=AccessoryGrade.Legendary, accessoryType="특수", description="[황금의 저주] 1000골드당 모든 스탯 5%, 처치 시 골드 3배",         effectType=AccessoryEffectType.MidasGlove, modifiers=new List<StatModifier>{ Multi(StatType.GoldGainBonus, 2.0f) } });
        list.Add(new AccessoryDef { fileName="ACC_L_006", displayName="시간술사의 모래시계",    grade=AccessoryGrade.Legendary, accessoryType="특수", description="[크로노스] 10초마다 2초 시간 정지, 정지 중 공속·이속 200%",       effectType=AccessoryEffectType.TimeStop, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_007", displayName="신의 방패",              grade=AccessoryGrade.Legendary, accessoryType="방어", description="[절대 영역] 15초간 받는 피해 1 고정, 모든 상태이상 면역",         effectType=AccessoryEffectType.GodShield, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_008", displayName="무한의 마력",            grade=AccessoryGrade.Legendary, accessoryType="특수", description="[오버클럭] 3초간 모든 특수 효과 쿨타임 0초",                      effectType=AccessoryEffectType.InfiniteMana, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_009", displayName="재앙의 씨앗",            grade=AccessoryGrade.Legendary, accessoryType="특수", description="[엔트로피 붕괴] 적중 시 씨앗 심기, 3초 후 최대 체력 5% 고정 피해", effectType=AccessoryEffectType.CalamitySeed, modifiers=new List<StatModifier>() });
        list.Add(new AccessoryDef { fileName="ACC_L_010", displayName="용의 심장",              grade=AccessoryGrade.Legendary, accessoryType="속성", description="[원소의 군주] 화염·빙결·번개 동시 부여, 속성 200% 효율",          effectType=AccessoryEffectType.DragonHeart, modifiers=new List<StatModifier>{ Multi(StatType.FirePower, 1.0f), Multi(StatType.FreezePower, 1.0f), Multi(StatType.LightningPower, 1.0f) } });
        list.Add(new AccessoryDef { fileName="ACC_L_011", displayName="차원 여행자의 장화",     grade=AccessoryGrade.Legendary, accessoryType="유틸", description="[광속 항행] 이동 속도 +100%, 이속 50%만큼 공격력 증가, 이동 중 회피 +30%", effectType=AccessoryEffectType.DimensionBoots, modifiers=new List<StatModifier>{ Multi(StatType.MovementSpeed, 1.0f), Flat(StatType.Evasion, 0.3f) } });
        list.Add(new AccessoryDef { fileName="ACC_L_012", displayName="The Last Rune",         grade=AccessoryGrade.Legendary, accessoryType="특수", description="[설계도의 완성] 모든 룬 효과 최대 수치 고정, 룬 발동률 100%",      effectType=AccessoryEffectType.TheLastRune, modifiers=new List<StatModifier>() });

        return list;
    }

    // ═══════════════════════════════════════════════════════
    //  성물 정의 (12종)
    // ═══════════════════════════════════════════════════════

    static List<RelicDef> GetAllRelicDefs()
    {
        return new List<RelicDef>
        {
            new RelicDef { fileName="RELIC_001", relicName="검사 - 명예로운 기사의 인장",   description="[검성] 근접 공격 범위 100% 증가, 1초마다 가장 가까운 적에게 공격력 500% 관통 검기 발사, 검기 적중 시 2초 출혈" },
            new RelicDef { fileName="RELIC_002", relicName="카우보이 - 보안관의 황금 벳지", description="[도탄의 폭풍] 모든 투사체 튕기기 5회 부여, 튕길 때마다 데미지 50% 복리 증폭, 마지막 튕김 시 폭발" },
            new RelicDef { fileName="RELIC_003", relicName="스쿠버다이버 - 심해의 보물함", description="[심해의 유산] 룬 효과 3배, 처치 시 골드/아이템 드랍 300%, 상점 구매 시 확률적 골드 반환" },
            new RelicDef { fileName="RELIC_004", relicName="사신 - 영혼 수확자의 후드",    description="[영혼의 군주] 처치 시 영혼 스택 획득(최대 200), 스택당 피해 2%, 최대 시 5% 확률로 즉사 발동" },
            new RelicDef { fileName="RELIC_005", relicName="외계인 - 금단의 기술",         description="[멀티태스킹] 양옆에 동일 성능 분신 2개 상시 소환, 투사체와 특수 효과 100% 복사" },
            new RelicDef { fileName="RELIC_006", relicName="해바라기 - 태양의 가호",       description="[태양신의 광휘] 10초마다 구체 생성, 획득 시 체력 20% 회복 + 7초 무적 + 자동 추적 레이저 발사" },
            new RelicDef { fileName="RELIC_007", relicName="꿀벌 - 여왕벌의 로얄젤리",    description="[군단의 역습] 3초마다 자폭 벌 5마리 소환, 자폭 시 독 장판 생성, 장판 위 공격 속도 100% 증가" },
            new RelicDef { fileName="RELIC_008", relicName="다람쥐 - 황금 도토리",         description="[황금 가속] 재화 획득 3배, 이속 비례 공격력 증가, 이동 중 투사체 회피 75% + 이속 100% 증가" },
            new RelicDef { fileName="RELIC_009", relicName="루돌프 - 영원한 겨울밤",       description="[절대 영도] 주변 눈보라 영역 확장, 적 이속 -70% 방어 -50%, 5초 이상 머문 적 2초 동결" },
            new RelicDef { fileName="RELIC_010", relicName="상어 - 포식자의 이빨",         description="[최상위 포식자] 처형 임계치 50% 고정, 처형 성공 시 이속 200% + 다음 공격 3회 치명타 100%" },
            new RelicDef { fileName="RELIC_011", relicName="공룡 - 고대 생명의 화석",      description="[태초의 거인] 최대 체력 3배, 이동 시 지진 발생 + 공격 범위 100%, 피격 시 받은 데미지 100% 충격파 반사" },
            new RelicDef { fileName="RELIC_012", relicName="검은고양이 - 대마법사의 마도서", description="[5원소의 성좌] 주변 5성구 화염·빙결·전격·독·물 번갈아 방출, 모든 속성 위력 150% 증가" },
        };
    }
}
#endif
