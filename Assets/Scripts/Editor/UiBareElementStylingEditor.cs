#if UNITY_EDITOR

using System.Collections.Generic;

using System.IO;

using System.Text;

using TMPro;

using UnityEditor;

using UnityEditor.SceneManagement;

using UnityEngine;

using UnityEngine.SceneManagement;

using UnityEngine.UI;



/// <summary>

/// 기본 Unity UI 스프라이트/빈 Image를 Vol 6·Pixel Buttons 아트로 교체합니다.

/// Tools/UI/Style Bare UI Elements

/// </summary>

public static class UiBareElementStylingEditor

{

	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	const string Vol6Root = "Assets/Arts/UI/Vol 6 Ui Expansion Pack";

	const string PixelBtnRoot = "Assets/Arts/UI/Pixel Buttons";



	static readonly string[] ImportTexturePaths =

	{

		$"{Vol6Root}/Buttons/Gold Buttons/Gold Btn A_01.png",

		$"{Vol6Root}/Buttons/Gold Buttons/Gold Btn A_02.png",

		$"{Vol6Root}/Buttons/Gold Buttons/Gold Btn C_02.png",

		$"{Vol6Root}/Buttons/Silver Buttons/Silver Btn A_01.png",

		$"{Vol6Root}/Buttons/Silver Buttons/Silver Btn A_02.png",

		$"{Vol6Root}/Panels/Panels_03.png",

		$"{Vol6Root}/Panels/Panels_06.png",

		$"{Vol6Root}/Slide bars/Horizontal Slidebar_01.png",

		$"{Vol6Root}/Slide bars/Handle 1.png",

		$"{Vol6Root}/Resources Bars/Resources Bars/Resource bar 03.png",

		$"{Vol6Root}/Resources Bars/Bars and clusters/Red Bar.png",

		$"{Vol6Root}/Resources Bars/Bars and clusters/Green Bar.png",

		$"{Vol6Root}/Medallions/Medallions Bases/Medallions-base_01.png",

		$"{Vol6Root}/Banners/Short Banners.png",

		$"{PixelBtnRoot}/Cross_Idle.png",

		$"{PixelBtnRoot}/Cross_Pushed.png",

		"Assets/Arts/UI/BackPack/backpack.png",

		"Assets/Arts/UI/Setting/Setting1.png",

	};



	static readonly string[] MenuStyleRootNames =

	{

		"AuthScreen",

		"LoginPanel",

		"SignUpPanel",

		"ForgotPasswordPanel",

		"SettingPanel",

		"MainStoryPanel",

		"EndingStoryPanel",

		"BossAlarmPanel",

		"LoadoutPanel",

	};



	static readonly HashSet<string> ProtectedPanelBackgroundNames = new()

	{

		"MainMenuLeaderboard",

		"BossAlarmPanel",

	};



	static readonly List<string> Report = new();



	static Sprite GoldIdle;

	static Sprite GoldPressed;

	static Sprite GoldC02;

	static Sprite SilverIdle;

	static Sprite SilverPressed;

	static Sprite PanelInput;
	static Sprite PanelInputField;

	static Sprite SliderTrack;

	static Sprite SliderHandle;

	static Sprite HealthTrack;

	static Sprite HealthFill;

	static Sprite SettingsFill;

	static Sprite PortraitFrame;

	static Sprite ShortBanner;

	static Sprite CrossIdle;

	static Sprite CrossPushed;

	static Sprite BackpackIcon;

	static Sprite SettingIcon;
	static Sprite RuneHudIcon;

	static readonly Color MenuInputBackgroundColor = new(0.98f, 0.96f, 0.93f, 1f);
	static readonly Color AuthInputBackgroundColor = new(1f, 0.98f, 0.92f, 1f);


	[MenuItem("Window/The Last Rune/UI/Style Bare UI Elements")]

	public static void ApplyFromMenu()

	{

		ApplyInternal(showDialog: true);

	}



	public static void ApplyFromBatch()

	{

		ApplyInternal(showDialog: false);

	}



	static void ApplyInternal(bool showDialog)

	{

		Report.Clear();

		EnsureImportSettings();

		LoadSprites();



		if (!OpenTargetScene())

			return;



		RevertProtectedPanelBackgrounds();

		UiRegressionFixEditor.RevertRankingRowButtons();

		StyleMenuLikeButtonsAndInputs();

		StyleMenuStylePanelWidgets();

		StyleAllImages();

		StyleInputFields();

		StyleCloseButtons();

		StyleHudButtons();



		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

		EditorSceneManager.SaveOpenScenes();

		AssetDatabase.SaveAssets();



		string reportText = string.Join("\n", Report);

		Debug.Log($"[UiBareElementStylingEditor]\n{reportText}");



		if (showDialog)

		{

			EditorUtility.DisplayDialog(

				"UI 스타일 적용",

				$"{Report.Count}개 항목을 Vol 6 / Pixel Buttons 아트로 교체했습니다.\n\n자세한 목록은 Console 로그를 확인하세요.",

				"확인");

		}

	}



	static bool OpenTargetScene()

	{

		if (SceneManager.GetActiveScene().path == ScenePath)

			return true;



		if (!File.Exists(Path.Combine(Application.dataPath, "..", ScenePath)))

		{

			Debug.LogError($"[UiBareElementStylingEditor] 씬 없음: {ScenePath}");

			return false;

		}



		EditorSceneManager.OpenScene(ScenePath);

		return true;

	}



	static void EnsureImportSettings()

	{

		foreach (string path in ImportTexturePaths)

		{

			if (!File.Exists(path))

				continue;



			var importer = AssetImporter.GetAtPath(path) as TextureImporter;

			if (importer == null)

				continue;



			bool dirty = false;



			if (importer.textureType != TextureImporterType.Sprite)

			{

				importer.textureType = TextureImporterType.Sprite;

				dirty = true;

			}



			if (importer.spritePixelsPerUnit != 32f)

			{

				importer.spritePixelsPerUnit = 32f;

				dirty = true;

			}



			if (importer.filterMode != FilterMode.Point)

			{

				importer.filterMode = FilterMode.Point;

				dirty = true;

			}



			TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();

			if (platform.textureCompression != TextureImporterCompression.Uncompressed)

			{

				platform.textureCompression = TextureImporterCompression.Uncompressed;

				importer.SetPlatformTextureSettings(platform);

				dirty = true;

			}



			if (dirty)

			{

				importer.SaveAndReimport();

				Report.Add($"[Import] {path} → PPU 32, Point, Compression None");

			}

		}

	}



	static void LoadSprites()

	{

		GoldIdle = LoadSprite($"{Vol6Root}/Buttons/Gold Buttons/Gold Btn A_01.png", "Gold Btn A_01_0");

		GoldPressed = LoadSprite($"{Vol6Root}/Buttons/Gold Buttons/Gold Btn A_02.png", "Gold Btn A_02_0");

		GoldC02 = LoadSprite($"{Vol6Root}/Buttons/Gold Buttons/Gold Btn C_02.png", "Gold Btn C_02_0");

		SilverIdle = LoadSprite($"{Vol6Root}/Buttons/Silver Buttons/Silver Btn A_01.png", "Silver Btn A_01_0");

		SilverPressed = LoadSprite($"{Vol6Root}/Buttons/Silver Buttons/Silver Btn A_02.png", "Silver Btn A_02_0");

		PanelInput = LoadSprite($"{Vol6Root}/Panels/Panels_06.png", "Panels_06_0");

		PanelInputField = LoadSprite($"{Vol6Root}/Buttons/Gold Buttons/Gold Btn C_02.png", "Gold Btn C_02_center")
		               ?? LoadSprite($"{Vol6Root}/Panels/Panels_03.png", "Panels_03_0");

		SliderTrack = LoadSprite($"{Vol6Root}/Slide bars/Horizontal Slidebar_01.png", "Horizontal Slidebar_01_0");

		SliderHandle = LoadSprite($"{Vol6Root}/Slide bars/Handle 1.png", "Handle 1_0");

		HealthTrack = LoadSprite($"{Vol6Root}/Charge Bars/Charge Bars A_05.png", "Charge Bars A_05_0");

		HealthFill = LoadSprite($"{Vol6Root}/Resources Bars/Bars and clusters/Red Bar.png", "Red Bar_0");

		SettingsFill = LoadSprite($"{Vol6Root}/Resources Bars/Bars and clusters/Green Bar.png", "Green Bar_0");

		PortraitFrame = LoadSprite($"{Vol6Root}/Medallions/Medallions Bases/Medallions-base_01.png", "Medallions-base_01_0");

		ShortBanner = LoadSprite($"{Vol6Root}/Banners/Short Banners.png", "Short Banners_0");

		CrossIdle = LoadSprite($"{PixelBtnRoot}/Cross_Idle.png", "Cross_Idle_0");

		CrossPushed = LoadSprite($"{PixelBtnRoot}/Cross_Pushed.png", "Cross_Pushed_0");

		BackpackIcon = LoadFirstSprite("Assets/Arts/UI/BackPack/backpack.png");

		SettingIcon = LoadFirstSprite("Assets/Arts/UI/Setting/Setting1.png");
		RuneHudIcon = LoadFirstSprite("Assets/Arts/UI/Vol 6 Ui Expansion Pack/Runes/Runes_13_01.png");

	}



	static void RevertProtectedPanelBackgrounds()

	{

		Sprite builtinBackground = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

		if (builtinBackground == null)

			return;



		foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!ProtectedPanelBackgroundNames.Contains(transform.name))

				continue;



			if (!transform.TryGetComponent(out Image image))

				continue;



			if (transform.GetComponent<Button>() != null)

				continue;



			Color color = transform.name == "MainMenuLeaderboard"

				? new Color(0f, 0f, 0f, 0.82f)

				: new Color(0f, 0f, 0f, 0.5882353f);



			image.sprite = builtinBackground;

			image.type = Image.Type.Sliced;

			image.color = color;

			EditorUtility.SetDirty(image);

			Report.Add($"{GetPath(transform)} → Unity Background.psd (복원)");

		}

	}



	static void StyleMenuStylePanelWidgets()

	{

		HashSet<Transform> roots = CollectMenuStyleRoots();

		if (roots.Count == 0)

			return;



		foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!IsUnderAnyRoot(image.transform, roots))

				continue;



			if (image.GetComponent<Button>() != null)

				continue;



			TMP_InputField parentInput = image.GetComponentInParent<TMP_InputField>();

			if (parentInput != null && parentInput.gameObject == image.gameObject)

				continue;



			string path = GetPath(image.transform);

			string name = image.gameObject.name;



			if (TryStyleSliderPart(image, name, path))

				continue;



			TryStyleDropdownPart(image, name, path);

		}

	}



	static void StyleMenuLikeButtonsAndInputs()

	{

		HashSet<Transform> roots = CollectMenuStyleRoots();

		if (roots.Count == 0)

			return;



		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!IsUnderAnyRoot(button.transform, roots))

				continue;



			if (button.name.Contains("Close"))

				continue;



			Image image = button.GetComponent<Image>();

			if (image == null)

				continue;



			ApplySprite(image, GoldC02, Image.Type.Simple, Color.white, GetPath(button.transform), "Gold Btn C_02.png (메인 메뉴 스타일)");

		}



		foreach (TMP_InputField input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!IsUnderAnyRoot(input.transform, roots))

				continue;



			if (!IsUnderAuthPanel(input.transform))

				continue;



			Image bg = input.GetComponent<Image>();

			if (bg != null && PanelInputField != null)

				ApplySprite(bg, PanelInputField, Image.Type.Sliced, AuthInputBackgroundColor, $"{GetPath(input.transform)} (입력창 배경)", "Panels_03.png");



			if (input.targetGraphic is Image target && PanelInputField != null)

				ApplySprite(target, PanelInputField, Image.Type.Sliced, AuthInputBackgroundColor, $"{GetPath(input.transform)} (입력창 Target)", "Panels_03.png");

		}

	}



	static HashSet<Transform> CollectMenuStyleRoots()

	{

		var roots = new HashSet<Transform>();

		foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			foreach (string rootName in MenuStyleRootNames)

			{

				if (transform.name == rootName)

					roots.Add(transform);

			}

		}



		return roots;

	}



	static bool IsUnderAnyRoot(Transform transform, HashSet<Transform> roots)

	{

		while (transform != null)

		{

			if (roots.Contains(transform))

				return true;

			transform = transform.parent;

		}



		return false;

	}



	static bool IsUnderAuthPanel(Transform transform)

	{

		while (transform != null)

		{

			if (transform.name is "LoginPanel" or "SignUpPanel" or "ForgotPasswordPanel" or "DeleteAccountPanel")

				return true;

			transform = transform.parent;

		}



		return false;

	}



	static void StyleAllImages()

	{

		Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		foreach (Image image in images)

		{

			if (!IsUnderCanvas(image.transform))

				continue;



			if (ShouldSkipImage(image))

				continue;



			if (IsUnderRankingHierarchy(image.transform))

				continue;



			if (!IsBareImage(image))

				continue;



			if (IsUnderMenuStyleRoot(image.transform))

				continue;



			string path = GetPath(image.transform);

			string name = image.gameObject.name;



			if (TryStylePortrait(image, name, path))

				continue;



			if (TryStyleSliderPart(image, name, path))

				continue;



			if (TryStyleDropdownPart(image, name, path))

				continue;



			if (TryStyleInputBackground(image, name, path))

				continue;

		}

	}



	static bool IsUnderMenuStyleRoot(Transform transform)

	{

		while (transform != null)

		{

			foreach (string rootName in MenuStyleRootNames)

			{

				if (transform.name == rootName)

					return true;

			}



			transform = transform.parent;

		}



		return false;

	}



	static bool IsUnderRankingHierarchy(Transform transform)

	{

		while (transform != null)

		{

			if (transform.name == "MainMenuLeaderboard" || transform.name.StartsWith("RankRow"))

				return true;



			transform = transform.parent;

		}



		return false;

	}



	static void StyleInputFields()

	{

		foreach (TMP_InputField input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!IsUnderCanvas(input.transform))

				continue;



			if (IsUnderMenuStyleRoot(input.transform))

				continue;



			Image bg = input.GetComponent<Image>();

			if (bg != null && IsBareImage(bg))

				ApplySprite(bg, PanelInput, Image.Type.Sliced, Color.white, $"{GetPath(input.transform)} (입력창 배경)", "Panels_03.png");



			if (input.targetGraphic is Image target && IsBareImage(target))

				ApplySprite(target, PanelInput, Image.Type.Sliced, Color.white, $"{GetPath(input.transform)} (입력창 Target)", "Panels_03.png");

		}

	}



	static void StyleCloseButtons()

	{

		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (!button.name.Contains("Close"))

				continue;

			if (!IsUnderCanvas(button.transform))

				continue;



			Image image = button.GetComponent<Image>();

			if (image == null)

				continue;



			ApplySprite(image, CrossIdle, Image.Type.Simple, Color.white, $"{GetPath(button.transform)}", "Cross_Idle.png");

			ApplyButtonSpriteSwap(button, CrossIdle, CrossPushed);



			if (button.GetComponent<PixelButtonSpriteSwap>() == null)

				button.gameObject.AddComponent<PixelButtonSpriteSwap>();

		}

	}



	static void StyleHudButtons()

	{

		TryStyleHudIconButton("Inventory", BackpackIcon, "backpack.png", ShortBanner);

		TryStyleHudIconButton("RuneLoadout", RuneHudIcon, "Runes_13_01.png", ShortBanner);

		TryStyleHudIconButton("Setting", SettingIcon, "Setting1.png", ShortBanner);

		TryStyleHudIconButton("ShopHudButton", null, null, GoldIdle, useGoldButton: true);

	}



	static void TryStyleHudIconButton(string objectName, Sprite iconSprite, string iconFile, Sprite fallbackBanner = null, bool useGoldButton = false)

	{

		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))

		{

			if (button.name != objectName)

				continue;

			if (!IsUnderCanvas(button.transform))

				continue;



			Image image = button.GetComponent<Image>();

			if (image == null)

				continue;



			if (useGoldButton)

			{

				if (IsBareImage(image))

				{

					ApplySprite(image, GoldIdle, Image.Type.Simple, Color.white, GetPath(button.transform), "Gold Btn A_01.png");

					ApplyButtonSpriteSwap(button, GoldIdle, GoldPressed);

				}

				continue;

			}



			if (IsBareImage(image) && fallbackBanner != null)

				ApplySprite(image, fallbackBanner, Image.Type.Simple, Color.white, GetPath(button.transform), "Short Banners.png");



			if (iconSprite == null)

				continue;



			Transform icon = button.transform.Find("Icon") ?? button.transform.Find("Image");

			if (icon != null && icon.TryGetComponent(out Image iconImage))

			{

				ApplySprite(iconImage, iconSprite, Image.Type.Simple, Color.white, $"{GetPath(button.transform)}/Icon", iconFile);

			}

		}

	}



	static bool TryStylePortrait(Image image, string name, string path)

	{

		if (name != "Portrait" && name != "MerchantPortrait" && name != "BossAlarmPortrait")

			return false;



		ApplySprite(image, PortraitFrame, Image.Type.Simple, Color.white, path, "Medallions-base_01.png");

		return true;

	}



	static bool TryStyleSliderPart(Image image, string name, string path)

	{

		Slider slider = image.GetComponentInParent<Slider>();

		if (slider == null)

			return false;



		bool isHealth = slider.name is "Health" or "BossHealthBar"
		                || path.Contains("/Health/") || path.Contains("/BossHealthBar/");



		switch (name)

		{

			case "Background":

				if (path.Contains("/SettingPanel/") && (slider.name is "BgmSlider" or "SfxSlider"))

				{

					ApplySprite(image, PanelInput, Image.Type.Sliced, new Color(0.32f, 0.26f, 0.20f, 0.85f), path,

						"Panels_06.png");

				}

				else

				{

					ApplySprite(image, isHealth ? HealthTrack : SliderTrack, Image.Type.Simple, Color.white, path,

						isHealth ? "Charge Bars A_05.png" : "Horizontal Slidebar_01.png");

				}

				return true;

			case "Fill":

				ApplySprite(image, isHealth ? HealthFill : SettingsFill, Image.Type.Simple, Color.white, path,

					isHealth ? "Red Bar.png" : "Green Bar.png");

				return true;

			case "Handle":

				ApplySprite(image, SliderHandle, Image.Type.Simple, Color.white, path, "Handle 1.png");

				return true;

		}



		return false;

	}



	static bool TryStyleDropdownPart(Image image, string name, string path)

	{

		if (image.GetComponentInParent<TMP_Dropdown>() == null && image.GetComponentInParent<Dropdown>() == null)

			return false;



		bool inSettingPanel = path.Contains("/SettingPanel/");

		Color dropdownColor = inSettingPanel ? MenuInputBackgroundColor : Color.white;



		if (name is "Background" or "Item Background" or "Viewport" or "Template" or "Item Checkmark")

		{

			ApplySprite(image, PanelInput, Image.Type.Sliced, dropdownColor, path, "Panels_06.png");

			return true;

		}



		if (name == "Arrow")

		{

			if (inSettingPanel)

			{

				Sprite handleArrow = LoadSprite($"{Vol6Root}/Slide bars/Handle 1.png", "Handle 1_0");

				if (handleArrow != null)

				{

					ApplySprite(image, handleArrow, Image.Type.Simple, Color.white, path, "Handle 1.png");

					if (image.transform is RectTransform arrowRect)

					{

						arrowRect.sizeDelta = new Vector2(24f, 24f);

						arrowRect.localEulerAngles = Vector3.zero;

						EditorUtility.SetDirty(arrowRect);

					}

					return true;

				}

			}

			ApplySprite(image, GoldC02, Image.Type.Simple, Color.white, path, "Gold Btn C_02.png");

			return true;

		}



		return false;

	}



	static bool TryStyleInputBackground(Image image, string name, string path)

	{

		if (name != "Background" || image.GetComponentInParent<TMP_InputField>() == null)

			return false;



		ApplySprite(image, PanelInput, Image.Type.Sliced, Color.white, path, "Panels_03.png");

		return true;

	}



	static void ApplyButtonSpriteSwap(Button button, Sprite idle, Sprite pressed)

	{

		if (idle == null || pressed == null)

			return;



		button.transition = Selectable.Transition.SpriteSwap;

		SpriteState state = button.spriteState;

		state.pressedSprite = pressed;

		state.highlightedSprite = idle;

		state.selectedSprite = idle;

		button.spriteState = state;

		EditorUtility.SetDirty(button);

	}



	static void ApplySprite(Image image, Sprite sprite, Image.Type type, Color color, string targetPath, string sourceFile)

	{

		if (sprite == null || image == null)

			return;



		if (image.sprite == sprite && image.type == type)

			return;



		image.sprite = sprite;

		image.type = type;

		image.color = color;

		image.preserveAspect = type == Image.Type.Simple;

		EditorUtility.SetDirty(image);

		Report.Add($"{targetPath} → {sourceFile}");

	}



	static bool ShouldSkipImage(Image image)

	{

		if (ProtectedPanelBackgroundNames.Contains(image.gameObject.name))

			return true;



		if (image.name == "Icon")

		{

			Transform parent = image.transform.parent;

			if (parent != null && parent.name.StartsWith("Btn"))

				return true;

		}



		return false;

	}



	static bool IsBareImage(Image image)

	{

		if (image.sprite == null)

			return true;



		string path = AssetDatabase.GetAssetPath(image.sprite);

		return string.IsNullOrEmpty(path);

	}



	static bool IsUnderCanvas(Transform transform)

	{

		while (transform != null)

		{

			if (transform.GetComponent<Canvas>() != null)

				return true;

			transform = transform.parent;

		}



		return false;

	}



	static string GetPath(Transform transform)

	{

		var stack = new Stack<string>();

		while (transform != null)

		{

			stack.Push(transform.name);

			transform = transform.parent;

		}



		var sb = new StringBuilder();

		while (stack.Count > 0)

		{

			if (sb.Length > 0)

				sb.Append('/');

			sb.Append(stack.Pop());

		}



		return sb.ToString();

	}



	static Sprite LoadSprite(string assetPath, string spriteName)

	{

		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

		if (assets == null)

			return null;



		foreach (Object asset in assets)

		{

			if (asset is Sprite sprite && sprite.name == spriteName)

				return sprite;

		}



		return LoadFirstSprite(assetPath);

	}



	static Sprite LoadFirstSprite(string assetPath)

	{

		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

		if (assets == null)

			return null;



		foreach (Object asset in assets)

		{

			if (asset is Sprite sprite)

				return sprite;

		}



		return null;

	}

}

#endif

