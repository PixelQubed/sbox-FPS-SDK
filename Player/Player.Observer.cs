using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Source1
{
	partial class Source1Player
	{
		[Net] public ObserverMode ObserverMode { get; private set; }
		[Net] public bool IsForcedObserverMode { get; set; }
		[Net] public Entity ObserverTarget { get; set; }

		ObserverMode LastObserverMode { get; set; }
		public bool IsObserver => ObserverMode != ObserverMode.None;

		public void StopObserverMode()
		{
			Log.Info( "Source1Player.StopObserverMode()" );
			IsForcedObserverMode = false;

			if ( ObserverMode == ObserverMode.None )
				return;

			if ( ObserverMode > ObserverMode.Deathcam )
			{
				LastObserverMode = ObserverMode;
			}

			ObserverMode = ObserverMode.None;
		}

		bool StartObserverMode( ObserverMode mode )
		{
			Log.Info( "Source1Player.StartObserverMode()" );
			if ( !IsObserver )
			{
				// set position to last view offset
				Position = EyePosition;
				EyeLocalPosition = 0;
			}

			if ( ActiveWeapon != null )
				ActiveWeapon.Holster();

			GroundEntity = null;
			Tags.Remove( PlayerTags.Ducked );
			UsePhysicsCollision = false;

			SetObserverMode( mode );
			EnableDrawing = false;

			Health = 1;
			LifeState = LifeState.Dead;

			return true;
		}

		public void SetObserverMode( ObserverMode mode )
		{
			Log.Info( $"Source1Player.SetObserverMode( {mode} )" );
			if ( mode > ObserverMode.Fixed && TeamManager.IsPlayable( TeamNumber ) )
			{
				switch( mp_forcecamera )
				{
					case ObserverRestriction.All: 
						break;

					case ObserverRestriction.Team:
						mode = ObserverMode.InEye;
						break;

					case ObserverRestriction.None:
						mode = ObserverMode.Fixed;
						break;
				}
			}

			if ( ObserverMode > ObserverMode.Deathcam )
			{
				// remember mode if we were really spectating before
				LastObserverMode = ObserverMode;
			}

			ObserverMode = mode;

			switch(mode)
			{
				case ObserverMode.None:
				case ObserverMode.Fixed:
				case ObserverMode.Deathcam:
					// SetFOV(this, 0);
					// SetViewOffset(0);
					MoveType = MoveType.None;
					break;

				case ObserverMode.Chase:
				case ObserverMode.InEye:
					// SetObserverTarget( ObserverTarget );
					MoveType = MoveType.MOVETYPE_OBSERVER;
					break;

				case ObserverMode.Roaming:
				case ObserverMode.Freezecam:
					// SetFOV(this, 0);
					// SetObserverTarget( ObserverTarget );
					// SetViewOffset(0);
					MoveType = MoveType.MOVETYPE_OBSERVER;
					break;

			}
		}

		public void CheckObserverSettings()
		{
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

			// check forcecamera settings for active players
			if ( !TeamManager.IsPlayable( TeamNumber ) ) 
			{
				switch ( mp_forcecamera )
				{
					case ObserverRestriction.All: 
						break;

					case ObserverRestriction.Team:
						if ( !ITeam.IsSame( this, target ) )
							return false;

						break;

					case ObserverRestriction.None: 
						return false;
				}
			}

			return true;
		}

		[ConVar.Replicated] public static ObserverRestriction mp_forcecamera { get; set; }
	}


	public enum ObserverRestriction
	{
		/// <summary>
		/// Allow all modes, all targets
		/// </summary>
		All,
		/// <summary>
		/// Allow only own team and first person, no PIP
		/// </summary>
		Team,
		/// <summary>
		/// Don't allow any spectating after death (fixed & fade to black)
		/// </summary>
		None
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
