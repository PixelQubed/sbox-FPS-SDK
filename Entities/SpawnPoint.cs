using Sandbox;

namespace Amper.FPS;

public partial class SpawnPoint : Entity
{
	/// <summary>
	/// Can this player spawn on this spawn point.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public virtual bool CanSpawn( Source1Player player )
	{
		return true;
	}
}
