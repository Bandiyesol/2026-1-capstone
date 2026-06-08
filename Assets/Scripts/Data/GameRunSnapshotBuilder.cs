using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 플레이 세션 상태를 <see cref="GameRunRecord"/>로 만듭니다.</summary>
public static class GameRunSnapshotBuilder
{
	public static GameRunRecord Build(bool cleared)
	{
		GameRunSessionTracker.FinalizeRun(cleared);

		GameRunRecord record = GameRunRecord.CreateNew();
		record.cleared = cleared;

		GameManager game = GameManager.instance;
		if (game != null)
		{
			record.playTimeSeconds = game.gameTime;
			record.killCount = game.Kill;
		}

		StageManager stage = Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
		if (stage != null)
			record.stageReached = stage.CurrentStage;

		int bossStageIndex = Mathf.Clamp(record.stageReached - 1, 0, GameRunSessionTracker.MaxStages - 1);
		record.bossName = BossBriefingRuntime.GetBossDisplayName(bossStageIndex);

		record.characterId = GameRunSessionTracker.CharacterId;
		record.characterLabel = GameCharacterCatalog.GetDisplayName(record.characterId);
		record.playerNickname = UserAccountDisplay.CachedNickname ?? string.Empty;
		record.stageRecords = GameRunSessionTracker.GetStageSnapshots();
		record.coinCount = GameRunSessionTracker.GetTotalGoldEarnedForRecord();
		record.weaponNames = CollectWeaponNames(out string[] spriteIds);
		record.weaponSpriteIds = spriteIds;
		record.accessoryNames = CollectAccessoryNames();
		record.runeNames = CollectRuneNames();

		return record;
	}

	static string[] CollectWeaponNames(out string[] spriteIds)
	{
		WeaponInventory inventory = Object.FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);
		if (inventory == null || inventory.Weapons.Count == 0)
		{
			spriteIds = System.Array.Empty<string>();
			return System.Array.Empty<string>();
		}

		var names = new List<string>();
		var sprites = new List<string>();

		foreach (WeaponInstance weapon in inventory.Weapons)
		{
			if (weapon?.info == null)
				continue;

			names.Add(string.IsNullOrEmpty(weapon.info.name) ? weapon.info.id : weapon.info.name);
			sprites.Add(weapon.info.spriteId ?? string.Empty);
		}

		spriteIds = sprites.ToArray();
		return names.ToArray();
	}

	static string[] CollectAccessoryNames()
	{
		if (AccessoryInventory.Instance == null || AccessoryInventory.Instance.Accessories.Count == 0)
			return System.Array.Empty<string>();

		var names = new List<string>();
		foreach (AccessoryData accessory in AccessoryInventory.Instance.Accessories)
		{
			if (accessory == null)
				continue;

			names.Add(string.IsNullOrEmpty(accessory.displayName)
				? accessory.name
				: accessory.displayName);
		}

		return names.ToArray();
	}

	static string[] CollectRuneNames()
	{
		if (RuneManager.instance == null)
			return System.Array.Empty<string>();

		var names = new List<string>();
		for (int i = 0; i < RuneManager.instance.SlotCount_; i++)
		{
			RuneData rune = RuneManager.instance.GetSlot(i);
			if (rune == null)
				continue;

			names.Add(string.IsNullOrEmpty(rune.runeName) ? rune.name : rune.runeName);
		}

		return names.ToArray();
	}
}
