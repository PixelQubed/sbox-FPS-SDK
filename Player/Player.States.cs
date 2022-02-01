using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player
	{
		public bool InWater => WaterLevelType >= WaterLevelType.Feet;
		public bool IsGrounded => GroundEntity != null;
		public bool IsUnderwater => WaterLevelType >= WaterLevelType.Eyes;
		public bool IsDucked => Tags.Has( PlayerTags.Ducked );
		public bool IsAlive => LifeState == LifeState.Alive;
	}
}
