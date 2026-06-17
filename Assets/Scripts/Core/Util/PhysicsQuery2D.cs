using System;
using UnityEngine;

/// <summary>
/// Physics2D 재사용 버퍼 쿼리. Unity 6 OverlapCircle API 사용.
/// </summary>
public static class PhysicsQuery2D
{
	const int MaxDepth = 4;
	const int BufferSize = 128;

	static readonly Collider2D[][] OverlapBuffers = new Collider2D[MaxDepth][];

	static int overlapDepth;

	static PhysicsQuery2D()
	{
		for (int i = 0; i < MaxDepth; i++)
			OverlapBuffers[i] = new Collider2D[BufferSize];
	}

	static ContactFilter2D CreateLayerFilter(int layerMask)
	{
		ContactFilter2D filter = ContactFilter2D.noFilter;
		filter.useLayerMask = true;
		filter.layerMask = layerMask;
		return filter;
	}

	public readonly struct OverlapCircleScope : IDisposable
	{
		readonly int depth;
		readonly bool pushed;
		public int Count { get; }

		internal OverlapCircleScope(Vector2 center, float radius, int layerMask)
		{
			if (overlapDepth < MaxDepth)
			{
				depth = overlapDepth++;
				pushed = true;
			}
			else
			{
				depth = MaxDepth - 1;
				pushed = false;
			}

			ContactFilter2D filter = CreateLayerFilter(layerMask);
			Count = Physics2D.OverlapCircle(center, radius, filter, OverlapBuffers[depth]);
		}

		public Collider2D Get(int index) => OverlapBuffers[depth][index];

		public void Dispose()
		{
			if (pushed && overlapDepth > 0)
				overlapDepth--;
		}
	}

	public static OverlapCircleScope OverlapCircle(Vector2 center, float radius, int layerMask = Physics2D.DefaultRaycastLayers)
		=> new OverlapCircleScope(center, radius, layerMask);
}
