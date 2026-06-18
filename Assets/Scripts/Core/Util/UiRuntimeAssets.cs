using TMPro;
using UnityEngine;

/// <summary>
/// 빌드에서 Resources로 로드 가능한 UI 스프라이트·폰트 헬퍼.
/// Arts 폴더 AssetDatabase 경로는 에디터 전용입니다.
/// </summary>
public static class UiRuntimeAssets
{
	const string UiResourceRoot = "UI";

	static Sprite cachedRuneHudIcon;
	static Sprite cachedCrossIdle;
	static Sprite cachedCrossPushed;

	public static Sprite LoadSprite(string resourcePath, string spriteName = null)
	{
		if (string.IsNullOrEmpty(resourcePath))
			return null;

		if (!string.IsNullOrEmpty(spriteName))
		{
			Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
			for (int i = 0; i < sprites.Length; i++)
			{
				Sprite sprite = sprites[i];
				if (sprite != null && sprite.name == spriteName)
					return sprite;
			}
		}

		Sprite direct = Resources.Load<Sprite>(resourcePath);
		if (direct != null)
			return direct;

		Sprite[] all = Resources.LoadAll<Sprite>(resourcePath);
		return all.Length > 0 ? all[0] : null;
	}

	public static Sprite LoadSlotFrameSprite()
	{
		return InventorySlotVisualSettings.LoadResourceFrameSprite();
	}

	public static Sprite LoadRuneHudIcon()
	{
		if (cachedRuneHudIcon != null)
			return cachedRuneHudIcon;

		cachedRuneHudIcon = LoadSprite($"{UiResourceRoot}/Runes_13_01", "Runes_13_01_0");
		return cachedRuneHudIcon;
	}

	public static Sprite LoadCrossIdleSprite()
	{
		if (cachedCrossIdle != null)
			return cachedCrossIdle;

		cachedCrossIdle = LoadSprite($"{UiResourceRoot}/Cross_Idle", "Cross_Idle_0");
		return cachedCrossIdle;
	}

	public static Sprite LoadCrossPushedSprite()
	{
		if (cachedCrossPushed != null)
			return cachedCrossPushed;

		cachedCrossPushed = LoadSprite($"{UiResourceRoot}/Cross_Pushed", "Cross_Pushed_0");
		return cachedCrossPushed;
	}

	public static TMP_FontAsset LoadKoreanFont()
	{
		return Resources.Load<TMP_FontAsset>($"{UiResourceRoot}/Fonts/neodgm SDF");
	}
}
