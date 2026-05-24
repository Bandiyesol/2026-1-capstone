using UnityEngine;
using System.Collections.Generic;


public class WeaponManager : MonoBehaviour
{
	public static WeaponManager Instance;
	private Dictionary<string, WeaponInfo> infoDatabase = new Dictionary<string, WeaponInfo>();
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
		Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/Weapons");
		foreach (var s in sprites) weaponSprites[s.name] = s;

		GameObject[] motions = Resources.LoadAll<GameObject>("Prefabs/Motions");
		foreach (var p in motions) motionPrefabs[p.name] = p;

		Debug.Log($"리소스 로드 완료: 이미지 {weaponSprites.Count}개, 프리팹 {motionPrefabs.Count}개");
	}

	public WeaponInfo GetWeaponInfo(string id) => infoDatabase.GetValueOrDefault(id);
	
	public WeaponBalance GetWeaponBalance(string key) => balanceDatabase.GetValueOrDefault(key);

	public Sprite GetWeaponSprite(string spriteId) => weaponSprites.GetValueOrDefault(spriteId);

	public GameObject GetMotionPrefab(string motionId)
	{
		if (motionPrefabs.TryGetValue(motionId, out GameObject prefab)) return prefab;
		
		Debug.LogWarning($"프리팹 [{motionId}]을 찾을 수 없습니다!");
		return null;
	}
}