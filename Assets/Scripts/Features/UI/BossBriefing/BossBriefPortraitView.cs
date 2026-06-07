using UnityEngine;
using UnityEngine.UI;

/// <summary>BossAlarmPortrait 에 스프라이트만 넣습니다. 오브젝트 활성은 건드리지 않습니다.</summary>
public static class BossBriefPortraitView
{
	public static void Apply(Image portraitImage)
	{
		if (portraitImage == null)
			return;

		Sprite sprite = BossBriefingRuntime.GetPortrait();
		portraitImage.sprite = sprite;
		portraitImage.preserveAspect = true;
		portraitImage.enabled = sprite != null;

		// Hierarchy 에서 켜 둔 Portrait 는 코드가 끄지 않음 (스프라이트 없을 때만 Image 비활성).
	}
}
