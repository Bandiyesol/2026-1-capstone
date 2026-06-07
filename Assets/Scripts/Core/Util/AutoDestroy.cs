using UnityEngine;

/// <summary>일정 시간 후 오브젝트를 제거합니다. (예: Explosion VFX)</summary>
public class AutoDestroy : MonoBehaviour
{
	[SerializeField] float lifetime = 0.5f;

	void Start()
	{
		if (lifetime > 0f)
			Destroy(gameObject, lifetime);
	}
}
