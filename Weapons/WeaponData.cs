using Sandbox;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Amper.Source1;

public class WeaponData : GameResource
{
	/// <summary>
	/// All registered Player Classes are here.
	/// </summary>
	public static List<WeaponData> All { get; set; } = new();

	//
	// General
	//

	/// <summary>
	/// Title of the weapon that will be displayed to the client.
	/// </summary>
	public string Title { get; set; }
	/// <summary>
	/// Engine entity classname.
	/// </summary>
	public string ClassName { get; set; }
	/// <summary>
	/// Is this weapon hidden from the selection menu?
	/// </summary>
	public bool Hidden { get; set; }

	[ResourceType( "vmdl" )]
	public string WorldModel { get; set; }
	[ResourceType( "vmdl" )]
	public string ViewModel { get; set; }

	//
	// Properties
	//

	/// <summary>
	/// Base damage of this weapon. This will be modified by the in-game effects: i.e. crits, penalties, etc.
	/// </summary>
	[MinMax( 0, 500 )]
	public float Damage { get; set; }
	/// <summary>
	/// Amount of bullets that can fit inside the clip of this weapon.
	/// </summary>
	public int ClipSize { get; set; }
	/// <summary>
	/// Time that one shot requires to be made. If we fire, we will not be able to make another shot
	/// unti we wait this amount of time.
	/// </summary>
	[MinMax( 0, 3 )]
	public float AttackTime { get; set; }
	/// <summary>
	/// Delay between user presses the attack button and shot being actually made. This is commonly used for melee weapons.
	/// </summary>
	[MinMax( 0, 3 )]
	public float AttackDelay { get; set; }
	/// <summary>
	/// Amount of time this weapon requires to be deployed. We cannot use and fire this weapon until we wait this amount of time after switching to it.
	/// </summary>
	[MinMax( 0, 3 )]
	public float DeployTime { get; set; }
	/// <summary>
	/// How much bullets are being shot per one attack.
	/// </summary>
	[DefaultValue( 1 )]
	public int BulletsPerShot { get; set; }
	/// <summary>
	/// How much do bullets spread around.
	/// </summary>
	[MinMax( 0, 1 )]
	public float BulletSpread { get; set; }
	/// <summary>
	/// Maximum range of this weapon.
	/// </summary>
	[MinMax( 0, 8192 ), DefaultValue( 4096 )]
	public float Range { get; set; }
	/// <summary>
	/// How much ammo is consumed by one attack.
	/// </summary>
	[DefaultValue( 1 )]
	public int AmmoPerShot { get; set; }

	//
	// Distance Mod
	//

	/// <summary>
	/// Damage Falloff mechanic: Apply damage decrease at far range.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[DefaultValue( true )]
	public bool UseFalloff { get; set; }
	/// <summary>
	/// Maximum far range damage decrease. Default: 50%.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[DefaultValue( .5f ), MinMax( 0, 2 )]
	public float FalloffMultiplier { get; set; }
	/// <summary>
	/// Damage Rampup mechanic: Apply damage increase at close range.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[DefaultValue( true )]
	public bool UseRampup { get; set; }
	/// <summary>
	/// Maximum close range damage increase. Default: 150%.<br/>
	/// <i><b>Note: Melee weapons never use distance mod, regardless of this setting.</b></i>
	/// </summary>
	[DefaultValue( 1.5f ), MinMax( 0, 2 )]
	public float RampupMultiplier { get; set; }

	//
	// Reload
	//

	/// <summary>
	/// This defines how this weapon will be reloaded.
	/// </summary>
	public ReloadType ReloadType { get; set; }
	/// <summary>
	/// Amount of time that this weapon needs to take to be reloaded. If a shot is being made before this time passes during a reload, it will be cancelled.
	/// </summary>
	public float ReloadTime { get; set; }
	/// <summary>
	/// A small delay before reload cycle starts. This is commonly used for weapons that reload one clip at a time, to give to for reload_start sequence to be played.
	/// </summary>
	public float ReloadStartTime { get; set; }
	/// <summary>
	/// How much does the player view is punched when making an attack?
	/// </summary>
	public float PunchAngle { get; set; }
	[ResourceType( "vpcf" )]
	public string MuzzleFlash { get; set; }

	//
	// Visuals
	//

	public TracersData Tracers { get; set; }
	public struct TracersData
	{
		//
		// Tracers
		//

		[DefaultValue( 2 )]
		public int Frequency { get; set; }

		[ResourceType( "vpcf" ), DefaultValue( "particles/bullet_tracers/bullet_tracer01_red.vpcf" )]
		public string Red { get; set; }

		[ResourceType( "vpcf" ), DefaultValue( "particles/bullet_tracers/bullet_tracer01_blue.vpcf" )]
		public string Blue { get; set; }

		[ResourceType( "vpcf" ), DefaultValue( "particles/bullet_tracers/bullet_tracer01_red_crit.vpcf" )]
		public string RedCritical { get; set; }

		[ResourceType( "vpcf" ), DefaultValue( "particles/bullet_tracers/bullet_tracer01_blue_crit.vpcf" )]
		public string BlueCritical { get; set; }
	}

	//
	// UI
	//

	/// <summary>
	/// Main image, used to represent the weapon. Is used in the weapon selection menu.
	/// </summary>
	[Category( "Icons" ), ResourceType( "jpg" )] public string Icon { get; set; }
	/// <summary>
	/// This icon is the generic kill icon, used whenever a player makes a kill with this 
	/// weapon. This is also a fallback to all other kill icon types in case those are missing.
	/// </summary>
	[Category( "Icons" ), ResourceType( "jpg" )] public string KillIcon { get; set; }
	/// <summary>
	/// This icon is used for special kills of this weapon. For instance: backstabs and headshots count as "special" kills.
	/// </summary>
	[Category( "Icons" ), ResourceType( "jpg" )] public string KillIconSpecial { get; set; }

	public string TauntName { get; set; }

	public WeaponSoundList Sounds { get; set; }

	public struct WeaponSoundList
	{
		/// <summary>
		/// Sound that is played when we made a single shot.
		/// </summary>
		[FGDType( "sound" )]
		public string Single { get; set; }
		/// <summary>
		/// Sound that is played when we made a single critical shot.
		/// </summary>
		[FGDType( "sound" )]
		public string Crit { get; set; }
		/// <summary>
		/// Sound that is played when we are making a reload.
		/// </summary>
		[FGDType( "sound" )]
		public string Reload { get; set; }

		/// <summary>
		/// Sound that is played when we made hit world as melee weapons.
		/// </summary>
		[FGDType( "sound" ), Category( "Melee" )]
		public string HitWorld { get; set; }
		/// <summary>
		/// Sound that is played when we made hit flesh as melee weapons.
		/// </summary>
		[FGDType( "sound" ), Category( "Melee" )]
		public string HitFlesh { get; set; }
	}

	protected override void PostLoad()
	{
		Precache.Add( WorldModel );

		// Add this asset to the registry.
		All.Add( this );
	}

	/// <summary>
	/// Creates an instance of this weapon.
	/// </summary>
	/// <returns></returns>
	public TFWeaponBase CreateInstance()
	{
		if ( string.IsNullOrEmpty( ClassName ) )
			return null;

		var type = TypeLibrary.GetTypeByName<TFWeaponBase>( ClassName );
		if ( type == null )
			return null;

		var weapon = TypeLibrary.Create<TFWeaponBase>( ClassName );
		weapon.Initialize( this );

		return weapon;
	}

	public bool CanBeOwnedByPlayerClass( PlayerClass pclass )
	{
		return Owners.Select( x => x.GetData() ).Contains( pclass );
	}

	public bool TryGetOwnerDataForPlayerClass( PlayerClass pclass, out OwnerData data )
	{
		data = default;

		if ( !CanBeOwnedByPlayerClass( pclass ) )
			return false;

		data = Owners.FirstOrDefault( x => x.GetData() == pclass );
		return true;
	}
}

public enum ViewModelModeChoices
{
	ParentWeaponToHands,
	ParentHandsToWeapon,
	WeaponOnly
}
