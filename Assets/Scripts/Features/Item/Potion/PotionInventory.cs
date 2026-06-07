using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 소지한 물약. 획득·사용 로직은 추후 연동. 준비 전에는 Player에 붙이지 않아도 됩니다.
/// </summary>
public class PotionInventory : MonoBehaviour
{
	public static PotionInventory Instance { get; private set; }

	[Serializable]
	public class PotionStack
	{
		public string potionId;
		public string displayName;
		public Sprite icon;
		public int count = 1;
	}

	[SerializeField] int maxStacks = 999;

	readonly List<PotionStack> stacks = new List<PotionStack>();

	public IReadOnlyList<PotionStack> Stacks => stacks;
	public int MaxStacks => maxStacks;
	public event Action OnInventoryChanged;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}

		Instance = this;

		if (maxStacks <= 12)
			maxStacks = 999;
	}

	public bool TryAdd(string potionId, Sprite icon, int count = 1, string displayName = null)
	{
		if (string.IsNullOrEmpty(potionId) || count <= 0)
			return false;

		foreach (PotionStack stack in stacks)
		{
			if (stack.potionId != potionId)
				continue;

			stack.count += count;
			if (icon != null)
				stack.icon = icon;
			if (!string.IsNullOrEmpty(displayName))
				stack.displayName = displayName;
			NotifyChange();
			return true;
		}

		if (stacks.Count >= maxStacks)
		{
			Debug.LogWarning($"[PotionInventory] 물약 슬롯 상한({maxStacks})에 도달했습니다.");
			return false;
		}

		stacks.Add(new PotionStack
		{
			potionId = potionId,
			displayName = displayName,
			icon = icon,
			count = count
		});
		NotifyChange();
		return true;
	}

	public void Clear()
	{
		stacks.Clear();
		NotifyChange();
	}

	void NotifyChange() => OnInventoryChanged?.Invoke();
}
