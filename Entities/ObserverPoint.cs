using Sandbox;

namespace TFS2
{
	/// <summary>
	/// Marks a camera location for players. Any spectating players (due to selecting the Spectator team or being unable to respawn at the time) have the ability to select this camera location to observe.
	/// </summary>
	[Library( "info_observer_point" )]
	public partial class ObserverPoint : PointCamera
	{
		[Property] public bool WelcomePoint { get; set; }
	}
}
