using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RuneType → Effect 컴포넌트 매핑. 리플렉션 GetType 대신 사용.
/// </summary>
public static class RuneEffectRegistry
{
	static readonly Dictionary<RuneType, Type> EffectTypes = new()
	{
		{ RuneType.Homing, typeof(EffectHoming) },
		{ RuneType.Orbit, typeof(EffectOrbit) },
		{ RuneType.Wave, typeof(EffectWave) },
		{ RuneType.Spiral, typeof(EffectSpiral) },
		{ RuneType.Split, typeof(EffectSplit) },
		{ RuneType.Ricochet, typeof(EffectRicochet) },
		{ RuneType.Recursion, typeof(EffectRecursion) },
		{ RuneType.Vampire, typeof(EffectVampire) },
		{ RuneType.Freeze, typeof(EffectFreeze) },
		{ RuneType.Chain, typeof(EffectChain) },
		{ RuneType.Explode, typeof(EffectExplode) },
		{ RuneType.Gravity, typeof(EffectGravity) },
		{ RuneType.Growth, typeof(EffectGrowth) },
		{ RuneType.Blink, typeof(EffectBlink) },
		{ RuneType.Boing, typeof(EffectBoing) },
	};

	public static bool TryGetEffectType(RuneType runeType, out Type effectType) =>
		EffectTypes.TryGetValue(runeType, out effectType);

	public static RuneEffect AddEffect(GameObject target, RuneType runeType, WeaponInstance weapon, Motion motion, RuneData runeData)
	{
		if (!TryGetEffectType(runeType, out Type effectType))
		{
			Debug.LogWarning($"[RuneEffectRegistry] 미구현 룬 Effect: {runeType}");
			return null;
		}

		RuneEffect effect = (RuneEffect)target.AddComponent(effectType);
		effect.InitEffect(weapon, motion, runeData);
		return effect;
	}
}
