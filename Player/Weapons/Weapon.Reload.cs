using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Weapon
{
	[Net, Predicted] public bool IsReloading { get; set; }
	[Net, Predicted] public float NextReloadCycleTime { get; set; }
	[Net, Predicted] public bool FullReloadCycle { get; set; }

	public virtual bool WishReload() => Input.Down( InputButton.Reload );

	public virtual void SimulateReload()
	{
		// Player has requested a reload.
		if ( WishReload() || ShouldAutoReload() )
			Reload();

		// We're in the process of reloading.
		if ( IsReloading )
			ContinueReload();
	}

	/// <summary>
	/// Starts reloading.
	/// </summary>
	public virtual void Reload()
	{
		// Can't reload now.
		if ( !CanReload() )
			return;

		// We're already reloading.
		if ( IsReloading )
			return;

		// Play the reload start animation.
		// If weapon reloads entire clip at once, this will trigger the full animation.
		// If it reloads clip one by one, this will raise our hand, and insert animation will be triggered by b_insert.
		SendAnimParameter( "b_reload" );

		// We're now reloading.
		IsReloading = true;

		// Schedule our next reload. Full cycle will begin after we reach the first one.
		FullReloadCycle = false;
		NextReloadCycleTime = Time.Now + GetReloadStartTime();
	}

	public virtual void ContinueReload()
	{
		if ( !CanReload() )
		{
			// If we can't reload, and we're reloading now. Finish our reloading.
			if ( IsReloading )
				FinishReload();

			// Then prevent reloading from happening again, until we can reload again.
			return;
		}

		// We have reached a new reload cycle.
		if ( NextReloadCycleTime <= Time.Now )
		{
			// If we have made a full reload cycle, then add clip to the magazine
			if ( FullReloadCycle )
				ReloadRefillClip();

			// If we still can reload, start a new cycle.
			if ( CanReload() )
			{
				StartReloadCycle();
			}
		}
	}

	public virtual void ReloadRefillClip()
	{
		var neededClips = GetClipsPerReloadCycle();
		var addedClips = ConsumeAmmoFromReserve( neededClips );
		Clip += addedClips;
	}

	public virtual int GetClipsPerReloadCycle()
	{
		if ( IsReloadingEntireClip() )
			return Math.Max( GetClipSize() - Clip, 0 );

		return 1;
	}

	public virtual void StartReload()
	{
		if ( !CanReload() )
			return;

		SendAnimParameter( "b_reload" );
		IsReloading = true;
		FullReloadCycle = false;

		NextReloadCycleTime = Time.Now + GetReloadStartTime();
	}

	public virtual void FinishReload()
	{
		if ( !IsReloading )
			return;

		SendAnimParameter( "b_reload_end" );
		IsReloading = false;
	}

	public virtual void StartReloadCycle()
	{
		if ( !IsReloading )
			return;

		FullReloadCycle = true;
		SendAnimParameter( "b_insert" );
		NextReloadCycleTime = Time.Now + GetReloadTime();
	}

	public bool CanReload()
	{
		// If we don't need ammo, we can't reload.
		if ( !NeedsAmmo() )
			return false;

		// Don't have any reserve.
		if ( GetReserveAmmoCount() <= 0 ) 
			return false;

		// Our clip is full
		if ( Clip >= GetClipSize() )
			return false;

		// We're not done shooting yet.
		if ( NextAttackTime >= Time.Now )
			return false;

		return true;
	}

	public virtual int GetReserveAmmoCount()
	{
		if ( Player == null )
			return 0;

		return Player.GetAmmoCount( AmmoTypeNumber );
	}

	public virtual int ConsumeAmmoFromReserve( int ammoNeeded )
	{
		return Player.TakeAmmo( AmmoTypeNumber, ammoNeeded );
	}
}
