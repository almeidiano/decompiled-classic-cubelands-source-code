using UnityEngine;

public class IENumeratorOutput
{
	private object output;

	private bool isDone;

	private bool failed;

	public void SetOutput(object obj)
	{
		output = obj;
		isDone = true;
	}

	public object GetOutput()
	{
		if (!IsDone())
		{
			Debug.LogError("ScheduledWWWRequests: GetOuput but is not done yet!");
		}
		return output;
	}

	public bool IsDone()
	{
		return isDone;
	}

	public void SetFailed()
	{
		failed = true;
	}

	public bool Failed()
	{
		return failed;
	}
}
