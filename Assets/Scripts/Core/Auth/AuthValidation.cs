using System.Text.RegularExpressions;

public static class AuthValidation
{
	static readonly Regex EmailRegex = new Regex(
		@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	static readonly Regex UsernameRegex = new Regex(
		@"^[a-zA-Z0-9_]{3,20}$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static bool IsValidEmail(string email)
	{
		return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
	}

	public static bool IsValidUsername(string username)
	{
		return !string.IsNullOrWhiteSpace(username) && UsernameRegex.IsMatch(username.Trim());
	}

	public static bool IsValidPassword(string password, out string error)
	{
		error = null;

		if (string.IsNullOrEmpty(password))
		{
			error = "비밀번호를 입력하세요.";
			return false;
		}

		if (password.Length < 6)
		{
			error = "비밀번호는 6자 이상이어야 합니다.";
			return false;
		}

		return true;
	}

	public static bool PasswordsMatch(string password, string confirm, out string error)
	{
		error = null;

		if (password != confirm)
		{
			error = "비밀번호가 일치하지 않습니다.";
			return false;
		}

		return true;
	}

	public static bool IsValidNickname(string nickname, out string error)
	{
		error = null;
		string trimmed = nickname?.Trim();

		if (string.IsNullOrEmpty(trimmed))
		{
			error = "닉네임을 입력하세요.";
			return false;
		}

		if (trimmed.Length < 2 || trimmed.Length > 12)
		{
			error = "닉네임은 2~12자여야 합니다.";
			return false;
		}

		return true;
	}
}
