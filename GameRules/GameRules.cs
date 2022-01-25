using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public abstract partial class GameRules : Entity
	{
		public static GameRules Instance { get; set; }

		public GameRules()
		{
			Instance = this;
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

		public virtual GameSettings GameSettings => new()
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


		public virtual float GetGravityMultiplier()
		{
			return 1;
		}
	}

	public struct GameSettings
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
