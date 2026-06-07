using UnityEngine;

/// <summary>설정 BoxPanel — 화면모드·해상도·볼륨 행 위치.</summary>
public static class SettingsPanelLayout
{
	const float LabelX = -250f;
	const float ControlX = 60f;
	const float LabelSizeX = 200f;
	const float LabelSizeY = 50f;
	const float ControlSizeX = 400f;
	const float ControlSizeY = 50f;

	static readonly (string row, string label, string control, float y)[] Rows =
	{
		("ScreenModeRow", "ScreenModeLabel", "ScreenModeDropdown", 170f),
		("ResolutionRow", "ResolutionLabel", "ResolutionDropdown", 70f),
		("BgmRow", "BgmLabel", "BgmSlider", -30f),
		("SfxRow", "SfxLabel", "SfxSlider", -130f),
	};

	public static void Apply(Transform settingsRoot)
	{
		if (settingsRoot == null)
			return;

		Transform box = OverlayPanelUILayout.FindBoxPanel(settingsRoot) ?? settingsRoot;

		foreach ((string row, string label, string control, float y) entry in Rows)
		{
			Transform rowTransform = FindChild(box, entry.row);
			if (rowTransform == null)
				continue;

			ApplyCenteredChild(rowTransform, entry.label, LabelX, entry.y, new Vector2(LabelSizeX, LabelSizeY));
			ApplyCenteredChild(rowTransform, entry.control, ControlX, entry.y, new Vector2(ControlSizeX, ControlSizeY));
		}
	}

	static void ApplyCenteredChild(Transform row, string childName, float x, float y, Vector2 size)
	{
		if (FindChild(row, childName) is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(x, y);
		rect.sizeDelta = size;
	}

	static Transform FindChild(Transform root, string name)
	{
		if (root == null || string.IsNullOrEmpty(name))
			return null;

		Transform direct = root.Find(name);
		if (direct != null)
			return direct;

		foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
		{
			if (t.name == name)
				return t;
		}

		return null;
	}
}
