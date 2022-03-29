﻿using Sandbox;
using System;

namespace Source1;

partial class Source1Camera : CameraMode
{
	Vector3 LastPosition { get; set; }
	Rotation LastRotation { get; set; }

	bool LerpEnabled { get; set; }

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
		LerpEnabled = true;
		ViewModelFieldOfView = cl_viewmodel_fov;

		CalculateView( player );

		LastPosition = Position;
		LastRotation = Rotation;
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

		if ( LerpEnabled )
		{
			CalculateLerp();
		}
	}

	public void CalculateLerp()
	{
		if ( cl_camera_lerp_amount <= 0 )
			return;

		if ( Position.Distance( LastPosition ) < 15 ) 
		{
			Position = LastPosition.LerpTo( Position, cl_camera_lerp_amount * Time.Delta );
		}

		Rotation = Rotation.Lerp( LastRotation, Rotation, cl_camera_lerp_amount * Time.Delta );
	}

	[ClientVar] public static float cl_camera_lerp_amount { get; set; } = 0;

	public virtual void CalculatePlayerView( Source1Player player )
	{
		Rotation *= player.ViewPunchAngle;
		Position += player.ViewPunchOffset;

		if( cl_thirdperson )
		{
			Viewer = null;

			LerpEnabled = false;
			Position -= Rotation.Forward * 64;
		}
	}

	[ClientVar] public static bool cl_thirdperson { get; set; }

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
}
