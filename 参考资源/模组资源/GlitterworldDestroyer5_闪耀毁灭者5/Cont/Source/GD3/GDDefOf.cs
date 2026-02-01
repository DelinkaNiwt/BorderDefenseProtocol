using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace GD3
{
    [DefOf]
    public static class GDDefOf
    {
        public static AbilityDef GD_BlackShieldSupport;

        public static AbilityDef ThrowPsychicGrenade;

        public static AbilityDef MosquitoBombardment;

        public static AbilityDef MechCallReinforcement;

        [MayRequireOdyssey]
        public static AbilityDef Annihilator_JumpAbility;

        [MayRequireOdyssey]
        public static AbilityDef Annihilator_JumpAndSuspend;

        [MayRequireOdyssey]
        public static AbilityDef Annihilator_Suspend;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Ambient;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Jump;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Jumping;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_ReadySuspend;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Suspending;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_ResetPosture;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Jump_Light;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_Dying;

        [MayRequireOdyssey]
        public static AnimationDef Annihilator_ReadySuspend_Light;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_LFront;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_RFront;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_LMiddle;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_RMiddle;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_LHind;

        [MayRequireOdyssey]
        public static BodyPartGroupDef Annihilator_Leg_RHind;

        [MayRequireOdyssey]
        public static BodyPartDef MechanicalThoraxAnnihilator;

        public static FactionDef BlackMechanoid;

        public static DamageDef BombCharge;

        public static DamageDef BombFrostBite;

        public static DamageDef Vaporize;

        public static DamageDef VaporizeSec;

        public static DamageDef BombSuper;

        public static DamageDef GD_Beam;

        public static EffecterDef GDReinforceFlareAttached;

        public static IncidentDef GD3BlackPassedBy;

        public static IncidentDef MechCluster_Giant_Incident;

        public static HediffDef GD_HitArmor;

        public static HediffDef PsychicInvisibility;

        public static HediffDef PsychicRadioSupport_Fst;

        public static HediffDef PsychicRadioSupport_Sec;

        public static HediffDef PsychicRadioSupport_Thd;

        public static HediffDef PsychicShieldMarrowHediff;

        public static HediffDef PsychicSuppressionMarrowHediff;

        public static HediffDef MedalFamineHediff;

        public static HediffDef Reinforce_FireControl;

        public static HediffDef Reinforce_Dodge;

        public static HediffDef Reinforce_Scrap;

        public static HediffDef PsychicVertigo;

        public static HediffDef GD_BlackShield;

        public static HediffDef GD_Militor;

        public static HediffDef GD_CallReinforcement;

        public static HediffDef GD_SavingMech;

        public static JobDef GD_OperateStation;

        public static JobDef CastAbilityGoToThing;

        public static JobDef GD_CastAbilityOnThing_Flying;

        public static JobDef GD_FleeFlying;

        public static JobDef GD_HaulToTurret;

        public static JobDef GD_UpgradeTurret;

        public static JobDef GD_FixTurret;

        public static JobDef GD_CallArtillery;

        public static JobDef GD_ModifySavingMech;

        public static LayoutRoomDef GDBlackCorridor;

        public static LetterDef SavingMech;

        public static EffecterDef JumpFlightEffect;

        public static EffecterDef GiantExplosion;

        public static EffecterDef GD_ImpactDustCloud;

        public static EffecterDef AnnihilatorLaserRing;

        public static FleckDef GD_CometStrikeWarning;

        public static MapGeneratorDef Drysea;

        public static PawnKindDef Mech_BlackLancer;

        public static PawnKindDef Mech_BlackScyther;

        public static PawnKindDef Mech_BlackTesseron;

        public static PawnKindDef Mech_BlackLegionary;

        public static PawnKindDef Mech_Militor;

        public static PawnKindDef Mech_BlackMilitor;

        public static PawnKindDef Mech_Mosquito;

        public static PawnKindDef Drone_ArchoHunter;

        public static PawnsArrivalModeDef RandomDrop;

        public static QuestScriptDef GD_Quest_Cluster_S;

        public static QuestScriptDef GD_Quest_Cluster_M;

        public static QuestScriptDef GD_Quest_Cluster_L;

        public static QuestScriptDef GD_Quest_Cluster_U;

        public static QuestScriptDef GD_Quest_SendCorpse;

        public static QuestScriptDef GD_Quest_BlackApocriton;

        public static ResearchProjectDef GD3_GiantCluster_Small;

        public static ResearchProjectDef GD3_GiantCluster_Medium;

        public static ResearchProjectDef GD3_GiantCluster_Large;

        public static ResearchProjectDef GD3_GiantCluster_Ultra;

        public static ResearchProjectDef GD3_GiantCluster_ArtilleryA;

        public static ResearchProjectDef GD3_GiantCluster_ArtilleryB;

        public static ResearchProjectDef GD3_GiantCluster_CentipedeAbility;

        public static ResearchProjectDef GD3_Weapons;

        public static ResearchProjectDef GD3_Intelligence;

        public static GameConditionDef SolarFlare;

        public static SoundDef JumpPackLand;

        public static SoundDef Pawn_Mech_Apocriton_Call;

        public static SoundDef Interact_ChargeRifle;

        public static SoundDef ChargeLance_Fire;

        public static SoundDef Explosion_Bomb;

        public static SoundDef PuzzleTrigger;

        public static SoundDef GD_Morse_IMHERE;

        public static SoundDef GD_Morse_STOLEN;

        public static SoundDef GD_Morse_CLEAR;

        public static SoundDef GD_Controled;

        public static SoundDef ExostriderDeath;

        public static SoundDef GDReinforceFlare;

        public static SoundDef GDMosquitosPassing;

        public static SoundDef GDDeckReinforce;

        public static SoundDef GDGearStart;

        public static SoundDef Mortar_LaunchA;

        public static SoundDef SnowScreenTriggered;

        public static SoundDef GDDistantArtilleryEMP;

        public static SoundDef IceSpreading;

        public static SoundDef Annihilator_DoJump;

        public static SoundDef Annihilator_PreJump;

        public static SoundDef Annihilator_Laser;

        public static SoundDef Annihilator_Transform;

        public static SongDef Xenanis;

        public static StatDef RangedCooldownFactorBuilding;

        public static SketchResolverDef MechCluster_Giant;

        public static TerrainDef FlagstoneMarble;

        public static ThingDef GD_BandNode;

        public static ThingDef Gun_MarineChargeLance;

        public static ThingDef Gun_MarinePsyLance;

        public static ThingDef CataphractCentipede_SR;

        public static ThingDef CataphractCentipede_FY;

        public static ThingDef Gun_GiantInfernoLauncher;

        public static ThingDef GD_FullAngelShieldProjector;

        public static ThingDef GD_LowAngelShieldProjector;

        public static ThingDef GD_AbilityShieldProjector;

        public static ThingDef HiTechResearchBench;

        public static ThingDef GD_CommunicationStation;

        public static ThingDef LargeMechCapsule_Empty;

        public static ThingDef GD_MechCorpse;

        public static ThingDef GD_Subcore;

        public static ThingDef Gun_ExplodeFloatingTurret;

        public static ThingDef GD_ServerKey;

        public static ThingDef GD_ServerDummy;

        public static ThingDef GD_DryseaDummy;

        public static ThingDef GD_EliminateDummy;

        public static ThingDef GD_ExostriderDummy;

        public static ThingDef BlackPersonaData;

        public static ThingDef PrecisionDeck;

        public static ThingDef OrbitalTargeterMechCluster;

        public static ThingDef Plant_PeaceLily;

        public static ThingDef Artillery_Exostrider;

        public static ThingDef ExostriderShell_Up;

        public static ThingDef ExostriderShell_Down;

        public static ThingDef Bullet_MosquitoChargeLance;

        public static ThingDef GD_ReinforceFlare;

        public static ThingDef GD_ReinforceMosquito;

        public static ThingDef PlayerTurret_Broken;

        public static ThingDef GD_PenetrationArtilleryShell;

        public static ThingDef GD_InfernoArtilleryShell;

        public static ThingDef GD_InfernoArtilleryShellLarge;

        public static ThingDef GD_PenetrationArtilleryStrike;

        public static ThingDef GD_InfernoArtilleryStrike;

        public static ThingDef GD_EMPArtilleryStrike;

        public static ThingDef GD_DroneTerminal;

        public static ThingDef Apparel_GD_BlackWindbreaker;

        public static WeatherDef GDBlizzard;

        public static WorldObjectDef GD_AllyCluster;

        public static AbilityDef BlackApocriton_Comet;

        public static AbilityDef BlackApocriton_Thunder;

        public static AbilityDef BlackApocriton_Arrows;

        public static AnimationDef MechCane_Normal;

        public static AnimationDef BlackApocriton_Shake;

        public static BodyPartDef MechanicalWing_Apocriton;

        public static BodyPartTagDef BlackApocritonWing;

        public static DamageDef MechBandShockwave;

        public static DamageDef Beam;

        public static HediffDef GD_ControlRangelinkImplant;

        public static HediffDef BlackSupport_Fst;

        public static HediffDef BlackSupport_Sec;

        public static HediffDef BlackSupport_Thr;

        public static HediffDef GD_MedalSupport;

        public static EffecterDef MechResurrected;

        public static EffecterDef ApocrionAoeWarmup;

        public static EffecterDef PocketThunderEffect;

        public static EffecterDef GD_BigWave;

        public static EffecterDef ApocrionAttached;

        public static EffecterDef BlackCane_Head_Directional;

        public static EffecterDef BlackCane_Stick_Directional;

        public static EffecterDef BlackApocritonDeath;

        public static FleckDef PsycastPsychicLine;

        public static FleckDef ApocritonResurrectFlashGrowing;

        public static FleckDef GD_LightningChain_Red;

        public static FleckDef GD_LightningChain_Blue;

        public static FleckDef GDRedSpark;

        public static FleckDef GDBlueSpark;

        public static FleckDef GDSwapLine;

        public static FleckDef GD_GroundCrack;

        public static FleckDef Annihilator_BeamSpark;

        public static QuestScriptDef GD_Puzzle;

        public static QuestScriptDef GD_Quest_FixTurret;

        public static QuestScriptDef GD_Quest_BuildCluster;

        public static QuestScriptDef GD_Quest_ExploreBase;

        public static QuestScriptDef GD_Quest_HelpMech;

        public static ResearchProjectDef GD3_Puzzle;

        [MayRequireOdyssey]
        public static ResearchProjectDef GD3_Annihilator;

        public static ThingDef GD_AlphaBombardment;

        public static ThingDef GD_DummyBombardment;

        public static ThingDef GD_DummyMine;

        public static ThingDef BlackStrike;

        public static ThingDef AlphaStrike;

        public static ThingDef GD_DescriptionChip;

        public static ThingDef Mech_BlackApocriton;

        public static ThingDef Wastepack_Red;

        public static ThingDef BlackStrike_Pod;

        public static ThingDef GD_MilitorDoll;

        public static ThingDef AncientExostriderLeg;

        public static ThingDef Mote_HellsphereCannon_Target;

        public static ThingDef BlackNanoChip;

        public static ThingDef Bullet_PocketThunder;

        public static ThingDef GD_ThunderArrow;

        public static ThingDef Bullet_RedThunderArrow;

        public static ThingDef GD_ThrowingCane;

        [MayRequireOdyssey]
        public static ThingDef Mech_Annihilator;

        [MayRequireOdyssey]
        public static ThingDef GD_Skyfaller_JumpingMech;

        [MayRequireOdyssey]
        public static ThingDef GD_Skyfaller_LandingMech;

        [MayRequireOdyssey]
        public static ThingDef GD_Skyfaller_SuspendReady;

        [MayRequireOdyssey]
        public static ThingDef AnnihilatorCorpse_Ancient;

        [MayRequireOdyssey]
        public static ThingDef AnnihilatorCorpse;

        [MayRequireOdyssey]
        public static ThingDef PlayerBuilding_Annihilator;

        public static SitePartDef GD_Sitepart_BlackApocriton;

        public static SoundDef Pawn_Mech_Scyther_Call;

        public static SoundDef Pawn_Mech_Apocriton_Wounded;

        public static SoundDef MechResurrect_Warmup;

        public static SoundDef Explosion_MechBandShockwave;

        public static SoundDef Silver_Drop;

        public static SoundDef MechbandDishUsed;

        public static SoundDef HellsphereCannon_Aiming;

        public static SoundDef Pawn_Mech_Diabolus_Wounded;

        public static SoundDef Pawn_Mech_Diabolus_Death;

        public static SoundDef PocketThunderWave;

        public static SoundDef ThumpCannon_Fire;

        public static SoundDef ThunderArrowShoot;

        public static SoundDef ThrowGrenade;

        public static SoundDef ThunderArrowWarning;

        public static SoundDef MechCaneBroke;

        public static SoundDef HugePsychicWave;

        public static SoundDef ArcThrowerBurst;

        public static SongDef MiraculousFlower;

        public static SongDef CommandSignal;

        public static SongDef MechCommander;
    }

    [DefOf]
    public static class GDDefOf_Another
    {
        public static PawnKindDef Mech_BlackApocriton;

        [MayRequireOdyssey]
        public static PawnKindDef Mech_Annihilator;
    }
}
