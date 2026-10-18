using UnityEngine;

public class CreditsMenu : MenuPage
{
	public static CreditsMenu SP;

	private Rect windowRect;

	private void Awake()
	{
		SP = this;
		SetNewWindowHeight(250);
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
		GUILayout.Label("Credits", "Label_Header");
		GUILayout.Label("Cubeland is being developed by the indie game studio M2H.\n\n For more information see: http://www.Cubelands.com");
		GUILayout.Label("Game version: " + Utils.GetGameVersion());
	}
}
