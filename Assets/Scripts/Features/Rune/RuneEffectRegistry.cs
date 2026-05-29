using System;
using System.Collections.Generic;
using UnityEngine;

// [RuneEffectRegistry.cs]
// 런타임에 어떤 룬(열거형)이 어떤 C# 스크립트(Type)로 매핑되는지 기록하고 부착하는 공장(Factory)
public static class RuneEffectRegistry
{
	// 리플렉션으로 인한 성능 저하를 막기 위해 미리 딕셔너리로 타입 캐싱
	static readonly Dictionary<RuneType, Type> EffectTypes = new()
	{
		{ RuneType.Homing, typeof(EffectHoming) },
		{ RuneType.Orbit, typeof(EffectOrbit) },
		{ RuneType.Split, typeof(EffectSplit) },
		{ RuneType.Ricochet, typeof(EffectRicochet) },
		{ RuneType.Recursion, typeof(EffectRecursion) },
	};

	public static bool TryGetEffectType(RuneType runeType, out Type effectType) =>
		EffectTypes.TryGetValue(runeType, out effectType);

	// 실제 오브젝트(target)에 알맞은 룬 컴포넌트를 붙여주고 초기화해주는 함수
	public static RuneEffect AddEffect(GameObject target, RuneType runeType, WeaponInstance weapon, Motion motion, RuneData runeData)
	{
		if (!TryGetEffectType(runeType, out Type effectType))
		{
			Debug.LogWarning($"[RuneEffectRegistry] 미구현 룬 Effect: {runeType}");
			return null;
		}

		// 컴포넌트 부착 후 공통 초기화 함수(InitEffect) 호출
		RuneEffect effect = (RuneEffect)target.AddComponent(effectType);
		effect.InitEffect(weapon, motion, runeData);
		return effect;
	}
}