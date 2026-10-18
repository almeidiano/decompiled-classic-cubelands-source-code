using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
	private static bool isGuest;

	private static int userID;

	private static string username;

	private static string userSkinID;

	private static string userEmail;

	private static string sessionKey;

	private static bool boughtGame;

	private static bool hasDoneTutorial;

	private static List<Friend> friendsList;

	private static List<Friend> pendingFriends;

	private static List<Friend> friendInvites;

	private static string wwwURL;

	public static bool IsGuest()
	{
		return isGuest;
	}

	public static bool LoggedIn()
	{
		return true;
	}

	public static bool RealAccount()
	{
		return userID > 0;
	}

	public static MonoBehaviour GetMono()
	{
		if (Application.loadedLevel == 1)
		{
			return MainMenu.SP;
		}
		return GameManager.SP;
	}

	public static IEnumerator Login(IENumeratorOutput output, string loginName, string password)
	{
		if (loginName.Trim() == "")
		{
			output.SetFailed();
			yield break;
		}
		userID = Random.Range(1, 999999);
		username = loginName;
		userEmail = Random.Range(1, 999999) + "@gmail.com";
		sessionKey = Random.Range(1, 999999).ToString();
		output.SetOutput("succes");
		friendsList = new List<Friend>();
	}

	public static IEnumerator WebLogin(IENumeratorOutput output, int webUserID, string webSessionKey, string passwordPart)
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "weblogin");
		wWWForm.AddField("userID", webUserID);
		wWWForm.AddField("webSessionKey", webSessionKey);
		wWWForm.AddField("pass", passwordPart);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		Debug.Log(www.text);
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			output.SetFailed();
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			output.SetFailed();
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			output.SetFailed();
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Login failed");
			output.SetFailed();
			yield break;
		}
		Debug.Log("WebLogin: " + text);
		if (((string)jsonData["userID"]).Length > 0)
		{
			int newUserID = int.Parse(string.Concat(jsonData["userID"], string.Empty));
			string sesK = string.Concat(jsonData["sessionKey"], string.Empty);
			string newUserName = string.Concat(jsonData["username"], string.Empty);
			string email = string.Concat(jsonData["email"], string.Empty);
			hasDoneTutorial = (string)jsonData["doneTutorial"] == "1";
			boughtGame = (string)jsonData["boughtGame"] == "1";
			int num = text.IndexOf("userSkinID");
			Debug.Log("Pos = " + num);
			if (num > 0)
			{
				userSkinID = string.Concat(jsonData["userSkinID"], string.Empty);
			}
			else
			{
				userSkinID = string.Empty;
			}
			LoggedIn(newUserID, newUserName, email, sesK);
			output.SetOutput("succes");
		}
		else
		{
			output.SetOutput("wrongpassword");
		}
	}

	public static IEnumerator AddFriend(int friendID, string friendName)
	{
		if (FriendListContains(friendName, pendingFriends))
		{
			yield break;
		}
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "addfriend");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("friendID", friendID);
		wWWForm.AddField("friendName", friendName);
		wWWForm.AddField("sessionKey", sessionKey);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		Debug.Log("AddFriend" + friendID + " " + friendName + " =" + text);
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			yield break;
		}
		GetMono().StartCoroutine(FetchFriends());
		GetMono().StartCoroutine(FetchFriendInvites());
		GetMono().StartCoroutine(FetchPendingFriends());
	}

	public static IEnumerator DeleteFriend(int friendID, string friendName)
	{
		if (friendName != string.Empty && !FriendListContains(friendName, friendInvites) && !FriendListContains(friendName, pendingFriends) && !FriendListContains(friendName, friendsList))
		{
			yield break;
		}
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "deletefriend");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("friendID", friendID);
		wWWForm.AddField("friendName", friendName);
		wWWForm.AddField("sessionKey", sessionKey);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		Debug.Log("DeleteFriend" + friendID + " " + friendName + " =" + text);
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			yield break;
		}
		GetMono().StartCoroutine(FetchFriends());
		GetMono().StartCoroutine(FetchFriendInvites());
		GetMono().StartCoroutine(FetchPendingFriends());
	}

	public static IEnumerator FetchFriends()
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "getfriends");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("sessionKey", sessionKey);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Fetch friends failed");
			yield break;
		}
		friendsList = new List<Friend>();
		JsonData jsonData2 = jsonData["Friends"];
		for (int i = 0; i < jsonData2.Count; i++)
		{
			JsonData jsonData3 = jsonData2[i];
			Friend item = new Friend
			{
				userID = int.Parse(string.Concat(jsonData3["friendID"], string.Empty)),
				name = string.Concat(jsonData3["username"], string.Empty),
				isOnline = ((string)jsonData3["isOnline"] == "1"),
				lastIP = string.Concat(jsonData3["lastServerIP"], string.Empty),
				lastPort = int.Parse(string.Concat(jsonData3["lastServerPort"], string.Empty)),
				lastLogin = string.Concat(jsonData3["lastLogin"], string.Empty)
			};
			friendsList.Add(item);
		}
		friendsList.Sort(new FriendsSorter());
	}

	public static IEnumerator FetchPendingFriends()
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "getpendingfriends");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("sessionKey", sessionKey);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Fetch friends failed");
			yield break;
		}
		pendingFriends = new List<Friend>();
		JsonData jsonData2 = jsonData["Friends"];
		for (int i = 0; i < jsonData2.Count; i++)
		{
			JsonData jsonData3 = jsonData2[i];
			Friend item = new Friend
			{
				userID = int.Parse(string.Concat(jsonData3["friendID"], string.Empty)),
				name = string.Concat(jsonData3["username"], string.Empty)
			};
			pendingFriends.Add(item);
		}
	}

	public static IEnumerator FetchFriendInvites()
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "getfriendinvites");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("sessionKey", sessionKey);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data" + text);
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Fetch friends failed");
			yield break;
		}
		friendInvites = new List<Friend>();
		JsonData jsonData2 = jsonData["Friends"];
		for (int i = 0; i < jsonData2.Count; i++)
		{
			JsonData jsonData3 = jsonData2[i];
			Friend item = new Friend
			{
				userID = int.Parse(string.Concat(jsonData3["friendID"], string.Empty)),
				name = string.Concat(jsonData3["username"], string.Empty)
			};
			friendInvites.Add(item);
		}
		int count = friendInvites.Count;
		if (count > 0)
		{
			Notifications.AddNotification("New friend requests", "You have " + count + " new friend request(s)!", NotificationTypes.NewFriendInvites);
		}
	}

	public static List<Friend> GetPendingFriends()
	{
		return pendingFriends;
	}

	public static List<Friend> GetFriendInvites()
	{
		return friendInvites;
	}

	public static List<Friend> GetFriends()
	{
		return friendsList;
	}

	public static bool HasFriend(string name)
	{
		return FriendListContains(name, friendsList);
	}

	public static bool FriendListContains(string name, List<Friend> fList)
	{
		foreach (Friend f in fList)
		{
			if (name == f.name)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerator Register(IENumeratorOutput output, string username, string email, string password)
	{
		if (password.Length < 5)
		{
			output.SetOutput("The password must be at least 5 characters long");
			yield break;
		}
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "register");
		wWWForm.AddField("username", username);
		wWWForm.AddField("email", email);
		wWWForm.AddField("pasW", Utils.Md5Sum(password.ToLower() + "mySEED"));
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			output.SetFailed();
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data");
			output.SetFailed();
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			output.SetFailed();
			yield break;
		}
		string text2 = text.Substring(1);
		Debug.Log(text2);
		JsonData jsonData = JsonMapper.ToObject(text2);
		if (jsonData == null)
		{
			Debug.LogError("Login failed");
			output.SetFailed();
			yield break;
		}
		string text3 = (string)jsonData["result"];
		if (text3 == string.Empty)
		{
			TextAsset textAsset = (TextAsset)Resources.Load("defaultWorld", typeof(TextAsset));
			if (PlayerPrefs.GetString("MyWorld", string.Empty) == string.Empty)
			{
				PlayerPrefs.SetString("MyWorld", textAsset.text);
			}
			int newUserID = int.Parse(string.Concat(jsonData["userID"], string.Empty));
			string sesK = string.Concat(jsonData["sessionKey"], string.Empty);
			hasDoneTutorial = false;
			LoggedIn(newUserID, username, email, sesK);
			output.SetOutput("succes");
		}
		else
		{
			output.SetOutput(text3);
		}
	}

	public static IEnumerator Ping(IENumeratorOutput output)
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("action", "ping");
		wWWForm.AddField("userID", userID);
		wWWForm.AddField("sessionKey", sessionKey);
		string value = string.Empty;
		string value2 = string.Empty;
		if (Network.isClient)
		{
			value = Network.connections[0].externalIP;
			value2 = Network.connections[0].externalPort + string.Empty;
		}
		else if (Network.isServer)
		{
			value2 = GameSettings.port + string.Empty;
			value = "localhost";
		}
		wWWForm.AddField("serverIP", value);
		wWWForm.AddField("serverPort", value2);
		WWW www = new WWW(wwwURL, wWWForm);
		yield return www;
		if (www.error != null)
		{
			Debug.LogError("ERROR: WWW call failed!");
			output.SetFailed();
			yield break;
		}
		string text = www.text;
		if (text == null || text == string.Empty)
		{
			Debug.LogError("ERROR: WWW had invalid return data");
			output.SetFailed();
			yield break;
		}
		if (text.Substring(0, 1) != "1")
		{
			Debug.LogError("ERROR: WWW had invalid return data; not 1" + text);
			output.SetFailed();
			yield break;
		}
		JsonData jsonData = JsonMapper.ToObject(text.Substring(1));
		if (jsonData == null)
		{
			Debug.LogError("Login failed");
			output.SetFailed();
		}
		else if (int.Parse(string.Concat(jsonData["result"], string.Empty)) != 1)
		{
			output.SetOutput(int.Parse(string.Concat(jsonData["result"], string.Empty)));
			output.SetFailed();
		}
	}

	public static IEnumerator Logout(IENumeratorOutput output)
	{
		Loggedout();
		yield break;
	}

	public static void GuestLoggedin(string usr)
	{
		isGuest = true;
		userID = 0;
		username = usr;
		userEmail = string.Empty;
		sessionKey = string.Empty;
		hasDoneTutorial = PlayerPrefs.GetInt("didTut_" + usr, 0) == 1;
		boughtGame = false;
	}

	public static void LoggedIn(int newUserID, string newUserName, string email, string sesK)
	{
		isGuest = false;
		userID = newUserID;
		username = newUserName;
		userEmail = email;
		sessionKey = sesK;
	}

	public static void Loggedout()
	{
		isGuest = false;
		userID = -1;
		username = (sessionKey = string.Empty);
	}

	public static string GetUsername()
	{
		return PlayerPrefs.GetString("username", "Player");
	}

	public static int GetUserID()
	{
		if (IsGuest())
		{
			Debug.LogWarning("GetUSERID on guest!");
			return 0;
		}
		return userID;
	}

	public static string GetUserSkinID()
	{
		return userSkinID;
	}

	public static string GetSessionKey()
	{
		if (IsGuest())
		{
			Debug.LogError("GetSessionKey on guest!");
			return "00000000000000000000";
		}
		return sessionKey;
	}

	public static string GetEmail()
	{
		if (IsGuest())
		{
			return "Guest";
		}
		return userEmail;
	}

	public static bool DidTutorial()
	{
		return hasDoneTutorial;
	}

	public static void JustFinishedTutorial()
	{
		hasDoneTutorial = true;
	}

	public static bool BoughtGame()
	{
		return boughtGame;
	}

	static AccountManager()
	{
		isGuest = false;
		boughtGame = true;
		hasDoneTutorial = true;
		friendsList = new List<Friend>();
		pendingFriends = new List<Friend>();
		friendInvites = new List<Friend>();
		wwwURL = "http://gamedata.cubelands.com/accounts.php";
	}
}
