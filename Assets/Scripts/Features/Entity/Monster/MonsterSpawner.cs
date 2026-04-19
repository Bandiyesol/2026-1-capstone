using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
	[Header("[ Spawn Setting ]")]
	public float minSpawnDistance;
	public float maxSpawnDistance;

	private Transform playerTransform;
	private Coroutine spawnCoroutine;

	void Start()
	{
		SpawnMonster();
	}

	void SpawnMonster()
	{
		if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
		spawnCoroutine = StartCoroutine(SpawnRoutine());
	}

	public void StopSpawnMonster()
	{
		if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
	}

	IEnumerator SpawnRoutine()
	{
		while (playerTransform == null)
		{
			GameObject p = GameObject.FindGameObjectWithTag("Player");
			if (p != null) playerTransform = p.transform;
			yield return new WaitForSeconds(0.5f);
		}

		Debug.Log("몬스터 스포너 작동 시작!");

		while (true)
		{
			if (StageManager.Instance.isSpawning && StageManager.Instance.spawnCount < StageManager.Instance.totalToSpawnMonster)
			{
				yield return new WaitForSeconds(StageManager.Instance.spawnInterval);

				for (int i = 0; i < StageManager.Instance.perToSpawnMonster; i++)
				{
					if (StageManager.Instance.spawnCount >= StageManager.Instance.totalToSpawnMonster) break;

					Vector3 spawnPos = GetRandomSpawnPosition();
					MonsterPoolManager.Instance.GetMonster(spawnPos);
					StageManager.Instance.spawnCount++;
				}
			}

			else yield return new WaitForSeconds(1f);
		}
	}
	Vector3 GetRandomSpawnPosition()
	{
		if (playerTransform == null) return Vector3.zero;
		Vector2 randomPoint = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
		return playerTransform.position + new Vector3(randomPoint.x, randomPoint.y, 0);
	}
}
