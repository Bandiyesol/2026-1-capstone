using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// 빌드에서 EventSystem UI Input Actions 참조가 깨지면 마우스 클릭/호버가 전부 무효화됩니다.
/// 에디터는 프로젝트 전역 Input 설정으로 동작해 이 문제가 숨겨질 수 있습니다.
/// </summary>
public static class UiInputSystemBootstrap
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void EnsureAfterSceneLoad()
	{
		EnsureUiInputModule();
	}

	public static void EnsureUiInputModule()
	{
#if ENABLE_INPUT_SYSTEM
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem == null)
			eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

		if (eventSystem == null)
			return;

		if (!eventSystem.TryGetComponent<InputSystemUIInputModule>(out InputSystemUIInputModule uiModule))
			return;

		if (HasUsableActions(uiModule.actionsAsset))
		{
			LinkPlayerInputModule(uiModule);
			return;
		}

		InputActionAsset fallback = ResolveFallbackActions();
		if (fallback == null)
		{
			Debug.LogError(
				"[UiInputSystemBootstrap] UI Input Actions를 찾지 못했습니다. " +
				"빌드에서 상점·인벤토리 클릭/툴팁이 동작하지 않을 수 있습니다.");
			return;
		}

		uiModule.actionsAsset = fallback;
		LinkPlayerInputModule(uiModule);
		Debug.Log("[UiInputSystemBootstrap] InputSystemUIInputModule actions를 런타임에 재연결했습니다.");
#endif
	}

#if ENABLE_INPUT_SYSTEM
	static bool HasUsableActions(InputActionAsset asset)
	{
		if (asset == null)
			return false;

		try
		{
			return asset.actionMaps.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	static InputActionAsset ResolveFallbackActions()
	{
		PlayerInput playerInput = Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
		if (playerInput != null && HasUsableActions(playerInput.actions))
			return playerInput.actions;

		if (HasUsableActions(InputSystem.actions))
			return InputSystem.actions;

		return null;
	}

	static void LinkPlayerInputModule(InputSystemUIInputModule uiModule)
	{
		PlayerInput playerInput = Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
		if (playerInput != null && playerInput.uiInputModule == null)
			playerInput.uiInputModule = uiModule;
	}
#endif
}
