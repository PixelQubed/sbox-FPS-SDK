using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public float SurfaceFriction { get; set; } = 1;
	[Net, Predicted] public float MaxSpeed { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }

	[Net, Predicted] public float DuckAmount { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
}
