using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD의 Status 아이콘 버튼에 붙입니다. 비활성 StatusPanel의 StatusUI도 찾아 Toggle 합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class StatusHudButton : MonoBehaviour
{
	void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	void OnClick()
	{
		StatusUI ui = FindFirstObjectByType<StatusUI>(FindObjectsInactive.Include);
		if (ui == null)
		{
			Debug.LogError("[StatusHudButton] StatusUI를 찾을 수 없습니다. StatusPanel에 StatusUI 컴포넌트를 추가하세요.");
			return;
		}

		ui.Toggle();
	}
}
