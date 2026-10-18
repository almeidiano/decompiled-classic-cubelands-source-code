using System.Collections.Generic;

public class CubelandsHostDataSorter : IComparer<CubelandsHostData>
{
	private int sortType = 4;

	public CubelandsHostDataSorter(int sorting)
	{
		sortType = sorting;
	}

	public int Compare(CubelandsHostData a, CubelandsHostData b)
	{
		int num = 0;
		if (sortType == 1)
		{
			num = a.title.CompareTo(b.title);
		}
		if (sortType == 2)
		{
			num = b.connectedPlayers - a.connectedPlayers;
		}
		if (sortType == 3)
		{
			num = a.IP[0].CompareTo(b.IP[0]);
		}
		if (sortType == 4)
		{
			num = b.isDedicated.CompareTo(a.isDedicated);
		}
		if (sortType == 5)
		{
			num = a.useCustomTextures.CompareTo(b.useCustomTextures);
		}
		if (sortType == 6)
		{
			num = a.useBuilderList.CompareTo(b.useBuilderList);
		}
		if (num == 0)
		{
			num = b.connectedPlayers - a.connectedPlayers;
		}
		if (num == 0)
		{
			num = b.maxPlayers - a.maxPlayers;
		}
		if (num == 0)
		{
			num = a.title.CompareTo(b.title);
		}
		return num;
	}
}
