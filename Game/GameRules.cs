using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	public partial class GameRules : Game
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

		public virtual float GetGravityMultiplier()
		{
			return 1;
		}

		public virtual float GetDamageMultiplier()
		{
			return 1;
		}

		public virtual bool AllowThirdPersonCamera()
		{
			return false;
		}

		public virtual void RadiusDamage( DamageInfo info, Vector3 src, float radius, Entity ignore )
		{
			// TODO
		}

		public virtual void PlayerRespawn( Source1Player player )
		{

		}

		public virtual void CanPlayerRespawn( Source1Player player )
		{

		}

		public virtual void GetPlayerRespawnTime( Source1Player player )
		{

		}

		public virtual void PlayerKilled( Source1Player player, DamageInfo info )
		{

		}

		public virtual void IsSpawnPointValid( Entity point, Source1Player player )
		{

		}

		public virtual void CreateStandardEntities()
		{

		}
	}
}
