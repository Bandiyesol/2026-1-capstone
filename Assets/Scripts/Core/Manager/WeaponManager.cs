using System.Collections.Generic;
using UnityEngine;


public class WeaponManager : MonoBehaviour
{
	public static WeaponManager Instance;
	private Dictionary<string, WeaponInfo> infoDatabase = new Dictionary<string, WeaponInfo>();
	private Dictionary<string, WeaponBalance> balanceDatabase = new Dictionary<string, WeaponBalance>();
	private Dictionary<string, GameObject> motionPrefabs = new Dictionary<string, GameObject>();
	private Dictionary<string, Sprite> weaponSprites = new Dictionary<string, Sprite>();
	private Sprite fallbackWeaponSprite;


	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;

			if (transform.parent != null)
				transform.SetParent(null);
			DontDestroyOnLoad(gameObject);

			LoadWeaponData();
			LoadResourceData();
			ValidateLoadedData();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void LoadWeaponData()
	{
		TextAsset infoJson = Resources.Load<TextAsset>("Data/WeaponInfo");
		TextAsset balanceJson = Resources.Load<TextAsset>("Data/WeaponBalance");
	
		if (infoJson == null || balanceJson == null) 
		{
			Debug.LogError("Resources 폴더에서 JSON 파일을 찾을 수 없습니다! 경로를 확인하세요.");
			return;
		}

		WeaponDataLoader infoLoader = JsonUtility.FromJson<WeaponDataLoader>(infoJson.text);
		infoDatabase.Clear();
		foreach (var i in infoLoader.info)
		{
			if (!infoDatabase.ContainsKey(i.id)) infoDatabase.Add(i.id, i);
		}

		WeaponDataLoader balanceLoader = JsonUtility.FromJson<WeaponDataLoader>(balanceJson.text);
		balanceDatabase.Clear();
		foreach (var b in balanceLoader.balance)
		{
			if (!balanceDatabase.ContainsKey(b.key)) balanceDatabase.Add(b.key, b);
		}

		Debug.Log($"[Resources] 데이터 로드 완료: 정보({infoDatabase.Count}개), 밸런스({balanceDatabase.Count}개)");
	}


	void LoadResourceData()
	{
		RegisterSpritesFromResources("Sprites/Weapons");

		foreach (var info in infoDatabase.Values)
		{
			if (!string.IsNullOrEmpty(info.spriteId))
				GetWeaponSprite(info.spriteId);
		}

		GameObject[] motions = Resources.LoadAll<GameObject>("Prefabs/Motions");
		foreach (var p in motions) motionPrefabs[p.name] = p;

		Debug.Log($"리소스 로드 완료: 이미지 {weaponSprites.Count}개, 프리팹 {motionPrefabs.Count}개");
	}

	void RegisterSpritesFromResources(string resourcesFolder)
	{
		Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesFolder);
		foreach (Sprite sprite in sprites)
			RegisterWeaponSprite(sprite);
	}

	void RegisterWeaponSprite(Sprite sprite)
	{
		if (sprite == null)
			return;

		RegisterSpriteAlias(sprite.name, sprite);
		fallbackWeaponSprite ??= sprite;

		if (sprite.name.EndsWith("_0"))
			RegisterSpriteAlias(sprite.name.Substring(0, sprite.name.Length - 2), sprite);

		int spaceIndex = sprite.name.IndexOf(' ');
		if (spaceIndex > 0)
			RegisterSpriteAlias(sprite.name.Substring(0, spaceIndex), sprite);
	}

	void RegisterSpriteAlias(string key, Sprite sprite)
	{
		if (string.IsNullOrEmpty(key) || sprite == null)
			return;

		weaponSprites[key] = sprite;
	}

	public WeaponInfo GetWeaponInfo(string id) => infoDatabase.GetValueOrDefault(id);

	public IEnumerable<WeaponInfo> GetAllWeaponInfos() => infoDatabase.Values;
	
	public WeaponBalance GetWeaponBalance(string key) => balanceDatabase.GetValueOrDefault(key);

	/// <summary>등록된 전체 무기 ID 목록 반환. RewardRollService에서 사용.</summary>
	public List<string> GetAllWeaponIds() => new List<string>(infoDatabase.Keys);

	public Sprite GetWeaponSprite(string spriteId)
	{
		if (string.IsNullOrEmpty(spriteId))
			return null;

		if (weaponSprites.TryGetValue(spriteId, out Sprite cached))
			return cached;

		const string folder = "Sprites/Weapons";
		Sprite sprite = Resources.Load<Sprite>($"{folder}/{spriteId}");
		if (sprite == null)
		{
			Sprite[] subs = Resources.LoadAll<Sprite>($"{folder}/{spriteId}");
			if (subs != null && subs.Length > 0)
				sprite = subs[0];
		}

		if (sprite == null)
		{
			string altId = $"{spriteId}_0";
			if (weaponSprites.TryGetValue(altId, out sprite))
				RegisterSpriteAlias(spriteId, sprite);
		}

		if (sprite != null)
			RegisterSpriteAlias(spriteId, sprite);
		else
			Debug.LogWarning($"[WeaponManager] 스프라이트를 찾지 못했습니다: {spriteId} (경로: Resources/{folder}/{spriteId})");

		return sprite;
	}

	public Sprite GetWeaponSprite(WeaponInfo info)
	{
		if (info == null)
			return fallbackWeaponSprite;

		Sprite sprite = GetWeaponSprite(info.spriteId);
		if (sprite != null)
			return sprite;

		string fallbackId = GetFallbackSpriteId(info.type);
		if (!string.IsNullOrEmpty(fallbackId))
		{
			sprite = GetWeaponSprite(fallbackId);
			if (sprite != null)
				return sprite;
		}

		return fallbackWeaponSprite;
	}

	static string GetFallbackSpriteId(string weaponType)
	{
		return weaponType switch
		{
			"Sword" => "sword-practice-wooden",
			"Bow" => "bow_short",
			"Orb" => "orb_poison",
			_ => "sword-practice-wooden"
		};
	}

	public GameObject GetMotionPrefab(string motionId)
	{
		if (motionPrefabs.TryGetValue(motionId, out GameObject prefab)) return prefab;
		
		Debug.LogWarning($"프리팹 [{motionId}]을 찾을 수 없습니다!");
		return null;
	}

	[ContextMenu("Validate Weapon Resources")]
	void ValidateLoadedData()
	{
		foreach (WeaponInfo info in infoDatabase.Values)
		{
			if (string.IsNullOrWhiteSpace(info.id))
			{
				Debug.LogWarning("[WeaponManager] id가 비어 있는 무기 항목이 있습니다.");
				continue;
			}

			if (string.IsNullOrWhiteSpace(info.balanceKey) || !balanceDatabase.ContainsKey(info.balanceKey))
				Debug.LogWarning($"[WeaponManager] balanceKey 누락/불일치: {info.id} -> {info.balanceKey}");

			if (string.IsNullOrWhiteSpace(info.motionId) || !motionPrefabs.ContainsKey(info.motionId))
				Debug.LogWarning($"[WeaponManager] motionId 누락/불일치: {info.id} -> {info.motionId}");

			if (!string.IsNullOrWhiteSpace(info.spriteId) && GetWeaponSprite(info.spriteId) == null)
				Debug.LogWarning($"[WeaponManager] spriteId 누락/불일치: {info.id} -> {info.spriteId}");
		}
	}
}