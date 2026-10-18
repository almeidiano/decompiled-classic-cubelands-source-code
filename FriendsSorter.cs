using System.Collections.Generic;

public class FriendsSorter : IComparer<Friend>
{
	public int Compare(Friend a, Friend b)
	{
		int num = 0;
		if (a.isOnline && !b.isOnline)
		{
			return 1;
		}
		if (num == 0)
		{
			num = b.lastLogin.CompareTo(a.lastLogin);
		}
		if (num == 0)
		{
			num = a.name.CompareTo(b.name);
		}
		return num;
	}
}
