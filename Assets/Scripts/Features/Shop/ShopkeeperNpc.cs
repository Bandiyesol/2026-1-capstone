using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보스 처치 후 마법진 옆에 등장하는 상점 주인. 클릭 시 ShopUI를 엽니다.
/// </summary>
public class ShopkeeperNpc : MonoBehaviour
{
	[SerializeField] Sprite[] idleFrames;
	[SerializeField] float idleFps = 8f;
	[SerializeField] float clickPadding = 0.12f;

	SpriteRenderer spriter;
	BoxCollider2D clickCollider;
	int frameIndex;
	float frameTimer;

	void Awake()
	{
		spriter = GetComponent<SpriteRenderer>();
		clickCollider = GetComponent<BoxCollider2D>();
	}

	void OnEnable()
	{
		frameIndex = 0;
		frameTimer = 0f;

		if (spriter != null && idleFrames != null && idleFrames.Length > 0)
			spriter.sprite = idleFrames[0];

		MatchPlayerScale();
		AlignClickColliderToSprite();
	}

	void Update()
	{
		AnimateIdle();
		TryHandleClick();
	}

	void MatchPlayerScale()
	{
		if (GameManager.instance?.player == null)
			return;

		transform.localScale = GameManager.instance.player.transform.localScale;
	}

	void AnimateIdle()
	{
		if (spriter == null || idleFrames == null || idleFrames.Length == 0)
			return;

		frameTimer += Time.deltaTime;
		float interval = 1f / Mathf.Max(idleFps, 1f);
		if (frameTimer < interval)
			return;

		frameTimer -= interval;
		frameIndex = (frameIndex + 1) % idleFrames.Length;
		spriter.sprite = idleFrames[frameIndex];
		AlignClickColliderToSprite();
	}

	void AlignClickColliderToSprite()
	{
		if (spriter == null || spriter.sprite == null || clickCollider == null)
			return;

		Bounds bounds = spriter.sprite.bounds;
		float pad = Mathf.Max(clickPadding, 0f);
		clickCollider.offset = bounds.center;
		Vector2 spriteSize = bounds.size;
		clickCollider.size = spriteSize + new Vector2(pad * 2f, pad * 2f);
	}

	void TryHandleClick()
	{
		if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
			return;

		if (!CanOpenShop())
			return;

		if (!TryGetClickHit(out Collider2D hit) || hit.gameObject != gameObject)
			return;

		ShopUI ui = ShopUIBootstrap.EnsureShopUI();
		if (ui != null)
			ui.Open();
	}

	bool CanOpenShop()
	{
		if (!isActiveAndEnabled)
			return false;

		ShopUI ui = ShopUIBootstrap.EnsureShopUI();
		if (ui != null && ui.IsPanelOpen)
			return false;

		return true;
	}

	bool TryGetClickHit(out Collider2D hit)
	{
		hit = null;

		Camera cam = Camera.main;
		if (cam == null)
			return false;

		Vector2 world = GetMouseWorldPoint(cam);

		if (clickCollider != null && clickCollider.OverlapPoint(world))
		{
			hit = clickCollider;
			return true;
		}

		Collider2D[] hits = Physics2D.OverlapPointAll(world);
		for (int i = 0; i < hits.Length; i++)
		{
			if (hits[i] != null && hits[i].GetComponent<ShopkeeperNpc>() == this)
			{
				hit = hits[i];
				return true;
			}
		}

		return false;
	}

	public void SetIdleFrames(Sprite[] frames)
	{
		idleFrames = frames;
	}

	Vector2 GetMouseWorldPoint(Camera cam)
	{
		Vector3 screen = Mouse.current.position.ReadValue();
		screen.z = cam.WorldToScreenPoint(transform.position).z;
		return cam.ScreenToWorldPoint(screen);
	}
}
