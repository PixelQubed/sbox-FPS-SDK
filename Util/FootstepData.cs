using Sandbox;
using System;
using System.Linq;

namespace Source1;

/// <summary>
/// Asset used to override game specific foosteps, needs to be one per game.
/// </summary>
[Library( "footstep" ), AutoGenerate]
public class FootstepData : Asset
{
	public static FootstepData Instance { get; set; }

	protected override void PostLoad()
	{
		base.PostLoad();
		Instance = this;
	}

	public FootstepEntry[] Entries { get; set; }

	public struct FootstepEntry
	{
		[ResourceType( "surface" )]
		public string SurfacePath { get; set; }
		public FootstepSounds Sounds { get; set; }
	}

	public static bool GetSoundsForSurface( Surface surface, out FootstepSounds sounds )
	{
		sounds = default;

		if ( Instance == null )
			return false;

		var results = Instance.Entries.Where( x => x.SurfacePath == surface.Path ).Take( 1 ).ToList();
		if ( !results.Any() )
			return false;

		sounds = results[0].Sounds;
		return true;
	}

	public struct FootstepSounds
	{
		[FGDType( "sound" )]
		public string FootLeft { get; set; }
		[FGDType( "sound" )]
		public string FootRight { get; set; }
		[FGDType( "sound" )]
		public string FootLaunch { get; set; }
		[FGDType( "sound" )]
		public string FootLand { get; set; }
	}
}
