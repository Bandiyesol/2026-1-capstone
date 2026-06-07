using UnityEngine;

/// <summary>씬에 ShopPanel + ShopUI가 있어야 합니다.</summary>
public static class ShopUIBootstrap
{
	const string ShopPanelName = "ShopPanel";

	public static ShopUI EnsureShopUI()
	{
		ShopUI existing = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		if (existing != null)
		{
			existing.EnsureReady();
			return existing;
		}

		GameObject shopPanel = FindSceneObject(ShopPanelName);
		if (shopPanel == null)
		{
			Debug.LogError(
				"[ShopUIBootstrap] ShopPanel이 없습니다. Canvas 아래 ShopPanel + ShopUI를 배치하세요.");
			return null;
		}

		ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
		if (shopUI == null)
		{
			Debug.LogError("[ShopUIBootstrap] ShopPanel에 ShopUI 컴포넌트가 없습니다.");
			return null;
		}

		shopUI.EnsureReady();
		return shopUI;
	}

	public static GameObject FindSceneObject(string objectName)
	{
		if (string.IsNullOrEmpty(objectName))
			return null;

		GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject root in roots)
		{
			if (TryFindByName(root.transform, objectName, out GameObject found))
				return found;
		}

		return null;
	}

	static bool TryFindByName(Transform parent, string objectName, out GameObject found)
	{
		if (parent.name == objectName)
		{
			found = parent.gameObject;
			return true;
		}

		for (int i = 0; i < parent.childCount; i++)
		{
			if (TryFindByName(parent.GetChild(i), objectName, out found))
				return true;
		}

		found = null;
		return false;
	}
}
