using UnityEngine;


public abstract class Motion : MonoBehaviour
{
	protected WeaponInstance instance;
	protected Rigidbody2D rigidBody;


	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody2D>();
	}
	
	public virtual void Initialize(WeaponInstance instance) 
	{
		this.instance = instance;
		float weaponSize = instance.size;

		transform.localScale = new Vector3(weaponSize, weaponSize, 1f);
		
		OnStart();
	}

	protected abstract void OnStart();
	
	protected void ApplyKnockback(Collider2D collision)
	{
		Rigidbody2D rigidbody = collision.GetComponent<Rigidbody2D>();

		if (rigidbody != null)
		{
			Vector2 direction = transform.right;
			float force = instance.weight * 0.5f;
			rigidbody.AddForce(direction * force, ForceMode2D.Impulse);
		}
	}
}