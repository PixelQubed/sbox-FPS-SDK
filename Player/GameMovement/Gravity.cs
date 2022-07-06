using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public virtual void StartGravity()
	{
		float ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Move.Velocity.z -= (ent_gravity * GetCurrentGravity() * 0.5f * Time.Delta);
		Move.Velocity.z += Player.BaseVelocity.z * Time.Delta;

		var temp = Player.BaseVelocity;
		temp.z = 0;
		Player.BaseVelocity = temp;

		CheckVelocity();
	}

	public virtual void FinishGravity()
	{
		if ( Player.WaterJumpTime != 0 )
			return;

		var ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Move.Velocity[2] -= (ent_gravity * GetCurrentGravity() * Time.Delta * 0.5f);
		CheckVelocity();
	}
}
