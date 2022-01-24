using Sandbox;
using System;

namespace Source1
{
	partial class S1GameMovement
	{
		public virtual Vector3 GetPlayerMins( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMin
				: GameRules.Instance.ViewVectors.HullMin
				) * Pawn.Scale;
		}

		public virtual Vector3 GetPlayerMaxs( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckHullMax
				: GameRules.Instance.ViewVectors.HullMax
				) * Pawn.Scale;
		}

		public virtual Vector3 GetPlayerMins()
		{
			return GetPlayerMins( false );
		}

		public virtual Vector3 GetPlayerMaxs()
		{
			return GetPlayerMaxs( false );
		}

		public virtual Vector3 GetPlayerExtents()
		{
			var mins = GetPlayerMins();
			var maxs = GetPlayerMaxs();

			return new(
				MathF.Abs( mins.x ) + MathF.Abs( maxs.x ),
				MathF.Abs( mins.y ) + MathF.Abs( maxs.y ),
				MathF.Abs( mins.z ) + MathF.Abs( maxs.z )
			);
		}

		public virtual Vector3 GetViewPosition( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckViewPosition
				: GameRules.Instance.ViewVectors.ViewPosition
				) * Pawn.Scale;
		}

		public virtual Vector3 GetViewPosition()
		{
			return GetViewPosition( false );
		}
	}
}
