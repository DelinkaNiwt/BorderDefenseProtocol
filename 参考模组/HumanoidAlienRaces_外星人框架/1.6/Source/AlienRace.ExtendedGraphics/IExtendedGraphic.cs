using System.Collections.Generic;

namespace AlienRace.ExtendedGraphics;

public interface IExtendedGraphic
{
	void Init();

	string GetPath();

	string GetPath(int index);

	int GetPathCount();

	string GetPathFromVariant(ref int variantIndex, out bool zero);

	int GetVariantCount();

	int GetVariantCount(int index);

	int IncrementVariantCount();

	int IncrementVariantCount(int index);

	bool UseFallback();

	IEnumerable<IExtendedGraphic> GetSubGraphics();

	IEnumerable<IExtendedGraphic> GetSubGraphics(ExtendedGraphicsPawnWrapper pawn, ResolveData data);

	bool IsApplicable(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data);
}
