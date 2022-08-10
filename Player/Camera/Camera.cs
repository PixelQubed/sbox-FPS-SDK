using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Camera : CameraMode
{
	public override void Build( ref CameraSetup camSetup )
	{
		var player = Source1Player.LocalPlayer;
		if ( player == null )
			return;

		DefaultFieldOfView = camSetup.FieldOfView;

		base.Build( ref camSetup );

		camSetup.ViewModel.FieldOfView = ViewModelFieldOfView;
	}

	public override void Update()
	{
		var player = Source1Player.LocalPlayer;
		if ( player == null ) 
			return;

		//
		// Reset default values
		//

		Viewer = player;
		Position = player.EyePosition;
		Rotation = player.EyeRotation;
		ZNear = 1;
		FieldOfView = 0;
		ViewModelFieldOfView = cl_viewmodel_fov;

		CalculateView( player );

		LastFieldOfView = FieldOfView;
	}

	public virtual void CalculateView( Source1Player player )
	{
		if ( player.IsObserver )
		{
			CalculateObserverView( player );
		}
		else
		{
			CalculatePlayerView( player );
		}

		CalculateFieldOfView( player );
	}


	public virtual void CalculatePlayerView( Source1Player player )
	{
		var punch = player.ViewPunchAngle;
		Rotation *= Rotation.From( punch.x, punch.y, punch.z );
		SmoothViewOnStairs( player );

		if( cl_thirdperson )
		{
			Viewer = null;

			var tpPos = Position - Rotation.Forward * cl_thirdperson_distance;
			var tr = Trace.Ray( Position, tpPos )
				.Size( 5 )
				.WorldOnly()
				.Run();

			Position = tr.EndPosition;
		}
	}

	[ConVar.Client] public static bool cl_thirdperson { get; set; }
	[ConVar.Client] public static float cl_thirdperson_distance { get; set; } = 120;

	public virtual void CalculateObserverView( Source1Player player )
	{
		switch( player.ObserverMode )
		{
			case ObserverMode.Roaming:
				CalculateRoamingCamView( player );
				break;

			case ObserverMode.InEye:
				CalculateInEyeCamView( player );
				break;

			case ObserverMode.Chase:
				CalculateChaseCamView( player );
				break;

			case ObserverMode.Deathcam:
				CalculateDeathCamView( player );
				break;
		}
	}


	float m_flOldPlayerZ;
	float m_flOldPlayerViewOffsetZ;
	[ConVar.Client] public static bool cl_smoothstairs { get; set; } = true;

	public virtual void SmoothViewOnStairs( Source1Player player )
	{
		var pGroundEntity = player.GroundEntity;
		float flCurrentPlayerZ = player.Position.z;
		float flCurrentPlayerViewOffsetZ = player.EyeLocalPosition.z;

		// Smooth out stair step ups
		// NOTE: Don't want to do this when the ground entity is moving the player
		if ( pGroundEntity.IsValid() && (flCurrentPlayerZ != m_flOldPlayerZ) && cl_smoothstairs &&
			 m_flOldPlayerViewOffsetZ == flCurrentPlayerViewOffsetZ )
		{
			int dir = (flCurrentPlayerZ > m_flOldPlayerZ) ? 1 : -1;

			float steptime = Time.Delta;
			if ( steptime < 0 )
			{
				steptime = 0;
			}

			m_flOldPlayerZ += steptime * 150 * dir;

			const float stepSize = 18.0f;

			if ( dir > 0 )
			{
				if ( m_flOldPlayerZ > flCurrentPlayerZ )
				{
					m_flOldPlayerZ = flCurrentPlayerZ;
				}
				if ( flCurrentPlayerZ - m_flOldPlayerZ > stepSize )
				{
					m_flOldPlayerZ = flCurrentPlayerZ - stepSize;
				}
			}
			else
			{
				if ( m_flOldPlayerZ < flCurrentPlayerZ )
				{
					m_flOldPlayerZ = flCurrentPlayerZ;
				}
				if ( flCurrentPlayerZ - m_flOldPlayerZ < -stepSize )
				{
					m_flOldPlayerZ = flCurrentPlayerZ + stepSize;
				}
			}

			Position += Vector3.Up * (m_flOldPlayerZ - flCurrentPlayerZ);
		}
		else
		{
			m_flOldPlayerZ = flCurrentPlayerZ;
			m_flOldPlayerViewOffsetZ = flCurrentPlayerViewOffsetZ;
		}
	}
}
