using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	public class Border
	{
		public float minX;

		public float maxX;

		public float minY;

		public float maxY;
	}

	private WorldData worldData;

	public List<BlockData> inventoryItems = new List<BlockData>();

	private List<Rect> rectList = new List<Rect>();

	public static Inventory SP;

	public int selectedItem;

	public Texture2D selectedItemImage;

	private Vector2 inventoryOffset = Vector2.zero;

	private Texture2D dragTexture;

	private int lastDraggedItem = -1;

	private void Awake()
	{
		SP = this;
	}

	private void Start()
	{
		worldData = WorldData.SP;
	}

	public void LoadItems(Dictionary<int, BlockData> blockDataHashtable)
	{
		foreach (KeyValuePair<int, BlockData> item in blockDataHashtable)
		{
			inventoryItems.Add(item.Value);
		}
		for (int i = 0; i < inventoryItems.Count; i++)
		{
			int num = i / 10;
			int num2 = i % 10;
			rectList.Add(new Rect(num2 * 37, num * 37, 32f, 32f));
		}
	}

	public void ShowInventory(int windowID)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.MaxWidth(150f)))
		{
			GameMenu.SP.SwitchMenu(MenuStates.hide);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label("Inventory", "Label_Header");
		GUILayout.Label("You can reorder your inventory by dragging the items around.");
		int num = 0;
		GUI.BeginGroup(new Rect(25f, 120f, 390f, 200f));
		inventoryOffset = GUIUtility.GUIToScreenPoint(new Vector2(0f, 0f));
		foreach (BlockData inventoryItem in inventoryItems)
		{
			GUI.DrawTexture(rectList[num], GetImage(inventoryItem.blockDataID));
			if (num == selectedItem)
			{
				GUI.DrawTexture(rectList[num], selectedItemImage);
			}
			if (num <= 9)
			{
				GUI.Label(rectList[num], " " + (num + 1) % 10);
			}
			num++;
		}
		GUI.EndGroup();
		if (Input.GetMouseButtonDown(0))
		{
			MouseDown();
		}
		if (Input.GetMouseButtonUp(0))
		{
			MouseUp();
		}
		if ((bool)dragTexture)
		{
			Vector2 vector = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y));
			vector.x = Mathf.Clamp(vector.x, 0f, 368f);
			vector.y = Mathf.Clamp(vector.y, 0f, 228f);
			GUI.DrawTexture(new Rect(vector.x, vector.y, 32f, 32f), dragTexture);
		}
	}

	private void MouseUp()
	{
		if ((bool)dragTexture)
		{
			int inventoryItem = GetInventoryItem(Input.mousePosition);
			if (inventoryItem != -1)
			{
				BlockData value = inventoryItems[lastDraggedItem];
				BlockData value2 = inventoryItems[inventoryItem];
				inventoryItems[inventoryItem] = value;
				inventoryItems[lastDraggedItem] = value2;
				ChangedInventoryItem(inventoryItem);
				ChangedInventoryItem(lastDraggedItem);
				PlayerBuilding.SP.SetBlock(lastDraggedItem, realNumber: false);
			}
			dragTexture = null;
		}
	}

	private void MouseDown()
	{
		if (!dragTexture)
		{
			int inventoryItem = GetInventoryItem(Input.mousePosition);
			if (inventoryItem != -1)
			{
				dragTexture = GetImage(inventoryItems[inventoryItem].blockDataID);
				lastDraggedItem = inventoryItem;
			}
		}
	}

	private int GetInventoryItem(Vector3 pos)
	{
		Vector2 point = new Vector3(pos.x, (float)Screen.height - pos.y);
		int num = 0;
		foreach (Rect rect in rectList)
		{
			if (new Rect(rect.xMin + inventoryOffset.x, rect.yMin + inventoryOffset.y, rect.width, rect.height).Contains(point))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	private Texture2D GetImage(int ID)
	{
		return worldData.GetImageByNR(ID);
	}

	private void ChangedInventoryItem(int ID)
	{
		if (ID == selectedItem)
		{
			PlayerBuilding.SP.SetBlock(ID, realNumber: false);
		}
	}
}
