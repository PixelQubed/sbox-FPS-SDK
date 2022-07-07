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

		UpdateRotation();
		UpdateLookAt();

		UpdateMovement();
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

	float AnimDuckProgress { get; set; }
	public virtual float DuckMaxAnimSpeed => 10;

	public virtual void UpdateDucking()
	{
		var sign = AnimDuckProgress <= Player.DuckProgress ? 1 : -1;

		var newLerpedValue = AnimDuckProgress.LerpTo( Player.DuckProgress, Time.Delta * DuckMaxAnimSpeed );
		var lerpedDelta = MathF.Abs( newLerpedValue - AnimDuckProgress );
		var directDelta = MathF.Abs( Player.DuckProgress - AnimDuckProgress );
		var delta = Math.Min( directDelta, lerpedDelta );

		AnimDuckProgress += delta * sign;

		SetAnimParameter( "f_duck", AnimDuckProgress );
		SetAnimParameter( "b_ducked", Player.IsDucked );
	}
}
