using System;
using System.Text;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Quickstart;

/// <summary>
/// Displays at game startup when the quickstarter is scheduled to run.
/// Shows the pending operation and allows to abort or disable the quickstart.
/// </summary>
internal class QuickstartStatusBox
{
	public delegate void AbortHandler(bool abortAndDisable);

	public interface IOperationMessageProvider
	{
		string Message { get; }
	}

	public class LoadSaveOperation : IOperationMessageProvider
	{
		private readonly string fileName;

		public string Message => "load save file: " + fileName;

		public LoadSaveOperation(string fileName)
		{
			this.fileName = fileName;
		}
	}

	public class GenerateMapOperation : IOperationMessageProvider
	{
		private readonly string scenario;

		private readonly int mapSize;

		public string Message => $"generate map: {scenario} ({mapSize}x{mapSize})";

		public GenerateMapOperation(string scenario, int mapSize)
		{
			this.scenario = scenario;
			this.mapSize = mapSize;
		}
	}

	private static readonly Vector2 StatusRectSize = new Vector2(240f, 75f);

	private static readonly Vector2 StatusRectPadding = new Vector2(26f, 18f);

	private readonly IOperationMessageProvider pendingOperation;

	public event AbortHandler AbortRequested;

	public QuickstartStatusBox(IOperationMessageProvider pendingOperation)
	{
		this.pendingOperation = pendingOperation ?? throw new ArgumentNullException("pendingOperation");
	}

	public void OnGUI()
	{
		string statusBoxText = GetStatusBoxText();
		Rect statusBoxRect = GetStatusBoxRect(statusBoxText);
		DrawStatusBox(statusBoxRect, statusBoxText);
		HandleKeyPressEvents();
	}

	private string GetStatusBoxText()
	{
		StringBuilder stringBuilder = new StringBuilder("HugsLib quickstarter preparing to\n");
		stringBuilder.Append(pendingOperation.Message);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.Append("<color=#777777>");
		stringBuilder.AppendLine("Press Space to abort");
		stringBuilder.Append("Press Shift+Space to disable");
		stringBuilder.Append("</color>");
		return stringBuilder.ToString();
	}

	private static Rect GetStatusBoxRect(string statusText)
	{
		Vector2 vector = Text.CalcSize(statusText);
		float num = Mathf.Max(StatusRectSize.x, vector.x + StatusRectPadding.x * 2f);
		float num2 = Mathf.Max(StatusRectSize.y, vector.y + StatusRectPadding.y * 2f);
		Rect r = new Rect(((float)UI.screenWidth - num) / 2f, ((float)UI.screenHeight / 2f - num2) / 2f, num, num2);
		return r.Rounded();
	}

	private static void DrawStatusBox(Rect rect, string statusText)
	{
		Widgets.DrawShadowAround(rect);
		Widgets.DrawWindowBackground(rect);
		TextAnchor anchor = Text.Anchor;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, statusText);
		Text.Anchor = anchor;
	}

	private void HandleKeyPressEvents()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 4 && Event.current.keyCode == KeyCode.Space)
		{
			bool shiftIsHeld = HugsLibUtility.ShiftIsHeld;
			Event.current.Use();
			this.AbortRequested?.Invoke(shiftIsHeld);
		}
	}
}
