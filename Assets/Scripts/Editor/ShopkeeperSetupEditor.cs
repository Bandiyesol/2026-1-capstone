#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopkeeperSetupEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const string TexturePath = "Assets/Arts/Characters/Player/shop v1.png";
	const string PrefabPath = "Assets/Prefabs/Gimmick Objects/ShopkeeperNpc.prefab";

	[MenuItem("Tools/Game/Setup Shopkeeper NPC")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("상점 주인 설정", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		if (!TrySetupInActiveScene())
		{
			EditorUtility.DisplayDialog("상점 주인 설정", "PoolManager를 찾지 못했습니다.", "확인");
			return;
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		EditorUtility.DisplayDialog(
			"상점 주인 설정",
			"ShopkeeperNpc 프리팹 생성 및 PoolManager.gimmickPrefabs 등록을 완료했습니다.",
			"확인");
	}

	public static bool TrySetupInActiveScene()
	{
		Sprite[] idleFrames = LoadIdleSprites();
		if (idleFrames == null || idleFrames.Length == 0)
		{
			Debug.LogError("[ShopkeeperSetup] shop v1.png에서 idle 스프라이트를 찾지 못했습니다.");
			return false;
		}

		GameObject prefab = CreateOrUpdatePrefab(idleFrames);
		if (prefab == null)
			return false;

		PoolManager pool = Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);
		if (pool == null)
			return false;

		RegisterInPool(pool, prefab);
		return true;
	}

	static Sprite[] LoadIdleSprites()
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TexturePath);
		var frames = new List<Sprite>(8);

		for (int i = 0; i < 8; i++)
		{
			string targetName = $"Character 4 v1_{i}";
			foreach (Object asset in assets)
			{
				if (asset is Sprite sprite && sprite.name == targetName)
				{
					frames.Add(sprite);
					break;
				}
			}
		}

		return frames.Count > 0 ? frames.ToArray() : null;
	}

	static GameObject CreateOrUpdatePrefab(Sprite[] idleFrames)
	{
		GameObject root = new GameObject(
			"ShopkeeperNpc",
			typeof(SpriteRenderer),
			typeof(BoxCollider2D),
			typeof(ShopkeeperNpc));

		try
		{
			root.transform.localScale = Vector3.one;

			SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
			renderer.sprite = idleFrames[0];
			renderer.sortingOrder = 10;

			BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
			collider.isTrigger = true;
			Bounds spriteBounds = idleFrames[0].bounds;
			const float clickPadding = 0.12f;
			collider.offset = spriteBounds.center;
			Vector2 spriteSize = spriteBounds.size;
			collider.size = spriteSize + new Vector2(clickPadding * 2f, clickPadding * 2f);

			ShopkeeperNpc npc = root.GetComponent<ShopkeeperNpc>();
			SerializedObject so = new SerializedObject(npc);
			so.FindProperty("idleFrames").arraySize = idleFrames.Length;
			for (int i = 0; i < idleFrames.Length; i++)
				so.FindProperty("idleFrames").GetArrayElementAtIndex(i).objectReferenceValue = idleFrames[i];
			so.FindProperty("idleFps").floatValue = 8f;
			so.ApplyModifiedPropertiesWithoutUndo();

			return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	static void RegisterInPool(PoolManager pool, GameObject prefab)
	{
		int existingIndex = pool.FindShopkeeperGimmickIndex();
		if (existingIndex >= 0)
		{
			pool.gimmickPrefabs[existingIndex] = prefab;
			EditorUtility.SetDirty(pool);
			return;
		}

		GameObject[] old = pool.gimmickPrefabs;
		int oldLength = old != null ? old.Length : 0;
		var expanded = new GameObject[oldLength + 1];
		for (int i = 0; i < oldLength; i++)
			expanded[i] = old[i];
		expanded[oldLength] = prefab;
		pool.gimmickPrefabs = expanded;
		EditorUtility.SetDirty(pool);
	}
}
#endif
