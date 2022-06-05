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
		Falloff = GameRules.Current.GetRadiusDamageFalloff();
	}

	public void ApplyToEntity( Entity entity )
	{
		// we're ignoring this entity.
		if ( entity == Ignore )
			return;

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
		adjustedInfo.Force = GameRules.Current.CalculateForceFromDamage( dir, adjustedDamage );

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

	/// <summary>
	/// Calculates how much force the attack will inflict, based on the amount of damage.
	/// </summary>
	public virtual Vector3 CalculateForceFromDamage( Vector3 direction, float damage, float scale = 1 )
	{
		var force = direction.Normal;
		force *= damage;
		force *= scale;
		force *= sv_damageforce_scale;
		return force;
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
