using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD 룬 정보 버튼 — RuneLoadoutViewUI를 Toggle 합니다.</summary>
[RequireComponent(typeof(Button))]
public class RuneLoadoutHudButton : MonoBehaviour
{
	[SerializeField] Sprite hudIcon;

	void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
		ApplyHudIcon();
	}

	public void ConfigureIcon(Sprite icon)
	{
		hudIcon = icon;
		ApplyHudIcon();
	}

	void ApplyHudIcon()
	{
		if (hudIcon == null)
			return;

		Transform iconTransform = transform.Find("Icon") ?? transform.Find("Image");
		if (iconTransform != null && iconTransform.TryGetComponent(out Image image))
		{
			image.sprite = hudIcon;
			image.preserveAspect = true;
			image.raycastTarget = false;
		}
	}

	void OnClick()
	{
		RuneLoadoutViewUI ui = FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (ui == null)
		{
			Debug.LogError("[RuneLoadoutHudButton] RuneLoadoutViewUI를 찾을 수 없습니다. Tools/UI/Setup Rune Loadout HUD를 실행하세요.");
			return;
		}

		ui.Toggle();
	}
}
