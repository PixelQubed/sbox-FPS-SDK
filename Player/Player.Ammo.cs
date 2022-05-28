using Sandbox;
using System;
using System.Collections.Generic;

namespace Amper.Source1;

partial class Source1Player
{
	[Net] public IDictionary<int, int> Ammo { get; set; }

	public int GetAmmoCount( int type )
	{
		if ( Ammo.TryGetValue( type, out int count ) ) return count;
		return 0;
	}

	public bool SetAmmo( int type, int amount )
	{
		if ( !Host.IsServer ) return false;
		if ( Ammo == null ) return false;

		Ammo[type] = amount;
		return true;
	}

	public bool GiveAmmo( int type, int amount )
	{
		if ( !Host.IsServer ) return false;
		if ( Ammo == null ) return false;

		SetAmmo( type, GetAmmoCount( type ) + amount );
		return true;
	}

	public int TakeAmmo( int type, int amount )
	{
		if ( Ammo == null ) return 0;

		var available = GetAmmoCount( type );
		amount = Math.Min( available, amount );

		SetAmmo( type, available - amount );
		return amount;
	}
}
