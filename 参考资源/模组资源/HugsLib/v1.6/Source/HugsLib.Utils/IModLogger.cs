using System;

namespace HugsLib.Utils;

internal interface IModLogger
{
	void Message(string message, params object[] substitutions);

	void Warning(string message, params object[] substitutions);

	void Error(string message, params object[] substitutions);

	void ReportException(Exception e, string modIdentifier = null, bool reportOnceOnly = false, string location = null);
}
