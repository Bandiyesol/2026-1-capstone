using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 빌드에서 async continuation이 메인 스레드를 벗어날 때 Unity API 호출 전에 복귀합니다.
/// </summary>
public sealed class UnityMainThread : MonoBehaviour
{
	static UnityMainThread instance;
	static int mainThreadId;
	static readonly Queue<Action> Pending = new Queue<Action>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Bootstrap()
	{
		mainThreadId = Thread.CurrentThread.ManagedThreadId;

		if (instance != null)
			return;

		var go = new GameObject(nameof(UnityMainThread));
		instance = go.AddComponent<UnityMainThread>();
		DontDestroyOnLoad(go);
	}

	public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

	public static Task EnsureAsync()
	{
		if (IsMainThread)
			return Task.CompletedTask;

		var tcs = new TaskCompletionSource<bool>();
		Post(() => tcs.SetResult(true));
		return tcs.Task;
	}

	public static async Task WaitForNextFrameAsync()
	{
		await EnsureAsync();

		var tcs = new TaskCompletionSource<bool>();
		instance.StartCoroutine(WaitFrameCoroutine(tcs));
		await tcs.Task;
	}

	static IEnumerator WaitFrameCoroutine(TaskCompletionSource<bool> tcs)
	{
		yield return null;
		tcs.TrySetResult(true);
	}

	static void Post(Action action)
	{
		if (action == null)
			return;

		if (instance == null)
			Bootstrap();

		lock (Pending)
			Pending.Enqueue(action);
	}

	void Update()
	{
		lock (Pending)
		{
			while (Pending.Count > 0)
				Pending.Dequeue().Invoke();
		}
	}
}
