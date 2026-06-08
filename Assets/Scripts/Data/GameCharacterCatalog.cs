using UnityEngine;

/// <summary>플레이어 캐릭터 ID → 표시 이름·초상화.</summary>
public static class GameCharacterCatalog
{
	public const string DefaultCharacterId = "Player0";

	public static string ResolveCharacterId(Player player)
	{
		if (player == null)
			return DefaultCharacterId;

		Transform current = player.transform;
		while (current != null)
		{
			string name = current.name;
			if (name.Contains("Player0"))
				return "Player0";

			current = current.parent;
		}

		return DefaultCharacterId;
	}

	public static string GetDisplayName(string characterId)
	{
		switch (characterId)
		{
			case DefaultCharacterId:
				return "기본 캐릭터";
			default:
				return "기본 캐릭터";
		}
	}

	public static Sprite GetPortrait(string characterId, Player livePlayer = null)
	{
		if (livePlayer != null && livePlayer.spriter != null && livePlayer.spriter.sprite != null)
			return livePlayer.spriter.sprite;

		Sprite fromResources = Resources.Load<Sprite>($"CharacterPortraits/{characterId}");
		if (fromResources != null)
			return fromResources;

		if (GameManager.instance != null && GameManager.instance.player != null)
		{
			SpriteRenderer spriter = GameManager.instance.player.spriter;
			if (spriter != null && spriter.sprite != null)
				return spriter.sprite;
		}

		return null;
	}
}
