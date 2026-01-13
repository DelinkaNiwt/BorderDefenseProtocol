using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonoMod.Core;

[CLSCompliant(true)]
internal readonly record struct CreateDetourRequest
{
	public MethodBase Source { get; set; }

	public MethodBase Target { get; set; }

	public bool ApplyByDefault { get; set; }

	public bool CreateSourceCloneIfNotILClone { get; set; }

	public CreateDetourRequest(MethodBase Source, MethodBase Target)
	{
		CreateSourceCloneIfNotILClone = false;
		this.Source = Source;
		this.Target = Target;
		ApplyByDefault = true;
	}

	[CompilerGenerated]
	public void Deconstruct(out MethodBase Source, out MethodBase Target)
	{
		Source = this.Source;
		Target = this.Target;
	}
}
