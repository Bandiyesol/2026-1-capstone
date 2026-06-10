using UnityEngine;

/// <summary>룬 선택 UI — 카테고리 라벨·색상·아이콘 표시.</summary>
public static class RuneCategoryDisplay
{
	public static string GetLabel(RuneCategory category) => category switch
	{
		RuneCategory.Active => "Active",
		RuneCategory.Trigger => "Trigger",
		RuneCategory.State => "State",
		RuneCategory.Logic => "Logic",
		RuneCategory.Final => "Final",
		_ => category.ToString(),
	};

	public static Color GetTint(RuneCategory category) => category switch
	{
		RuneCategory.Active => new Color(0.35f, 0.75f, 1f),
		RuneCategory.Trigger => new Color(1f, 0.55f, 0.35f),
		RuneCategory.State => new Color(0.55f, 0.95f, 0.45f),
		RuneCategory.Logic => new Color(0.85f, 0.55f, 1f),
		RuneCategory.Final => new Color(1f, 0.85f, 0.25f),
		_ => Color.white,
	};

	public static Sprite GetIcon(RuneData rune)
	{
		if (rune == null)
			return null;

		if (rune.runeIcon != null)
			return rune.runeIcon;

		return Resources.Load<Sprite>($"Sprites/RuneCategories/{rune.category}");
	}

	public static string FormatColoredCategory(RuneData rune)
	{
		if (rune == null)
			return string.Empty;

		Color tint = GetTint(rune.category);
		string hex = ColorUtility.ToHtmlStringRGB(tint);
		return $"<color=#{hex}>{GetLabel(rune.category)}</color>";
	}

	public static void ApplyChoiceIcon(UnityEngine.UI.Image image, RuneData rune)
	{
		if (image == null || rune == null)
			return;

		Sprite sprite = GetIcon(rune);
		image.sprite = sprite;
		image.enabled = true;
		image.color = sprite != null ? Color.white : GetTint(rune.category);
	}
}
