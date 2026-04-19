using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MonsterPoolManager : MonoBehaviour
{
	public static MonsterPoolManager Instance;
	public int poolSize;
	public GameObject[] activeMapMonsterPrefabs;

	private List<GameObject> pool = new List<GameObject>();


	void Awake()
	{
		Instance = this;
		if (activeMapMonsterPrefabs != null && activeMapMonsterPrefabs.Length > 0) SetMapMonsters(activeMapMonsterPrefabs);
	}

	void CreatePool(int count)
	{
		for (int i = 0; i < count; i++)
		{
			GameObject randomPrefab = activeMapMonsterPrefabs[Random.Range(0, activeMapMonsterPrefabs.Length)];
			GameObject obj = Instantiate(randomPrefab, transform);
			obj.SetActive(false);
			pool.Add(obj);
		}
	}

	public void SetMapMonsters(GameObject[] newMonsters)
	{
		if (newMonsters == null || newMonsters.Length == 0) return;

		foreach (GameObject obj in pool) Destroy(obj);
		activeMapMonsterPrefabs = newMonsters;
		pool.Clear();
		CreatePool(poolSize);
	}

	public GameObject GetMonster(Vector3 spawnPosition)
	{
		foreach (GameObject monster in pool)
		{
			if (!monster.activeInHierarchy)
			{
				monster.transform.position = spawnPosition;
				monster.SetActive(true);
				return monster;
			}
		}

		if (activeMapMonsterPrefabs != null && activeMapMonsterPrefabs.Length > 0)
		{
			GameObject extraObj = Instantiate(activeMapMonsterPrefabs[0], transform);
			extraObj.transform.position = spawnPosition;
			extraObj.SetActive(true);
			pool.Add(extraObj);
			return extraObj;
		}

		return null;
	}
}
