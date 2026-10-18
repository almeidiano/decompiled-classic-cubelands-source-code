using System.Collections.Generic;
using UnityEngine;

public class FriendsMenu : MenuPage
{
	public static FriendsMenu SP;

	private Rect windowRect;

	private static Vector2 scrollPos = Vector2.zero;

	private static string addName = string.Empty;

	private void Awake()
	{
		SP = this;
		SetNewWindowHeight(400);
	}

	public override void EnableMenu()
	{
	}

	public override void DisableMenu()
	{
	}

	public override void ShowGUI()
	{
		windowRect = GUI.Window(21, windowRect, MenuMain, string.Empty);
	}

	private void SetNewWindowHeight(int newHeight)
	{
		int num = 400;
		Vector3 vector = new Vector3(Screen.width / 2 - num / 2, Screen.height / 2 - newHeight / 2, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	private void MenuMain(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Back", GUILayout.MaxWidth(150f)))
		{
			mainMenu.ShowMain();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		FriendContent();
	}

	public static void FriendContent()
	{
		List<Friend> friendInvites = AccountManager.GetFriendInvites();
		if (friendInvites.Count > 0)
		{
			GUILayout.Label("New friend request(s):", "Label_Header");
			foreach (Friend item in friendInvites)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(item.name, GUILayout.MinWidth(80f));
				if (GUILayout.Button("Add", GUILayout.MaxWidth(65f)))
				{
					AccountManager.GetMono().StartCoroutine(AccountManager.AddFriend(item.userID, item.name));
				}
				if (GUILayout.Button("Ignore", GUILayout.MaxWidth(65f)))
				{
					AccountManager.GetMono().StartCoroutine(AccountManager.DeleteFriend(item.userID, item.name));
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.Space(10f);
		}
		GUILayout.Label("Friends", "Label_Header");
		List<Friend> friends = AccountManager.GetFriends();
		if (friends.Count > 0)
		{
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			foreach (Friend item2 in friends)
			{
				GUILayout.BeginHorizontal();
				if (item2.isOnline)
				{
					GUI.color = Color.green;
				}
				else
				{
					GUI.color = Color.red;
				}
				GUILayout.Label(item2.name, GUILayout.MinWidth(80f));
				GUI.color = Color.white;
				if (!item2.isOnline)
				{
					GUILayout.Label("Last online: " + item2.lastLogin);
				}
				else if (item2.lastIP != string.Empty)
				{
					GUILayout.Label(item2.lastIP + ":" + item2.lastPort);
					if (GUILayout.Button("Connect", GUILayout.MaxWidth(65f)))
					{
						if (Application.loadedLevel == 1)
						{
							MainMenu.SP.DoConnect(item2.lastIP, item2.lastPort);
						}
						else
						{
							PlayerPrefs.SetString("connectIP", item2.lastIP);
							PlayerPrefs.SetInt("connectPort", item2.lastPort);
							GameManager.SP.StartCoroutine(GameManager.SP.QuitGame());
						}
					}
				}
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("X", GUILayout.MaxWidth(25f)))
				{
					AccountManager.GetMono().StartCoroutine(AccountManager.DeleteFriend(item2.userID, item2.name));
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
		}
		else
		{
			GUILayout.Label("You have added no friends");
		}
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal();
		addName = GUILayout.TextField(addName, GUILayout.MaxWidth(80f));
		if (GUILayout.Button("Add", GUILayout.MaxWidth(65f)) && addName.Length > 1)
		{
			AccountManager.GetMono().StartCoroutine(AccountManager.AddFriend(0, addName));
			addName = string.Empty;
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		List<Friend> pendingFriends = AccountManager.GetPendingFriends();
		if (pendingFriends.Count <= 0)
		{
			return;
		}
		GUILayout.Label("Sent requests:", "Label_Header2");
		foreach (Friend item3 in pendingFriends)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(item3.name, GUILayout.MinWidth(80f));
			if (GUILayout.Button("X", GUILayout.MaxWidth(65f)))
			{
				AccountManager.GetMono().StartCoroutine(AccountManager.DeleteFriend(item3.userID, item3.name));
			}
			GUILayout.EndHorizontal();
		}
	}
}
