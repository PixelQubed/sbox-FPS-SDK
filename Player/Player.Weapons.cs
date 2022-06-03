using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public Source1Weapon ActiveWeapon { get; set; }
	[Predicted] Source1Weapon LastWeapon { get; set; }

	public Source1Weapon PreviousWeapon { get; set; }

	public virtual void SimulateActiveWeapon( Client cl, Source1Weapon weapon )
	{
		if ( LastWeapon != weapon )
		{
			OnActiveWeaponChanged( LastWeapon, weapon );
			LastWeapon = weapon;
		}

		if ( !LastWeapon.IsValid() )
			return;

		if ( LastWeapon.IsAuthority )
			LastWeapon.Simulate( cl );
	}

	public virtual void SimulateWeaponSwitch()
	{
		if ( Input.ActiveChild != null )
		{
			var newWeapon = Input.ActiveChild as Source1Weapon;
			if ( newWeapon != null )
			{
				ActiveWeapon = newWeapon;
			}
		}
	}

	/// <summary>
	/// Called when the Active child is detected to have changed
	/// </summary>
	public virtual void OnActiveWeaponChanged( Source1Weapon previous, Source1Weapon next )
	{
		previous?.OnHolster( this );
		next?.OnDeploy( this );
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
			ActiveWeapon = weapon;

		return true;
	}

	public virtual bool IsEquipped( Source1Weapon weapon )
	{
		return Children.Contains( weapon );
	}

	/// <summary>
	/// Force client to switch to a specific weapon.
	/// </summary>
	/// <param name="weapon"></param>
	[ClientRpc]
	public void SwitchToWeapon( Source1Weapon weapon )
	{
		Host.AssertClient();
		// ForcedWeapon = weapon;
	}

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

	/// <summary>
	/// Can this player attack using their weapons?
	/// </summary>
	public virtual bool CanAttack() => true;
}
