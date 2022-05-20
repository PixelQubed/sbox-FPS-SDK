using Sandbox;

namespace Source1;

public partial class Source1Weapon : BaseWeapon
{
	public virtual bool CanEquip( Source1Player player )
	{
		return true;
	}

	public virtual void Holster()
	{

	}
}
