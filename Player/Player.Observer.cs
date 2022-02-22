using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	partial class Source1Player
	{
		[Net] public ObserverMode ObserverMode { get; private set; }
		public ObserverMode LastObserverMode { get; set; }
		public bool IsObserver => ObserverMode != ObserverMode.None;
		[Net] public Entity ObserverTarget { get; set; }


		public void StopObserverMode()
		{
			Log.Info( "Source1Player.StopObserverMode()" );

			if ( ObserverMode == ObserverMode.None )
				return;

			LastObserverMode = ObserverMode;
			ObserverMode = ObserverMode.None;
		}

		public bool StartObserverMode( ObserverMode mode )
		{
			Log.Info( $"Source1Player.StartObserverMode( {mode} )" );

			UsePhysicsCollision = false;

			SetObserverMode( mode );
			EnableDrawing = false;

			Health = 1;
			LifeState = LifeState.Dead;

			return true;
		}

		public void SetObserverMode( ObserverMode mode )
		{
			Log.Info( $"Source1Player.StartObserverMode( {mode} )" );
			ObserverMode = mode;
			switch ( mode )
			{
				case ObserverMode.None:
				case ObserverMode.Fixed:
				case ObserverMode.Deathcam:
					Log.Info( $"SetObserverMode - Entered static mode" );
					MoveType = MoveType.None;
					break;

				case ObserverMode.Chase:
				case ObserverMode.InEye:
					Log.Info( $"SetObserverMode - Entered target follow mode" );
					MoveType = MoveType.MOVETYPE_OBSERVER;
					break;

				case ObserverMode.Roaming:
					Log.Info( $"SetObserverMode - Entered roaming mode" );
					MoveType = MoveType.MOVETYPE_OBSERVER;
					break;

			}
		}

		public virtual float DeathAnimationTime => 3;

		public virtual IEnumerable<Entity> FindObserverTargetCandiates()
		{
			return All.OfType<Source1Player>();
		}


		public virtual bool IsValidObserverTarget( Entity target )
		{
			if ( target == null ) 
				return false;

			// We can't observe ourselves.
			if ( target == this )
				return false; 

			// don't watch invisible players
			if ( !target.EnableDrawing ) 
				return false;

			// target is dead, waiting for respawn
			if ( target.LifeState == LifeState.Respawnable )
				return false;

			if ( target is Source1Player player )
			{
				if ( target.LifeState == LifeState.Dead || target.LifeState == LifeState.Dying )
				{
					// allow watching until 3 seconds after death to see death animation
					if ( TimeSinceDeath > DeathAnimationTime ) 
						return false;   
				}
			}

			return true;
		}
	}

	public enum ObserverMode
	{
		/// <summary>
		/// Not in spectator mode
		/// </summary>
		None,
		/// <summary>
		/// Special mode for death cam animation
		/// </summary>
		Deathcam,
		/// <summary>
		/// Zooms to a target, and freeze-frames on them
		/// </summary>
		Freezecam,
		/// <summary>
		/// Ziew from a fixed camera position
		/// </summary>
		Fixed,
		/// <summary>
		/// Follow a player in first person view
		/// </summary>
		InEye,
		/// <summary>
		/// Follow a player in third person view
		/// </summary>
		Chase,
		/// <summary>
		/// Free roaming
		/// </summary>
		Roaming
	}

}
