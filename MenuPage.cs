using System;
using UnityEngine;

public class MenuPage : MonoBehaviour
{
	public delegate void GUIMethod();

	[NonSerialized]
	public MainMenu mainMenu;

	public GUIMethod currentGUIMethod;

	public void Start()
	{
		mainMenu = MainMenu.SP;
	}

	public virtual void EnableMenu()
	{
	}

	public virtual void DisableMenu()
	{
	}

	public void SetGUIMethod(GUIMethod newMethod)
	{
		currentGUIMethod = newMethod;
	}

	public virtual void ShowGUI()
	{
	}
}
