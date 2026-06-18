using UnityEngine;

[CreateAssetMenu(
	fileName = "InventorySlotVisualSettings",
	menuName = "Scriptable/UI/Inventory Slot Visual")]
public class InventorySlotVisualSettings : ScriptableObject
{
	public const string ResourceFrameSpriteName = "Panels_06_0";
	public const string ResourceFrameTexturePath = "UI/Panels_06";

	public Sprite slotFrameSprite;
	public float iconPadding = 12f;

	static InventorySlotVisualSettings cached;
	static Sprite cachedResourceFrame;
	static Sprite fallbackHitSprite;

	public static InventorySlotVisualSettings Instance
	{
		get
		{
			if (cached == null)
				cached = Resources.Load<InventorySlotVisualSettings>("UI/InventorySlotVisualSettings");

			return cached;
		}
	}

	/// <summary>빌드에서도 슬롯 프레임·클릭 영역을 확보합니다.</summary>
	public static Sprite ResolveSlotFrameSprite(Sprite preferred)
	{
		if (preferred != null)
			return preferred;

		InventorySlotVisualSettings settings = Instance;
		if (settings != null && settings.slotFrameSprite != null)
			return settings.slotFrameSprite;

		Sprite resourceFrame = LoadResourceFrameSprite();
		if (resourceFrame != null)
			return resourceFrame;

		return GetFallbackHitSprite();
	}

	public static Sprite LoadResourceFrameSprite()
	{
		if (cachedResourceFrame != null)
			return cachedResourceFrame;

		Sprite[] sprites = Resources.LoadAll<Sprite>(ResourceFrameTexturePath);
		for (int i = 0; i < sprites.Length; i++)
		{
			Sprite sprite = sprites[i];
			if (sprite != null && sprite.name == ResourceFrameSpriteName)
			{
				cachedResourceFrame = sprite;
				return cachedResourceFrame;
			}
		}

		for (int i = 0; i < sprites.Length; i++)
		{
			if (sprites[i] != null)
			{
				cachedResourceFrame = sprites[i];
				return cachedResourceFrame;
			}
		}

		return null;
	}

	/// <summary>프레임을 못 찾았을 때만 쓰는 투명 히트 영역.</summary>
	public static Sprite GetFallbackHitSprite()
	{
		if (fallbackHitSprite != null)
			return fallbackHitSprite;

		var texture = Texture2D.whiteTexture;
		fallbackHitSprite = Sprite.Create(
			texture,
			new Rect(0f, 0f, texture.width, texture.height),
			new Vector2(0.5f, 0.5f),
			100f);
		fallbackHitSprite.name = "InventorySlotHitFallback";
		return fallbackHitSprite;
	}

	public static bool IsFallbackHitSprite(Sprite sprite)
	{
		return sprite != null && sprite.name == "InventorySlotHitFallback";
	}
}
