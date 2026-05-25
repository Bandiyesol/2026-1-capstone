using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 — UI 클릭 이벤트가 끝난 뒤 Play 모드를 멈춰 frustum 경고를 줄입니다.
/// </summary>
public static class GameQuitUtility
{
#if UNITY_EDITOR
	static bool editorQuitScheduled;
#endif

	public static void RequestQuit()
	{
		Time.timeScale = 1f;
		ClearUiFocus();

#if UNITY_EDITOR
		if (editorQuitScheduled)
			return;

		editorQuitScheduled = true;
		UnityEditor.EditorApplication.update += EditorQuitOnNextUpdate;
#else
		DisableUiInput();

		if (GameManager.instance != null)
			GameManager.instance.StartCoroutine(QuitNextFrame());
		else
			Application.Quit();
#endif
	}

#if UNITY_EDITOR
	static void EditorQuitOnNextUpdate()
	{
		UnityEditor.EditorApplication.update -= EditorQuitOnNextUpdate;
		editorQuitScheduled = false;

		DisableUiInput();

		if (UnityEditor.EditorApplication.isPlaying)
			UnityEditor.EditorApplication.isPlaying = false;
	}
#endif

	static IEnumerator QuitNextFrame()
	{
		yield return new WaitForEndOfFrame();
		yield return null;
		Application.Quit();
	}

	static void DisableInputModules(EventSystem eventSystem)
	{
		MonoBehaviour[] behaviours = eventSystem.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour behaviour in behaviours)
		{
			if (behaviour == null)
				continue;

			string typeName = behaviour.GetType().FullName;
			if (typeName == null)
				continue;

			if (typeName.Contains("InputSystemUIInputModule")
			    || typeName.Contains("StandaloneInputModule"))
				behaviour.enabled = false;
		}
	}

	static void ClearUiFocus()
	{
		if (EventSystem.current == null)
			return;

		EventSystem.current.SetSelectedGameObject(null);
	}

	static void DisableUiInput()
	{
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem != null)
		{
			DisableInputModules(eventSystem);
			eventSystem.enabled = false;
		}

		GraphicRaycaster[] raycasters = UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(
			FindObjectsInactive.Include, FindObjectsSortMode.None);

		foreach (GraphicRaycaster raycaster in raycasters)
		{
			if (raycaster != null)
				raycaster.enabled = false;
		}
	}
}
