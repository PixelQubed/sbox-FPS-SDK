using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
