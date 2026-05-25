using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 인벤토리 버튼 → 비활성 InventoryPanel 의 InventoryUI 를 Toggle 합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryHudButton : MonoBehaviour
{
	void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	void OnClick()
	{
		InventoryUI ui = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
		if (ui == null)
		{
			Debug.LogError("[InventoryHudButton] InventoryUI를 찾을 수 없습니다. InventoryPanel에 InventoryUI를 추가하세요.");
			return;
		}

		ui.Toggle();
	}
}
