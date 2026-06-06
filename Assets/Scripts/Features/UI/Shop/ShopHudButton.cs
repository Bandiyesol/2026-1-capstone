using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD 상점 버튼 → ShopPanel 의 ShopUI 를 Toggle 합니다.</summary>
[RequireComponent(typeof(Button))]
public class ShopHudButton : MonoBehaviour
{
	void OnEnable()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	void OnDisable()
	{
		GetComponent<Button>().onClick.RemoveListener(OnClick);
	}

	void OnClick()
	{
		ShopUI ui = ShopUIBootstrap.EnsureShopUI();
		if (ui == null)
		{
			Debug.LogError("[ShopHudButton] ShopUI를 준비하지 못했습니다. ShopPanel + ShopUI를 확인하세요.");
			return;
		}

		ui.Toggle();
	}
}
