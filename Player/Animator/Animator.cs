using System;
using Sandbox;

namespace Amper.Source1;

public partial class PlayerAnimator : BaseNetworkable
{
	Source1Player Player;

	QAngle ViewAngles;
	Rotation Rotation;
	Vector3 Velocity;
	Vector3 Position;

	public void Simulate( Source1Player player ) 
	{
		Player = player;

		UpdateFromPlayer( player );
		Update();
	}

	public virtual void UpdateFromPlayer( Source1Player player )
	{
		ViewAngles = player.EyeRotation;
		Rotation = player.Rotation;
		Velocity = player.Velocity;
		Position = player.Position;
	}

	public virtual void Update()
	{
		SetAnimParameter( "b_grounded", Player.IsGrounded );

		DoRotation();
		DoMovement();
		DoDucking();
	}

	public virtual void DoRotation()
	{
		Player.Rotation = Rotation.LookAt( ViewAngles.Forward.WithZ( 0 ), Vector3.Up );
	}

	public virtual void DoMovement()
	{
		var velocity = Velocity;
		var forward = Rotation.Forward.Dot( velocity );
		var sideward = Rotation.Right.Dot( velocity );

		SetAnimParameter( "move_speed", velocity.Length );
		SetAnimParameter( "move_y", sideward / Player.MaxSpeed );
		SetAnimParameter( "move_x", forward / Player.MaxSpeed );
	}

	float AnimDuckProgress { get; set; }
	public virtual float DuckMaxAnimSpeed => 10;

	public virtual void DoDucking()
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
