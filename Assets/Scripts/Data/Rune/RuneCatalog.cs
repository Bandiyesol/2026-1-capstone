using UnityEngine;

/// <summary>스테이지 시작 룬 선택 UI용 전체 룬 목록.</summary>
[CreateAssetMenu(fileName = "RuneCatalog", menuName = "Rune/Catalog")]
public class RuneCatalog : ScriptableObject
{
	public RuneData[] runes;
}
