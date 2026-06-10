using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 시 로그인 화면만 표시하고, 로그인 성공 후 GameStart(타이틀)로 전환합니다.
/// Canvas 직접 자식(예: AuthScreen)에 붙이고 Inspector에서 연결하세요.
/// </summary>
public class AuthFlowController : MonoBehaviour
{
	[Header("Screens")]
	[Tooltip("로그인 성공 전까지 비활성. 보통 GameStart 오브젝트")]
	[SerializeField] GameObject gameStartRoot;

	[Tooltip("로그인/회원가입/비밀번호찾기 UI 묶음. 성공 시 전체 비활성")]
	[SerializeField] GameObject authScreenRoot;

	[Header("Panels")]
	[SerializeField] GameObject pressAnyKeyPanel;
	[SerializeField] TMP_Text pressAnyKeyText;
	[SerializeField] string pressAnyKeyMessage = "아무 키나 누르시오...";
	[SerializeField] GameObject loginPanel;
	[SerializeField] GameObject signUpPanel;
	[SerializeField] GameObject forgotPasswordPanel;

	[Header("Login")]
	[SerializeField] Component loginIdInput;
	[SerializeField] Component loginPasswordInput;
	[SerializeField] Button loginButton;
	[SerializeField] Button goToSignUpButton;
	[SerializeField] Button forgotPasswordButton;
	[SerializeField] Button quitButton;

	[Header("Sign Up")]
	[SerializeField] Component signUpUsernameInput;
	[SerializeField] Component signUpEmailInput;
	[SerializeField] Component signUpPasswordInput;
	[SerializeField] Component signUpPasswordConfirmInput;
	[SerializeField] Component signUpNicknameInput;
	[SerializeField] Button signUpButton;
	[SerializeField] Button backToLoginButton;

	[Header("Forgot Password")]
	[SerializeField] Component forgotPasswordIdOrEmailInput;
	[SerializeField] Button sendResetEmailButton;
	[SerializeField] Button forgotBackToLoginButton;

	[Header("Status")]
	[Tooltip("AuthScreen 직계 자식 StatusText 권장 (모든 패널 공용)")]
	[SerializeField] TMP_Text statusText;

	[Header("Loading")]
	[Tooltip("로그인/가입/화면 전환 시 표시. AuthScreen 최상위에 두고 처음엔 비활성")]
	[SerializeField] GameObject loadingPanel;
	[SerializeField] TMP_Text loadingText;
	[SerializeField] string loadingMessage = "로딩 중...";
	[SerializeField] float loadingMinDisplaySeconds = 0.45f;

	[Header("Optional")]
	[SerializeField] Button logoutButton;

	bool busy;
	bool awaitingPressAnyKey;
	int loadingDepth;
	bool loadingPanelResolved;
	bool loadingOverlayPrepared;
	bool pressAnyKeyPanelResolved;
	AuthFormKeyboardNavigation formKeyboardNavigation;

	void Awake()
	{
		EnsureLoadingOverlay();
		TryResolvePressAnyKeyPanel();
		TryResolveStatusText();
		EnsureFormKeyboardNavigation();
		if (loginButton != null)
			loginButton.onClick.AddListener(() => _ = OnLoginClickedAsync());

		if (goToSignUpButton != null)
			goToSignUpButton.onClick.AddListener(() => ShowSignUp());

		if (forgotPasswordButton != null)
			forgotPasswordButton.onClick.AddListener(() => ShowForgotPassword());

		if (quitButton != null)
			quitButton.onClick.AddListener(OnQuitClicked);

		if (signUpButton != null)
			signUpButton.onClick.AddListener(() => _ = OnSignUpClickedAsync());

		if (backToLoginButton != null)
			backToLoginButton.onClick.AddListener(() => _ = NavigateBackToLoginAsync());

		if (sendResetEmailButton != null)
			sendResetEmailButton.onClick.AddListener(() => _ = OnSendResetEmailAsync());

		if (forgotBackToLoginButton != null)
			forgotBackToLoginButton.onClick.AddListener(() => _ = NavigateBackToLoginAsync());

		if (logoutButton != null)
			logoutButton.onClick.AddListener(() => _ = OnLogoutClickedAsync());
	}

	void EnsureFormKeyboardNavigation()
	{
		formKeyboardNavigation = GetComponent<AuthFormKeyboardNavigation>();
		if (formKeyboardNavigation == null)
			formKeyboardNavigation = gameObject.AddComponent<AuthFormKeyboardNavigation>();

		formKeyboardNavigation.Initialize(
			new[]
			{
				new AuthFormKeyboardNavigation.FormConfig
				{
					panel = loginPanel,
					inputs = new[] { loginIdInput, loginPasswordInput },
					submitButton = loginButton
				},
				new AuthFormKeyboardNavigation.FormConfig
				{
					panel = signUpPanel,
					inputs = new[]
					{
						signUpUsernameInput,
						signUpEmailInput,
						signUpPasswordInput,
						signUpPasswordConfirmInput,
						signUpNicknameInput
					},
					submitButton = signUpButton
				},
				new AuthFormKeyboardNavigation.FormConfig
				{
					panel = forgotPasswordPanel,
					inputs = new[] { forgotPasswordIdOrEmailInput },
					submitButton = sendResetEmailButton
				}
			},
			() => busy || loadingDepth > 0 || awaitingPressAnyKey);
	}

	async void Start()
	{
		SetLoadingVisible(false);
		HideGameplayOverlayPanels();

		if (gameStartRoot != null)
			gameStartRoot.SetActive(false);

		if (authScreenRoot != null)
			authScreenRoot.SetActive(true);

		if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
		{
			await RunAuthActionAsync(async () =>
			{
				bool verified = await AuthManager.Instance.RefreshAndCheckEmailVerifiedAsync();
				if (verified)
					await EnterGameStartAsync();
			});
		}
		else if (pressAnyKeyPanel != null)
		{
			ShowPressAnyKey();
		}
		else
		{
			Debug.LogWarning(
				"[AuthFlow] PressAnyKeyPanel이 없습니다. AuthScreen에 패널을 만들거나 Inspector에 연결하세요. LoginPanel로 시작합니다.");
			ShowLogin();
		}
	}

	void Update()
	{
		if (!awaitingPressAnyKey || busy)
			return;

		if (WasAnyInputPressedThisFrame())
			_ = OnPressAnyKeyContinueAsync();
	}

	void ShowPressAnyKey()
	{
		awaitingPressAnyKey = true;
		ClearAllAuthInputs();
		SetPanelActive(pressAnyKeyPanel, true);
		SetPanelActive(loginPanel, false);
		SetPanelActive(signUpPanel, false);
		SetPanelActive(forgotPasswordPanel, false);
		SetStatusTextVisible(false);

		if (pressAnyKeyText != null)
			pressAnyKeyText.text = pressAnyKeyMessage;
	}

	void ShowLogin(bool clearStatus = true)
	{
		awaitingPressAnyKey = false;
		ClearSignUpInputs();
		ClearForgotPasswordInputs();
		ClearLoginInputs();
		SetPanelActive(pressAnyKeyPanel, false);
		SetPanelActive(loginPanel, true);
		SetPanelActive(signUpPanel, false);
		SetPanelActive(forgotPasswordPanel, false);
		SetStatusTextVisible(true);
		if (clearStatus)
			SetStatus("");
	}

	void ShowSignUp(bool clearStatus = true)
	{
		awaitingPressAnyKey = false;
		ClearLoginInputs();
		ClearForgotPasswordInputs();
		ClearSignUpInputs();
		SetPanelActive(pressAnyKeyPanel, false);
		SetPanelActive(loginPanel, false);
		SetPanelActive(signUpPanel, true);
		SetPanelActive(forgotPasswordPanel, false);
		SetStatusTextVisible(true);
		if (clearStatus)
			SetStatus("");
	}

	void ShowForgotPassword()
	{
		awaitingPressAnyKey = false;
		ClearLoginInputs();
		ClearSignUpInputs();
		ClearForgotPasswordInputs();
		SetPanelActive(pressAnyKeyPanel, false);
		SetPanelActive(loginPanel, false);
		SetPanelActive(signUpPanel, false);
		SetPanelActive(forgotPasswordPanel, true);
		SetStatusTextVisible(true);
		SetStatus("");
	}

	async Task OnPressAnyKeyContinueAsync()
	{
		if (busy || !awaitingPressAnyKey)
			return;

		awaitingPressAnyKey = false;
		await TransitionToLoginPanelAsync();
	}

	async Task EnterGameStartAsync()
	{
		using (BeginLoadingScope())
		{
			await ApplyEnterGameStartAsync();
		}
	}

	async Task ApplyEnterGameStartAsync()
	{
		CloseSettingsIfOpen();
		await WaitLoadingMinDisplayAsync();
		await UserAccountDisplay.RefreshAsync();

		if (authScreenRoot != null)
			authScreenRoot.SetActive(false);

		if (gameStartRoot != null)
			gameStartRoot.SetActive(true);

		ClearAllAuthInputs();
		SetStatus("");
		GameManager.RefreshMainMenuLeaderboard();
	}

	/// <summary>GameStart 로그아웃 버튼에서 호출</summary>
	public void RequestLogout()
	{
		_ = OnLogoutClickedAsync();
	}

	/// <summary>설정 화면 회원탈퇴 확인에서 호출</summary>
	public void RequestDeleteAccount(string password)
	{
		_ = OnDeleteAccountClickedAsync(password);
	}

	/// <summary>설정 화면 메인 메뉴 버튼에서 호출</summary>
	public void RequestReturnToMainMenu()
	{
		_ = ReturnToMainMenuWithLoadingAsync();
	}

	async Task ExitToLoginAsync()
	{
		using (BeginLoadingScope())
		{
			await ApplyExitToLoginAsync();
		}
	}

	async Task ApplyExitToLoginAsync()
	{
		await WaitLoadingMinDisplayAsync();
		UserAccountDisplay.ClearCache();

		if (gameStartRoot != null)
			gameStartRoot.SetActive(false);

		if (authScreenRoot != null)
			authScreenRoot.SetActive(true);

		ShowLogin();
	}

	async Task NavigateBackToLoginAsync()
	{
		if (busy)
			return;

		busy = true;
		SetButtonsInteractable(false);
		try
		{
			await TransitionToLoginPanelAsync();
		}
		finally
		{
			busy = false;
			SetButtonsInteractable(true);
		}
	}

	async Task TransitionToLoginPanelAsync(string statusMessage = null, bool clearStatus = true)
	{
		using (BeginLoadingScope())
		{
			await WaitLoadingMinDisplayAsync();
			ShowLogin(clearStatus: clearStatus);

			if (!clearStatus && !string.IsNullOrEmpty(statusMessage))
				SetStatus(statusMessage);
		}
	}

	async Task ReturnToMainMenuWithLoadingAsync()
	{
		using (BeginLoadingScope())
		{
			await WaitLoadingMinDisplayAsync();
			CloseSettingsIfOpen();
			GameManager.instance?.ReturnToMainMenu();

			if (authScreenRoot != null)
				authScreenRoot.SetActive(false);

			if (gameStartRoot != null)
				gameStartRoot.SetActive(true);
		}
	}

	/// <summary>GameStart 등에서 실패 시 로딩 없이 로그인 화면에 오류 표시</summary>
	async Task ShowAuthErrorAsync(string message)
	{
		await Task.Yield();

		if (gameStartRoot != null)
			gameStartRoot.SetActive(false);

		if (authScreenRoot != null)
			authScreenRoot.SetActive(true);

		ShowLogin(clearStatus: false);
		SetStatus(message);
	}

	async Task OnLoginClickedAsync()
	{
		if (busy || AuthManager.Instance == null)
		{
			SetStatus("AuthManager 가 씬에 없습니다.");
			return;
		}

		string id = AuthInputUtility.GetText(loginIdInput);
		string password = AuthInputUtility.GetText(loginPasswordInput);

		await RunAuthActionAsync(async () =>
		{
			var (ok, message) = await AuthManager.Instance.LoginAsync(id, password);
			if (!ok)
			{
				SetStatus(message);
				return;
			}

			await EnterGameStartAsync();
		});
	}

	async Task OnSignUpClickedAsync()
	{
		if (busy || AuthManager.Instance == null)
		{
			SetStatus("AuthManager 가 씬에 없습니다.");
			return;
		}

		string username = AuthInputUtility.GetText(signUpUsernameInput);
		string email = AuthInputUtility.GetText(signUpEmailInput);
		string password = AuthInputUtility.GetText(signUpPasswordInput);
		string confirm = AuthInputUtility.GetText(signUpPasswordConfirmInput);
		string nickname = AuthInputUtility.GetText(signUpNicknameInput);

		Debug.Log($"[AuthFlow] 가입 입력값 확인 - 아이디:'{username}', 이메일:'{email}', 닉네임:'{nickname}'");

		await RunAuthActionAsync(async () =>
		{
			var (ok, message) = await AuthManager.Instance.SignUpAsync(
				username, email, password, confirm, nickname);

			Debug.Log($"[AuthFlow] 회원가입 결과 ok={ok}, message={message}");
			if (!ok)
			{
				SetStatus(message);
				return;
			}

			await TransitionToLoginPanelAsync(message, clearStatus: false);
		});
	}

	async Task OnSendResetEmailAsync()
	{
		if (busy || AuthManager.Instance == null)
			return;

		string idOrEmail = AuthInputUtility.GetText(forgotPasswordIdOrEmailInput);

		await RunAuthActionAsync(async () =>
		{
			var (ok, message) = await AuthManager.Instance.SendPasswordResetAsync(idOrEmail);
			if (!ok)
			{
				SetStatus(message);
				return;
			}

			await TransitionToLoginPanelAsync(message, clearStatus: false);
		});
	}

	async Task OnLogoutClickedAsync()
	{
		if (busy || AuthManager.Instance == null)
			return;

		await RunAuthActionAsync(async () =>
		{
			using (BeginLoadingScope())
			{
				await AuthManager.Instance.SignOutAsync();
				SetStatus("로그아웃했습니다.");
				await ApplyExitToLoginAsync();
			}
		});
	}

	async Task OnDeleteAccountClickedAsync(string password)
	{
		if (busy || AuthManager.Instance == null)
			return;

		await RunAuthActionAsync(async () =>
		{
			var (ok, message) = await AuthManager.Instance.DeleteAccountAsync(password);
			if (!ok)
			{
				await ShowAuthErrorAsync(message);
				return;
			}

			using (BeginLoadingScope())
			{
				SetStatus(message);
				await ApplyExitToLoginAsync();
			}
		});
	}

	static void CloseSettingsIfOpen()
	{
		SettingsUI ui = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		ui?.Close();
	}

	void OnQuitClicked()
	{
		GameQuitUtility.RequestQuit();
	}

	/// <summary>버튼만 잠그고 작업 실행. 로딩 UI는 성공 후 화면 전환 시에만 표시합니다.</summary>
	async Task RunAuthActionAsync(System.Func<Task> action)
	{
		busy = true;
		SetButtonsInteractable(false);

		try
		{
			await action();
		}
		finally
		{
			busy = false;
			SetButtonsInteractable(true);
		}
	}

	LoadingScope BeginLoadingScope() => new LoadingScope(this);

	readonly struct LoadingScope : System.IDisposable
	{
		readonly AuthFlowController owner;

		public LoadingScope(AuthFlowController owner)
		{
			this.owner = owner;
			owner?.PushLoading();
		}

		public void Dispose()
		{
			owner?.PopLoading();
		}
	}

	void PushLoading()
	{
		EnsureLoadingOverlay();

		loadingDepth++;
		if (loadingDepth == 1)
		{
			// GameStart 화면에서 로그아웃/탈퇴 시 AuthScreen이 꺼져 있으면 로딩 패널이 안 보임
			if (authScreenRoot != null && !authScreenRoot.activeInHierarchy)
				authScreenRoot.SetActive(true);

			SetLoadingVisible(true);
		}
	}

	void EnsureLoadingOverlay()
	{
		if (!loadingPanelResolved)
		{
			TryResolveLoadingPanel();
			loadingPanelResolved = true;
		}

		if (loadingPanel == null)
			return;

		if (!loadingOverlayPrepared)
		{
			// AuthScreen 비활성화 시 같이 사라지지 않도록 Canvas 최상위로 이동 (최초 1회)
			if (authScreenRoot != null && loadingPanel.transform.IsChildOf(authScreenRoot.transform))
			{
				Canvas canvas = authScreenRoot.GetComponentInParent<Canvas>();
				if (canvas == null)
					canvas = FindFirstObjectByType<Canvas>();

				if (canvas != null)
					loadingPanel.transform.SetParent(canvas.transform, worldPositionStays: true);
			}

			loadingOverlayPrepared = true;
		}

		loadingPanel.transform.SetAsLastSibling();
	}

	void TryResolvePressAnyKeyPanel()
	{
		if (pressAnyKeyPanelResolved)
			return;

		pressAnyKeyPanelResolved = true;

		if (pressAnyKeyPanel != null)
			return;

		Transform searchRoot = authScreenRoot != null ? authScreenRoot.transform : transform;
		Transform found = FindDeep(searchRoot, "PressAnyKeyPanel");
		if (found == null)
			found = FindDeep(searchRoot, "PressAnyKey");

		if (found != null)
		{
			pressAnyKeyPanel = found.gameObject;
			if (pressAnyKeyText == null)
				pressAnyKeyText = found.GetComponentInChildren<TMP_Text>(true);
		}
	}

	static bool WasAnyInputPressedThisFrame()
	{
		if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
			return true;

		if (Mouse.current != null)
		{
			foreach (InputControl control in Mouse.current.allControls)
			{
				if (control is ButtonControl button && button.wasPressedThisFrame)
					return true;
			}
		}

		if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
			return true;

		if (Gamepad.current != null)
		{
			foreach (InputControl control in Gamepad.current.allControls)
			{
				if (control is ButtonControl button && button.wasPressedThisFrame)
					return true;
			}
		}

		return false;
	}

	void TryResolveStatusText()
	{
		if (statusText != null)
			return;

		Transform searchRoot = authScreenRoot != null ? authScreenRoot.transform : transform;

		// AuthScreen 바로 아래 StatusText (패널 안 중복 오브젝트 제외)
		for (int i = 0; i < searchRoot.childCount; i++)
		{
			Transform child = searchRoot.GetChild(i);
			if (child.name != "StatusText")
				continue;

			statusText = child.GetComponent<TMP_Text>();
			if (statusText != null)
				return;
		}

		Transform found = FindDeep(searchRoot, "StatusText");
		if (found != null)
			statusText = found.GetComponent<TMP_Text>();
	}

	void SetStatusTextVisible(bool visible)
	{
		if (statusText != null)
			statusText.gameObject.SetActive(visible);
	}

	void TryResolveLoadingPanel()
	{
		if (loadingPanel != null)
			return;

		Transform searchRoot = authScreenRoot != null ? authScreenRoot.transform : transform;
		Transform found = FindDeep(searchRoot, "LoadingPanel");
		if (found == null)
			found = FindDeep(searchRoot, "Loading");

		if (found == null && searchRoot != transform)
		{
			found = FindDeep(transform, "LoadingPanel");
			if (found == null)
				found = FindDeep(transform, "Loading");
		}

		if (found != null)
		{
			loadingPanel = found.gameObject;
			if (loadingText == null)
				loadingText = found.GetComponentInChildren<TMP_Text>(true);
		}
	}

	static Transform FindDeep(Transform root, string objectName)
	{
		if (root == null)
			return null;

		if (root.name == objectName)
			return root;

		for (int i = 0; i < root.childCount; i++)
		{
			Transform match = FindDeep(root.GetChild(i), objectName);
			if (match != null)
				return match;
		}

		return null;
	}

	async Task WaitLoadingMinDisplayAsync()
	{
		if (loadingMinDisplaySeconds <= 0f)
		{
			await Task.Yield();
			return;
		}

		float elapsed = 0f;
		while (elapsed < loadingMinDisplaySeconds)
		{
			await Task.Yield();
			elapsed += Time.unscaledDeltaTime;
		}
	}

	void PopLoading()
	{
		if (loadingDepth <= 0)
			return;

		loadingDepth--;
		if (loadingDepth == 0)
			SetLoadingVisible(false);
	}

	void HideGameplayOverlayPanels()
	{
		if (GameManager.instance != null)
		{
			GameManager.instance.HidePreGameSelectPanels();
			return;
		}

		foreach (RuneSelectUI runeUi in FindObjectsByType<RuneSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (runeUi != null)
				runeUi.gameObject.SetActive(false);
		}

		foreach (WeaponSelectUI weaponUi in FindObjectsByType<WeaponSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (weaponUi != null)
				weaponUi.gameObject.SetActive(false);
		}
	}

	void SetLoadingVisible(bool visible)
	{
		if (loadingPanel == null)
		{
			if (visible)
				Debug.LogWarning("[AuthFlow] Loading Panel이 연결되지 않았습니다. AuthScreen에 LoadingPanel을 만들고 Inspector에 연결하세요.");
			return;
		}

		if (visible)
			EnsureLoadingOverlay();

		loadingPanel.SetActive(visible);

		if (visible)
		{
			loadingPanel.transform.SetAsLastSibling();
			if (loadingText != null)
				loadingText.text = loadingMessage;

			// 같은 프레임에 화면에 그리도록 강제 갱신
			Canvas.ForceUpdateCanvases();
		}
	}

	void SetButtonsInteractable(bool interactable)
	{
		if (loginButton != null) loginButton.interactable = interactable;
		if (signUpButton != null) signUpButton.interactable = interactable;
		if (goToSignUpButton != null) goToSignUpButton.interactable = interactable;
		if (forgotPasswordButton != null) forgotPasswordButton.interactable = interactable;
		if (quitButton != null) quitButton.interactable = interactable;
		if (backToLoginButton != null) backToLoginButton.interactable = interactable;
		if (sendResetEmailButton != null) sendResetEmailButton.interactable = interactable;
		if (forgotBackToLoginButton != null) forgotBackToLoginButton.interactable = interactable;
		if (logoutButton != null) logoutButton.interactable = interactable;

		AuthInputUtility.SetInteractable(loginIdInput, interactable);
		AuthInputUtility.SetInteractable(loginPasswordInput, interactable);
		AuthInputUtility.SetInteractable(signUpUsernameInput, interactable);
		AuthInputUtility.SetInteractable(signUpEmailInput, interactable);
		AuthInputUtility.SetInteractable(signUpPasswordInput, interactable);
		AuthInputUtility.SetInteractable(signUpPasswordConfirmInput, interactable);
		AuthInputUtility.SetInteractable(signUpNicknameInput, interactable);
		AuthInputUtility.SetInteractable(forgotPasswordIdOrEmailInput, interactable);
	}

	void SetPanelActive(GameObject panel, bool active)
	{
		if (panel != null)
			panel.SetActive(active);
	}

	void SetStatus(string message)
	{
		if (statusText != null)
			statusText.text = message ?? "";
	}

	void ClearLoginInputs()
	{
		AuthInputUtility.ClearAll(loginIdInput, loginPasswordInput);
	}

	void ClearSignUpInputs()
	{
		AuthInputUtility.ClearAll(
			signUpUsernameInput,
			signUpEmailInput,
			signUpPasswordInput,
			signUpPasswordConfirmInput,
			signUpNicknameInput);
	}

	void ClearForgotPasswordInputs()
	{
		AuthInputUtility.ClearAll(forgotPasswordIdOrEmailInput);
	}

	void ClearAllAuthInputs()
	{
		ClearLoginInputs();
		ClearSignUpInputs();
		ClearForgotPasswordInputs();
	}
}
