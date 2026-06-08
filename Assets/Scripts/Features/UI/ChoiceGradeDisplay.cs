/// <summary>WeaponSelectUI / RewardSelectUI 등급 텍스트 색상 (TMP rich text).</summary>
public static class ChoiceGradeDisplay
{
	public static string ColorHex(string grade) => grade switch
	{
		"Common"    or "일반"   => "FFFFFF",
		"Rare"      or "희귀"   => "4D99FF",
		"Unique"    or "유니크" => "B24DFF",
		"Legendary" or "전설"   => "FF9900",
		_                       => "FFFFFF"
	};

	public static string FormatColored(AccessoryGrade grade) => FormatColored(grade.ToString());

	public static string FormatColored(string gradeText, string hexOverride = null)
	{
		if (string.IsNullOrEmpty(gradeText))
			return string.Empty;

		string hex = hexOverride ?? ColorHex(gradeText);
		return $"<color=#{hex}>{gradeText}</color>";
	}

	public static string FormatGradeLine(string grade, string suffix = null)
	{
		string colored = FormatColored(grade);
		return string.IsNullOrEmpty(suffix) ? colored : $"{colored} · {suffix}";
	}

	/// <summary>등급 색으로 아이템 표시 이름(스택 포함)을 감쌉니다.</summary>
	public static string FormatColoredItemLabel(string label, string grade)
	{
		if (string.IsNullOrEmpty(label))
			return label;

		if (string.IsNullOrEmpty(grade))
			return label;

		string hex = ColorHex(grade);
		return $"<color=#{hex}>{label}</color>";
	}
}
