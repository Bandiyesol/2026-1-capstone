using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TMP_Dropdown 목록 클릭·표시 보정: 정렬 순서, Blocker, 항목 raycast.
/// RenderMode 는 TMP 기본값을 유지해 위치가 깨지지 않게 합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public class TmpDropdownOpenedListFix : MonoBehaviour
{
	public const int ListSortOrder = 4000;

	TMP_Dropdown dropdown;
	Canvas rootCanvas;

	void Awake()
	{
		dropdown = GetComponent<TMP_Dropdown>();
		rootCanvas = dropdown != null ? dropdown.GetComponentInParent<Canvas>() : null;
	}

	void LateUpdate()
	{
		if (dropdown == null || rootCanvas == null)
			return;

		if (dropdown.template != null && dropdown.template.gameObject.activeSelf)
			dropdown.template.gameObject.SetActive(false);

		Transform parent = dropdown.template != null ? dropdown.template.parent : rootCanvas.transform;
		if (parent == null)
			return;

		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child == null || child.name != "Dropdown List")
				continue;

			FixOpenedList(child);
			SyncBlockerOrder(rootCanvas.transform);
		}
	}

	void FixOpenedList(Transform listRoot)
	{
		Canvas canvas = listRoot.GetComponent<Canvas>();
		if (canvas == null)
			canvas = listRoot.gameObject.AddComponent<Canvas>();

		canvas.overrideSorting = true;
		canvas.sortingOrder = ListSortOrder;

		if (listRoot.GetComponent<GraphicRaycaster>() == null)
			listRoot.gameObject.AddComponent<GraphicRaycaster>();

		if (listRoot.TryGetComponent(out CanvasGroup group))
		{
			group.interactable = true;
			group.blocksRaycasts = true;
		}

		Graphic[] graphics = listRoot.GetComponentsInChildren<Graphic>(true);
		for (int i = 0; i < graphics.Length; i++)
		{
			if (graphics[i] != null)
				graphics[i].raycastTarget = true;
		}

		Toggle[] toggles = listRoot.GetComponentsInChildren<Toggle>(true);
		for (int i = 0; i < toggles.Length; i++)
		{
			Toggle toggle = toggles[i];
			if (toggle == null)
				continue;

			toggle.interactable = true;
			if (toggle.targetGraphic != null)
				toggle.targetGraphic.raycastTarget = true;
		}
	}

	static void SyncBlockerOrder(Transform canvasRoot)
	{
		for (int i = 0; i < canvasRoot.childCount; i++)
		{
			Transform child = canvasRoot.GetChild(i);
			if (child == null || child.name != "Blocker")
				continue;

			if (!child.TryGetComponent(out Canvas blockerCanvas))
				return;

			blockerCanvas.overrideSorting = true;
			blockerCanvas.sortingOrder = ListSortOrder - 1;
			return;
		}
	}
}
