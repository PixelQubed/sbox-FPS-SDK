using Sandbox;
using System;

namespace Amper.Source1;

public partial class GameMovement
{
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
