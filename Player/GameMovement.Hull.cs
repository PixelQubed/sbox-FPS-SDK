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
			return GetPlayerMins( IsDucked );
		}

		public virtual Vector3 GetPlayerMaxs()
		{
			return GetPlayerMaxs( IsDucked );
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

		public virtual Vector3 GetPlayerViewOffset( bool ducked )
		{
			return (ducked
				? GameRules.Instance.ViewVectors.DuckViewOffset
				: GameRules.Instance.ViewVectors.ViewOffset
				) * Pawn.Scale;
		}

		public virtual void SetDuckedEyeOffset( float duckFraction )
		{
			Vector3 vDuckHullMin = GetPlayerMins( true );
			Vector3 vStandHullMin = GetPlayerMins( false );

			float fMore = vDuckHullMin.z - vStandHullMin.z;

			Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
			Vector3 vecStandViewOffset = GetPlayerViewOffset( false );
			Vector3 temp = EyePosLocal;

			temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction) +
						(vecStandViewOffset.z * (1 - duckFraction));
			EyePosLocal = temp;
		}
	}
}
