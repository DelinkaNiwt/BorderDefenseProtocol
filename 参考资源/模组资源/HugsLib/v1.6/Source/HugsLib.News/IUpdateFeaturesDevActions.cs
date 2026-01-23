using System;
using System.Collections.Generic;

namespace HugsLib.News;

internal interface IUpdateFeaturesDevActions
{
	Version GetLastSeenNewsVersion(string modIdentifier);

	IEnumerable<UpdateFeatureDef> ReloadAllUpdateFeatureDefs();

	bool TryShowAutomaticNewsPopupDialog();

	void SetLastSeenNewsVersion(string modIdentifier, Version version);
}
