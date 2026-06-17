using UnityEngine;

/// <summary>런타임 효과음·짧은 BGM 재생 진입점.</summary>
public static class GameAudio
{
	public static void Play(SfxId id, float volumeMultiplier = 1f)
	{
		GameAudioSettings.Instance?.PlaySfx(id, volumeMultiplier);
	}

	public static void PlayOnce(SfxId id, float volumeMultiplier = 1f)
	{
		GameAudioSettings.Instance?.PlaySfxOnce(id, volumeMultiplier);
	}

	public static void PlayLoop(SfxId id)
	{
		GameAudioSettings.Instance?.PlaySfxLoop(id);
	}

	public static void StopLoop(SfxId id)
	{
		GameAudioSettings.Instance?.StopSfxLoop(id);
	}

	/// <summary>메인 메뉴 복귀·새 게임 시작 시 악세사리 루프·잔여 효과음을 모두 끕니다.</summary>
	public static void ResetGameplaySfx()
	{
		GameAudioSettings.Instance?.ResetGameplaySfx();
	}

	public static void PlayTogether(SfxId first, SfxId second)
	{
		Play(first);
		Play(second);
	}

	public static void PlayWeapon(string weaponType)
	{
		GameAudioSettings.Instance?.PlayWeaponSfx(weaponType);
	}

	public static void PlayEnemyHit(GameObject target)
	{
		GameAudioSettings.Instance?.PlayEnemyHitSfx(target);
	}

	public static void PlayPlayerHit()
	{
		GameAudioSettings.Instance?.PlayPlayerHitSfx();
	}

	public static void PlayStageClear()
	{
		GameAudioSettings.Instance?.PlayStageClearStinger();
	}

	public static void PlayDeath()
	{
		GameAudioSettings.Instance?.PlayDeathStinger();
	}

	public static void PlayMainMenu()
	{
		GameAudioSettings.Instance?.TransitionToMainMenuBgm();
	}

	public static void EnsureStageBgm(int stageIndex)
	{
		GameAudioSettings.Instance?.EnsureStageBgmPlaying(stageIndex);
	}

	public static void PlayPanelOpen() => Play(SfxId.PanelOpen);
	public static void PlayUiClick() => Play(SfxId.UiClick);
	public static void PlayUiTextInput() => Play(SfxId.UiTextInput);
	public static void PlayPortalTravel() => Play(SfxId.PortalTravel);
	public static void PlayPurchase() => Play(SfxId.ItemPurchaseReward);
}
