using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	/// <summary>
	/// Traces the current bbox and returns the result.
	/// liftFeet will move the start position up by this amount, while keeping the top of the bbox at the same
	/// position. This is good when tracing down because you won't be tracing through the ceiling above.
	/// </summary>
	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end )
	{
		return TraceBBox( start, end, GetPlayerMins(), GetPlayerMaxs() );
	}

	/// <summary>
	/// Traces the bbox and returns the trace result.
	/// LiftFeet will move the start position up by this amount, while keeping the top of the bbox at the same 
	/// position. This is good when tracing down because you won't be tracing through the ceiling above.
	/// </summary>
	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		if( r_debug_movement_trace )
			DebugOverlay.Box( end, mins, maxs, Host.IsServer ? Color.Blue : Color.Red, 0 );

		return SetupBBoxTrace( start, end, mins, maxs ).Run();
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end )
	{
		return SetupBBoxTrace( start, end, GetPlayerMins(), GetPlayerMaxs() );
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		return Trace.Ray( start, end )
			.Size( mins, maxs )
			.HitLayer( CollisionLayer.All, false )
			.HitLayer( CollisionLayer.Solid, true )
			.HitLayer( CollisionLayer.GRATE, true )
			.HitLayer( CollisionLayer.PLAYER_CLIP, true )
			.HitLayer( CollisionLayer.WINDOW, true )
			.HitLayer( CollisionLayer.SKY, true )
			.Ignore( Player );
	}

	[ConVar.Client] public static bool r_debug_movement_trace { get; set; }
}
