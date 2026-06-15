#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI 스타일 적용 후 깨진 레이아웃·랭킹 행·스킵 버튼을 복구합니다.
/// Tools/UI/Fix UI Regressions
/// </summary>
public static class UiRegressionFixEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	static readonly Color RankRowBackgroundColor = new(0.18f, 0.22f, 0.3f, 0.55f);

	[MenuItem("Tools/UI/Fix UI Regressions")]
	[MenuItem("Window/The Last Rune/UI/Fix UI Regressions")]
	public static void ApplyFromMenu()
	{
		if (SceneManager.GetActiveScene().path != ScenePath)
			EditorSceneManager.OpenScene(ScenePath);

		ApplyFixesWithoutDialog();

		EditorUtility.DisplayDialog("UI 회귀 수정", "클리어 랭킹·비밀번호 찾기·스킵 버튼을 복구했습니다.", "확인");
	}

	public static void ApplyFixesWithoutDialog()
	{
		RevertRankingRowButtons();
		FixForgotPasswordLayout();
		FixSkipButtons();

		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		EditorSceneManager.SaveOpenScenes();
	}

	public static void RevertRankingRowButtons()
	{
		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (!button.name.StartsWith("RankRow"))
				continue;

			Image image = button.GetComponent<Image>();
			if (image == null)
				continue;

			button.transition = Selectable.Transition.ColorTint;
			SpriteState state = button.spriteState;
			state.highlightedSprite = null;
			state.pressedSprite = null;
			state.selectedSprite = null;
			state.disabledSprite = null;
			button.spriteState = state;

			image.sprite = null;
			image.type = Image.Type.Simple;
			image.color = RankRowBackgroundColor;
			image.preserveAspect = false;

			EditorUtility.SetDirty(button);
			EditorUtility.SetDirty(image);
		}
	}

	static void FixForgotPasswordLayout()
	{
		Transform panel = FindChildByName("ForgotPasswordPanel");
		if (panel == null)
			return;

		SetRect(panel.Find("ForgotPasswordInput"), new Vector2(0f, -100f), new Vector2(500f, 64f));
		SetRect(panel.Find("SendResetEmailButton"), new Vector2(-190f, -210f), new Vector2(400f, 96f));
		SetRect(panel.Find("ForgotBackToLoginButton"), new Vector2(190f, -210f), new Vector2(340f, 96f));

		Transform sendButton = panel.Find("SendResetEmailButton");
		if (sendButton != null)
		{
			foreach (TMP_Text label in sendButton.GetComponentsInChildren<TMP_Text>(true))
			{
				label.text = "재설정 메일 보내기";
				label.fontSize = 22f;
				label.enableAutoSizing = false;
				EditorUtility.SetDirty(label);
			}
		}
	}

	static void FixSkipButtons()
	{
		string[] paths =
		{
			"MainStoryPanel/SkipButton",
			"EndingStoryPanel/SkipButton",
		};

		foreach (string path in paths)
		{
			Transform button = FindByPath(path);
			if (button == null || !button.TryGetComponent(out RectTransform rect))
				continue;

			rect.anchorMin = new Vector2(1f, 0f);
			rect.anchorMax = new Vector2(1f, 0f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = new Vector2(-220f, 120f);
			rect.sizeDelta = new Vector2(340f, 96f);
			EditorUtility.SetDirty(rect);

			foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
			{
				label.fontSize = Mathf.Max(label.fontSize, 28f);
				label.enableAutoSizing = false;
				EditorUtility.SetDirty(label);
			}
		}
	}

	static Transform FindByPath(string hierarchyPath)
	{
		string[] parts = hierarchyPath.Split('/');
		foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
		{
			if (root.name != parts[0])
				continue;

			Transform current = root.transform;
			for (int i = 1; i < parts.Length && current != null; i++)
				current = current.Find(parts[i]);

			if (current != null)
				return current;
		}

		foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (transform.name != parts[0])
				continue;

			Transform current = transform;
			for (int i = 1; i < parts.Length && current != null; i++)
				current = current.Find(parts[i]);

			if (current != null)
				return current;
		}

		return null;
	}

	static Transform FindChildByName(string objectName)
	{
		foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
		{
			foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
			{
				if (transform.name == objectName)
					return transform;
			}
		}

		return null;
	}

	static void SetRect(Transform target, Vector2 anchoredPosition, Vector2 sizeDelta)
	{
		if (target == null || !target.TryGetComponent(out RectTransform rect))
			return;

		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = sizeDelta;
		EditorUtility.SetDirty(rect);
	}
}
#endif
