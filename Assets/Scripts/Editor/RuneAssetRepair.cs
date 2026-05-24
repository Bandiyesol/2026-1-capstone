#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arts/Data 의 구 RuneData SO를 카테고리별 서브클래스 SO로 재생성합니다.
/// Unity 메뉴: Tools → Rune → Repair All Rune Assets
/// </summary>
public static class RuneAssetRepair
{
	[MenuItem("Tools/Rune/Fix Asset Object Names (노란 경고)")]
	public static void FixAssetObjectNames()
	{
		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { "Assets/Arts/Data" });
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!path.EndsWith(".asset")) continue;

			var asset = AssetDatabase.LoadMainAssetAtPath(path);
			if (asset == null) continue;

			string want = System.IO.Path.GetFileNameWithoutExtension(path);
			if (asset.name == want) continue;

			asset.name = want;
			EditorUtility.SetDirty(asset);
			count++;
		}

		AssetDatabase.SaveAssets();
		Debug.Log($"[RuneAssetRepair] 오브젝트 이름 수정: {count}개");
	}

	[MenuItem("Tools/Rune/Fix Missing Script References")]
	public static void FixMissingScriptReferences()
	{
		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { "Assets/Arts/Data" });
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!path.EndsWith(".asset")) continue;

			if (TryRecreateBrokenAsset(path))
				count++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[RuneAssetRepair] 스크립트 참조 복구: {count}개");
	}

	[MenuItem("Tools/Rune/Build Rune Catalog")]
	public static void BuildCatalog()
	{
		const string catalogPath = "Assets/Arts/Data/RuneCatalog.asset";
		var catalog = AssetDatabase.LoadAssetAtPath<RuneCatalog>(catalogPath);
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<RuneCatalog>();
			AssetDatabase.CreateAsset(catalog, catalogPath);
		}

		string[] guids = AssetDatabase.FindAssets("t:RuneData", new[] { "Assets/Arts/Data" });
		var list = new System.Collections.Generic.List<RuneData>();
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.Contains("RuneCatalog")) continue;
			var rune = AssetDatabase.LoadAssetAtPath<RuneData>(path);
			if (rune != null) list.Add(rune);
		}

		list.Sort((a, b) => string.CompareOrdinal(a.runeName, b.runeName));
		catalog.runes = list.ToArray();
		EditorUtility.SetDirty(catalog);
		AssetDatabase.SaveAssets();
		Debug.Log($"[RuneAssetRepair] RuneCatalog 갱신: {catalog.runes.Length}개 → {catalogPath}");
	}

	[MenuItem("Tools/Rune/Repair All Rune Assets")]
	public static void RepairAll()
	{
		string[] guids = AssetDatabase.FindAssets("t:RuneData", new[] { "Assets/Arts/Data" });
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			RuneData old = AssetDatabase.LoadAssetAtPath<RuneData>(path);
			if (old == null) continue;

			if (RepairOne(old, path))
				count++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[RuneAssetRepair] 완료: {count}개 에셋을 서브클래스 SO로 변환했습니다.");
	}

	static bool RepairOne(RuneData old, string path)
	{
		if (old is ActiveRuneData || old is SplitRuneData || old is RicochetRuneData
		    || old is FreezeRuneData || old is ExplodeRuneData || old is GravityRuneData
		    || old is GrowthRuneData || old is LogicRuneData || old is FinalRuneData
		    || old is ChainRuneData || old is VampireRuneData)
			return false;

		RuneData created = CreateReplacement(old);
		if (created == null)
		{
			Debug.LogWarning($"[RuneAssetRepair] 스킵: {path} ({old.runeType})");
			return false;
		}

		CopyBaseFields(old, created);
		ApplyDefaults(created);
		created.name = System.IO.Path.GetFileNameWithoutExtension(path);

		string tempPath = path + ".repairtmp";
		AssetDatabase.CreateAsset(created, tempPath);
		AssetDatabase.DeleteAsset(path);
		AssetDatabase.MoveAsset(tempPath, path);
		EditorUtility.SetDirty(created);

		Debug.Log($"[RuneAssetRepair] {path} → {created.GetType().Name}");
		return true;
	}

	static RuneData CreateReplacement(RuneData old)
	{
		switch (old.category)
		{
			case RuneCategory.Active:
				return ScriptableObject.CreateInstance<ActiveRuneData>();
			case RuneCategory.Trigger:
				return CreateTrigger(old.runeType);
			case RuneCategory.Final:
				return ScriptableObject.CreateInstance<FinalRuneData>();
			case RuneCategory.State:
				return old.runeType == RuneType.Growth
					? ScriptableObject.CreateInstance<GrowthRuneData>()
					: ScriptableObject.CreateInstance<GravityRuneData>();
			case RuneCategory.Logic:
				return ScriptableObject.CreateInstance<LogicRuneData>();
			default:
				return null;
		}
	}

	static RuneData CreateTrigger(RuneType type)
	{
		switch (type)
		{
			case RuneType.Split: return ScriptableObject.CreateInstance<SplitRuneData>();
			case RuneType.Ricochet: return ScriptableObject.CreateInstance<RicochetRuneData>();
			case RuneType.Freeze: return ScriptableObject.CreateInstance<FreezeRuneData>();
			case RuneType.Explode: return ScriptableObject.CreateInstance<ExplodeRuneData>();
			case RuneType.Chain: return ScriptableObject.CreateInstance<ChainRuneData>();
			case RuneType.Vampire: return ScriptableObject.CreateInstance<VampireRuneData>();
			default:
				Debug.LogWarning($"[RuneAssetRepair] 미구현 Trigger: {type}");
				return null;
		}
	}

	static void CopyBaseFields(RuneData from, RuneData to)
	{
		to.runeName = from.runeName;
		to.category = from.category;
		to.runeType = from.runeType;
		to.runeIcon = from.runeIcon;
		to.runeDescription = from.runeDescription;
		to.isDestroyed = from.isDestroyed;
		to.power = from.power;
	}

	static void ApplyDefaults(RuneData data)
	{
		switch (data)
		{
			case ActiveRuneData active:
				active.duration = 3f;
				active.speedMultiplier = data.runeType == RuneType.Orbit && data.power > 0f ? data.power : 2f;
				active.affectedRange = 6f;
				data.power = 0f;
				break;

			case SplitRuneData split:
				split.spawnsPerTrigger = 3;
				split.spreadDegrees = 30f;
				if (data.power <= 0f) data.power = 0.5f;
				break;

			case RicochetRuneData ricochet:
				ricochet.bounceCount = 3;
				ricochet.interval = 0.3f;
				break;

			case FreezeRuneData freeze:
				freeze.freezeRadius = 3f;
				freeze.freezeDuration = 1.5f;
				freeze.interval = 0.5f;
				break;

			case ExplodeRuneData explode:
				explode.explodeRadius = 2.5f;
				explode.interval = 0.5f;
				if (data.power <= 0f) data.power = 1.5f;
				data.isDestroyed = true;
				break;

			case GravityRuneData gravity:
				gravity.duration = 3f;
				gravity.pullForce = 5f;
				break;

			case GrowthRuneData growth:
				growth.maxGrowthTime = 3f;
				growth.maxScaleRatio = 2f;
				if (data.power <= 0f) data.power = 1.5f;
				break;

			case LogicRuneData logic:
				logic.interval = 1f;
				logic.distance = 2f;
				break;

			case ChainRuneData chain:
				chain.chainCount = 3;
				chain.interval = 0.3f;
				break;

			case VampireRuneData vampire:
				vampire.interval = 0.5f;
				if (data.power <= 0f) data.power = 0.3f;
				break;
		}
	}

	/// <summary>m_Script가 끊긴 SO(한 .cs에 클래스 여러 개였을 때)를 타입에 맞게 재생성.</summary>
	static bool TryRecreateBrokenAsset(string path)
	{
		var main = AssetDatabase.LoadMainAssetAtPath(path);
		if (main == null) return false;
		if (main is RuneData loaded && loaded.GetType() != typeof(RuneData))
			return false;

		var so = new SerializedObject(main);
		var typeProp = so.FindProperty("runeType");
		var catProp = so.FindProperty("category");
		if (typeProp == null || catProp == null) return false;

		var stub = ScriptableObject.CreateInstance<RuneData>();
		CopySerializedBase(so, stub);
		RuneType runeType = stub.runeType;
		RuneCategory category = stub.category;
		Object.DestroyImmediate(stub);

		RuneData created = CreateReplacementFromEnums(category, runeType);
		if (created == null) return false;

		created.runeName = so.FindProperty("runeName")?.stringValue ?? created.runeName;
		created.runeIcon = so.FindProperty("runeIcon")?.objectReferenceValue as Sprite;
		created.runeDescription = so.FindProperty("runeDescription")?.stringValue ?? string.Empty;
		created.isDestroyed = so.FindProperty("isDestroyed")?.boolValue ?? false;
		created.power = so.FindProperty("power")?.floatValue ?? 0f;
		created.category = category;
		created.runeType = runeType;
		ApplyDefaults(created);
		CopySubtypeFields(so, created);
		created.name = System.IO.Path.GetFileNameWithoutExtension(path);

		string tempPath = path + ".repairtmp";
		AssetDatabase.CreateAsset(created, tempPath);
		AssetDatabase.DeleteAsset(path);
		AssetDatabase.MoveAsset(tempPath, path);
		return true;
	}

	static RuneData CreateReplacementFromEnums(RuneCategory category, RuneType type)
	{
		switch (category)
		{
			case RuneCategory.Active: return ScriptableObject.CreateInstance<ActiveRuneData>();
			case RuneCategory.Trigger: return CreateTrigger(type);
			case RuneCategory.Final: return ScriptableObject.CreateInstance<FinalRuneData>();
			case RuneCategory.State:
				return type == RuneType.Growth
					? ScriptableObject.CreateInstance<GrowthRuneData>()
					: ScriptableObject.CreateInstance<GravityRuneData>();
			case RuneCategory.Logic: return ScriptableObject.CreateInstance<LogicRuneData>();
			default: return null;
		}
	}

	static void CopySerializedBase(SerializedObject from, RuneData to)
	{
		to.runeName = from.FindProperty("runeName")?.stringValue;
		to.category = (RuneCategory)from.FindProperty("category").enumValueIndex;
		to.runeType = (RuneType)from.FindProperty("runeType").enumValueIndex;
	}

	static void CopySubtypeFields(SerializedObject old, RuneData created)
	{
		switch (created)
		{
			case GrowthRuneData g:
				g.maxGrowthTime = old.FindProperty("maxGrowthTime")?.floatValue ?? 3f;
				g.maxScaleRatio = old.FindProperty("maxScaleRatio")?.floatValue ?? 2f;
				break;
			case RicochetRuneData r:
				r.bounceCount = old.FindProperty("bounceCount")?.intValue ?? 3;
				r.interval = old.FindProperty("interval")?.floatValue ?? 0.3f;
				break;
			case FreezeRuneData f:
				f.freezeRadius = old.FindProperty("freezeRadius")?.floatValue ?? 3f;
				f.freezeDuration = old.FindProperty("freezeDuration")?.floatValue ?? 1.5f;
				f.interval = old.FindProperty("interval")?.floatValue ?? 0.5f;
				break;
			case ExplodeRuneData e:
				e.explodeRadius = old.FindProperty("explodeRadius")?.floatValue ?? 2.5f;
				e.interval = old.FindProperty("interval")?.floatValue ?? 0.5f;
				break;
			case SplitRuneData s:
				var count = old.FindProperty("spawnsPerTrigger") ?? old.FindProperty("splitCount");
				if (count != null) s.spawnsPerTrigger = count.intValue > 0 ? count.intValue : 3;
				break;
			case ChainRuneData chain:
				chain.chainCount = old.FindProperty("chainCount")?.intValue ?? 3;
				chain.interval = old.FindProperty("interval")?.floatValue ?? 0.3f;
				break;
			case VampireRuneData vampire:
				vampire.interval = old.FindProperty("interval")?.floatValue ?? 0.5f;
				break;
		}
	}
}
#endif
