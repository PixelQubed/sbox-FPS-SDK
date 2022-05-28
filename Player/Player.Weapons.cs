using Sandbox;
using System;
using System.Linq;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] 
	public Source1Weapon ActiveWeapon { get; set; }
	[Predicted]
	Source1Weapon LastWeapon { get; set; }

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
		previous?.ActiveEnd( this, previous.Owner != this );
		next?.ActiveStart( this );
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
		weapon.OnCarryStart( this );

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

	public virtual void DeleteChildren()
	{
		Ammo.Clear();

		// At this point even if entities are deleted next tick, we still own them on this one.
		// Manually go through every children and set their parent to null.

		int count = Children.Count;
		for ( int i = count - 1; i >= 0; i-- )
		{
			var child = Children[i];
			if ( child == null || !child.IsValid ) 
				continue;

			child.Parent = null;
			child.SetParent( null );

			child.Delete();
		}
	}
}
