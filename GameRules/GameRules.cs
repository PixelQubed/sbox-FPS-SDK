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
		public virtual ViewVectors ViewVectors => DefaultViewVectors;

		public GameRules()
		{
			Instance = this;
		}

		static ViewVectors DefaultViewVectors = new()
		{
			ViewPosition = new( 0, 0, 64 ),

			HullMin = new( -16, -16, 0 ),
			HullMax = new( 16, 16, 72 ),

			DuckHullMin = new( -16, -16, 0 ),
			DuckHullMax = new( 16, 16, 36 ), 
			DuckViewPosition = new( 0, 0, 28 ),

			ObserverHullMin = new( -10, -10, -10 ),
			ObserverHullMax = new( 10, 10, 10 ),

			ObserverDeadViewPosition = new( 0, 0, 14 )
		};

		public virtual float GetGravityMultiplier()
		{
			return 1;
		}
	}
}
