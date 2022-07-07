using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public Source1Weapon ActiveWeapon { get; set; }

	/// <summary>
	/// Can this player attack using their weapons?
	/// </summary>
	public virtual bool CanAttack() => true;

	public virtual void SimulateActiveWeapon( Client cl )
	{
		if ( Input.ActiveChild is Source1Weapon newWeapon )
			SwitchToWeapon( newWeapon );

		if ( !ActiveWeapon.IsValid() )
			return;

		if ( ActiveWeapon.IsAuthority )
			ActiveWeapon.Simulate( cl );
	}

	public bool SwitchToWeapon( Source1Weapon weapon, bool rememberLast = true )
	{
		if ( !weapon.IsValid() )
			return false;

		// Cant switch to something we don't have equipped.
		if ( !IsEquipped( weapon ) )
			return false;

		// Check if we can switch to this weapon.
		if ( !CanSwitchTo( weapon ) )
			return false;

		// TODO:
		// Check if weapon allows us to switch to it.
		// if( !weapon.CanDeploy() )
		// return false;

		// We already have some weapon out.
		if ( ActiveWeapon.IsValid() )
		{
			// Check if we can switch from this weapon.
			if ( !CanSwitchFrom( ActiveWeapon ) )
				return false;

			// TODO:
			// Check if weapon allows us to be switched from.
			// if( !ActiveWeapon.CanHolster() )
			// return false;

			ActiveWeapon.OnHolster( this );
		}

		ActiveWeapon = weapon;
		ActiveWeapon.OnDeploy( this );

		return true;
	}

	//
	// Unfixed
	//

	public virtual bool EquipWeapon( Source1Weapon weapon, bool makeActive = false )
	{
		Host.AssertServer();

		if ( weapon.Owner != null )
			return false;

		if ( !CanEquipWeapon( weapon ) )
			return false;

		if ( !weapon.CanEquip( this ) )
			return false;

		weapon.OnEquip( this );

		weapon.Parent = this;
		weapon.Owner = this;

		if ( makeActive )
			SwitchToWeapon( weapon );

		return true;
	}

	public virtual bool CanEquipWeapon( Source1Weapon weapon ) => true;


	public virtual bool IsEquipped( Source1Weapon weapon ) => Children.Contains( weapon );

	public virtual void DeleteAllWeapons()
	{
		Ammo.Clear();

		var weapons = Children.OfType<Source1Weapon>().ToArray();
		foreach ( var child in weapons )
		{
			if ( !child.IsValid() ) 
				continue;

			child.Parent = null;
			child.SetParent( null );

			child.Delete();
		}
	}

	public bool CanSwitchTo( Source1Weapon weapon ) => true;
	public bool CanSwitchFrom( Source1Weapon weapon ) => true;

	public virtual void SwitchToNextBestWeapon()
	{
		var weapons = Children.OfType<Source1Weapon>()
			.Where( x => x != ActiveWeapon && CanSwitchTo( x ) );

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
			if ( ViewModels[index] != null )
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
		vm.Parent = this;

		ViewModels[index] = vm;
		return vm;
	}

	public virtual ViewModel CreateViewModel() => new ViewModel();
}
