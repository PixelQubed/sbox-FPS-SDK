using System;
using Sandbox;

namespace Amper.Source1;

public partial class PlayerAnimator : BaseNetworkable
{
	Source1Player Player;

	public void Simulate( Source1Player player ) 
	{	
		Player = player;
		Update();
	}

	public virtual void Update()
	{
		SetAnimParameter( "b_grounded", Player.IsGrounded );

		UpdateMovement();
		UpdateRotation();
		UpdateLookAt();
		UpdateDucking();
	}

	public virtual Rotation GetIdealRotation()
	{
		return Rotation.LookAt( Player.EyeRotation.Forward.WithZ( 0 ).Normal, Vector3.Up );
	}

	public virtual void UpdateRotation()
	{
		if( LegShuffleEnabled )
		{
			UpdateLegShuffle();
			return;
		}

		var idealRotation = GetIdealRotation();

		// If we're moving, rotate to our ideal rotation
		Player.Rotation = Rotation.Slerp( Player.Rotation, idealRotation, Time.Delta * 10 );
		// Clamp the foot rotation to within 90 degrees of the ideal rotation
		Player.Rotation = Player.Rotation.Clamp( idealRotation, 60 );
	}

	public virtual void UpdateLookAt()
	{
		float pitch = -Player.EyeLocalRotation.Pitch();
		float yaw = Player.EyeLocalRotation.Yaw();
		Vector3 lookAtPos = Player.EyePosition + Player.EyeRotation.Forward * 200;

		SetAnimParameter( "body_pitch", pitch );
		SetAnimParameter( "body_yaw", yaw );
		SetLookAt( "aim_body", lookAtPos );
	}

	public virtual void UpdateMovement()
	{
		var velocity = Player.Velocity;
		var forward = Player.Rotation.Forward.Dot( velocity );
		var sideward = Player.Rotation.Right.Dot( velocity );

		SetAnimParameter( "move_speed", velocity.Length );
		SetAnimParameter( "move_y", sideward / Player.MaxSpeed );
		SetAnimParameter( "move_x", forward / Player.MaxSpeed );
	}

	public virtual void UpdateDucking()
	{
		SetAnimParameter( "f_duck", Player.DuckProgress );
		SetAnimParameter( "b_ducked", Player.IsDucked );
	}
}
