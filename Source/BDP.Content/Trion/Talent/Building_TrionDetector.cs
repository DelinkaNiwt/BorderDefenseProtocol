using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDP.Core.Trion;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// 可重复使用、需要供电的固定 Trion 天赋检测仪。
    /// 受检者选择、入舱和搬运均服从原版 Building_Enterable 工作流。
    /// </summary>
    public sealed class Building_TrionDetector : Building_Enterable
    {
        /// <summary>研究速度 100% 时需要完成的基础工作量，即 1 个游戏小时。</summary>
        public const float WorkRequired = 2500f;

        /// <summary>选人按钮使用原版安装图标，避免引入额外贴图资源。</summary>
        private static readonly Texture2D SelectSubjectIcon = TexCommand.Install;

        /// <summary>取消按钮使用原版取消图标。</summary>
        private static readonly Texture2D CancelIcon = TexCommand.ClearPrioritizedWork;

        /// <summary>本次检测已经累计的研究工作量。</summary>
        private float completedWork;

        /// <summary>原版进度条特效；只在舱内有受检者且已经开始检测时存在。</summary>
        private Effecter progressBarEffecter;

        /// <summary>舱内角色不额外偏移绘制。</summary>
        public override Vector3 PawnDrawOffset
        {
            get { return Vector3.zero; }
        }

        /// <summary>当前舱内受检者；固定检测仪只允许容纳一名角色。</summary>
        public Pawn Occupant
        {
            get { return innerContainer.OfType<Pawn>().FirstOrDefault(); }
        }

        /// <summary>当前是否具备工作所需电力。</summary>
        public bool PowerAvailable
        {
            get
            {
                CompPowerTrader power = this.TryGetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        /// <summary>公开当前累计工作量，供工作驱动和进度显示只读使用。</summary>
        public float CompletedWork
        {
            get { return completedWork; }
        }

        /// <summary>当前检测是否已经达到所需工作量。</summary>
        public bool WorkComplete
        {
            get { return completedWork >= WorkRequired; }
        }

        /// <summary>当前检测的归一化进度。</summary>
        public float WorkProgress
        {
            get { return Mathf.Clamp01(completedWork / WorkRequired); }
        }

        /// <summary>统一检查某研究员现在是否可以操作本建筑。</summary>
        public bool CanBeOperatedBy(Pawn operatorPawn)
        {
            Pawn occupant = Occupant;
            return PowerAvailable
                && occupant != null
                && !WorkComplete
                && TrionTalentAssessmentService.Instance.CanAssess(operatorPawn, occupant).Succeeded;
        }

        /// <summary>由当前操作员工作增加持久化在建筑上的检测工作量。</summary>
        public bool AddWork(float workAmount, Pawn operatorPawn)
        {
            if (workAmount <= 0f || WorkComplete)
            {
                return WorkComplete;
            }

            completedWork = Mathf.Min(WorkRequired, completedWork + workAmount);
            if (!WorkComplete)
            {
                return false;
            }

            CompleteAssessment(operatorPawn);
            return true;
        }

        /// <summary>按原版进入建筑协议检查所选角色是否仍可入舱。</summary>
        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (pawn == null || selectedPawn != pawn)
            {
                return "BDP_Message_TrionTalent_NotSelected".Translate();
            }

            if (Occupant != null)
            {
                return "BDP_Message_TrionTalent_DeviceOccupied".Translate();
            }

            if (!PowerAvailable)
            {
                return "BDP_Message_TrionTalent_DeviceNoPower".Translate();
            }

            TrionTalentAssessmentResult result = TrionTalentAssessmentEligibility.CanSelectSubject(pawn);
            return result.Succeeded ? AcceptanceReport.WasAccepted : result.Message;
        }

        /// <summary>将受检者从地图或搬运者容器转入建筑自身容器。</summary>
        public override void TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn).Accepted)
            {
                return;
            }

            bool wasSelected = pawn.DeSpawnOrDeselect();
            if (pawn.holdingOwner != null)
            {
                pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
            }
            else
            {
                innerContainer.TryAdd(pawn);
            }

            if (wasSelected)
            {
                Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
            }
        }

        /// <summary>安全弹出舱内角色，并清除当前选择。</summary>
        public void EjectContents()
        {
            if (Spawned)
            {
                innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            }

            selectedPawn = null;
            completedWork = 0f;
            CleanupProgressBar();
        }

        /// <summary>建筑移除时清理仅存在于地图上的进度条资源。</summary>
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            CleanupProgressBar();
            base.DeSpawn(mode);
        }

        /// <summary>维护原版进度条；断电只暂停操作，不清除也不隐藏已有进度。</summary>
        protected override void Tick()
        {
            base.Tick();
            if (Occupant == null || completedWork <= 0f)
            {
                CleanupProgressBar();
                return;
            }

            if (progressBarEffecter == null)
            {
                progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
            }

            progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
            SubEffecter_ProgressBar progressSubEffecter =
                progressBarEffecter.children[0] as SubEffecter_ProgressBar;
            if (progressSubEffecter?.mote != null)
            {
                progressSubEffecter.mote.progress = WorkProgress;
                progressSubEffecter.mote.offsetZ = -0.8f;
            }
        }

        /// <summary>提供原版扫描仪式的“选择受检者”和“取消”按钮。</summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (selectedPawn == null && Occupant == null)
            {
                Command_Action selectCommand = new Command_Action
                {
                    defaultLabel = "BDP_Command_TrionDetector_Select".Translate(),
                    defaultDesc = "BDP_Command_TrionDetector_SelectDesc".Translate(),
                    icon = SelectSubjectIcon,
                    action = OpenSubjectMenu
                };

                if (!PowerAvailable)
                {
                    selectCommand.Disable("BDP_Command_TrionDetector_NoPower".Translate());
                }

                yield return selectCommand;
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = Occupant == null
                    ? "BDP_Command_TrionDetector_CancelWaiting".Translate()
                    : "BDP_Command_TrionDetector_CancelAssessment".Translate(),
                defaultDesc = "BDP_Command_TrionDetector_CancelDesc".Translate(),
                icon = CancelIcon,
                activateSound = SoundDefOf.Designate_Cancel,
                action = EjectContents
            };
        }

        /// <summary>列出本地图内受玩家控制且仍可检测的角色。</summary>
        private void OpenSubjectMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            IReadOnlyList<Pawn> pawns = Map.mapPawns.AllPawnsSpawned;
            for (int index = 0; index < pawns.Count; index++)
            {
                Pawn candidate = pawns[index];
                if (!TrionTalentAssessmentEligibility.IsPlayerControlledSubject(candidate))
                {
                    continue;
                }

                TrionTalentAssessmentResult result = TrionTalentAssessmentEligibility.CanSelectSubject(candidate);
                if (!result.Succeeded)
                {
                    options.Add(new FloatMenuOption(
                        candidate.LabelShortCap + "：" + result.Message,
                        null,
                        candidate,
                        Color.white));
                    continue;
                }

                Pawn capturedCandidate = candidate;
                options.Add(new FloatMenuOption(
                    capturedCandidate.LabelShortCap,
                    delegate { SelectPawn(capturedCandidate); },
                    capturedCandidate,
                    Color.white));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("BDP_Command_TrionDetector_NoSubject".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        /// <summary>完成前重验并提交检测结果；无论成功与否都解除舱内锁定。</summary>
        private void CompleteAssessment(Pawn operatorPawn)
        {
            Pawn occupant = Occupant;
            if (occupant == null)
            {
                EjectContents();
                return;
            }

            TrionTalentAssessmentResult result =
                TrionTalentAssessmentService.Instance.TryCommit(operatorPawn, occupant);
            if (result.Succeeded)
            {
                Messages.Message(result.Message, occupant, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message(result.Message, occupant, MessageTypeDefOf.RejectInput, false);
            }

            EjectContents();
        }

        /// <summary>释放原版进度条特效，避免建筑取消或移除后残留视觉对象。</summary>
        private void CleanupProgressBar()
        {
            progressBarEffecter?.Cleanup();
            progressBarEffecter = null;
        }

        /// <summary>在检查信息中显示状态、持久进度和基准耗时口径。</summary>
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder(base.GetInspectString());
            if (selectedPawn != null && Occupant == null)
            {
                builder.AppendLineIfNotEmpty();
                builder.Append("BDP_Job_TrionTalent_Waiting".Translate(
                    selectedPawn.LabelShortCap));
            }
            else if (Occupant != null)
            {
                builder.AppendLineIfNotEmpty();
                builder.Append(
                    PowerAvailable
                        ? "BDP_Job_TrionTalent_Progress".Translate(WorkProgress.ToStringPercent())
                        : "BDP_Job_TrionTalent_Paused".Translate(WorkProgress.ToStringPercent()));
                builder.Append("BDP_Job_TrionTalent_Baseline".Translate());
            }

            return builder.ToString();
        }

        /// <summary>保存本次检测累计工作量；临时视觉对象不进入存档。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref completedWork, "completedWork", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                completedWork = Mathf.Clamp(completedWork, 0f, WorkRequired);
            }
        }
    }
}
