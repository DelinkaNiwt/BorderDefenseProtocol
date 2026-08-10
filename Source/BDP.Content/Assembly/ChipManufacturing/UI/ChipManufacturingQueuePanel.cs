using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Bill;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>右栏下半区：直接展示并操作制造台真实 BillStack。</summary>
    public sealed class ChipManufacturingQueuePanel
    {
        /// <summary>队列滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>绘制真实芯片账单队列。</summary>
        public void Draw(
            Rect rect,
            Building_ChipFabricator building,
            ChipManufacturingEditorState editorState,
            Action onConfigurationLoaded = null)
        {
            Rect inner = rect;
            if (rect.width <= 0f || rect.height <= 26f)
            {
                return;
            }

            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f),
                "BDP_ChipManufacturing_Queue_Title".Translate());

            Rect outRect = new Rect(inner.x, inner.y + 26f, inner.width, inner.height - 26f);
            BillStack stack = building?.BillStack;
            List<Bill_ChipProduction> bills = CollectChipBills(stack);
            if (bills.Count == 0)
            {
                Widgets.Label(
                    new Rect(outRect.x, outRect.y, outRect.width, 24f),
                    "BDP_ChipManufacturing_Queue_Empty".Translate());
                return;
            }

            float viewWidth = outRect.width - 16f;
            float contentHeight = CalculateContentHeight(viewWidth, bills);
            Rect viewRect = new Rect(0f, 0f, viewWidth,
                Mathf.Max(outRect.height, contentHeight));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float y = 0f;
            for (int index = 0; index < bills.Count; index++)
            {
                Bill_ChipProduction bill = bills[index];
                string title = BuildRowTitle(bill, index);
                float rowHeight = CalculateRowHeight(viewRect.width, title);
                Rect row = new Rect(0f, y, viewRect.width, rowHeight);
                DrawBillRow(
                    row,
                    stack,
                    bill,
                    index,
                    bills.Count,
                    editorState,
                    title,
                    onConfigurationLoaded);
                y += rowHeight + 6f;
            }
            Widgets.EndScrollView();
        }

        /// <summary>累计所有动态卡片高度，作为队列滚动内容高度。</summary>
        private static float CalculateContentHeight(
            float width,
            IList<Bill_ChipProduction> bills)
        {
            float height = 0f;
            for (int index = 0; index < bills.Count; index++)
            {
                height += CalculateRowHeight(width, BuildRowTitle(bills[index], index)) + 6f;
            }

            return height;
        }

        /// <summary>建立含完整动态芯片名的队列标题。</summary>
        private static string BuildRowTitle(Bill_ChipProduction bill, int index)
        {
            ChipCombinationRecord record = bill?.CombinationRecord;
            ChipCombinationResolution resolution =
                new ChipCombinationResolver().Resolve(record);
            string label = !resolution.ResolvedLabel.NullOrEmpty()
                ? resolution.ResolvedLabel
                : record?.LastResolvedLabel;
            return "BDP_ChipManufacturing_Queue_RowTitle".Translate(index + 1, label);
        }

        /// <summary>按完整标题实际行数、状态和两排操作按钮计算卡片高度。</summary>
        private static float CalculateRowHeight(float width, string title)
        {
            float titleHeight = Mathf.Max(24f, Text.CalcHeight(title ?? "", width - 12f));
            return 6f + titleHeight + 4f + 22f + 6f + 26f + 4f + 24f + 6f;
        }

        /// <summary>收集账单栈中的芯片账单，顺序与真实队列一致。</summary>
        private static List<Bill_ChipProduction> CollectChipBills(BillStack stack)
        {
            List<Bill_ChipProduction> result = new List<Bill_ChipProduction>();
            if (stack == null)
            {
                return result;
            }

            for (int index = 0; index < stack.Bills.Count; index++)
            {
                if (stack.Bills[index] is Bill_ChipProduction bill)
                {
                    result.Add(bill);
                }
            }

            return result;
        }

        /// <summary>绘制单条账单的顺序、状态、剩余数和操作按钮。</summary>
        private static void DrawBillRow(
            Rect rect,
            BillStack stack,
            Bill_ChipProduction bill,
            int index,
            int count,
            ChipManufacturingEditorState editorState,
            string title,
            Action onConfigurationLoaded)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.12f, 0.38f));
            float titleHeight = Mathf.Max(
                24f,
                Text.CalcHeight(title ?? "", rect.width - 12f));
            Rect titleRect = new Rect(
                rect.x + 6f,
                rect.y + 4f,
                rect.width - 12f,
                titleHeight);
            Widgets.Label(titleRect, title);

            string statusKey = bill.repeatCount == 0
                ? "BDP_ChipManufacturing_Queue_Completed"
                : bill.suspended
                    ? "BDP_ChipManufacturing_Queue_Suspended"
                    : "BDP_ChipManufacturing_Queue_Waiting";
            Rect statusRect = new Rect(
                rect.x + 6f,
                titleRect.yMax + 4f,
                rect.width - 12f,
                22f);
            Widgets.Label(statusRect,
                statusKey.Translate(bill.repeatCount));

            const float buttonGap = 4f;
            float buttonY = statusRect.yMax + 6f;
            float buttonWidth = (rect.width - 12f - buttonGap * 3f) / 4f;
            if (Widgets.ButtonText(new Rect(rect.x + 6f, buttonY, buttonWidth, 26f), "↑"))
            {
                ChipBillQueueOperations.Move(stack, bill, -1);
            }
            if (Widgets.ButtonText(new Rect(
                rect.x + 6f + (buttonWidth + buttonGap),
                buttonY,
                buttonWidth,
                26f), "↓"))
            {
                ChipBillQueueOperations.Move(stack, bill, 1);
            }
            if (Widgets.ButtonText(new Rect(
                rect.x + 6f + (buttonWidth + buttonGap) * 2f,
                buttonY,
                buttonWidth,
                26f), "−"))
            {
                ChipBillQueueOperations.SetRemainingCount(bill, bill.repeatCount - 1);
            }
            if (Widgets.ButtonText(new Rect(
                rect.x + 6f + (buttonWidth + buttonGap) * 3f,
                buttonY,
                buttonWidth,
                26f), "+"))
            {
                ChipBillQueueOperations.SetRemainingCount(bill, bill.repeatCount + 1);
            }

            float secondY = buttonY + 30f;
            float third = (rect.width - 12f - buttonGap * 2f) / 3f;
            if (Widgets.ButtonText(new Rect(rect.x + 6f, secondY, third, 24f),
                bill.suspended
                    ? "BDP_ChipManufacturing_Queue_Resume".Translate()
                    : "BDP_ChipManufacturing_Queue_Pause".Translate()))
            {
                ChipBillQueueOperations.ToggleSuspended(bill);
            }
            if (Widgets.ButtonText(new Rect(
                rect.x + 6f + third + buttonGap,
                secondY,
                third,
                24f),
                "BDP_ChipManufacturing_Queue_Load".Translate()))
            {
                ChipBillQueueOperations.LoadConfiguration(editorState, bill);
                onConfigurationLoaded?.Invoke();
            }
            if (Widgets.ButtonText(new Rect(
                rect.x + 6f + (third + buttonGap) * 2f,
                secondY,
                third,
                24f),
                "BDP_ChipManufacturing_Queue_Delete".Translate()))
            {
                ChipBillQueueOperations.Delete(stack, bill);
            }
        }
    }
}
