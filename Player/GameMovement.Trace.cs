using Sandbox;
using System;

namespace Source1
{
	partial class S1GameMovement
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
		public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
		{
			return TraceBBox( start, end, GetPlayerMins(), GetPlayerMaxs(), liftFeet );
		}

		/// <summary>
		/// Traces the bbox and returns the trace result.
		/// LiftFeet will move the start position up by this amount, while keeping the top of the bbox at the same 
		/// position. This is good when tracing down because you won't be tracing through the ceiling above.
		/// </summary>
		public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
		{
			if ( liftFeet > 0 )
			{
				start += Vector3.Up * liftFeet;
				maxs = maxs.WithZ( maxs.z - liftFeet );
			}

			var tr = Trace.Ray( start + TraceOffset, end + TraceOffset )
						.Size( mins, maxs )
						.HitLayer( CollisionLayer.All, false )
						.HitLayer( CollisionLayer.Solid, true )
						.HitLayer( CollisionLayer.GRATE, true )
						.HitLayer( CollisionLayer.PLAYER_CLIP, true )
						.Ignore( Pawn )
						.Run();

			tr.EndPos -= TraceOffset;
			return tr;
		}
	}
}
