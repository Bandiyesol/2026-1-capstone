using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameStart(타이틀) 화면 — 게임 시작(스토리), 설정, 로그아웃.
/// GameStart 오브젝트에 붙이고 버튼을 Inspector에서 연결하세요.
/// </summary>
public class GameStartMenuController : MonoBehaviour
{
	[SerializeField] Button startButton;
	[SerializeField] Button settingsButton;
	[SerializeField] Button logoutButton;

	[Tooltip("비우면 자식에서 ButtonStart 를 찾습니다.")]
	[SerializeField] string startButtonObjectName = "ButtonStart";

	void Awake()
	{
		TryResolveStartButton();

		if (startButton != null)
		{
			// Inspector 에 연결된 GameStart() 직접 호출을 스토리 플로우로 교체합니다.
			startButton.onClick.RemoveAllListeners();
			startButton.onClick.AddListener(OnStartClicked);
		}

		if (settingsButton != null)
			settingsButton.onClick.AddListener(OnSettingsClicked);

		if (logoutButton != null)
			logoutButton.onClick.AddListener(OnLogoutClicked);
	}

	void OnDestroy()
	{
		if (startButton != null)
			startButton.onClick.RemoveListener(OnStartClicked);

		if (settingsButton != null)
			settingsButton.onClick.RemoveListener(OnSettingsClicked);

		if (logoutButton != null)
			logoutButton.onClick.RemoveListener(OnLogoutClicked);
	}

	void TryResolveStartButton()
	{
		if (startButton != null)
			return;

		Transform found = transform.Find(startButtonObjectName);
		if (found != null)
			startButton = found.GetComponent<Button>();
	}

	void OnStartClicked()
	{
		if (GameManager.instance != null)
			GameManager.instance.BeginGameFromMenu();
	}

	void OnSettingsClicked()
	{
		SettingsUI ui = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		if (ui == null)
		{
			Debug.LogError("[GameStartMenu] SettingsUI를 찾을 수 없습니다. SettingPanel에 SettingsUI가 있는지 확인하세요.");
			return;
		}

		ui.Open();
	}

	void OnLogoutClicked()
	{
		SettingsUI ui = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		ui?.Close();

		AuthFlowController auth = FindFirstObjectByType<AuthFlowController>(FindObjectsInactive.Include);
		if (auth == null)
		{
			Debug.LogError("[GameStartMenu] AuthFlowController를 찾을 수 없습니다. AuthScreen에 있는지 확인하세요.");
			return;
		}

		auth.RequestLogout();
	}
}
