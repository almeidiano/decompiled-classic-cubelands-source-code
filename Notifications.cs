using System.Collections.Generic;
using UnityEngine;

public class Notifications : MonoBehaviour
{
	public class Notification
	{
		public string title;

		public string description;

		public NotificationTypes type;
	}

	public static Notifications SP;

	public GUISkin skin;

	public AudioClip newNotification;

	public Texture2D trophyImage;

	public Texture2D charIcon;

	private static List<Notification> notList = new List<Notification>();

	private int boxWidth;

	private Notification currentNot;

	private float notXPos;

	private float waitUntill;

	private void Awake()
	{
		SP = this;
		boxWidth = 350;
		if (!DedicatedServer.isDedicated)
		{
			InvokeRepeating("CheckStatUpload", 30f, 300f);
		}
	}

	private void CheckStatUpload()
	{
		StartCoroutine(Statistics.UploadStats());
	}

	private void Update()
	{
		if (notList.Count <= 0)
		{
			return;
		}
		if (currentNot != null)
		{
			if (waitUntill == 0f && notXPos < 0f)
			{
				notXPos += Time.deltaTime * (float)boxWidth * 2f;
				notXPos = Mathf.Clamp(notXPos, -boxWidth, 0f);
				return;
			}
			if (waitUntill == 0f)
			{
				waitUntill = Time.realtimeSinceStartup + 3f;
			}
			if (waitUntill < Time.realtimeSinceStartup)
			{
				notXPos -= Time.deltaTime * (float)boxWidth * 2f;
				notXPos = Mathf.Clamp(notXPos, -boxWidth, 0f);
				if (notXPos <= (float)(-boxWidth))
				{
					StopNotification();
				}
			}
		}
		else
		{
			StartNewNotification();
		}
	}

	private void StopNotification()
	{
		notList.Remove(currentNot);
		currentNot = null;
	}

	private void StartNewNotification()
	{
		currentNot = notList[0];
		base.audio.PlayOneShot(newNotification, 1f);
		notXPos = -boxWidth;
		waitUntill = 0f;
	}

	public static void AddNotification(string title, string description, NotificationTypes notType)
	{
		Notification notification = new Notification();
		notification.title = title;
		notification.description = description;
		notification.type = notType;
		notList.Add(notification);
	}

	private void OnGUI()
	{
		if (currentNot != null)
		{
			GUI.depth = -100;
			GUI.skin = skin;
			GUI.Box(new Rect(notXPos, 5f, boxWidth, 80f), string.Empty);
			GUILayout.BeginArea(new Rect(notXPos, 5f, boxWidth, 80f));
			GUILayout.BeginHorizontal();
			if (currentNot.type == NotificationTypes.Achievement)
			{
				GUILayout.Label(trophyImage);
			}
			else
			{
				GUILayout.Label(charIcon);
			}
			GUILayout.BeginVertical();
			GUILayout.Space(5f);
			GUILayout.Label(currentNot.title, "Label_Header2");
			GUILayout.Label(currentNot.description);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}
}
