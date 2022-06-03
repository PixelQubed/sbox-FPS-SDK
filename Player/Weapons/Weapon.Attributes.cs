﻿using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	// 
	// Timings
	//
	public virtual float GetAttackTime() => 1;
	public virtual float GetReloadStartTime() => 1;
	public virtual float GetReloadTime() => 1;
	public virtual float GetDeployTime() => 1;

	//
	// Properties
	//
	public virtual int AmmoPerShot => 1;
	public virtual int BulletsPerShot => 1;
	public virtual float GetDamage() => 1;
	public virtual int TracerFrequency => 1;
	public virtual int Range => 4096;
	public virtual float GetBulletSpread() => 0;
	public virtual int GetClipSize() => 1;
	public virtual bool IsReloadingEntireClip() => false;
}
