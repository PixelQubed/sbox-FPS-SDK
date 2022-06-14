using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public Source1Weapon ActiveWeapon { get; private set; }
	[Net, Predicted] public Source1Weapon LastWeapon { get; set; }
	[Predicted] Source1Weapon LastActiveWeapon { get; set; }

	public virtual void SimulateWeaponSwitch()
	{
		if ( Input.ActiveChild is Source1Weapon weapon ) 
			SwitchToWeapon( weapon );

		if ( LastActiveWeapon != ActiveWeapon )
		{
			OnActiveWeaponChanged( LastActiveWeapon, ActiveWeapon );
			LastActiveWeapon = ActiveWeapon;
		} 
	}

	public virtual void SimulateActiveWeapon( Client cl, Source1Weapon weapon )
	{
		if ( !weapon.IsValid() )
			return;

		if ( !weapon.IsAuthority )
			return;

		weapon.Simulate( cl );
	}

	public virtual bool CanEquipWeapon( Source1Weapon weapon ) => true;
	public virtual bool EquipWeapon( Source1Weapon weapon, bool makeActive = false )
	{
		Host.AssertServer();

		if ( weapon.Owner != null )
			return false;

		if ( !CanEquipWeapon( weapon ) )
			return false;

		if ( !weapon.CanEquip( this ) )
			return false;

		weapon.Parent = this;
		weapon.Owner = this;
		weapon.OnEquip( this );

		if ( makeActive )
			SwitchToWeapon( weapon );

		return true;
	}

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

	/// <summary>
	/// Can this player attack using their weapons?
	/// </summary>
	public virtual bool CanAttack() => true;

	public bool SwitchToWeapon( Source1Weapon weapon, bool rememberLast = true )
	{
		if ( weapon == null )
			return false;

		// We already have some weapon out.
		if ( ActiveWeapon.IsValid() )
		{
			// We already are using this weapon.
			if ( ActiveWeapon == weapon )
				return false;

			// We can't switch from this weapon.
			if ( !CanSwitchFrom( ActiveWeapon ) )
				return false;
		}

		// We can't switch to this weapon.
		if ( !CanSwitchTo( weapon ) )
			return false;

		var lastWeapon = ActiveWeapon;
		ActiveWeapon = weapon;

		if ( rememberLast )
			LastWeapon = lastWeapon;

		return true;
	}

	/// <summary>
	/// Called when the Active child is detected to have changed
	/// </summary>
	public virtual void OnActiveWeaponChanged( Source1Weapon previous, Source1Weapon next )
	{
		previous?.OnHolster( this );
		next?.OnDeploy( this );
	}

	public bool CanSwitchTo( Source1Weapon weapon ) => true;
	public bool CanSwitchFrom( Source1Weapon weapon ) => true;

	public virtual void SwitchToPreviousWeapon()
	{
		if ( !LastWeapon.IsValid() )
			return;

		SwitchToWeapon( LastWeapon );
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
