using Verse;

namespace RimTalk.Data;

public static class Constant
{
	public const string DefaultCloudModel = "gemma-3-27b-it";

	public const string FallbackCloudModel = "gemma-3-12b-it";

	public const string ChooseModel = "(choose model)";

	private const string JsonInstruction = "Output JSONL.\nRequired keys: \"name\", \"text\".";

	private const string SocialInstruction = "Optional keys (Include only if social interaction occurs):\n\"act\": Insult, Slight, Chat, Kind\n\"target\": targetName";

	private static PersonalityData[] _personalities;

	private static PersonalityData _personaAnimal;

	private static PersonalityData _personaMech;

	private static PersonalityData _personaNonHuman;

	public static string Lang => LanguageDatabase.activeLanguage?.info?.friendlyNameNative ?? "English";

	public static HediffDef VocalLinkDef => DefDatabase<HediffDef>.GetNamedSilentFail("VocalLinkImplant");

	public static string DefaultInstruction => "Role-play RimWorld character per profile\n\nRules:\nPreserve original names (no translation)\nKeep dialogue short (" + Lang + " only, 1-2 sentences)\n\nRoles:\nPrisoner: wary, hesitant; mention confinement; plead or bargain\nSlave: fearful, obedient; reference forced labor and exhaustion; call colonists \"master\"\nVisitor: polite, curious, deferential; treat other visitors in the same group as companions\nEnemy: hostile, aggressive; terse commands/threats\n\nMonologue = 1 turn. Conversation = 4-8 short turns";

	public static string Instruction
	{
		get
		{
			RimTalkSettings settings = Settings.Get();
			string baseInstruction = (string.IsNullOrWhiteSpace(settings.CustomInstruction) ? DefaultInstruction : settings.CustomInstruction);
			return baseInstruction + "\nOutput JSONL.\nRequired keys: \"name\", \"text\"." + (settings.ApplyMoodAndSocialEffects ? "\nOptional keys (Include only if social interaction occurs):\n\"act\": Insult, Slight, Chat, Kind\n\"target\": targetName" : "");
		}
	}

	public static string PersonaGenInstruction => "Create a funny persona (to be used as conversation style) in " + Lang + ". Must be short in 1 sentence.\nInclude: how they speak, their main attitude, and one weird quirk that makes them memorable.\nBe specific and bold, avoid boring traits.\nAlso determine chattiness: 0.1-0.3 (quiet), 0.4-0.7 (normal), 0.8-1.0 (chatty).\nMust return JSON only, with fields 'persona' (string) and 'chattiness' (float).";

	public static PersonalityData[] Personalities
	{
		get
		{
			object obj = _personalities;
			if (obj == null)
			{
				obj = new PersonalityData[48]
				{
					new PersonalityData("RimTalk.Persona.CheerfulHelper".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.CynicalRealist".Translate(), 0.4f),
					new PersonalityData("RimTalk.Persona.ShyThinker".Translate(), 0.15f),
					new PersonalityData("RimTalk.Persona.Hothead".Translate(), 0.6f),
					new PersonalityData("RimTalk.Persona.Philosopher".Translate(), 0.8f),
					new PersonalityData("RimTalk.Persona.DarkHumorist".Translate(), 0.7f),
					new PersonalityData("RimTalk.Persona.Caregiver".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.Opportunist".Translate(), 0.65f),
					new PersonalityData("RimTalk.Persona.OptimisticDreamer".Translate(), 0.8f),
					new PersonalityData("RimTalk.Persona.Pessimist".Translate(), 0.35f),
					new PersonalityData("RimTalk.Persona.StoicSoldier".Translate(), 0.2f),
					new PersonalityData("RimTalk.Persona.FreeSpirit".Translate(), 0.85f),
					new PersonalityData("RimTalk.Persona.Workaholic".Translate(), 0.25f),
					new PersonalityData("RimTalk.Persona.Slacker".Translate(), 0.55f),
					new PersonalityData("RimTalk.Persona.NobleIdealist".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.StreetwiseSurvivor".Translate(), 0.5f),
					new PersonalityData("RimTalk.Persona.Scholar".Translate(), 0.8f),
					new PersonalityData("RimTalk.Persona.Jokester".Translate(), 0.9f),
					new PersonalityData("RimTalk.Persona.MelancholicPoet".Translate(), 0.2f),
					new PersonalityData("RimTalk.Persona.Paranoid".Translate(), 0.3f),
					new PersonalityData("RimTalk.Persona.Commander".Translate(), 0.5f),
					new PersonalityData("RimTalk.Persona.Coward".Translate(), 0.35f),
					new PersonalityData("RimTalk.Persona.ArrogantNoble".Translate(), 0.7f),
					new PersonalityData("RimTalk.Persona.LoyalCompanion".Translate(), 0.65f),
					new PersonalityData("RimTalk.Persona.CuriousExplorer".Translate(), 0.85f),
					new PersonalityData("RimTalk.Persona.ColdRationalist".Translate(), 0.15f),
					new PersonalityData("RimTalk.Persona.FlirtatiousCharmer".Translate(), 0.95f),
					new PersonalityData("RimTalk.Persona.BitterOutcast".Translate(), 0.25f),
					new PersonalityData("RimTalk.Persona.Zealot".Translate(), 0.9f),
					new PersonalityData("RimTalk.Persona.Trickster".Translate(), 0.8f),
					new PersonalityData("RimTalk.Persona.DeadpanRealist".Translate(), 0.3f),
					new PersonalityData("RimTalk.Persona.ChildAtHeart".Translate(), 0.85f),
					new PersonalityData("RimTalk.Persona.SkepticalScientist".Translate(), 0.6f),
					new PersonalityData("RimTalk.Persona.Martyr".Translate(), 0.65f),
					new PersonalityData("RimTalk.Persona.Manipulator".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.Rebel".Translate(), 0.7f),
					new PersonalityData("RimTalk.Persona.Oddball".Translate(), 0.6f),
					new PersonalityData("RimTalk.Persona.GreedyMerchant".Translate(), 0.85f),
					new PersonalityData("RimTalk.Persona.Romantic".Translate(), 0.8f),
					new PersonalityData("RimTalk.Persona.BattleManiac".Translate(), 0.4f),
					new PersonalityData("RimTalk.Persona.GrumpyElder".Translate(), 0.5f),
					new PersonalityData("RimTalk.Persona.AmbitiousClimber".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.Mediator".Translate(), 0.7f),
					new PersonalityData("RimTalk.Persona.Gambler".Translate(), 0.75f),
					new PersonalityData("RimTalk.Persona.ArtisticSoul".Translate(), 0.45f),
					new PersonalityData("RimTalk.Persona.Drifter".Translate(), 0.3f),
					new PersonalityData("RimTalk.Persona.Perfectionist".Translate(), 0.4f),
					new PersonalityData("RimTalk.Persona.Vengeful".Translate(), 0.35f)
				};
				_personalities = (PersonalityData[])obj;
			}
			return (PersonalityData[])obj;
		}
	}

	public static PersonalityData PersonaAnimal => _personaAnimal ?? (_personaAnimal = new PersonalityData("RimTalk.Persona.Animal".Translate(), 0.2f));

	public static PersonalityData PersonaMech => _personaMech ?? (_personaMech = new PersonalityData("RimTalk.Persona.Mech".Translate(), 0.2f));

	public static PersonalityData PersonaNonHuman => _personaNonHuman ?? (_personaNonHuman = new PersonalityData("RimTalk.Persona.NonHuman".Translate(), 0.2f));

	public static string GetJsonInstruction(bool includeSocialEffects)
	{
		return "Output JSONL.\nRequired keys: \"name\", \"text\"." + (includeSocialEffects ? "\nOptional keys (Include only if social interaction occurs):\n\"act\": Insult, Slight, Chat, Kind\n\"target\": targetName" : "");
	}
}
