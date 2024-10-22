﻿using Sandbox;

namespace Amper.FPS;

partial class Projectile
{
	[Net] ProjectileMoveType _moveType { get; set; }
	public new ProjectileMoveType MoveType { get => _moveType; set => UpdateMoveType( value ); }

	private void UpdateMoveType( ProjectileMoveType type )
	{
		if ( !IsServer )
			return;

		// Movetype didn't change.
		if ( _moveType == type )
			return;

		_moveType = type;

		var physicsEnabled = MoveType == ProjectileMoveType.Physics;
		PhysicsEnabled = physicsEnabled;
		UsePhysicsCollision = physicsEnabled;
	}

	public void SimulateMovement()
	{
		switch ( MoveType )
		{
			case ProjectileMoveType.None:
			case ProjectileMoveType.Physics:
				break;

			case ProjectileMoveType.Fly:
				FlyMoveSimulate();
				break;

			case ProjectileMoveType.Custom:
				MoveCustom();
				break;
		}
	}

	public void FlyMoveSimulate()
	{
		Velocity += Map.Physics.Gravity * Gravity * Time.Delta;
		Position += Velocity * Time.Delta;

		var angles = Rotation.Angles();
		angles += AngularVelocity * Time.Delta;
		Rotation = angles.ToRotation();
	}

	public virtual void MoveCustom()
	{

	}
}
