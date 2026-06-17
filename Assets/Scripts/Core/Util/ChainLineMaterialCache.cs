using UnityEngine;

/// <summary>체인 룬 LineRenderer용 공유 머티리얼 (proc마다 new Material 방지).</summary>
public static class ChainLineMaterialCache
{
	static Material sharedMaterial;

	public static Material Shared
	{
		get
		{
			if (sharedMaterial == null)
				sharedMaterial = new Material(Shader.Find("Sprites/Default"));

			return sharedMaterial;
		}
	}
}
