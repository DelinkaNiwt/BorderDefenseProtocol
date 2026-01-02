using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class StyleSettings
{
	public bool hasStyle = true;

	public bool genderRespected = true;

	public List<string> styleTags;

	public List<string> styleTagsOverride;

	public List<string> bannedTags;

	public ShaderTypeDef shader;

	public bool IsValidStyle(StyleItemDef styleItemDef, Pawn pawn, bool useOverrides = false)
	{
		if (!hasStyle)
		{
			return styleItemDef.styleTags.Contains("alienNoStyle");
		}
		bool num;
		if (!useOverrides)
		{
			if (!styleTags.NullOrEmpty())
			{
				num = styleTags.Any((string s) => styleItemDef.styleTags.Contains(s));
				goto IL_0083;
			}
		}
		else if (!styleTagsOverride.NullOrEmpty())
		{
			num = styleTagsOverride.Any((string s) => styleItemDef.styleTags.Contains(s));
			goto IL_0083;
		}
		goto IL_0085;
		IL_0085:
		int num2 = ((bannedTags.NullOrEmpty() || !bannedTags.Any((string s) => styleItemDef.styleTags.Contains(s))) ? 1 : 0);
		goto IL_00b2;
		IL_00b2:
		bool flag = (byte)num2 != 0;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = !genderRespected || pawn.gender == Gender.None;
			bool flag4 = flag3;
			if (!flag4)
			{
				StyleGender styleGender = pawn.Ideo?.style.GetGender(styleItemDef) ?? styleItemDef.styleGender;
				Gender gender = pawn.gender;
				bool flag5;
				switch (styleGender)
				{
				case StyleGender.Male:
					if (gender == Gender.Male)
					{
						goto case StyleGender.MaleUsually;
					}
					goto default;
				case StyleGender.Female:
					if (gender == Gender.Female)
					{
						goto case StyleGender.MaleUsually;
					}
					goto default;
				case StyleGender.MaleUsually:
				case StyleGender.Any:
				case StyleGender.FemaleUsually:
					flag5 = true;
					break;
				default:
					flag5 = false;
					break;
				}
				flag4 = flag5;
			}
			flag2 = flag4;
		}
		return flag2;
		IL_0083:
		if (num)
		{
			goto IL_0085;
		}
		num2 = 0;
		goto IL_00b2;
	}
}
