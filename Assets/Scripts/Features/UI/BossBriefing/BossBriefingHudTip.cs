using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BossBriefTrigger 호버 시 텍스트 툴팁 표시. 트리거 아이콘은 에디터에서 직접 지정.
/// </summary>
public class BossBriefingHudTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Header("# 툴팁 (Inspector 연결)")]
	[SerializeField] GameObject tooltipRoot;
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] TextMeshProUGUI traitsText;
	[SerializeField] TextMeshProUGUI patternsText;

	[Header("# 호버 위치만 코드 처리")]
	[Tooltip("비우면 Canvas")]
	[SerializeField] RectTransform tooltipParent;
	[SerializeField] Vector2 offsetFromTrigger = new Vector2(12f, -8f);

	[Header("# 텍스트 (비우면 폰트만 자동 적용 안 함)")]
	[SerializeField] TMP_FontAsset koreanFont;

	[Tooltip("브리핑 없을 때 트리거 숨김")]
	[SerializeField] bool hideTriggerWhenNoBrief = true;

	RectTransform tooltipRect;
	RectTransform triggerRect;

	void Awake()
	{
		triggerRect = transform as RectTransform;
		EnsureTooltipRefs();
		AttachTooltipToCanvas();

		if (tooltipRoot != null)
		{
			tooltipRoot.SetActive(false);
			Image bg = tooltipRoot.GetComponent<Image>();
			if (bg != null)
				bg.raycastTarget = false;
		}

		RefreshVisibility();
	}

	void OnEnable()
	{
		RefreshVisibility();
	}

	public void RefreshVisibility()
	{
		bool show = BossBriefingRuntime.HasBrief;

		if (hideTriggerWhenNoBrief)
			gameObject.SetActive(show);

		if (tooltipRoot != null && !show)
			tooltipRoot.SetActive(false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!BossBriefingRuntime.HasBrief)
			return;

		FillTooltip();

		if (tooltipRoot == null)
			return;

		tooltipRoot.SetActive(true);
		tooltipRoot.transform.SetAsLastSibling();
		PositionTooltipNearTrigger();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (tooltipRoot != null)
			tooltipRoot.SetActive(false);
	}

	void AttachTooltipToCanvas()
	{
		if (tooltipRoot == null)
			return;

		RectTransform parent = tooltipParent;
		if (parent == null)
		{
			Canvas canvas = GetComponentInParent<Canvas>();
			if (canvas != null)
				parent = canvas.GetComponent<RectTransform>();
		}

		if (parent != null && tooltipRoot.transform.parent != parent)
			tooltipRoot.transform.SetParent(parent, false);

		tooltipRect = tooltipRoot.GetComponent<RectTransform>();
	}

	void PositionTooltipNearTrigger()
	{
		if (tooltipRect == null || triggerRect == null)
			return;

		tooltipRect.pivot = new Vector2(0f, 1f);
		tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
		tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);

		var corners = new Vector3[4];
		triggerRect.GetWorldCorners(corners);

		Vector3 bottomRight = corners[3];
		Vector3 right = triggerRect.TransformDirection(Vector3.right);
		Vector3 down = triggerRect.TransformDirection(Vector3.down);

		tooltipRect.position = bottomRight
			+ right * offsetFromTrigger.x
			+ down * Mathf.Abs(offsetFromTrigger.y);
	}

	void FillTooltip()
	{
		if (titleText != null)
		{
			titleText.text = BossBriefingRuntime.DisplayName;
			if (koreanFont != null)
			{
				TmpKoreanFontUtility.ApplyFont(titleText, koreanFont);
				TmpKoreanFontUtility.EnsureGlyphs(titleText, koreanFont, titleText.text);
			}
		}

		if (traitsText != null)
		{
			traitsText.text = BossBriefingRuntime.TraitsHudShort;
			if (koreanFont != null)
			{
				TmpKoreanFontUtility.ApplyFont(traitsText, koreanFont);
				TmpKoreanFontUtility.EnsureGlyphs(traitsText, koreanFont, traitsText.text);
			}
		}

		if (patternsText != null)
		{
			patternsText.text = BossBriefingRuntime.PatternsHudShort;
			if (koreanFont != null)
			{
				TmpKoreanFontUtility.ApplyFont(patternsText, koreanFont);
				TmpKoreanFontUtility.EnsureGlyphs(patternsText, koreanFont, patternsText.text);
			}
		}
	}

	void EnsureTooltipRefs()
	{
		if (tooltipRoot == null)
		{
			Transform t = transform.Find("BossBriefTooltip");
			if (t != null)
				tooltipRoot = t.gameObject;
		}

		if (tooltipRoot == null)
			return;

		if (titleText == null)
		{
			Transform tt = FindDeep(tooltipRoot.transform, "TooltipTitle");
			if (tt != null)
				titleText = tt.GetComponent<TextMeshProUGUI>();
		}

		if (traitsText == null)
		{
			Transform tt = FindDeep(tooltipRoot.transform, "TooltipTraits");
			if (tt != null)
				traitsText = tt.GetComponent<TextMeshProUGUI>();
		}

		if (patternsText == null)
		{
			Transform tt = FindDeep(tooltipRoot.transform, "TooltipPatterns");
			if (tt != null)
				patternsText = tt.GetComponent<TextMeshProUGUI>();
		}
	}

	static Transform FindDeep(Transform root, string childName)
	{
		if (root == null)
			return null;

		if (root.name == childName)
			return root;

		for (int i = 0; i < root.childCount; i++)
		{
			Transform found = FindDeep(root.GetChild(i), childName);
			if (found != null)
				return found;
		}

		return null;
	}
}
