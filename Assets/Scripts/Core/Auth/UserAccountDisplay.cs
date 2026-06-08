using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>로그인 계정 닉네임 캐시 — 랭킹·기록 표시용.</summary>
public static class UserAccountDisplay
{
	static string cachedNickname;

	public static string CachedNickname =>
		string.IsNullOrWhiteSpace(cachedNickname) ? null : cachedNickname.Trim();

	public static async Task RefreshAsync()
	{
		cachedNickname = null;

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
			return;

		string userId = AuthManager.Instance.CurrentUser?.UserId;
		if (string.IsNullOrEmpty(userId))
			return;

		try
		{
			var repository = new UserProfileRepository();
			UserProfileRepository.UserProfileRecord profile = await repository.GetProfileAsync(userId);
			if (profile != null && !string.IsNullOrWhiteSpace(profile.Nickname))
				cachedNickname = profile.Nickname.Trim();
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[UserAccountDisplay] 프로필 로드 실패: {exception.Message}");
		}
	}

	public static void ClearCache()
	{
		cachedNickname = null;
	}

	public static string ResolveRecordDisplayName(GameRunRecord record)
	{
		if (record != null && !string.IsNullOrWhiteSpace(record.playerNickname))
			return record.playerNickname.Trim();

		if (!string.IsNullOrEmpty(CachedNickname))
			return CachedNickname;

		if (record != null && !string.IsNullOrWhiteSpace(record.characterLabel))
			return record.characterLabel.Trim();

		return "모험가";
	}
}
