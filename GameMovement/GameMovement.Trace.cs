using Sandbox;
using System;

namespace Source1
{
	partial class Source1GameMovement
	{
		/// <summary>
		/// Any bbox traces we do will be offset by this amount.
		/// todo: this needs to be predicted
		/// </summary>
		public Vector3 TraceOffset { get; set; }

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
			var tr = SetupBBoxTrace( start + TraceOffset, end + TraceOffset, mins, maxs ).Run();
			tr.EndPos -= TraceOffset;
			return tr;
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
				.Ignore( Pawn );
		}
	}
}
