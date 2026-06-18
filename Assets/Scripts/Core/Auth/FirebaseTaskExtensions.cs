using System;
using System.Threading.Tasks;
using Firebase.Extensions;

/// <summary>
/// Firebase Task는 빌드에서 백그라운드 스레드로 이어질 수 있어 Unity API 호출 전에 메인 스레드로 되돌립니다.
/// </summary>
public static class FirebaseTaskExtensions
{
	public static Task AwaitOnMainThread(this Task task)
	{
		if (task == null)
			throw new ArgumentNullException(nameof(task));

		if (task.IsCompleted)
			return task;

		var tcs = new TaskCompletionSource<bool>();
		task.ContinueWithOnMainThread(t =>
		{
			if (t.IsCanceled)
				tcs.SetCanceled();
			else if (t.IsFaulted)
				tcs.SetException(t.Exception?.GetBaseException() ?? t.Exception);
			else
				tcs.SetResult(true);
		});
		return tcs.Task;
	}

	public static Task<T> AwaitOnMainThread<T>(this Task<T> task)
	{
		if (task == null)
			throw new ArgumentNullException(nameof(task));

		if (task.IsCompleted)
			return task;

		var tcs = new TaskCompletionSource<T>();
		task.ContinueWithOnMainThread(t =>
		{
			if (t.IsCanceled)
				tcs.SetCanceled();
			else if (t.IsFaulted)
				tcs.SetException(t.Exception?.GetBaseException() ?? t.Exception);
			else
				tcs.SetResult(t.Result);
		});
		return tcs.Task;
	}
}
