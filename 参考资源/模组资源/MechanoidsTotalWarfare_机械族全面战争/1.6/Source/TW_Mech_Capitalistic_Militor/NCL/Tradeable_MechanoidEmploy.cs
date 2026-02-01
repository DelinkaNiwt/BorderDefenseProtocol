using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Tradeable_MechanoidEmploy : Tradeable
{
	private int _countToTransfer;

	public override bool IsFavor => false;

	public override bool IsCurrency => false;

	public override bool IsThing => false;

	public override Thing AnyThing => null;

	public override bool TraderWillTrade => true;

	public override bool Interactive => true;

	public override string Label
	{
		get
		{
			int countToTransferToSource = base.CountToTransferToSource;
			int num = countToTransferToSource % 1 * 24;
			return $"雇佣: {countToTransferToSource}天{num}时";
		}
	}

	public override string TipDescription => "用零部件购买机械单位的服务时间";

	public override AcceptanceReport UnderflowReport()
	{
		return default(AcceptanceReport);
	}

	public override AcceptanceReport OverflowReport()
	{
		return default(AcceptanceReport);
	}

	public override int CostToInt(float cost)
	{
		return Mathf.CeilToInt(cost);
	}

	public override int CountHeldBy(Transactor trans)
	{
		return (trans == Transactor.Trader) ? 99999 : 0;
	}

	public override int GetHashCode()
	{
		return -51;
	}

	public override void ResolveTrade()
	{
		if (base.ActionToDo != TradeAction.PlayerBuys || !(TradeSession.trader is Pawn pawn))
		{
			return;
		}
		Log.Message("[NCL] 处理机械雇佣交易...");
		Comp_MechEmployable comp = pawn.GetComp<Comp_MechEmployable>();
		if (comp == null)
		{
			Log.Error("[NCL] 错误：交易对象缺少雇佣组件");
			return;
		}
		float num = base.CountToTransferToSource;
		float num2 = num * comp.Props.silverPerDay;
		Log.Message($"[NCL] 雇佣详情 | 天数: {num} | 价值: {num2}银");
		comp.Employ(num2);
		if (pawn.Faction == Faction.OfPlayer)
		{
			Log.Message("[NCL] 验证通过：机械单位已加入玩家阵营");
			Find.Selector.SelectedObjects.Add(pawn);
		}
		else
		{
			Log.Warning("[NCL] 警告：阵营未变更！");
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref _countToTransfer, "_countToTransfer", 0);
	}
}
