using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Amper.Source1;

partial class Source1Player
{
	public IEnumerable<Source1Weapon> Weapons => Children.OfType<Source1Weapon>();
	[Net, Predicted] public Source1Weapon ActiveWeapon { get; set; }
	[Predicted] Source1Weapon LastActiveWeapon { get; set; }

	/// <summary>
	/// Can this player attack using their weapons?
	/// </summary>
	public virtual bool CanAttack() => true;

	public virtual void SimulateActiveWeapon( Client cl )
	{
		if ( Input.ActiveChild is Source1Weapon newWeapon )
			SwitchToWeapon( newWeapon );

		if ( LastActiveWeapon != ActiveWeapon )
		{
			OnSwitchedActiveWeapon( LastActiveWeapon, ActiveWeapon );
			LastActiveWeapon = ActiveWeapon;
		}

		if ( !ActiveWeapon.IsValid() )
			return;

		if ( ActiveWeapon.IsAuthority )
			ActiveWeapon.Simulate( cl );
	}

	public virtual void OnSwitchedActiveWeapon( Source1Weapon lastWeapon, Source1Weapon newWeapon )
	{
		if ( lastWeapon.IsValid() )
		{
			if ( IsEquipped( lastWeapon ) )
				lastWeapon.OnHolster( this );

			// If we change a weapon always clean their viewmodel, as 
			// a fallback in case OnHolster on client doesn't get called.
			// i.e. if weapon is removed serverside.
			lastWeapon?.ClearViewModel();
		}

		if ( newWeapon.IsValid() )
		{
			newWeapon.OnDeploy( this );
		}
	}

	public bool SwitchToWeapon( Source1Weapon weapon, bool rememberLast = true )
	{
		if ( !weapon.IsValid() )
			return false;

		// Cant switch to something we don't have equipped.
		if ( !IsEquipped( weapon ) )
			return false;

		// Check if we can switch to this weapon.
		if ( !CanDeploy( weapon ) )
			return false;

		// We already have some weapon out.
		if ( ActiveWeapon.IsValid() )
		{
			// Check if we can switch from this weapon.
			if ( !CanHolster( ActiveWeapon ) )
				return false;
		}

		ActiveWeapon = weapon;
		return true;
	}

	public virtual bool EquipWeapon( Source1Weapon weapon, bool makeActive = false )
	{
		Host.AssertServer();

		if ( !weapon.IsValid() )
			return false;

		if ( weapon.Owner != null )
			return false;

		if ( !CanEquip( weapon ) )
			return false;

		if ( !PreEquipWeapon( weapon, makeActive ) )
			return false;

		weapon.OnEquip( this );

		if ( makeActive )
			SwitchToWeapon( weapon );

		return true;
	}

	/// <summary>
	/// Prepare weapon to being equipped. Return false to prevent from being equipped.
	/// </summary>
	protected virtual bool PreEquipWeapon( Source1Weapon weapon, bool makeActive )
	{
		// See if have another weapon in this weapon's slot.
		// If we have, throw it away.
		var slotWeapon = GetWeaponInSlot( weapon.SlotNumber );
		if ( slotWeapon.IsValid() )
		{
			// Check if we can drop a weapon.
			if ( !CanDrop( weapon ) )
				return false;

			ThrowWeapon( slotWeapon );
		}

		return true;
	}

	public virtual bool CanEquip( Source1Weapon weapon ) => weapon.CanEquip( this );
	public virtual bool CanDrop( Source1Weapon weapon ) => weapon.CanDrop( this );

	public virtual bool CanDeploy( Source1Weapon weapon ) => weapon.CanDeploy( this );
	public virtual bool CanHolster( Source1Weapon weapon ) => weapon.CanHolster( this );

	public virtual bool IsEquipped( Source1Weapon weapon ) => Children.Contains( weapon );

	public virtual void DeleteAllWeapons()
	{
		var weapons = Children.OfType<Source1Weapon>().ToArray();
		foreach ( var child in weapons )
		{
			if ( !child.IsValid() ) 
				continue;

			if ( ActiveWeapon == child )
				child?.OnHolster( this );

			child.OnDrop( this );
			child.Delete();
		}
	}

	public virtual void SwitchToNextBestWeapon()
	{
		var weapons = Children.OfType<Source1Weapon>()
			.Where( x => x != ActiveWeapon && CanDeploy( x ) )
			.OrderBy( x => x.SlotNumber );

		var first = weapons.FirstOrDefault();
		if ( first.IsValid() )
		{
			SwitchToWeapon( first );
			return;
		}
	}

	public virtual Vector3 GetAttackPosition() => EyePosition;
	public virtual Rotation GetAttackRotation()
	{
		var eyeAngles = EyeRotation;
		var punch = ViewPunchAngle;
		eyeAngles *= Rotation.From( punch.x, punch.y, punch.z );
		return eyeAngles;
	}

	public T GetWeaponOfType<T>() where T : Source1Weapon => Children.OfType<T>().FirstOrDefault();
	public bool HasWeaponOfType<T>() where T : Source1Weapon => GetWeaponOfType<T>() != null;

	public List<ViewModel> ViewModels { get; set; } = new();

	public ViewModel GetViewModel( int index = 0 )
	{
		if ( !IsClient )
			return null;

		if ( index < ViewModels.Count )
		{
			if ( ViewModels[index].IsValid() )
				return ViewModels[index];
		}

		var i = ViewModels.Count;
		while ( i <= index )
		{
			ViewModels.Add( null );
			i++;
		}

		var vm = CreateViewModel();
		vm.Position = Position;
		vm.Owner = this;

		ViewModels[index] = vm;
		return vm;
	}

	public virtual ViewModel CreateViewModel() => new ViewModel();

	public int GetActiveSlot()
	{
		if ( ActiveWeapon.IsValid() )
			return ActiveWeapon.SlotNumber;

		return 0;
	}

	public Source1Weapon GetWeaponInSlot( int slot )
	{
		return Children.OfType<Source1Weapon>().Where( x => x.SlotNumber == slot ).FirstOrDefault();
	}

	public virtual bool ThrowWeapon( Source1Weapon weapon, float force = 400 )
	{
		var origin = WorldSpaceBounds.Center;
		var vecForce = EyeRotation.Forward * 100 + Vector3.Up * 100;
		vecForce = vecForce.Normal;
		vecForce *= 400;

		if ( DropWeapon( weapon, origin, vecForce ) )
		{
			weapon.ApplyLocalAngularImpulse( new Vector3( Rand.Float( -600, 600 ), Rand.Float( -600, 600 ), 0 ) );
			return true;
		}

		return false;
	}

	public virtual bool DropWeapon( Source1Weapon weapon, Vector3 origin, Vector3 force )
	{
		if ( !weapon.IsValid() )
			return false;

		// We can't drop something that we dont have equipped.
		if ( !IsEquipped( weapon ) )
			return false;

		// We can't drop this weapon.
		if ( !CanDrop( weapon ) )
			return false;

		// This is the weapon we have equipped right now.
		if ( ActiveWeapon == weapon )
		{
			// We can't switch away from this weapon right now.
			if ( !CanHolster( weapon ) )
				return false;

			// Holster it immediately to negate all effects.
			ActiveWeapon.OnHolster( this );
			ActiveWeapon = null;
		}

		// Drop it.
		weapon.OnDrop( this );
		weapon.Position = origin;

		// Account our own velocity when throwing stuff.
		var velocity = force + Velocity;
		weapon.Rotation = Rotation.LookAt( velocity );
		weapon.ApplyAbsoluteImpulse( velocity );

		return true;
	}

	public virtual void RegenerateAllWeapons()
	{
		foreach ( var weapon in Weapons )
			weapon.Regenerate();
	}
}
