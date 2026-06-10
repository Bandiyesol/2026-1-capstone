#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Data/Rune Datas 의 구 RuneData SO를 카테고리별 서브클래스 SO로 재생성합니다.
/// Unity 메뉴: Tools → Rune → Repair All Rune Assets
/// </summary>
public static class RuneAssetRepair
{
	[MenuItem("Tools/Rune/Fix Asset Object Names (노란 경고)")]
	[MenuItem("Window/The Last Rune/Rune/Fix Asset Object Names (노란 경고)")]
	public static void FixAssetObjectNames()
	{
		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { RunePaths.DataFolder });
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
	[MenuItem("Window/The Last Rune/Rune/Fix Missing Script References")]
	public static void FixMissingScriptReferences()
	{
		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { RunePaths.DataFolder });
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
	[MenuItem("Window/The Last Rune/Rune/Build Rune Catalog")]
	public static void BuildCatalog()
	{
		const string catalogPath = RunePaths.CatalogAssetPath;
		EnsureRuntimeDataFolder();

		var catalog = AssetDatabase.LoadAssetAtPath<RuneCatalog>(catalogPath);
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<RuneCatalog>();
			AssetDatabase.CreateAsset(catalog, catalogPath);
		}

		string[] guids = AssetDatabase.FindAssets("t:RuneData", new[] { RunePaths.DataFolder });
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

	static void EnsureRuntimeDataFolder()
	{
		if (!AssetDatabase.IsValidFolder("Assets/Resources"))
			AssetDatabase.CreateFolder("Assets", "Resources");
		if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
			AssetDatabase.CreateFolder("Assets/Resources", "Data");
	}

	[MenuItem("Tools/Rune/Repair All Rune Assets")]
	[MenuItem("Window/The Last Rune/Rune/Repair All Rune Assets")]
	public static void RepairAll()
	{
		string[] guids = AssetDatabase.FindAssets("t:RuneData", new[] { RunePaths.DataFolder });
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

	/// <summary>
	/// 잘못된 서브클래스·Category·Type을 파일명(Rune_Homing 등) 기준으로 강제 재생성합니다.
	/// 아이콘·설명·튜닝 값은 가능한 한 유지합니다.
	/// </summary>
	[MenuItem("Tools/Rune/Force Reassign Rune Scripts By Filename")]
	[MenuItem("Window/The Last Rune/Rune/Force Reassign Rune Scripts By Filename")]
	public static void ForceReassignAllByFilename()
	{
		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { RunePaths.DataFolder });
		int reassigned = 0;
		var alreadyCorrect = new System.Collections.Generic.List<string>();

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!path.EndsWith(".asset") || path.Contains("RuneCatalog")) continue;

			if (ForceRecreateFromFilename(path, out bool skippedAsCorrect))
			{
				if (skippedAsCorrect)
					alreadyCorrect.Add(System.IO.Path.GetFileNameWithoutExtension(path));
				else
					reassigned++;
			}
		}

		int created = CreateMissingDefaultRuneAssets(logEach: true);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		BuildCatalog();

		string skipNote = alreadyCorrect.Count > 0
			? $"\n이미 올바름(건너뜀): {alreadyCorrect.Count}개 → {string.Join(", ", alreadyCorrect)}"
			: string.Empty;
		string createNote = created > 0
			? $"\n누락 에셋 생성: {created}개"
			: string.Empty;

		Debug.Log(
			$"[RuneAssetRepair] 파일명 기준 스크립트 재할당: {reassigned}개{skipNote}{createNote}\n" +
			$"카탈로그: {CountCatalogRunes()}/{DefaultRuneDefinitions.Length}개");
	}

	/// <summary>15종 중 없는 Rune_*.asset만 기본값으로 생성합니다.</summary>
	[MenuItem("Tools/Rune/Create Missing Rune Assets")]
	[MenuItem("Window/The Last Rune/Rune/Create Missing Rune Assets")]
	public static void CreateMissingRuneAssetsMenu()
	{
		EnsureDataFolderExists();
		int created = CreateMissingDefaultRuneAssets(logEach: true);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		BuildCatalog();
		Debug.Log($"[RuneAssetRepair] 누락 룬 생성: {created}개 → 카탈로그 {CountCatalogRunes()}/{DefaultRuneDefinitions.Length}개");
	}

	static int CreateMissingDefaultRuneAssets(bool logEach)
	{
		int created = 0;
		foreach (var def in DefaultRuneDefinitions)
		{
			string path = $"{RunePaths.DataFolder}/Rune_{def.assetSuffix}.asset";
			if (AssetDatabase.LoadAssetAtPath<RuneData>(path) != null) continue;

			RuneData data = CreateReplacementFromEnums(def.category, def.type);
			if (data == null) continue;

			data.runeName = def.displayName;
			data.category = def.category;
			data.runeType = def.type;
			data.name = $"Rune_{def.assetSuffix}";
			ApplyDefaults(data);

			AssetDatabase.CreateAsset(data, path);
			created++;
			if (logEach)
				Debug.Log($"[RuneAssetRepair] 생성: Rune_{def.assetSuffix}.asset ({def.category}/{def.type})");
		}

		return created;
	}

	static int CountCatalogRunes()
	{
		var catalog = AssetDatabase.LoadAssetAtPath<RuneCatalog>(RunePaths.CatalogAssetPath);
		return catalog?.runes?.Length ?? 0;
	}

	/// <summary>
	/// Rune_*.asset을 모두 삭제하고 올바른 Script·Category·Type으로 15개를 새로 만듭니다.
	/// 아이콘·설명 등 커스텀 값은 초기화됩니다.
	/// </summary>
	[MenuItem("Tools/Rune/Rebuild All Default Rune Assets")]
	[MenuItem("Window/The Last Rune/Rune/Rebuild All Default Rune Assets")]
	public static void RebuildAllDefaults()
	{
		if (!EditorUtility.DisplayDialog(
			    "Rebuild All Default Rune Assets",
			    "Rune_*.asset 15개를 삭제하고 기본값으로 다시 만듭니다.\n\n" +
			    "아이콘·설명·밸런스 튜닝은 모두 사라집니다.\n" +
			    "커스텀 값을 유지하려면 'Force Reassign Rune Scripts By Filename'을 사용하세요.\n\n" +
			    "계속할까요?",
			    "Rebuild", "Cancel"))
			return;

		EnsureDataFolderExists();

		string[] guids = AssetDatabase.FindAssets("Rune_", new[] { RunePaths.DataFolder });
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!path.EndsWith(".asset") || path.Contains("RuneCatalog")) continue;
			AssetDatabase.DeleteAsset(path);
		}

		int created = 0;
		foreach (var def in DefaultRuneDefinitions)
		{
			string path = $"{RunePaths.DataFolder}/Rune_{def.assetSuffix}.asset";
			if (AssetDatabase.LoadAssetAtPath<RuneData>(path) != null) continue;

			RuneData data = CreateReplacementFromEnums(def.category, def.type);
			if (data == null) continue;

			data.runeName = def.displayName;
			data.category = def.category;
			data.runeType = def.type;
			data.name = $"Rune_{def.assetSuffix}";
			ApplyDefaults(data);

			AssetDatabase.CreateAsset(data, path);
			created++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		BuildCatalog();
		Debug.Log($"[RuneAssetRepair] 기본 룬 에셋 재생성: {created}개");
	}

	static void EnsureDataFolderExists()
	{
		if (!AssetDatabase.IsValidFolder("Assets/Data"))
			AssetDatabase.CreateFolder("Assets", "Data");
		if (!AssetDatabase.IsValidFolder(RunePaths.DataFolder))
			AssetDatabase.CreateFolder("Assets/Data", "Rune Datas");
	}

	static readonly (RuneType type, RuneCategory category, string assetSuffix, string displayName)[] DefaultRuneDefinitions =
	{
		(RuneType.Homing, RuneCategory.Active, "Homing", "Homing"),
		(RuneType.Orbit, RuneCategory.Active, "Orbit", "Orbit"),
		(RuneType.Wave, RuneCategory.Active, "Wave", "Wave"),
		(RuneType.Spiral, RuneCategory.Active, "Spiral", "Spiral"),
		(RuneType.Split, RuneCategory.Trigger, "Split", "Split"),
		(RuneType.Ricochet, RuneCategory.Trigger, "Ricochet", "Ricochet"),
		(RuneType.Vampire, RuneCategory.Trigger, "Vampire", "Vampire"),
		(RuneType.Freeze, RuneCategory.Trigger, "Freeze", "Freeze"),
		(RuneType.Chain, RuneCategory.Trigger, "Chain", "Chain"),
		(RuneType.Explode, RuneCategory.Trigger, "Explode", "Explode"),
		(RuneType.Gravity, RuneCategory.State, "Gravity", "Gravity"),
		(RuneType.Growth, RuneCategory.State, "Growth", "Growth"),
		(RuneType.Blink, RuneCategory.Logic, "Blink", "Blink"),
		(RuneType.Boing, RuneCategory.Logic, "Boing", "Boing"),
		(RuneType.Recursion, RuneCategory.Final, "Recursion", "Recursion"),
	};

	static bool ForceRecreateFromFilename(string path, out bool skippedAsCorrect)
	{
		skippedAsCorrect = false;
		string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
		if (!TryGetDefinitionFromAssetName(fileName, out RuneType runeType, out RuneCategory category))
			return false;

		var main = AssetDatabase.LoadMainAssetAtPath(path);
		if (main == null) return false;

		var so = new SerializedObject(main);
		RuneData expected = CreateReplacementFromEnums(category, runeType);
		if (expected == null)
		{
			Object.DestroyImmediate(expected);
			return false;
		}

		bool alreadyCorrect = main is RuneData loaded
		                      && loaded.GetType() == expected.GetType()
		                      && loaded.category == category
		                      && loaded.runeType == runeType;
		Object.DestroyImmediate(expected);
		if (alreadyCorrect)
		{
			skippedAsCorrect = true;
			return true;
		}

		var stub = ScriptableObject.CreateInstance<RuneData>();
		CopySerializedBase(so, stub);
		RuneData created = CreateReplacementFromEnums(category, runeType);
		if (created == null) return false;

		created.runeName = so.FindProperty("runeName")?.stringValue;
		if (string.IsNullOrEmpty(created.runeName))
			created.runeName = runeType.ToString();
		created.runeIcon = so.FindProperty("runeIcon")?.objectReferenceValue as Sprite;
		created.runeDescription = so.FindProperty("runeDescription")?.stringValue ?? string.Empty;
		created.isDestroyed = so.FindProperty("isDestroyed")?.boolValue ?? false;
		created.power = so.FindProperty("power")?.floatValue ?? 0f;
		created.category = category;
		created.runeType = runeType;
		ApplyDefaults(created);
		CopySubtypeFields(so, created);
		created.name = fileName;
		Object.DestroyImmediate(stub);

		string tempPath = path + ".repairtmp";
		AssetDatabase.CreateAsset(created, tempPath);
		AssetDatabase.DeleteAsset(path);
		AssetDatabase.MoveAsset(tempPath, path);
		return true;
	}

	static bool TryGetDefinitionFromAssetName(string fileName, out RuneType type, out RuneCategory category)
	{
		type = RuneType.None;
		category = RuneCategory.Active;
		const string prefix = "Rune_";
		if (!fileName.StartsWith(prefix)) return false;

		string key = fileName.Substring(prefix.Length);
		foreach (RuneType t in System.Enum.GetValues(typeof(RuneType)))
		{
			if (t == RuneType.None || t.ToString() != key) continue;
			type = t;
			category = CategoryForType(t);
			return true;
		}

		return false;
	}

	static RuneCategory CategoryForType(RuneType type)
	{
		switch (type)
		{
			case RuneType.Homing:
			case RuneType.Orbit:
			case RuneType.Wave:
			case RuneType.Spiral:
				return RuneCategory.Active;
			case RuneType.Split:
			case RuneType.Ricochet:
			case RuneType.Vampire:
			case RuneType.Freeze:
			case RuneType.Chain:
			case RuneType.Explode:
				return RuneCategory.Trigger;
			case RuneType.Gravity:
			case RuneType.Growth:
				return RuneCategory.State;
			case RuneType.Blink:
			case RuneType.Boing:
				return RuneCategory.Logic;
			case RuneType.Recursion:
				return RuneCategory.Final;
			default:
				return RuneCategory.Active;
		}
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
