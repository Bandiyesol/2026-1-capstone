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

	static Dictionary<string, string> weaponGradeByName;
	static Dictionary<string, string> accessoryGradeByName;

	/// <summary>무기 이름 목록을 등급 색상이 적용된 스택 문자열로 반환합니다.</summary>
	public static string FormatStackedWeaponNames(IReadOnlyList<string> names) =>
		FormatStackedColoredNames(names, LookupWeaponGrade);

	/// <summary>악세서리 이름 목록을 등급 색상이 적용된 스택 문자열로 반환합니다.</summary>
	public static string FormatStackedAccessoryNames(IReadOnlyList<string> names) =>
		FormatStackedColoredNames(names, LookupAccessoryGrade);

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

	static string FormatStackedColoredNames(IReadOnlyList<string> names, Func<string, string> gradeLookup)
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

		var entries = new List<StackedNameEntry>(counts.Count);
		foreach (KeyValuePair<string, int> pair in counts)
		{
			string grade = gradeLookup?.Invoke(pair.Key);
			entries.Add(new StackedNameEntry(pair.Key, pair.Value, grade));
		}

		entries.Sort(CompareStackedNameEntries);

		var labels = new List<string>(entries.Count);
		foreach (StackedNameEntry entry in entries)
		{
			string label = entry.Count > 1 ? $"{entry.DisplayName} *{entry.Count}" : entry.DisplayName;
			if (!string.IsNullOrEmpty(entry.Grade))
				label = ChoiceGradeDisplay.FormatColoredItemLabel(label, entry.Grade);

			labels.Add(label);
		}

		return string.Join(", ", labels);
	}

	readonly struct StackedNameEntry
	{
		public string DisplayName { get; }
		public int Count { get; }
		public string Grade { get; }

		public StackedNameEntry(string displayName, int count, string grade)
		{
			DisplayName = displayName;
			Count = count;
			Grade = grade;
		}
	}

	static int CompareStackedNameEntries(StackedNameEntry a, StackedNameEntry b)
	{
		int grade = GetGradeRank(b.Grade).CompareTo(GetGradeRank(a.Grade));
		if (grade != 0)
			return grade;

		return CompareKorean(a.DisplayName, b.DisplayName);
	}

	static string LookupWeaponGrade(string displayName)
	{
		EnsureWeaponGradeLookup();
		return weaponGradeByName != null && weaponGradeByName.TryGetValue(displayName, out string grade)
			? grade
			: null;
	}

	static string LookupAccessoryGrade(string displayName)
	{
		EnsureAccessoryGradeLookup();
		return accessoryGradeByName != null && accessoryGradeByName.TryGetValue(displayName, out string grade)
			? grade
			: null;
	}

	static void EnsureWeaponGradeLookup()
	{
		if (weaponGradeByName != null)
			return;

		weaponGradeByName = new Dictionary<string, string>(StringComparer.Ordinal);

		if (WeaponManager.Instance != null)
		{
			foreach (WeaponInfo info in WeaponManager.Instance.GetAllWeaponInfos())
			{
				if (info == null || string.IsNullOrEmpty(info.name))
					continue;

				weaponGradeByName[NormalizeName(info.name)] = NormalizeGrade(info.grade);
			}

			return;
		}

		TextAsset infoJson = Resources.Load<TextAsset>("Data/WeaponInfo");
		if (infoJson == null)
			return;

		WeaponDataLoader loader = JsonUtility.FromJson<WeaponDataLoader>(infoJson.text);
		if (loader?.info == null)
			return;

		foreach (WeaponInfo info in loader.info)
		{
			if (info == null || string.IsNullOrEmpty(info.name))
				continue;

			weaponGradeByName[NormalizeName(info.name)] = NormalizeGrade(info.grade);
		}
	}

	static void EnsureAccessoryGradeLookup()
	{
		if (accessoryGradeByName != null)
			return;

		accessoryGradeByName = new Dictionary<string, string>(StringComparer.Ordinal);

		RewardCatalogSettings catalog = RewardCatalogSettings.Load();
		if (catalog?.allAccessories == null)
			return;

		foreach (AccessoryData accessory in catalog.allAccessories)
		{
			if (accessory == null || string.IsNullOrEmpty(accessory.displayName))
				continue;

			accessoryGradeByName[NormalizeName(accessory.displayName)] = NormalizeGrade(accessory.GradeString);
		}
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
			sb.AppendLine(ChoiceGradeDisplay.FormatGradeLine(Grade, Type));
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

		readonly List<string> descriptions = new List<string>();
		// StatModifier 합산용 (StatType → 누적값)
		readonly Dictionary<string, float> statSums = new Dictionary<string, float>();

		public AccessoryStackGroup(AccessoryData first)
		{
			DisplayName = NormalizeName(first.displayName);
			Grade = NormalizeGrade(first.GradeString);
			Type = first.accessoryType ?? string.Empty;
			icon = AccessoryIconResolver.Resolve(first);
			Add(first);
		}

		public void Add(AccessoryData data)
		{
			Count++;

			// StatModifier 합산
			if (data.modifiers != null)
			{
				foreach (StatModifier mod in data.modifiers)
				{
					string key = $"{mod.statType}_{(mod.isMulti ? "%" : "+")}";
					statSums.TryGetValue(key, out float cur);
					statSums[key] = cur + mod.value;
				}
			}

			if (!string.IsNullOrEmpty(data.description) && !descriptions.Contains(data.description))
				descriptions.Add(data.description);

			if (icon == null)
				icon = AccessoryIconResolver.Resolve(data);
		}

		public InventorySlotViewData ToSlotData()
		{
			string title = Count > 1 ? $"{DisplayName} *{Count}" : DisplayName;

			var sb = new StringBuilder(256);
			sb.AppendLine(title);
			if (!string.IsNullOrEmpty(Grade) || !string.IsNullOrEmpty(Type))
				sb.AppendLine(ChoiceGradeDisplay.FormatGradeLine(Grade, Type));

			foreach (var kv in statSums)
			{
				// key 형식: "AttackPower_%" or "Defense_+"
				string[] parts = kv.Key.Split('_');
				string statName = parts.Length > 0 ? parts[0] : kv.Key;
				string unit     = parts.Length > 1 ? parts[1] : "";
				string display  = unit == "%" ? $"{kv.Value * 100f:F0}%" : $"{kv.Value:F1}";
				sb.AppendLine($"{statName}: {display}{(Count > 1 ? " (합산)" : "")}");
			}

			if (descriptions.Count > 0)
				sb.AppendLine(descriptions[0]);

			return new InventorySlotViewData
			{
				icon       = icon,
				stackCount = Count,
				tooltip    = sb.ToString().TrimEnd()
			};
		}
	}
}