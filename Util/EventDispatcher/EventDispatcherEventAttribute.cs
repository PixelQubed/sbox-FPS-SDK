using Sandbox;

namespace Amper.Source1;

public class EventDispatcherEventAttribute : LibraryAttribute
{
	public DispatchType dispatchTypes { get; private set; }

	public EventDispatcherEventAttribute()
	{
		dispatchTypes = DispatchType.Client | DispatchType.Server;
	}

	public EventDispatcherEventAttribute( params DispatchType[] types)
	{
		foreach(var type in types)
		{
			dispatchTypes |= type;
		}
	}
}

