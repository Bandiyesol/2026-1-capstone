using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
	public static WeaponManager Instance;
	private Dictionary<string, WeaponInfo> weaponDatabase = new Dictionary<string, WeaponInfo>();
	private Dictionary<string, WeaponBalance> balanceDatabase = new Dictionary<string, WeaponBalance>();
	private Dictionary<string, GameObject> motionPrefabs = new Dictionary<string, GameObject>();
	private Dictionary<string, Sprite> weaponSprites = new Dictionary<string, Sprite>();

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);

			LoadWeaponData();
			LoadResourceData();
		}

		else Destroy(gameObject);
	}

	void LoadWeaponData()
{
	TextAsset weaponJson = Resources.Load<TextAsset>("Data/Weapons");

	if (weaponJson != null)
	{
		string jsonText = weaponJson.text;
		WeaponDataLoader loader = JsonUtility.FromJson<WeaponDataLoader>(jsonText);

		weaponDatabase.Clear();
		foreach (var w in loader.weapons)
		{
			if (!weaponDatabase.ContainsKey(w.id)) weaponDatabase.Add(w.id, w);
		}

		balanceDatabase.Clear();
		foreach (var b in loader.balances)
		{
			if (!balanceDatabase.ContainsKey(b.key)) balanceDatabase.Add(b.key, b);
		}

		Debug.Log($"[Resources] 무기 데이터 로드 완료: {weaponDatabase.Count}개");
	}
	else
	{
		Debug.LogError("Resources 폴더에서 JSON 파일을 찾을 수 없습니다! 경로를 확인하세요.");
	}
}

	void LoadResourceData()
	{
		Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Weapons");
		foreach (var s in sprites) weaponSprites[s.name] = s;

		GameObject[] motions = Resources.LoadAll<GameObject>("Prefabs/Motions");
		foreach (var p in motions) motionPrefabs[p.name] = p;

		Debug.Log($"리소스 로드 완료: 이미지 {weaponSprites.Count}개, 프리팹 {motionPrefabs.Count}개");
	}

	public WeaponInfo GetWeaponInfo(string id) => weaponDatabase.GetValueOrDefault(id);
	
	public WeaponBalance GetWeaponBalance(string id) => balanceDatabase.GetValueOrDefault(id);

	public Sprite GetWeaponSprite(string spriteId) => weaponSprites.GetValueOrDefault(spriteId);

	public GameObject GetMotionPrefab(string motionId)
	{
		if (motionPrefabs.TryGetValue(motionId, out GameObject prefab)) return prefab;
		
		Debug.LogWarning($"투사체 프리팹 [{motionId}]을 찾을 수 없습니다!");
		return null;
	}
}