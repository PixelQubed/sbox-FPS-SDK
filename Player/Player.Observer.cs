using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	partial class Source1Player
	{
		[Net] public ObserverMode ObserverMode { get; private set; }
		[Net] public ObserverMode LastObserverMode { get; set; }
		[Net] public Entity ObserverTarget { get; private set; }

		/// <summary>
		/// The player is currently using the spectator mode to observe the map.
		/// </summary>
		public bool IsSpectating => ObserverMode >= ObserverMode.InEye;
		/// <summary>
		/// The player is currently observing something, might possibly be deathcam or freezecam.
		/// </summary>
		public bool IsObserver => ObserverMode != ObserverMode.None;

		public void SimulateObserver()
		{
			if ( IsServer )
			{
				if ( IsSpectating )
				{
					if ( Input.Pressed( InputButton.Jump ) )
						NextObserverMode();

					// fimd
					if ( Input.Pressed( InputButton.Attack1 ) )
					{
						var target = FindNextObserverTarget( false );
						if ( target != null ) SetObserverTarget( target );
					}

					if ( Input.Pressed( InputButton.Attack2 ) )
					{
						var target = FindNextObserverTarget( true );
						if ( target != null ) SetObserverTarget( target );
					}

					CheckObserverSettings();
				}
			}
		}

		public virtual bool SetObserverTarget( Entity target )
		{
			if ( !IsValidObserverTarget( target ) )
				return false;

			ObserverTarget = target;

			if ( ObserverMode == ObserverMode.Roaming ) 
			{
				var start = target.EyePosition;
				var dir = target.EyeRotation.Forward.WithZ( 0 );
				var end = start + dir * -64;

				var tr = Trace.Ray( start, end )
					.Size( GetPlayerMins( false ), GetPlayerMaxs( false ) )
					.HitLayer( CollisionLayer.Solid, true )
					.Run();

				Position = tr.EndPosition;
				Rotation = target.EyeRotation;
				Velocity = 0;
			}

			return true;
		}

		public void NextObserverMode()
		{
			if ( ObserverMode < ObserverMode.InEye )
				return;

			switch( ObserverMode )
			{
				case ObserverMode.InEye:
					SetObserverMode( ObserverMode.Chase );
					break;

				case ObserverMode.Chase:
					SetObserverMode( ObserverMode.Roaming );
					break;

				case ObserverMode.Roaming:
					SetObserverMode( ObserverMode.InEye );
					break;
			}
		}

		public void CheckObserverSettings()
		{
			if ( LastObserverMode < ObserverMode.Fixed )
				LastObserverMode = ObserverMode.Roaming;

			if ( ObserverMode >= ObserverMode.InEye )
			{
				ValidateCurrentObserverTarget();

				var target = ObserverTarget as Source1Player;

				if ( target != null && ObserverMode == ObserverMode.InEye )
				{
					// copy flags?
				}
			}
		}

		public void ValidateCurrentObserverTarget()
		{
			// Log.Info( $"::ValidateCurrentObserverTarget()" );
			if ( !IsValidObserverTarget( ObserverTarget ) )
			{
				var target = FindNextObserverTarget( false );
				if ( target != null )
				{
					SetObserverTarget( target );
				} else
				{
					SetObserverMode( ObserverMode.Fixed );
					ObserverTarget = null;
				}
			}
		}

		public virtual Entity FindNextObserverTarget( bool reverse )
		{
			var ents = FindObserverableEntities().ToList();

			// There's nothing to spectate.
			if ( ents.Count == 0 ) return null;

			var max = ents.Count - 1;
			var startIndex = ents.IndexOf( ObserverTarget );
			var index = startIndex;

			var delta = reverse ? -1 : 1;

			do
			{
				index += delta;

				if ( index > max )
					index = 0;
				else if ( index < 0 )
					index = max;

				var target = ents[index];

				if ( IsValidObserverTarget( target ) )
					return target;

			} while ( index != startIndex );

			return null;
		}

		public void StopObserverMode()
		{
			if ( ObserverMode == ObserverMode.None )
				return;

			LastObserverMode = ObserverMode;
			ObserverMode = ObserverMode.None;
		}

		public bool StartObserverMode( ObserverMode mode )
		{
			UsePhysicsCollision = false;

			SetObserverMode( mode );
			EnableDrawing = false;

			Health = 1;
			LifeState = LifeState.Dead;

			return true;
		}

		public void SetObserverMode( ObserverMode mode )
		{
			// skip roaming for dead players
			if ( TeamManager.IsPlayable( TeamNumber ) )
			{
				if ( !IsAlive && mode == ObserverMode.Roaming )
					mode = ObserverMode.Chase;
			}

			if ( ObserverMode > ObserverMode.Deathcam )
				LastObserverMode = ObserverMode;
			
			ObserverMode = mode;

			switch ( mode )
			{
				case ObserverMode.None:
				case ObserverMode.Fixed:
				case ObserverMode.Deathcam:	
					MoveType = MoveType.None;
					break;

				case ObserverMode.Chase:
				case ObserverMode.InEye:
				case ObserverMode.Roaming:
					MoveType = MoveType.MOVETYPE_OBSERVER;
					break;

			}

			CheckObserverSettings();
		}

		public virtual float DeathAnimationTime => 3;

		public virtual IEnumerable<Entity> FindObserverableEntities()
		{
			return All.OfType<Source1Player>().Where( x => x.IsAlive );
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

				if ( TeamManager.IsPlayable( TeamNumber ) )
				{
					if ( player.TeamNumber != TeamNumber )
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
		/// View from a fixed camera position
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
