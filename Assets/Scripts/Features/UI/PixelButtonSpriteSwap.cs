using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pixel Buttons 폴더 스프라이트용 — Idle / Pushed 를 Button Sprite Swap 으로 연결합니다.
/// CloseBtn 등에 붙이거나 StatusUI가 자동으로 추가합니다.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class PixelButtonSpriteSwap : MonoBehaviour
{
	const string CrossIdlePath = "Assets/Arts/UI/Pixel Buttons/Cross_Idle.png";
	const string CrossPushedPath = "Assets/Arts/UI/Pixel Buttons/Cross_Pushed.png";

	[SerializeField] Sprite idleSprite;
	[SerializeField] Sprite pressedSprite;
	[SerializeField] bool useIdleAsHighlighted = true;

	Button button;
	Image image;

	void Awake()
	{
		Apply();
	}

	public void Apply()
	{
		if (button == null)
			button = GetComponent<Button>();
		if (image == null)
			image = GetComponent<Image>();

		TryLoadDefaultCrossSpritesIfEmpty();

		if (idleSprite != null)
			image.sprite = idleSprite;

		if (pressedSprite == null)
			return;

		button.transition = Selectable.Transition.SpriteSwap;

		var state = button.spriteState;
		state.pressedSprite = pressedSprite;
		if (useIdleAsHighlighted && idleSprite != null)
		{
			state.highlightedSprite = idleSprite;
			state.selectedSprite = idleSprite;
		}

		button.spriteState = state;
	}

	void TryLoadDefaultCrossSpritesIfEmpty()
	{
		if (idleSprite != null && pressedSprite != null)
			return;

#if UNITY_EDITOR
		if (idleSprite == null)
			idleSprite = LoadSpriteFromTexture(CrossIdlePath, "Cross_Idle_0");
		if (pressedSprite == null)
			pressedSprite = LoadSpriteFromTexture(CrossPushedPath, "Cross_Pushed_0");
#endif
	}

#if UNITY_EDITOR
	static Sprite LoadSpriteFromTexture(string assetPath, string spriteName)
	{
		Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
		if (assets == null)
			return null;

		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite && sprite.name == spriteName)
				return sprite;
		}

		return null;
	}

	void OnValidate()
	{
		if (!Application.isPlaying)
			Apply();
	}
#endif
}
