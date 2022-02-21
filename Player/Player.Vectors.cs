using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player
	{
		public virtual Vector3 GetPlayerMins()
		{
			return GetPlayerMins( IsDucked );
		}

		public virtual Vector3 GetPlayerMaxs()
		{
			return GetPlayerMaxs( IsDucked );
		}

		public virtual Vector3 GetPlayerMins( bool ducked )
		{
			return (ducked ? ViewVectors.DuckHullMin : ViewVectors.HullMin);
		}

		public virtual Vector3 GetPlayerMaxs( bool ducked )
		{
			return (ducked ? ViewVectors.DuckHullMax : ViewVectors.HullMax);
		}

		public virtual Vector3 GetPlayerExtents( bool ducked )
		{
			var mins = GetPlayerMins( ducked );
			var maxs = GetPlayerMaxs( ducked );

			return mins.Abs() + maxs.Abs();
		}

		public virtual Vector3 GetPlayerViewOffset( bool ducked )
		{
			return (ducked ? ViewVectors.DuckViewOffset : ViewVectors.ViewOffset) * Scale;
		}

		public virtual ViewVectors ViewVectors => new()
		{
			ViewOffset = new( 0, 0, 64 ),

			HullMin = new( -16, -16, 0 ),
			HullMax = new( 16, 16, 72 ),

			DuckHullMin = new( -16, -16, 0 ),
			DuckHullMax = new( 16, 16, 36 ),
			DuckViewOffset = new( 0, 0, 28 ),

			ObserverHullMin = new( -10, -10, -10 ),
			ObserverHullMax = new( 10, 10, 10 ),

			ObserverDeadViewPosition = new( 0, 0, 14 )
		};
	}

	public struct ViewVectors
	{
		public Vector3 ViewOffset { get; set; }

		public Vector3 HullMin { get; set; }
		public Vector3 HullMax { get; set; }

		public Vector3 DuckHullMin { get; set; }
		public Vector3 DuckHullMax { get; set; }
		public Vector3 DuckViewOffset { get; set; }

		public Vector3 ObserverHullMax { get; set; }
		public Vector3 ObserverHullMin { get; set; }

		public Vector3 ObserverDeadViewPosition { get; set; }
	}
}
