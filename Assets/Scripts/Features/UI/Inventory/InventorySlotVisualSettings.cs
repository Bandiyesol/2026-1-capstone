using UnityEngine;

[CreateAssetMenu(
	fileName = "InventorySlotVisualSettings",
	menuName = "Scriptable/UI/Inventory Slot Visual")]
public class InventorySlotVisualSettings : ScriptableObject
{
	public Sprite slotFrameSprite;
	public float iconPadding = 12f;

	static InventorySlotVisualSettings cached;
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

	/// <summary>빌드에서도 슬롯 클릭/호버가 되도록 프레임 스프라이트를 확보합니다.</summary>
	public static Sprite ResolveSlotFrameSprite(Sprite preferred)
	{
		if (preferred != null)
			return preferred;

		InventorySlotVisualSettings settings = Instance;
		if (settings != null && settings.slotFrameSprite != null)
			return settings.slotFrameSprite;

		return GetFallbackHitSprite();
	}

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
}
