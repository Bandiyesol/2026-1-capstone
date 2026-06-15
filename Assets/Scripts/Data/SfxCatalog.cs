using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 클립·볼륨 보정 목록.
/// Resources/Data/SfxCatalog — Tools/Game/Rebuild All Audio Catalogs 로 갱신합니다.
/// </summary>
[CreateAssetMenu(fileName = "SfxCatalog", menuName = "Game/Sfx Catalog")]
public class SfxCatalog : ScriptableObject
{
	[Serializable]
	public struct Entry
	{
		public SfxId id;
		public AudioClip clip;
		[Range(0.1f, 3f)]
		public float volumeScale;
	}

	public Entry[] entries;

	static SfxCatalog cached;
	Dictionary<SfxId, Entry> lookup;

	public static SfxCatalog Load()
	{
		if (cached == null)
			cached = Resources.Load<SfxCatalog>("Data/SfxCatalog");

		return cached;
	}

	public bool TryGet(SfxId id, out Entry entry)
	{
		EnsureLookup();
		return lookup.TryGetValue(id, out entry);
	}

	public float GetVolumeScale(SfxId id)
	{
		if (!TryGet(id, out Entry entry))
			return 1f;

		return Mathf.Clamp(entry.volumeScale, 0.1f, 3f);
	}

	public AudioClip GetClip(SfxId id)
	{
		return TryGet(id, out Entry entry) ? entry.clip : null;
	}

	public static SfxId WeaponTypeToSfxId(string weaponType)
	{
		if (string.IsNullOrEmpty(weaponType))
			return SfxId.WeaponSword;

		return weaponType switch
		{
			"Sword" => SfxId.WeaponSword,
			"Scythe" => SfxId.WeaponScythe,
			"Sickle" => SfxId.WeaponScythe,
			"Hammer" => SfxId.WeaponHammer,
			"Gun" => SfxId.WeaponGun,
			"Bow" => SfxId.WeaponBow,
			"Whip" => SfxId.WeaponWhip,
			"Boomerang" => SfxId.WeaponBoomerang,
			"Orb" => SfxId.WeaponOrb,
			"Grimoire" => SfxId.WeaponGrimoire,
			"Staff" => SfxId.WeaponStaff,
			_ => SfxId.WeaponSword,
		};
	}

	void EnsureLookup()
	{
		if (lookup != null)
			return;

		lookup = new Dictionary<SfxId, Entry>();
		if (entries == null)
			return;

		foreach (Entry entry in entries)
		{
			if (!lookup.ContainsKey(entry.id))
				lookup.Add(entry.id, entry);
		}
	}

#if UNITY_EDITOR
	public static void SetCached(SfxCatalog catalog)
	{
		cached = catalog;
		catalog?.ClearLookupCache();
	}

	public void SetEntries(Entry[] newEntries)
	{
		entries = newEntries;
		ClearLookupCache();
	}
#endif

	void ClearLookupCache()
	{
		lookup = null;
	}
}
