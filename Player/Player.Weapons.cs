using Sandbox;
using System;
using System.Linq;

namespace Amper.Source1;

partial class Source1Player
{
	public Source1Weapon ActiveWeapon => ActiveChild as Source1Weapon;
	public Source1Weapon PreviousWeapon { get; set; }
	private Source1Weapon ForcedWeapon { get; set; }

	public virtual bool CanEquipWeapon( Source1Weapon weapon )
	{
		return true;
	}

	public virtual bool EquipWeapon( Source1Weapon weapon, bool makeActive = false )
	{
		Host.AssertServer();

		if ( weapon.Owner != null )
			return false;

		if ( CanEquipWeapon( weapon ) )
			return false;

		if ( !weapon.CanEquip( this ) )
			return false;

		weapon.Parent = this;
		weapon.OnCarryStart( this );

		if ( makeActive )
		{
			SetActive( weapon );
		}

		return true;
	}

	/// <summary>
	/// Make this entity the active one
	/// </summary>
	public virtual bool SetActive( Source1Weapon ent )
	{
		if ( ActiveChild == ent ) return false;
		if ( ent.Parent != this ) return false;

		ActiveChild = ent;
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
		ForcedWeapon = weapon;
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
