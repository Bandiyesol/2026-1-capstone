using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Firebase SDK 초기화. 씬에 빈 오브젝트 하나에 붙여 두세요.
/// </summary>
public class FirebaseBootstrap : MonoBehaviour
{
	public static bool IsReady { get; private set; }
	public static string LastError { get; private set; }

	static Task<bool> initTask;

	public static Task<bool> EnsureInitializedAsync()
	{
		if (IsReady)
			return Task.FromResult(true);

		if (initTask != null)
			return initTask;

		initTask = InitializeInternalAsync();
		return initTask;
	}

	static async Task<bool> InitializeInternalAsync()
	{
		try
		{
			DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
			if (status != DependencyStatus.Available)
			{
				LastError = $"Firebase 의존성 오류: {status}";
				Debug.LogError($"[FirebaseBootstrap] {LastError}");
				return false;
			}

			FirebaseApp app = FirebaseApp.DefaultInstance;
			if (app == null)
			{
				LastError = "FirebaseApp.DefaultInstance 가 null 입니다.";
				Debug.LogError($"[FirebaseBootstrap] {LastError}");
				return false;
			}

			IsReady = true;
			LastError = null;
			Debug.Log("[FirebaseBootstrap] Firebase 초기화 완료");
			return true;
		}
		catch (Exception exception)
		{
			LastError = exception.Message;
			Debug.LogError($"[FirebaseBootstrap] 초기화 실패: {exception}");
			return false;
		}
	}

	void Awake()
	{
		if (transform.parent != null)
			transform.SetParent(null);

		DontDestroyOnLoad(gameObject);
		_ = EnsureInitializedAsync();
	}
}
