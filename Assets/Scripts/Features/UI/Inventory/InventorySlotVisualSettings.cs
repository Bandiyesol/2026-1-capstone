using UnityEngine;

[CreateAssetMenu(
	fileName = "InventorySlotVisualSettings",
	menuName = "Scriptable/UI/Inventory Slot Visual")]
public class InventorySlotVisualSettings : ScriptableObject
{
	public Sprite slotFrameSprite;
	public float iconPadding = 12f;

	static InventorySlotVisualSettings cached;

	public static InventorySlotVisualSettings Instance
	{
		get
		{
			if (cached == null)
				cached = Resources.Load<InventorySlotVisualSettings>("UI/InventorySlotVisualSettings");

			return cached;
		}
	}
}
