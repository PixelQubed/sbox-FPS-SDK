using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	partial class Source1Player
	{
		[Net] public ObserverMode ObserverMode { get; private set; }
		[Net] public Entity ObserverTarget { get; private set; }
		public ObserverMode LastObserverMode { get; set; }
		public bool IsForcedObserverMode { get; private set; }

		/// <summary>
		/// The player is currently using the spectator mode to observe the map.
		/// </summary>
		public bool IsSpectating => ObserverMode >= ObserverMode.Fixed;
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
			var mode = ObserverMode + 1;
			var count = Enum.GetValues( typeof( ObserverMode ) ).Length;

			if ( (int)mode >= count )
			{
				mode = ObserverMode.InEye;
			}
			else if ( mode < ObserverMode.InEye )
			{
				mode = ObserverMode.Roaming;
			}

			if ( ObserverMode > ObserverMode.Deathcam )
			{
				SetObserverMode( mode );
			} else
			{
				LastObserverMode = mode;
			}
		}

		public void CheckObserverSettings()
		{
			if( IsForcedObserverMode )
			{
				var target = ObserverTarget;

				if ( !IsValidObserverTarget( target ) )
				{
					target = FindNextObserverTarget( false );
				}

				if ( target != null )
				{
					IsForcedObserverMode = false;
					SetObserverMode( LastObserverMode );
					SetObserverTarget( target );
				}

				return;
			}

			if ( LastObserverMode < ObserverMode.Fixed )
				LastObserverMode = ObserverMode.Roaming;

			if ( ObserverMode >= ObserverMode.InEye )
				CheckObserverTarget();
		}

		public void CheckObserverTarget()
		{
			if ( !IsValidObserverTarget( ObserverTarget ) )
			{
				var target = FindNextObserverTarget( false );
				if ( target != null )
				{
					SetObserverTarget( target );
				} else
				{
					ForceObserverMode( ObserverMode.Fixed );
					ObserverTarget = null;
				}
			}
		}

		public virtual Entity FindNextObserverTarget( bool reverse )
		{
			var ents = FindObserverableEntities().ToList();
			var count = ents.Count;

			// There's nothing to spectate.
			if ( count == 0 ) return null;
			var index = ents.IndexOf( ObserverTarget ); ;
			var delta = reverse ? -1 : 1;

			for ( int i = 0; i < count; i++ )
			{
				index += delta;

				// Put slot on the other side of the list if we overflow the list.
				if ( index >= count ) index = 0;
				else if ( index < 0 ) index = count - 1;

				var target = ents[index];

				if ( !IsValidObserverTarget( target ) ) 
					continue;

				Log.Info( $"target: {target}" );
				return target;
			}

			return null;
		}

		public void ForceObserverMode( ObserverMode mode )
		{
			var tempMode = ObserverMode.Roaming;

			if ( ObserverMode == mode )
				return;

			if ( IsForcedObserverMode ) 
				tempMode = LastObserverMode;

			SetObserverMode( mode );

			if ( IsForcedObserverMode )
				LastObserverMode = tempMode;

			IsForcedObserverMode = true;
		}

		public void StopObserverMode()
		{
			IsForcedObserverMode = false;
			if ( ObserverMode == ObserverMode.None )
				return;

			if ( ObserverMode > ObserverMode.Deathcam ) 
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

			if ( target is Source1Player player )
			{
				if ( !player.IsAlive )
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
