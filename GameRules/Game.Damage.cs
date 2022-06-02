using Sandbox;
using System;

namespace Amper.Source1;

public struct RadiusDamageInfo
{
	public DamageInfo DamageInfo { get; set; }
	public Entity Target { get; set; }
	public Entity Ignore { get; set; }
	public float Radius { get; set; }
	public float AttackerRadius { get; set; }
	/// <summary>
	/// Multiplier value that defines how much damage, compared to base damage we should deal to entities
	/// that are on the edge of the explosion radius.
	/// </summary>
	public float Falloff { get; set; }

	public RadiusDamageInfo( DamageInfo info, float radius, Entity ignore, float attackerRadius, Entity target )
	{
		DamageInfo = info;
		Radius = radius;
		Ignore = ignore;
		AttackerRadius = attackerRadius;
		Target = target;
		Falloff = 0;

		CalculateFalloff();
	}

	public void CalculateFalloff()
	{
		Falloff = 0.5f;

		// The code below exists in TF2, but isn't used for anything important.
		// If we ever need to use it it's here, but for now it's commented.

		/*
		if ( Radius > 0 )
			Falloff = Info.Damage / Radius;
		else
			Falloff = 1;
		*/
	}

	public void ApplyToEntity( Entity entity )
	{
		// we're ignoring this entity.
		if ( entity == Ignore )
			return;

		var player = entity as Source1Player;
		var victimIsPlayer = player != null;

		//
		// Check if we can damage this entity.
		//

		if ( victimIsPlayer )
		{
			// Game says we can't damage this player.
			if ( !GameRules.Current.CanPlayerTakeDamage( player, DamageInfo.Attacker, DamageInfo ) )
				return;
		}

		//
		// Check line of sight between explosion and the entity.
		//

		var dmgPos = DamageInfo.Position;
		var eyePos = entity.EyePosition;

		var tr = Trace.Ray( dmgPos, eyePos )
			.WorldOnly()
			.Ignore( Ignore )
			.Ignore( entity )
			.Run();

		// If we hit something, we're blocked by world.
		if ( tr.Hit )
			return;

		//
		// Apply falloff based on distance.
		//

		var distance = 0f;

		// if the entity we're trying to damage is not our main target, calculate distanceToEntity
		// main target will take 100% damage.
		if ( Target != entity )
		{
			// Use whichever is closer, absorigin or worldspacecenter
			float toWorldSpaceCenter = (DamageInfo.Position - entity.WorldSpaceBounds.Center).Length;
			float toOrigin = (DamageInfo.Position - entity.Position).Length;

			distance = Math.Min( toWorldSpaceCenter, toOrigin );
		}

		// If we are applying damage to the attacker and we have attacker radius set to some value,
		// use attacker radius, otherwise use normal radius.
		var radius = entity == DamageInfo.Attacker && AttackerRadius > 0
					? AttackerRadius
					: Radius;

		var maxDamage = DamageInfo.Damage;
		var minDamage = DamageInfo.Damage * Falloff;

		var adjustedDamage = distance.RemapClamped( 0, radius, maxDamage, minDamage );

		// If we end up doing 0 damage, exit now.
		if ( adjustedDamage <= 0 )
			return;

		//
		// Adjust damage info
		//

		var adjustedInfo = DamageInfo;
		adjustedInfo.Damage = adjustedDamage;

		var dir = (eyePos - dmgPos).Normal;
		adjustedInfo.Force = GetDamageForceFromDirection( adjustedDamage, dir );

		entity.TakeDamage( adjustedInfo );

		if( GameRules.sv_debug_draw_radius_damage )
		{
			DebugOverlay.Sphere( entity.Position, 5, Color.Magenta, 5, true );
			DebugOverlay.Line( DamageInfo.Position, entity.Position, Color.Magenta, 5, true );
			DebugOverlay.Text(
				$"{distance}HU\n" +
				$"{adjustedDamage}HP\n" +
				$"{distance / radius * 100}%"
			, entity.Position, 5 );
		}
	}

	/// <summary>
	/// Damage cannot generate force greater than what we need to push a 75kg object for 400 HU/seconds
	/// </summary>
	public const float DamageForceLimit = 75 * 400;

	/// <summary>
	/// Amount of force per point of damage. Large enough to push a 75kg object for 4 units per point of damage.
	/// </summary>
	public const float DamageForcePerPoint = 75 * 4;

	public Vector3 GetDamageForceFromDirection( float damage, Vector3 direction )
	{
		var force = Math.Min( damage * DamageForcePerPoint, DamageForceLimit );

		// Fudge blast forces a little bit, so that each
		// victim gets a slightly different trajectory. 
		// This simulates features that usually vary from
		// person-to-person variables such as bodyweight,
		// which are all indentical for characters using the same model.
		force *= Rand.Float( 0.85f, 1.15f );

		return direction.Normal * force;
	}

	public void DebugDrawRadius()
	{
		for( int i = 0; i <= 5; i++ )
		{
			var lerp = 0.2f * i;
			var falloff = lerp.RemapClamped( 0, 1, 1, Falloff );
			var damage = DamageInfo.Damage * falloff;

			if ( i > 0 )
			{
				Color color;
				switch ( i )
				{
					case 1: color = Color.Blue; break;
					case 2: color = Color.Green; break;
					case 3: color = Color.Yellow; break;
					case 4: color = Color.Orange; break;
					default: color = Color.Red; break;
				}

				DebugOverlay.Sphere( DamageInfo.Position, Radius * lerp, color, 5, true );
			}

			DebugOverlay.Text(
				$"{Math.Floor( Radius * lerp )}\n" +
				$"{Math.Floor( falloff * 100 )}%\n" +
				$"{Math.Floor( damage )}HP"
			, DamageInfo.Position + Radius * Vector3.Up * lerp, 5 );

		}
	}
}

partial class GameRules
{
	public void ApplyRadiusDamage( RadiusDamageInfo info )
	{
		if ( sv_debug_draw_radius_damage )
			info.DebugDrawRadius();

		if ( info.Radius > 0 )
		{
			var entities = FindInSphere( info.DamageInfo.Position, info.Radius );
			foreach ( var entity in entities )
			{
				info.ApplyToEntity( entity );
			}
		}
	}

	[ConVar.Replicated] public static bool sv_debug_draw_radius_damage { get; set; }

	/// <summary>
	/// Modifies dealt damage using global game rules. This is applied to all taken damage,
	/// regardless or who to where.
	/// </summary>
	public virtual void ApplyOnDamageModifyRules( ref DamageInfo info, Entity victim ) { }
}

public static class DamageInfoExtensions
{
	public static DamageInfo WithoutFlag( this ref DamageInfo info, DamageFlags flag )
	{
		info.Flags &= ~flag;
		return info;
	}
}
