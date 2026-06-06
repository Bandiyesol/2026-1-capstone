using UnityEngine;

/// <summary>
/// 씬에 ShopPanel이 없을 때 InventoryPanel을 복제해 상점 UI를 준비합니다.
/// </summary>
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
		if (shopPanel != null)
		{
			InventoryUI strayInventory = shopPanel.GetComponent<InventoryUI>();
			if (strayInventory != null)
				Object.Destroy(strayInventory);

			ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
			if (shopUI == null)
				shopUI = shopPanel.AddComponent<ShopUI>();

			shopUI.EnsureReady();
			return shopUI;
		}

		InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
		GameObject sourcePanel = FindSceneObject("InventoryPanel");
		if (sourcePanel == null && inventoryUI != null)
			sourcePanel = inventoryUI.gameObject;

		if (sourcePanel == null)
		{
			Debug.LogError(
				"[ShopUIBootstrap] ShopPanel과 InventoryPanel을 찾지 못했습니다. " +
				"Canvas 아래 InventoryPanel을 복제해 ShopPanel + ShopUI를 추가하세요.");
			return null;
		}

		Transform parent = sourcePanel.transform.parent;
		GameObject clone = Object.Instantiate(sourcePanel, parent);
		clone.name = ShopPanelName;
		clone.SetActive(false);

		InventoryUI cloneInventory = clone.GetComponent<InventoryUI>();
		if (cloneInventory != null)
			Object.Destroy(cloneInventory);

		ShopUI created = clone.AddComponent<ShopUI>();
		created.EnsureReady();

		Debug.Log("[ShopUIBootstrap] InventoryPanel을 복제해 ShopPanel을 생성했습니다.");
		return created;
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
