using System;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// 아이디(username) ↔ 이메일, 닉네임을 Firestore에 저장합니다.
/// </summary>
public class UserProfileRepository
{
	const string UsersCollection = "users";
	const string UsernamesCollection = "usernames";

	FirebaseFirestore database;

	FirebaseFirestore Database => database ??= FirebaseFirestore.DefaultInstance;

	public async Task<bool> IsUsernameTakenAsync(string username)
	{
		string normalized = NormalizeUsername(username);
		DocumentReference doc = Database.Collection(UsernamesCollection).Document(normalized);
		DocumentSnapshot snapshot = await doc.GetSnapshotAsync().AwaitOnMainThread();
		return snapshot.Exists;
	}

	public async Task SaveProfileAsync(string userId, string username, string nickname, string email)
	{
		string normalized = NormalizeUsername(username);
		string uid = userId?.Trim();
		string mail = email?.Trim();
		string nick = nickname?.Trim();

		if (string.IsNullOrEmpty(uid))
			throw new InvalidOperationException("userId 가 비어 있습니다.");

		DocumentReference userDoc = Database.Collection(UsersCollection).Document(uid);
		DocumentReference usernameDoc = Database.Collection(UsernamesCollection).Document(normalized);

		WriteBatch batch = Database.StartBatch();
		batch.Set(userDoc, new UserProfileRecord
		{
			Username = normalized,
			Nickname = nick,
			Email = mail,
			CreatedAt = Timestamp.GetCurrentTimestamp(),
		});
		batch.Set(usernameDoc, new UsernameRecord
		{
			Email = mail,
			Uid = uid,
		});
		await batch.CommitAsync().AwaitOnMainThread();
	}

	public async Task<string> GetEmailByUsernameAsync(string username)
	{
		if (FirestoreRestClient.UseRestInPlayerBuild)
			return await FirestoreRestClient.GetEmailByUsernameAsync(username);

		string normalized = NormalizeUsername(username);
		DocumentReference doc = Database.Collection(UsernamesCollection).Document(normalized);
		DocumentSnapshot snapshot = await doc.GetSnapshotAsync().AwaitOnMainThread();

		if (!snapshot.Exists)
			return null;

		UsernameRecord record = snapshot.ConvertTo<UsernameRecord>();
		return record?.Email;
	}

	public async Task<UserProfileRecord> GetProfileAsync(string userId)
	{
		string uid = userId?.Trim();
		if (string.IsNullOrEmpty(uid))
			return null;

		if (FirestoreRestClient.UseRestInPlayerBuild)
			return await FirestoreRestClient.GetUserProfileAsync(uid);

		DocumentReference doc = Database.Collection(UsersCollection).Document(uid);
		DocumentSnapshot snapshot = await doc.GetSnapshotAsync().AwaitOnMainThread();

		if (!snapshot.Exists)
			return null;

		return snapshot.ConvertTo<UserProfileRecord>();
	}

	public async Task DeleteProfileAsync(string userId, string username)
	{
		string uid = userId?.Trim();
		if (string.IsNullOrEmpty(uid))
			throw new InvalidOperationException("userId 가 비어 있습니다.");

		WriteBatch batch = Database.StartBatch();
		batch.Delete(Database.Collection(UsersCollection).Document(uid));

		string normalized = NormalizeUsername(username);
		if (!string.IsNullOrEmpty(normalized))
			batch.Delete(Database.Collection(UsernamesCollection).Document(normalized));

		await batch.CommitAsync().AwaitOnMainThread();
	}

	public static string NormalizeUsername(string username)
	{
		return username?.Trim().ToLowerInvariant();
	}

	[FirestoreData]
	public class UserProfileRecord
	{
		[FirestoreProperty("username")] public string Username { get; set; }
		[FirestoreProperty("nickname")] public string Nickname { get; set; }
		[FirestoreProperty("email")] public string Email { get; set; }
		[FirestoreProperty("createdAt")] public Timestamp CreatedAt { get; set; }
	}

	[FirestoreData]
	public class UsernameRecord
	{
		[FirestoreProperty("email")] public string Email { get; set; }
		[FirestoreProperty("uid")] public string Uid { get; set; }
	}
}
