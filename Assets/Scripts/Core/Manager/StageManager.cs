using UnityEngine;

public class StageManager : MonoBehaviour
{
	public static StageManager Instance;

	[Header("[ Stage Info ]")]
	public int currentStage;
	public int totalToSpawnMonster;
	public int perToSpawnMonster;
	public float spawnInterval;
	public bool isSpawning;

	[HideInInspector] public int spawnCount;
	private int deadCount;


	void Awake()
	{
		if (Instance == null) Instance = this;
	}

	void Start()
	{
		isSpawning = true;
	}

	void ClearStage()
	{
		Debug.Log($"STAGE {currentStage} CLEAR!");

		currentStage++;
		totalToSpawnMonster *= 2;
		spawnCount = 0;
		deadCount = 0;
	}

	public void StartNextStage()
	{
		isSpawning = true;
	}

	public void OnMonsterDead()
	{
		deadCount++;
		Debug.Log($"처치 수: {deadCount} / {totalToSpawnMonster}");

		if (deadCount >= totalToSpawnMonster) 
		{
			isSpawning = false;
			ClearStage();
		}
	}
}