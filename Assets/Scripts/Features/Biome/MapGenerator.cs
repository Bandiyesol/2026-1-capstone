using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

public class MapGenerator : MonoBehaviour
{
	[Header("[ References ]")]
	public Tilemap floorTilemap;
	public Tilemap obstacleTilemap;
	public TileBase[] tiles;
	public TileBase[] obstacles;
	public GameObject[] mapMonsterPrefabs;
	

	[Header("[ Setting ]")]
	public int chunkSize;
	public int viewDistance;
	public GameObject playerPrefab;

	public CinemachineCamera virtualCamera;
	
	private Transform playerTransform;
	private Vector2Int lastChunkCoord;
	private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();


	void Start()
	{
		if (LobbyManager.Instance.isNewGame) InitNewGame();
		else { LoadExistingGame(); }
	}

	void InitNewGame()
	{
		GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
		playerTransform = player.transform;
		if (virtualCamera != null) virtualCamera.Follow = playerTransform;
		if (MonsterPoolManager.Instance != null) MonsterPoolManager.Instance.SetMapMonsters(mapMonsterPrefabs);

		UpdateMap(0, 0, true);
	}

	void LoadExistingGame()
	{
		Debug.Log("데이터를 로드합니다.");
	}

	void Update()
	{
		if (playerTransform == null) return;

		int currentX = Mathf.FloorToInt(playerTransform.position.x / chunkSize);
		int currentY = Mathf.FloorToInt(playerTransform.position.y / chunkSize);
		Vector2Int currentChunkCoord = new Vector2Int(currentX, currentY);

		if (currentChunkCoord == lastChunkCoord) return;

		lastChunkCoord = currentChunkCoord;
		UpdateMap(currentX, currentY);
	}

	void UpdateMap(int px, int py, bool force = false)
	{
		for (int x = px - viewDistance; x <= px + viewDistance; x++)
		{
			for (int y = py - viewDistance; y <= py + viewDistance; y++)
			{
				Vector2Int chunkCoord = new Vector2Int(x, y);
				if (!generatedChunks.Contains(chunkCoord)) GenerateChunk(chunkCoord);
			}
		}
	}

	void GenerateChunk(Vector2Int coord)
	{
		int startX = coord.x * chunkSize;
		int startY = coord.y * chunkSize;
		TileBase[] floorArray = new TileBase[chunkSize * chunkSize];
		TileBase[] obstacleArray = new TileBase[chunkSize * chunkSize];

		for (int i = 0; i < floorArray.Length; i++)
		{
			floorArray[i] = tiles[Random.Range(0, tiles.Length)];

			if (Random.value < 0.05f) obstacleArray[i] = obstacles[Random.Range(0, obstacles.Length)];
			else obstacleArray[i] = null;
		}

		BoundsInt area = new BoundsInt(startX, startY, 0, chunkSize, chunkSize, 1);
		
		floorTilemap.SetTilesBlock(area, floorArray);
		obstacleTilemap.SetTilesBlock(area, obstacleArray);

		generatedChunks.Add(coord);
	}
}