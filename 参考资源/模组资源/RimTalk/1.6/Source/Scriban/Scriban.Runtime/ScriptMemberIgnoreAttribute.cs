using System;

namespace Scriban.Runtime;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public class ScriptMemberIgnoreAttribute : Attribute
{
}
