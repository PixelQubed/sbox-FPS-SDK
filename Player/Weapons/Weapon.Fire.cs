﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	public virtual DamageFlags DefaultDamageFlags => DamageFlags.Bullet;

	/// <summary>
	/// Weapons may modify the damage info if they have a need to do it. 
	/// </summary>
	public virtual void ApplyDamageModifications( Entity victim, ref DamageInfo info, TraceResult trace ) { }

	/// <summary>
	/// Creates the damage info struct that will then be passed to <see cref="ApplyDamageModifications"/>
	/// </summary>
	public virtual DamageInfo SetupDamageInfo( TraceResult tr, float damage )
	{
		return DamageInfo.Generic( damage )
			.UsingTraceResult( tr )
			.WithFlag( DefaultDamageFlags )
			.WithForce( GameRules.Current.CalculateForceFromDamage( tr.Direction, damage ) )
			.WithAttacker( Owner )
			.WithPosition( Owner.EyePosition )
			.WithWeapon( this );
	}

	/// <summary>
	/// Fire a bullet from this weapon.
	/// </summary>
	public virtual void FireBullet( float damage, int seedOffset = 0 )
	{
		FireBulletClient( damage, seedOffset );
		if ( IsServer ) FireBulletServer( damage, seedOffset );
	}

	//
	// Server
	//

	/// <summary>
	/// This does all the serverside code for the fired shot. This is where we deal damage.
	/// </summary>
	protected virtual TraceResult FireBulletServer( float damage, int seedOffset = 0 )
	{
		Host.AssertServer();
		var tr = TraceFireBullet( seedOffset );

		// We didn't hit any entity, early out.
		var entity = tr.Entity;
		if ( entity == null )
			return tr;

		var info = SetupDamageInfo( tr, damage );
		ApplyDamageModifications( entity, ref info, tr );
		entity.TakeDamage( info );

		return tr;
	}

	//
	// Client
	//

	[ClientRpc]
	void FireBulletClient( float damage, int seedOffset = 0 )
	{
		FireBulletEffects( damage, seedOffset );
	}


	/// <summary>
	/// Do client-side effects, related to firing a bullet.
	/// </summary>
	protected virtual TraceResult FireBulletEffects( float damage, int seedOffset = 0 )
	{
		Host.AssertClient();

		var tr = TraceFireBullet( seedOffset );

		// Create particle from the trace.
		CreateParticleFromTrace( tr );

		// If we hit some entity, do some effects on hit.
		if ( tr.Entity != null )
			OnHitEntity( tr.Entity, tr );

		return tr;
	}

	public virtual void OnHitEntity( Entity entity, TraceResult tr )
	{
		tr.Surface.DoBulletImpact( tr );
	}

	public virtual TraceResult TraceFireBullet( int seedOffset = 0 )
	{
		Rand.SetSeed( Time.Tick + seedOffset );

		Vector3 origin = GetAttackOrigin();
		Vector3 direction = GetAttackSpreadDirection();

		var target = origin + direction * Range;

		using ( LagCompensation() )
		{
			var tr = SetupFireBulletTrace( origin, target ).Run();
			if ( sv_debug_hitscan_hits ) DrawDebugTrace( tr );

			return tr;
		}
	}

	protected virtual Trace SetupFireBulletTrace( Vector3 Origin, Vector3 Target )
	{
		var tr = Trace.Ray( Origin, Target )
			.Size( 2 )
			.Ignore( this )
			.Ignore( Owner )
			.HitLayer( CollisionLayer.Solid )
			.HitLayer( CollisionLayer.WINDOW )
			.HitLayer( CollisionLayer.Water, false )
			.UseHitboxes();

		return tr;
	}

	[ConVar.Replicated] public static bool sv_debug_hitscan_hits { get; set; }
	void DrawDebugTrace( TraceResult tr, float time = 5 )
	{
		DebugOverlay.Line( tr.StartPosition, tr.EndPosition, IsServer ? Color.Yellow : Color.Green, time, true );

		DebugOverlay.Sphere( tr.StartPosition, 2f, Color.Cyan, time, true );
		DebugOverlay.Sphere( tr.EndPosition, 2f, Color.Red, time, true );

		DebugOverlay.Text(
			$"Distance: {tr.Distance}\n" +
			$"HitBox: {tr.HitboxIndex}\n" +
			$"Entity: {tr.Entity}\n" +
			$"Fraction: {tr.Fraction}",
			tr.EndPosition,
			time );
	}
}
