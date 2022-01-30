using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player
	{
		public virtual Vector3 GetPlayerMins( bool ducked )
		{
			var viewvectors = GameRules.Instance.ViewVectors;
			return (ducked ? viewvectors.DuckHullMin : viewvectors.HullMin);
		}

		public virtual Vector3 GetPlayerMaxs( bool ducked )
		{
			var viewvectors = GameRules.Instance.ViewVectors;
			return (ducked ? viewvectors.DuckHullMax : viewvectors.HullMax);
		}

		public virtual Vector3 GetPlayerExtents( bool ducked )
		{
			var mins = GetPlayerMins( ducked );
			var maxs = GetPlayerMaxs( ducked );

			return mins.Abs() + maxs.Abs();
		}

		public virtual Vector3 GetPlayerViewOffset( bool ducked )
		{
			var viewvectors = GameRules.Instance.ViewVectors;
			return (ducked ? viewvectors.DuckViewOffset : viewvectors.ViewOffset) * Scale;
		}
	}
}
