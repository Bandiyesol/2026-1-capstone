using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
	public void GoToLobby()
	{
		SceneManager.LoadScene("LobbyScene");
	}
}
