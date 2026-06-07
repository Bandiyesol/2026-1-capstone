using UnityEngine;

/// <summary>씬에 배치된 보상·악세사리 서비스 초기화. 씬에 없으면 런타임에 추가합니다.</summary>
public static class RewardSystemBootstrap
{
	public static void EnsureRewardSystem()
	{
		EnsureManagerComponents();
		EnsurePlayerComponents();
		EnsureGameManagerComponents();

		RewardSelectUI ui = RewardSelectUI.GetOrFind();
		ui?.EnsureReady();

		if (RewardRollService.instance != null)
			RewardRollService.instance.EnsureAccessoryPool();

		TmpKoreanFontUtility.EnsureAllAccessoryGlyphs(TmpKoreanFontUtility.ResolveNeoDgmFont(null));
	}

	static void EnsureManagerComponents()
	{
		GameObject manager = GameObject.Find("[ Manager ]");
		if (manager == null) return;

		if (manager.GetComponent<AccessoryManager>() == null)
			manager.AddComponent<AccessoryManager>();
		if (manager.GetComponent<AccessoryEffect>() == null)
			manager.AddComponent<AccessoryEffect>();
		if (manager.GetComponent<RewardRollService>() == null)
			manager.AddComponent<RewardRollService>();
	}

	static void EnsurePlayerComponents()
	{
		WeaponInventory weapon = Object.FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);
		if (weapon == null) return;

		GameObject player = weapon.gameObject;
		if (player.GetComponent<AccessoryInventory>() == null)
			player.AddComponent<AccessoryInventory>();
		if (player.GetComponent<PotionInventory>() == null)
			player.AddComponent<PotionInventory>();
		if (player.GetComponent<PotionEffect>() == null)
			player.AddComponent<PotionEffect>();
	}

	static void EnsureGameManagerComponents()
	{
		GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
		if (gm == null) return;

		if (gm.GetComponent<OverlayPanelEscapeInput>() == null)
			gm.gameObject.AddComponent<OverlayPanelEscapeInput>();
		if (gm.GetComponent<EndingSequenceController>() == null)
			gm.gameObject.AddComponent<EndingSequenceController>();
	}
}
