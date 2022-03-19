using Sandbox;

namespace Source1;

/// <summary>
/// This entity should be simulated if its a child of a pawn even if its not active
/// </summary>
public interface IPassiveChild
{
	public void PassiveSimulate( Client client );
}
