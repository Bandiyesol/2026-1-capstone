using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Windows/Mac/Linux 빌드에서 Firestore C++ SDK 조회가 네이티브 크래시를 일으키는 경우가 있어
/// 읽기 전용 조회는 REST API로 처리합니다. (에디터는 Firestore SDK 사용)
/// </summary>
public static class FirestoreRestClient
{
	const string ProjectId = "the-last-rune";
	const string ApiKey = "AIzaSyDdriuWDzGyRV8vUHn9tnu1xeoay2uCQco";

	static readonly Regex StringFieldRegex = new Regex(
		"\"(?<field>[^\"]+)\"\\s*:\\s*\\{\\s*\"stringValue\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
		RegexOptions.Compiled);

	static readonly Regex BoolFieldRegex = new Regex(
		"\"(?<field>[^\"]+)\"\\s*:\\s*\\{\\s*\"booleanValue\"\\s*:\\s*(?<value>true|false)",
		RegexOptions.Compiled);

	static readonly Regex DoubleFieldRegex = new Regex(
		"\"(?<field>[^\"]+)\"\\s*:\\s*\\{\\s*\"doubleValue\"\\s*:\\s*(?<value>-?[0-9]+(?:\\.[0-9]+)?)",
		RegexOptions.Compiled);

	static readonly Regex IntegerFieldRegex = new Regex(
		"\"(?<field>[^\"]+)\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"(?<value>-?[0-9]+)\"",
		RegexOptions.Compiled);

	public static bool UseRestInPlayerBuild => !Application.isEditor;

	public static async Task<string> GetEmailByUsernameAsync(string username)
	{
		string normalized = UserProfileRepository.NormalizeUsername(username);
		if (string.IsNullOrEmpty(normalized))
			return null;

		string url = DocumentUrl("usernames", normalized);
		using UnityWebRequest request = UnityWebRequest.Get(url);
		await SendAsync(request);

		if (request.responseCode == 404)
			return null;

		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning($"[FirestoreRest] 아이디 조회 실패: {request.responseCode} {request.error}");
			return null;
		}

		return ParseStringField(request.downloadHandler.text, "email");
	}

	public static async Task<UserProfileRepository.UserProfileRecord> GetUserProfileAsync(string userId)
	{
		if (string.IsNullOrEmpty(userId))
			return null;

		string idToken = await TryGetIdTokenAsync();
		if (string.IsNullOrEmpty(idToken))
			return null;

		string url = DocumentUrl("users", userId);
		using UnityWebRequest request = UnityWebRequest.Get(url);
		request.SetRequestHeader("Authorization", $"Bearer {idToken}");
		await SendAsync(request);

		if (request.responseCode == 404)
			return null;

		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning($"[FirestoreRest] 프로필 조회 실패: {request.responseCode} {request.error}");
			return null;
		}

		Dictionary<string, string> fields = ParseFields(request.downloadHandler.text);
		if (fields.Count == 0)
			return null;

		return new UserProfileRepository.UserProfileRecord
		{
			Username = GetField(fields, "username"),
			Nickname = GetField(fields, "nickname"),
			Email = GetField(fields, "email"),
		};
	}

	public static async Task<IReadOnlyList<GameRunRecord>> FetchTopClearsAsync(int count)
	{
		if (count <= 0)
			return Array.Empty<GameRunRecord>();

		string idToken = await TryGetIdTokenAsync();
		if (string.IsNullOrEmpty(idToken))
			return Array.Empty<GameRunRecord>();

		string url =
			$"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery?key={ApiKey}";

		string body = "{" +
		              "\"structuredQuery\":{" +
		              "\"from\":[{\"collectionId\":\"clearLeaderboard\"}]," +
		              "\"orderBy\":[{\"field\":{\"fieldPath\":\"playTimeSeconds\"},\"direction\":\"ASCENDING\"}]," +
		              $"\"limit\":{count}" +
		              "}}";

		using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
		byte[] payload = Encoding.UTF8.GetBytes(body);
		request.uploadHandler = new UploadHandlerRaw(payload);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		request.SetRequestHeader("Authorization", $"Bearer {idToken}");
		await SendAsync(request);

		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning($"[FirestoreRest] 랭킹 조회 실패: {request.responseCode} {request.error}");
			return Array.Empty<GameRunRecord>();
		}

		return ParseLeaderboardQueryResponse(request.downloadHandler.text);
	}

	public static async Task<GameRunRecord> FindClearRecordByIdAsync(string recordId)
	{
		if (string.IsNullOrEmpty(recordId))
			return null;

		string idToken = await TryGetIdTokenAsync();
		if (string.IsNullOrEmpty(idToken))
			return null;

		string url = DocumentUrl("clearLeaderboard", recordId);
		using UnityWebRequest request = UnityWebRequest.Get(url);
		request.SetRequestHeader("Authorization", $"Bearer {idToken}");
		await SendAsync(request);

		if (request.responseCode == 404)
			return null;

		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning($"[FirestoreRest] 기록 조회 실패: {request.responseCode} {request.error}");
			return null;
		}

		return DeserializeLeaderboardDocument(recordId, request.downloadHandler.text);
	}

	static string DocumentUrl(string collection, string documentId) =>
		$"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{collection}/{Uri.EscapeDataString(documentId)}?key={ApiKey}";

	static async Task<string> TryGetIdTokenAsync()
	{
		await UnityMainThread.EnsureAsync();

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
			return null;

		try
		{
			Firebase.Auth.FirebaseUser user = AuthManager.Instance.CurrentUser;
			if (user == null)
				return null;

			return await user.TokenAsync(false).AwaitOnMainThread();
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[FirestoreRest] ID 토큰 획득 실패: {exception.Message}");
			return null;
		}
	}

	static async Task SendAsync(UnityWebRequest request)
	{
		await UnityMainThread.EnsureAsync();
		UnityWebRequestAsyncOperation op = request.SendWebRequest();
		while (!op.isDone)
			await UnityMainThread.WaitForNextFrameAsync();
	}

	static string ParseStringField(string json, string fieldName)
	{
		foreach (Match match in StringFieldRegex.Matches(json ?? string.Empty))
		{
			if (match.Groups["field"].Value != fieldName)
				continue;

			return UnescapeJsonString(match.Groups["value"].Value);
		}

		return null;
	}

	static Dictionary<string, string> ParseFields(string json)
	{
		var fields = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(json))
			return fields;

		foreach (Match match in StringFieldRegex.Matches(json))
			fields[match.Groups["field"].Value] = UnescapeJsonString(match.Groups["value"].Value);

		foreach (Match match in BoolFieldRegex.Matches(json))
			fields[match.Groups["field"].Value] = match.Groups["value"].Value;

		foreach (Match match in DoubleFieldRegex.Matches(json))
			fields[match.Groups["field"].Value] = match.Groups["value"].Value;

		foreach (Match match in IntegerFieldRegex.Matches(json))
			fields[match.Groups["field"].Value] = match.Groups["value"].Value;

		return fields;
	}

	static string GetField(Dictionary<string, string> fields, string key) =>
		fields.TryGetValue(key, out string value) ? value : null;

	static IReadOnlyList<GameRunRecord> ParseLeaderboardQueryResponse(string json)
	{
		var records = new List<GameRunRecord>();
		if (string.IsNullOrEmpty(json))
			return records;

		int searchFrom = 0;
		while (true)
		{
			int documentIndex = json.IndexOf("\"document\"", searchFrom, StringComparison.Ordinal);
			if (documentIndex < 0)
				break;

			int braceStart = json.IndexOf('{', documentIndex);
			if (braceStart < 0)
				break;

			if (!TryReadJsonObject(json, braceStart, out int braceEnd, out string documentJson))
				break;

			string recordId = ExtractDocumentId(documentJson);
			GameRunRecord record = DeserializeLeaderboardDocument(recordId, documentJson);
			if (record != null && record.cleared)
				records.Add(record);

			searchFrom = braceEnd + 1;
		}

		records.Sort(GameRunLeaderboard.CompareByClearTime);
		return records;
	}

	static GameRunRecord DeserializeLeaderboardDocument(string recordId, string json)
	{
		try
		{
			Dictionary<string, string> fields = ParseFields(json);
			if (fields.Count == 0)
				return null;

			string recordJson = GetField(fields, "recordJson");
			if (!string.IsNullOrEmpty(recordJson))
			{
				GameRunRecord fromJson = JsonUtility.FromJson<GameRunRecord>(recordJson);
				if (fromJson != null)
					return fromJson;
			}

			string id = GetField(fields, "recordId");
			if (string.IsNullOrEmpty(id))
				id = recordId;

			float playTime = 0f;
			if (fields.TryGetValue("playTimeSeconds", out string playTimeRaw))
				float.TryParse(playTimeRaw, out playTime);

			int killCount = 0;
			if (fields.TryGetValue("killCount", out string killRaw))
				int.TryParse(killRaw, out killCount);

			bool cleared = !fields.TryGetValue("cleared", out string clearedRaw) ||
			               string.Equals(clearedRaw, "true", StringComparison.OrdinalIgnoreCase);

			return new GameRunRecord
			{
				id = id,
				playerNickname = GetField(fields, "playerNickname"),
				playTimeSeconds = playTime,
				killCount = killCount,
				playedAt = GetField(fields, "playedAt"),
				cleared = cleared,
				weaponNames = Array.Empty<string>(),
				accessoryNames = Array.Empty<string>(),
				runeNames = Array.Empty<string>(),
				weaponSpriteIds = Array.Empty<string>(),
				stageRecords = GameRunRecord.CreateNew().stageRecords,
			};
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[FirestoreRest] 기록 파싱 실패: {exception.Message}");
			return null;
		}
	}

	static string ExtractDocumentId(string documentJson)
	{
		const string nameKey = "\"name\"";
		int nameIndex = documentJson.IndexOf(nameKey, StringComparison.Ordinal);
		if (nameIndex < 0)
			return null;

		string name = ParseStringField(documentJson, "name");
		if (string.IsNullOrEmpty(name))
			return null;

		int slash = name.LastIndexOf('/');
		return slash >= 0 ? name.Substring(slash + 1) : name;
	}

	static bool TryReadJsonObject(string json, int startIndex, out int endIndex, out string objectJson)
	{
		endIndex = -1;
		objectJson = null;

		if (startIndex < 0 || startIndex >= json.Length || json[startIndex] != '{')
			return false;

		int depth = 0;
		bool inString = false;
		bool escaped = false;

		for (int i = startIndex; i < json.Length; i++)
		{
			char c = json[i];

			if (inString)
			{
				if (escaped)
					escaped = false;
				else if (c == '\\')
					escaped = true;
				else if (c == '"')
					inString = false;

				continue;
			}

			if (c == '"')
			{
				inString = true;
				continue;
			}

			if (c == '{')
				depth++;
			else if (c == '}')
			{
				depth--;
				if (depth == 0)
				{
					endIndex = i;
					objectJson = json.Substring(startIndex, i - startIndex + 1);
					return true;
				}
			}
		}

		return false;
	}

	static string UnescapeJsonString(string value) =>
		string.IsNullOrEmpty(value)
			? value
			: value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
}
