using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// 인벤토리 표시 규칙 — 등급(높은 순) → 이름(가나다), 동일 이름 스택(*n) 및 스탯 합산.
/// </summary>
public static class InventoryDisplayService
{
	static readonly CompareInfo KoreanComparer = CultureInfo.GetCultureInfo("ko-KR").CompareInfo;

	static readonly Dictionary<string, int> GradeRank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		["Common"] = 0,
		["Uncommon"] = 1,
		["Rare"] = 2,
		["Unique"] = 3,
		["Epic"] = 3,
		["Legendary"] = 4,
	};

	public static List<InventorySlotViewData> BuildWeaponSlots(IReadOnlyList<WeaponInstance> weapons)
	{
		var groups = new Dictionary<string, WeaponStackGroup>();

		if (weapons != null)
		{
			foreach (WeaponInstance weapon in weapons)
			{
				if (weapon?.info == null)
					continue;

				string key = NormalizeName(weapon.info.name);
				if (!groups.TryGetValue(key, out WeaponStackGroup group))
				{
					group = new WeaponStackGroup(weapon);
					groups[key] = group;
				}
				else
				{
					group.Add(weapon);
				}
			}
		}

		var sorted = new List<WeaponStackGroup>(groups.Values);
		sorted.Sort(CompareWeaponGroups);

		var result = new List<InventorySlotViewData>(sorted.Count);
		foreach (WeaponStackGroup group in sorted)
			result.Add(group.ToSlotData());

		return result;
	}

	public static List<InventorySlotViewData> BuildAccessorySlots(IReadOnlyList<AccessoryData> accessories)
	{
		var groups = new Dictionary<string, AccessoryStackGroup>();

		if (accessories != null)
		{
			foreach (AccessoryData accessory in accessories)
			{
				if (accessory == null)
					continue;

				string key = NormalizeName(accessory.displayName);
				if (!groups.TryGetValue(key, out AccessoryStackGroup group))
				{
					group = new AccessoryStackGroup(accessory);
					groups[key] = group;
				}
				else
				{
					group.Add(accessory);
				}
			}
		}

		var sorted = new List<AccessoryStackGroup>(groups.Values);
		sorted.Sort(CompareAccessoryGroups);

		var result = new List<InventorySlotViewData>(sorted.Count);
		foreach (AccessoryStackGroup group in sorted)
			result.Add(group.ToSlotData());

		return result;
	}

	/// <summary>동일 이름을 인벤토리 슬롯처럼 "이름 *n" 형식으로 묶어 표시합니다.</summary>
	public static string FormatStackedNames(IReadOnlyList<string> names)
	{
		if (names == null || names.Count == 0)
			return "—";

		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (string name in names)
		{
			if (string.IsNullOrWhiteSpace(name))
				continue;

			string key = NormalizeName(name);
			counts.TryGetValue(key, out int count);
			counts[key] = count + 1;
		}

		if (counts.Count == 0)
			return "—";

		var labels = new List<string>(counts.Count);
		foreach (KeyValuePair<string, int> pair in counts)
		{
			string label = pair.Value > 1 ? $"{pair.Key} *{pair.Value}" : pair.Key;
			labels.Add(label);
		}

		labels.Sort((a, b) => CompareKorean(a, b));
		return string.Join(", ", labels);
	}

	public static List<InventorySlotViewData> BuildPotionSlots(IReadOnlyList<PotionInventory.PotionStack> stacks)
	{
		var result = new List<InventorySlotViewData>();
		if (stacks == null)
			return result;

		var sorted = new List<PotionInventory.PotionStack>();
		foreach (PotionInventory.PotionStack stack in stacks)
		{
			if (stack != null && stack.count > 0)
				sorted.Add(stack);
		}

		sorted.Sort((a, b) => CompareKorean(GetPotionDisplayName(a), GetPotionDisplayName(b)));

		foreach (PotionInventory.PotionStack stack in sorted)
			result.Add(ToPotionSlot(stack));

		return result;
	}

	static int CompareWeaponGroups(WeaponStackGroup a, WeaponStackGroup b)
	{
		int grade = GetGradeRank(b.Grade).CompareTo(GetGradeRank(a.Grade));
		if (grade != 0)
			return grade;

		int name = CompareKorean(a.DisplayName, b.DisplayName);
		if (name != 0)
			return name;

		return string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);
	}

	static int CompareAccessoryGroups(AccessoryStackGroup a, AccessoryStackGroup b)
	{
		int grade = GetGradeRank(b.Grade).CompareTo(GetGradeRank(a.Grade));
		if (grade != 0)
			return grade;

		int name = CompareKorean(a.DisplayName, b.DisplayName);
		if (name != 0)
			return name;

		return string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);
	}

	static int GetGradeRank(string grade)
	{
		if (string.IsNullOrEmpty(grade))
			return 99;

		return GradeRank.TryGetValue(grade, out int rank) ? rank : 99;
	}

	static int CompareKorean(string a, string b)
	{
		return KoreanComparer.Compare(a ?? "", b ?? "", CompareOptions.StringSort);
	}

	static string NormalizeName(string name) => string.IsNullOrWhiteSpace(name) ? "(이름 없음)" : name.Trim();

	static string NormalizeGrade(string grade) =>
		string.IsNullOrWhiteSpace(grade) ? "Unknown" : grade.Trim();

	static string GetPotionDisplayName(PotionInventory.PotionStack stack)
	{
		if (!string.IsNullOrEmpty(stack.displayName))
			return stack.displayName;

		return string.IsNullOrEmpty(stack.potionId) ? "물약" : stack.potionId;
	}

	static InventorySlotViewData ToPotionSlot(PotionInventory.PotionStack stack)
	{
		string name = GetPotionDisplayName(stack);
		string title = stack.count > 1 ? $"{name} *{stack.count}" : name;

		var sb = new StringBuilder(128);
		sb.AppendLine(title);
		sb.AppendLine($"개수 {stack.count}");

		return new InventorySlotViewData
		{
			icon = stack.icon,
			stackCount = stack.count,
			tooltip = sb.ToString().TrimEnd()
		};
	}

	sealed class WeaponStackGroup
	{
		public string DisplayName { get; }
		public string Grade { get; }
		public string Type { get; }
		public int Count { get; private set; }
		Sprite icon;

		float damage;
		float weight;
		float size;
		float reach;
		float spawntime;
		float cooltime;
		float attackspeed;
		float movespeed;

		public WeaponStackGroup(WeaponInstance first)
		{
			DisplayName = NormalizeName(first.info.name);
			Grade = NormalizeGrade(first.info.grade);
			Type = first.info.type ?? string.Empty;
			icon = WeaponRewardService.GetIcon(first);
			Add(first);
		}

		public void Add(WeaponInstance weapon)
		{
			Count++;
			damage += weapon.damage;
			weight += weapon.weight;
			size += weapon.size;
			reach += weapon.reach;
			spawntime += weapon.spawntime;
			cooltime += weapon.cooltime;
			attackspeed += weapon.attackspeed;
			movespeed += weapon.movespeed;

			if (icon == null)
				icon = WeaponRewardService.GetIcon(weapon);
		}

		public InventorySlotViewData ToSlotData()
		{
			string title = Count > 1 ? $"{DisplayName} *{Count}" : DisplayName;

			var sb = new StringBuilder(256);
			sb.AppendLine(title);
			sb.AppendLine($"{Grade} · {Type}");
			sb.AppendLine($"데미지 {damage:F0} (합산)");
			sb.AppendLine($"쿨 {cooltime:F1}s (합산)");
			sb.AppendLine($"공속 {attackspeed:F2} (합산)");
			sb.AppendLine($"사거리 {reach:F1} (합산)");
			sb.AppendLine($"크기 {size:F2} (합산)");

			return new InventorySlotViewData
			{
				icon = icon,
				stackCount = Count,
				tooltip = sb.ToString().TrimEnd()
			};
		}
	}

	sealed class AccessoryStackGroup
	{
		public string DisplayName { get; }
		public string Grade { get; }
		public string Type { get; }
		public int Count { get; private set; }
		Sprite icon;

		float statA;
		float statB;
		readonly List<string> descriptions = new List<string>();

		public AccessoryStackGroup(AccessoryData first)
		{
			DisplayName = NormalizeName(first.displayName);
			Grade = NormalizeGrade(first.grade);
			Type = first.accessoryType ?? string.Empty;
			icon = first.icon;
			Add(first);
		}

		public void Add(AccessoryData data)
		{
			Count++;
			statA += data.statA;
			statB += data.statB;

			if (!string.IsNullOrEmpty(data.description) && !descriptions.Contains(data.description))
				descriptions.Add(data.description);

			if (icon == null)
				icon = data.icon;
		}

		public InventorySlotViewData ToSlotData()
		{
			string title = Count > 1 ? $"{DisplayName} *{Count}" : DisplayName;

			var sb = new StringBuilder(256);
			sb.AppendLine(title);
			if (!string.IsNullOrEmpty(Grade) || !string.IsNullOrEmpty(Type))
				sb.AppendLine($"{Grade} · {Type}");
			if (Count > 1)
			{
				sb.AppendLine($"스탯 A {statA:F1} (합산)");
				sb.AppendLine($"스탯 B {statB:F1} (합산)");
			}
			else
			{
				sb.AppendLine($"스탯 A {statA:F1}");
				sb.AppendLine($"스탯 B {statB:F1}");
			}

			if (descriptions.Count > 0)
				sb.AppendLine(descriptions[0]);

			return new InventorySlotViewData
			{
				icon = icon,
				stackCount = Count,
				tooltip = sb.ToString().TrimEnd()
			};
		}
	}
}
