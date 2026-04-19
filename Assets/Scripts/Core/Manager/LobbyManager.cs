using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
	public static LobbyManager Instance;
	public bool isNewGame;


	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		else
		{
			Destroy(gameObject);
		}
	}
	
	public void StartGame(bool newGame)
	{
		isNewGame = newGame;
		SceneManager.LoadScene("GameScene");
	}
}