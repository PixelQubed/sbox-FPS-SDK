using Sandbox;

namespace Amper.Source1;

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
