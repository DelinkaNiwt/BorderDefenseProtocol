using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

internal static class QualityColorsIntegration
{
	public static Assembly QCAss;

	public static void DoMatch()
	{
		if (QCAss == null)
		{
			Log.Warning(ModIntegration.ModLogPrefix + "DoMatch: QCAss is null — мод Quality Colors не найден или не загружен.");
			return;
		}
		FieldInfo fieldInfo = Utils.ReflectField(QCAss, "QualityColorsMod", "Settings", BindingFlags.Static | BindingFlags.Public);
		if (fieldInfo == null)
		{
			Log.Warning(ModIntegration.ModLogPrefix + "DoMatch: Не удалось найти статическое поле 'Settings' в моде Quality Colors. Возможно, изменилась структура мода.");
			return;
		}
		object value = fieldInfo.GetValue(null);
		if (value == null)
		{
			Log.Error(ModIntegration.ModLogPrefix + "DoMatch: Поле Settings существует, но возвращает null. Это критическая ошибка в Quality Colors.");
			return;
		}
		FieldInfo fieldInfo2 = Utils.ReflectField(QCAss, "QualityColors.ColorSettings", "Colors");
		if (fieldInfo2 == null)
		{
			Log.Warning(ModIntegration.ModLogPrefix + "DoMatch: Не удалось найти поле 'Colors' в объекте настроек. Возможно, мод обновился и поле переименовано/перемещено.");
			return;
		}
		object value2 = fieldInfo2.GetValue(value);
		if (value2 == null)
		{
			Log.Error(ModIntegration.ModLogPrefix + "DoMatch: Поле 'Colors' вернуло null. Настройки Quality Colors повреждены или не инициализированы.");
		}
		else if (value2 is Dictionary<QualityCategory, Color> dictionary)
		{
			QualityCategory[] array = new QualityCategory[7]
			{
				QualityCategory.Awful,
				QualityCategory.Poor,
				QualityCategory.Normal,
				QualityCategory.Good,
				QualityCategory.Excellent,
				QualityCategory.Masterwork,
				QualityCategory.Legendary
			};
			foreach (QualityCategory qualityCategory in array)
			{
				if (dictionary.TryGetValue(qualityCategory, out var value3))
				{
					Settings.SetQualityColor(qualityCategory, value3);
				}
				else
				{
					Log.Warning(ModIntegration.ModLogPrefix + $"DoMatch: В Quality Colors отсутствует цвет для качества {qualityCategory}. Используется дефолтный.");
				}
			}
			Log.Message(ModIntegration.ModLogPrefix + "DoMatch: Успешно синхронизированы цвета качества из Quality Colors мода.");
		}
		else
		{
			Log.Error(ModIntegration.ModLogPrefix + "DoMatch: Поле 'Colors' имеет неожиданный тип: " + value2.GetType().FullName + ". Ожидался Dictionary<QualityCategory, Color>. Совместимость нарушена.");
		}
	}
}
