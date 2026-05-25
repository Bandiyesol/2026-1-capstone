using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD의 Setting 버튼 — SettingsPanel의 SettingsUI를 Toggle 합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class SettingsHudButton : MonoBehaviour
{
	void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	void OnClick()
	{
		SettingsUI ui = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		if (ui == null)
		{
			Debug.LogError("[SettingsHudButton] SettingsUI를 찾을 수 없습니다. SettingsPanel에 SettingsUI를 추가하세요.");
			return;
		}

		ui.Toggle();
	}
}
