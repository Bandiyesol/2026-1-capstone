using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Firebase 이메일/비밀번호 로그인·회원가입·이메일 인증.
/// 로그인 아이디는 Firestore username → email 매핑으로 처리합니다.
/// </summary>
public class AuthManager : MonoBehaviour
{
	public static AuthManager Instance { get; private set; }

	public FirebaseUser CurrentUser => auth?.CurrentUser;
	public bool IsLoggedIn => auth?.CurrentUser != null;

	FirebaseAuth auth;
	UserProfileRepository profiles;

	static void MakePersistRoot(GameObject target)
	{
		if (target.transform.parent != null)
			target.transform.SetParent(null);

		DontDestroyOnLoad(target);
	}

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		MakePersistRoot(gameObject);
		profiles = new UserProfileRepository();
	}

	public async Task<(bool ok, string message)> LoginAsync(string idOrEmail, string password)
	{
		var ready = await EnsureReadyAsync();
		if (!ready.ok)
			return (false, ready.error);

		if (!AuthValidation.IsValidPassword(password, out string passwordError))
			return (false, passwordError);

		try
		{
			string email = await ResolveEmailAsync(idOrEmail);
			if (string.IsNullOrEmpty(email))
				return (false, "아이디를 찾을 수 없습니다.");

			AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
			FirebaseUser user = result.User;

			await user.ReloadAsync();
			user = auth.CurrentUser;

			if (user == null || !user.IsEmailVerified)
			{
				auth.SignOut();
				return (false, "이메일 인증이 완료되지 않았습니다. 메일함의 링크를 눌러 주세요.");
			}

			return (true, "로그인 성공");
		}
		catch (FirebaseException exception)
		{
			return (false, TranslateFirebaseError(exception));
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return (false, "로그인 중 오류가 발생했습니다.");
		}
	}

	public async Task<(bool ok, string message)> SignUpAsync(
		string username,
		string email,
		string password,
		string passwordConfirm,
		string nickname)
	{
		var ready = await EnsureReadyAsync();
		if (!ready.ok)
			return (false, ready.error);

		username = UserProfileRepository.NormalizeUsername(username);
		email = email?.Trim();
		nickname = nickname?.Trim();

		if (!AuthValidation.IsValidUsername(username))
		{
			string shown = string.IsNullOrEmpty(username) ? "(비어 있음)" : username;
			return (false, $"아이디 '{shown}' 는 사용할 수 없습니다. 영문/숫자/_ 3~20자만 가능합니다.");
		}

		if (!AuthValidation.IsValidEmail(email))
			return (false, "올바른 이메일 형식이 아닙니다.");

		if (!AuthValidation.IsValidPassword(password, out string passwordError))
			return (false, passwordError);

		if (!AuthValidation.PasswordsMatch(password, passwordConfirm, out string matchError))
			return (false, matchError);

		if (!AuthValidation.IsValidNickname(nickname, out string nicknameError))
			return (false, nicknameError);

		FirebaseUser createdUser = null;
		bool verificationEmailSent = false;

		try
		{
			if (await profiles.IsUsernameTakenAsync(username))
				return (false, "이미 사용 중인 아이디입니다.");

			Debug.Log($"[AuthManager] 계정 생성 시도: {email}");
			AuthResult createResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
			createdUser = createResult.User;

			await createdUser.SendEmailVerificationAsync();
			verificationEmailSent = true;
			Debug.Log($"[AuthManager] 인증 메일 발송 완료: {email}");

			try
			{
				await profiles.SaveProfileAsync(createdUser.UserId, username, nickname, email);
				Debug.Log($"[AuthManager] Firestore 프로필 저장 완료: {username}");
			}
			catch (Exception profileException)
			{
				Debug.LogError($"[AuthManager] Firestore 저장 실패 (인증 메일은 이미 발송됨): {profileException}");
				auth.SignOut();
				return (true,
					"인증 메일은 보냈습니다. Firestore 저장에 실패했습니다. " +
					"인증 후에는 이메일 주소로 로그인하세요. (Firebase Console → Firestore 확인)");
			}

			auth.SignOut();
			return (true, $"인증 메일을 {email} 로 보냈습니다. 스팸함도 확인한 뒤 링크를 누르고 로그인하세요.");
		}
		catch (FirebaseException exception)
		{
			if (createdUser != null && !verificationEmailSent)
				await TryDeleteUserAsync(createdUser);

			string message = TranslateFirebaseError(exception);
			Debug.LogWarning($"[AuthManager] 회원가입 Firebase 오류: {message}");
			return (false, message);
		}
		catch (Exception exception)
		{
			if (createdUser != null && !verificationEmailSent)
				await TryDeleteUserAsync(createdUser);

			Debug.LogException(exception);
			string detail = exception.Message;
			if (detail.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
			    detail.Contains("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase))
			{
				return (false, "Firestore 권한 오류입니다. Firebase Console에서 Firestore DB·보안 규칙을 확인하세요.");
			}

			return (false, $"회원가입 중 오류: {detail}");
		}
	}

	public async Task<(bool ok, string message)> SendPasswordResetAsync(string idOrEmail)
	{
		var ready = await EnsureReadyAsync();
		if (!ready.ok)
			return (false, ready.error);

		string email = await ResolveEmailAsync(idOrEmail);
		if (string.IsNullOrEmpty(email))
			return (false, "아이디 또는 이메일을 찾을 수 없습니다.");

		if (!AuthValidation.IsValidEmail(email))
			return (false, "올바른 이메일 형식이 아닙니다.");

		try
		{
			await auth.SendPasswordResetEmailAsync(email);
			return (true, "비밀번호 재설정 메일을 보냈습니다. 메일함을 확인하세요.");
		}
		catch (FirebaseException exception)
		{
			return (false, TranslateFirebaseError(exception));
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return (false, "비밀번호 재설정 메일 전송에 실패했습니다.");
		}
	}

	public async Task<bool> RefreshAndCheckEmailVerifiedAsync()
	{
		if (auth?.CurrentUser == null)
			return false;

		try
		{
			await auth.CurrentUser.ReloadAsync();
			return auth.CurrentUser.IsEmailVerified;
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[AuthManager] 사용자 정보 갱신 실패: {exception.Message}");
			return false;
		}
	}

	public Task SignOutAsync()
	{
		if (auth == null)
			return Task.CompletedTask;

		try
		{
			auth.SignOut();
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[AuthManager] 로그아웃 경고: {exception.Message}");
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// 비밀번호 재인증 후 Firebase Auth 계정 및 Firestore 프로필을 삭제합니다.
	/// </summary>
	public async Task<(bool ok, string message)> DeleteAccountAsync(string password)
	{
		var ready = await EnsureReadyAsync();
		if (!ready.ok)
			return (false, ready.error);

		FirebaseUser user = auth?.CurrentUser;
		if (user == null)
			return (false, "로그인되어 있지 않습니다.");

		if (!AuthValidation.IsValidPassword(password, out string passwordError))
			return (false, passwordError);

		string email = user.Email;
		if (string.IsNullOrEmpty(email))
			return (false, "계정 이메일 정보를 확인할 수 없습니다.");

		try
		{
			UserProfileRepository.UserProfileRecord profile =
				await profiles.GetProfileAsync(user.UserId);

			Credential credential = EmailAuthProvider.GetCredential(email, password);
			await user.ReauthenticateAsync(credential);

			await profiles.DeleteProfileAsync(user.UserId, profile?.Username);
			await user.DeleteAsync();

			try
			{
				auth.SignOut();
			}
			catch (Exception signOutException)
			{
				Debug.LogWarning($"[AuthManager] 탈퇴 후 로그아웃 경고: {signOutException.Message}");
			}

			return (true, "회원 탈퇴가 완료되었습니다.");
		}
		catch (FirebaseException exception)
		{
			return (false, TranslateFirebaseError(exception));
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			string detail = exception.Message;
			if (detail.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
			    detail.Contains("PERMISSION_DENIED", StringComparison.OrdinalIgnoreCase))
			{
				return (false,
					"Firestore 삭제 권한이 없습니다. Firebase Console 보안 규칙에 delete 권한을 추가하세요.");
			}

			return (false, "회원 탈퇴 중 오류가 발생했습니다.");
		}
	}

	public async Task<(bool ok, string message)> ResendVerificationEmailAsync(string idOrEmail, string password)
	{
		var ready = await EnsureReadyAsync();
		if (!ready.ok)
			return (false, ready.error);

		try
		{
			string email = await ResolveEmailAsync(idOrEmail);
			if (string.IsNullOrEmpty(email))
				return (false, "아이디를 찾을 수 없습니다.");

			AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
			FirebaseUser user = result.User;

			if (user.IsEmailVerified)
			{
				auth.SignOut();
				return (true, "이미 인증된 계정입니다. 로그인하세요.");
			}

			await user.SendEmailVerificationAsync();
			auth.SignOut();
			return (true, "인증 메일을 다시 보냈습니다.");
		}
		catch (FirebaseException exception)
		{
			return (false, TranslateFirebaseError(exception));
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return (false, "인증 메일 재전송에 실패했습니다.");
		}
	}

	async Task<(bool ok, string error)> EnsureReadyAsync()
	{
		bool ready = await FirebaseBootstrap.EnsureInitializedAsync();
		if (!ready)
			return (false, FirebaseBootstrap.LastError ?? "Firebase 초기화에 실패했습니다.");

		if (auth == null)
			auth = FirebaseAuth.DefaultInstance;

		if (auth == null)
			return (false, "FirebaseAuth 를 사용할 수 없습니다.");

		return (true, null);
	}

	async Task<string> ResolveEmailAsync(string idOrEmail)
	{
		string input = idOrEmail?.Trim();
		if (string.IsNullOrEmpty(input))
			return null;

		if (input.Contains("@"))
			return input;

		return await profiles.GetEmailByUsernameAsync(input);
	}

	static async Task TryDeleteUserAsync(FirebaseUser user)
	{
		try
		{
			await user.DeleteAsync();
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[AuthManager] 회원가입 롤백 삭제 실패: {exception.Message}");
		}
	}

	static string TranslateFirebaseError(FirebaseException exception)
	{
		AuthError error = (AuthError)exception.ErrorCode;

		return error switch
		{
			AuthError.EmailAlreadyInUse => "이미 가입된 이메일입니다.",
			AuthError.InvalidEmail => "이메일 형식이 올바르지 않습니다.",
			AuthError.WrongPassword => "비밀번호가 올바르지 않습니다.",
			AuthError.UserNotFound => "계정을 찾을 수 없습니다.",
			AuthError.WeakPassword => "비밀번호가 너무 약합니다. 6자 이상으로 설정하세요.",
			AuthError.TooManyRequests => "요청이 너무 많습니다. 잠시 후 다시 시도하세요.",
			AuthError.RequiresRecentLogin => "보안을 위해 비밀번호를 다시 입력해 주세요.",
			AuthError.InvalidCredential => "비밀번호가 올바르지 않습니다.",
			_ => exception.Message,
		};
	}
}
