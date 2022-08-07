using Sandbox;

namespace Amper.Source1;

public interface IHasMaxHealth : IValid
{
	public float Health { get; set; }
	public float MaxHealth { get; set; }
}
