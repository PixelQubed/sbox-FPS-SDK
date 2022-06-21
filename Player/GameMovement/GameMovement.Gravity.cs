using Sandbox;
using System;

namespace Amper.Source1;

public partial class GameMovement
{
	public virtual void StartGravity()
	{
		float ent_gravity = Player.PhysicsBody.GravityScale;
		if ( ent_gravity <= 0 )
			ent_gravity = 1;

		Move.Velocity.z -= ent_gravity * GetCurrentGravity() * 0.5f * Time.Delta;
		Move.Velocity.z += Player.BaseVelocity.z * Time.Delta;

		var temp = Player.BaseVelocity;
		temp.z = 0;
		Player.BaseVelocity = temp;

		CheckVelocity();
	}

	public virtual void FinishGravity()
	{
		Velocity -= new Vector3( 0, 0, GetCurrentGravity() * 0.5f ) * Time.Delta;
	}

	public virtual float GetCurrentGravity()
	{
		return sv_gravity * GameRules.Current.GetGravityMultiplier();
	}

	protected string DescribeAxis( int axis )
	{
		switch(axis)
		{
			case 0: return "X";
			case 1: return "Y";
			case 2: default: return "Z";
		}
	}

	public void CheckVelocity()
	{
		for ( int i = 0; i < 3; i++ )
		{
			if ( float.IsNaN( Move.Velocity[i] ) )
			{
				Log.Info( $"Got a NaN velocity {DescribeAxis( i )}" );
				Move.Velocity[i] = 0;
			}

			if ( float.IsNaN( Move.Position[i] ) )
			{
				Log.Info( $"Got a NaN position {DescribeAxis( i )}" );
				Move.Position[i] = 0;
			}

			if ( Move.Velocity[i] > sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too high on {DescribeAxis( i )}" );
				Move.Velocity[i] = sv_maxvelocity;
			}

			if ( Move.Velocity[i] < -sv_maxvelocity )
			{
				Log.Info( $"Got a velocity too low on {DescribeAxis( i )}" );
				Move.Velocity[i] = -sv_maxvelocity;
			}
		}
	}
}
