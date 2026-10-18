using System.Collections;
using UnityEngine;

public class OptionsMenu : MenuPage
{
	public static OptionsMenu SP;

	private Rect windowRect;

	private string usernameField;

	private void Awake()
	{
		SP = this;
		SetNewWindowHeight(570);
		StartCoroutine(MyStart());
	}

	private IEnumerator MyStart()
	{
		yield return 0;
		yield return 0;
		LoadOptions();
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
		int num = 460;
		Vector3 vector = new Vector3((float)Screen.width / 2f - (float)num / 2f, (float)Screen.height / 2f - (float)newHeight / 2f, 0f);
		windowRect = new Rect(vector.x, vector.y, num, newHeight);
	}

	private void LoadOptions()
	{
		Options.LoadOptions();
		usernameField = PlayerPrefs.GetString("username", "Player");
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
		GUILayout.Label("Options", "Label_Header");
		GUILayout.Label("Username:", "Label_Header2");
		usernameField = GUILayout.TextField(usernameField, GUILayout.Width(200f));
		if (GUILayout.Button("Save Username", GUILayout.Width(120f)))
		{
			PlayerPrefs.SetString("username", usernameField);
		}
		GUILayout.Space(20f);
		Options.ShowGUI();
	}
}
