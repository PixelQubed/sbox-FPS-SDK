using Sandbox;

namespace Amper.Source1;

partial class Projectile
{
	public virtual void SimulateMoveType()
	{
		UpdatePhysicsEnabled();

		switch (MoveType)
		{
			case ProjectileMoveType.None:
			case ProjectileMoveType.Physics:
				break;

			case ProjectileMoveType.Fly:
				FlyMoveSimulate();
				break;
		}
	}

	public void UpdatePhysicsEnabled()
	{
		var physicsEnabled = MoveType == ProjectileMoveType.Physics;
		if ( physicsEnabled != PhysicsEnabled )
			PhysicsEnabled = physicsEnabled;
	}

	public void FlyMoveSimulate()
	{
		Velocity += Map.Physics.Gravity * Gravity * Time.Delta;
		Position += Velocity * Time.Delta;
	}
}
