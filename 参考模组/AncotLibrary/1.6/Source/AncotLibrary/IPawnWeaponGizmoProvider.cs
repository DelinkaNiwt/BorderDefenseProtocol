using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public interface IPawnWeaponGizmoProvider
{
	IEnumerable<Gizmo> GetWeaponGizmos();
}
