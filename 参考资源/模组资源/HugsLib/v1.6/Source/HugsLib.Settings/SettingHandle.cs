using System;
using System.Collections.Generic;
using UnityEngine;

namespace HugsLib.Settings;

/// <summary>
/// An individual persistent setting owned by a mod.
/// The extra layer of inheritance allows for type abstraction and storing SettingHandles in lists.
/// </summary>
public abstract class SettingHandle
{
	public delegate bool ValueIsValid(string value);

	public delegate bool ShouldDisplay();

	public delegate bool DrawCustomControl(Rect rect);

	/// <summary>
	/// Unique identifier of the setting.
	/// </summary>
	public string Name { get; protected set; }

	/// <summary>
	/// Name displayed in the settings menu.
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Displayed as a tooltip in the settings menu.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Should return true if the passed value is valid for this setting. Optional.
	/// </summary>
	public ValueIsValid Validator { get; set; }

	/// <summary>
	/// The string identifier prefix used to display enum values in the settings menu (e.g. "prefix_" for "prefix_EnumValue")
	/// </summary>
	public string EnumStringPrefix { get; set; }

	/// <summary>
	/// Return true to make this setting visible in the menu. Optional.
	/// An invisible setting can still be reset to default using the Reset All button.
	/// </summary>
	public ShouldDisplay VisibilityPredicate { get; set; }

	/// <summary>
	/// Draw a custom control for the settings menu entry. Entry name is already drawn when this is called.
	/// Optional. Return value indicates if the handle value was changed during the drawer call.
	/// </summary>
	public DrawCustomControl CustomDrawer { get; set; }

	/// <summary>
	/// Fully override the drawing of the settings menu entry for this handle.
	/// This replaces both the title and the control half of the entry.
	/// Optional. Return value indicates if the handle value was changed during the drawer call.
	/// </summary>
	/// <remarks>
	/// The following built-in handle drawing features are also disabled when this property is assigned:
	/// hovering info/menu buttons (<see cref="M:HugsLib.Settings.ModSettingsWidgets.DrawHandleHoverMenu(UnityEngine.Vector2,System.String,System.Boolean,System.Boolean)" />).
	/// </remarks>
	public DrawCustomControl CustomDrawerFullWidth { get; set; }

	/// <summary>
	/// When true, setting will never appear. For serialized data.
	/// No longer affects value resetting, see <see cref="P:HugsLib.Settings.SettingHandle.CanBeReset" />
	/// </summary>
	public bool NeverVisible { get; set; }

	/// <summary>
	/// When true (true by default), the setting can be reset to its default value by the player.
	/// If the handle is visible, this can be done through the right-click menu, or using the "Reset all" button.
	/// Disabling this is generally not recommended, except for specific use cases (for example, content unlocked by the player).
	/// </summary>
	public bool CanBeReset { get; set; } = true;

	/// <summary>
	/// When true, will not save this setting to the xml file. Useful in conjunction with CustomDrawer for placing buttons in the settings menu.
	/// </summary>
	public bool Unsaved { get; set; }

	/// <summary>
	/// Specifies by how much the + and - buttons should change a numeric setting.
	/// </summary>
	public int SpinnerIncrement { get; set; }

	/// <summary>
	/// When CustomDrawer is used, specifies the height of the row for the handle. Leave at 0 for default height.
	/// </summary>
	public float CustomDrawerHeight { get; set; }

	/// <summary>
	/// Affects the order in which handles appear in the settings menu. Lower comes first, default is 0.
	/// </summary>
	public int DisplayOrder { get; set; }

	/// <summary>
	/// Returns true if the <see cref="P:HugsLib.Settings.SettingHandle`1.Value" /> of this handle has been modified
	/// after the creation of the handle or the last time its value was saved.
	/// Automatically resets to false when <see cref="T:HugsLib.Settings.ModSettingsManager" /> saves changes.
	/// Can be manually toggled when e.g. replacing null with an instance in a <see cref="T:HugsLib.Settings.SettingHandleConvertible" /> handle.
	/// </summary>
	public bool HasUnsavedChanges { get; set; }

	/// <summary>
	/// Additional context menu options for the entry of this handle in the mod settings dialog.
	/// Will be shown when the hovering menu button for this entry is clicked.
	/// </summary>
	/// <remarks>
	/// The "Reset to default" option is always present, but will be disabled if <see cref="P:HugsLib.Settings.SettingHandle.CanBeReset" /> is false.
	/// </remarks>
	public IEnumerable<ContextMenuEntry> ContextMenuEntries { get; set; }

	public abstract string StringValue { get; set; }

	public abstract Type ValueType { get; }

	internal abstract bool ShouldBeSaved { get; }

	internal ModSettingsPack ParentPack { get; set; }

	/// <summary>
	/// Dispatched after the Value of the handle changes.
	/// </summary>
	public event Action<SettingHandle> ValueChanged;

	public abstract void ResetToDefault();

	public abstract bool HasDefaultValue();

	/// <summary>
	/// Marks the handle as modified and forces all settings to be saved.
	/// This is necessary for <see cref="T:HugsLib.Settings.SettingHandleConvertible" /> values, as changes in reference types cannot be automatically detected.
	/// </summary>
	public void ForceSaveChanges()
	{
		HasUnsavedChanges = true;
		ParentPack.SaveChanges();
	}

	protected SettingHandle()
	{
		SpinnerIncrement = 1;
	}

	protected void DispatchValueChangedEvent()
	{
		this.ValueChanged?.Invoke(this);
	}
}
public class SettingHandle<T> : SettingHandle
{
	private T _value;

	/// <summary>
	/// Called when the Value of the handle changes. Optional.
	/// </summary>
	[Obsolete("Use SettingHandle.ValueChanged event")]
	public Action<T> OnValueChanged { get; set; }

	/// <summary>
	/// The actual value of the setting. 
	/// This is converted to its string representation when settings are saved.
	/// Assigning a new value will trigger the OnValueChanged delegate.
	/// </summary>
	public T Value
	{
		get
		{
			return _value;
		}
		set
		{
			T value2 = _value;
			_value = value;
			if (!SafeEquals(value2, _value))
			{
				base.HasUnsavedChanges = true;
				try
				{
					DispatchValueChangedEvent();
					OnValueChanged?.Invoke(_value);
				}
				catch (Exception ex)
				{
					HugsLibController.Logger.Error("Exception while calling SettingHandle.OnValueChanged. Handle name was: \"{0}\" Value was: \"{1}\". Exception was: {2}", base.Name, StringValue, ex);
					throw;
				}
			}
		}
	}

	/// <summary>
	/// The value the setting assumes when initially created or reset.
	/// </summary>
	public T DefaultValue { get; set; }

	/// <summary>
	/// Retrieves the string representation of the setting or assigns a new setting value using a string.
	/// Will trigger the Validator delegate if assigned and change the Value property if the validation passes.
	/// </summary>
	public override string StringValue
	{
		get
		{
			try
			{
				return (Value != null) ? Value.ToString() : "";
			}
			catch (Exception ex)
			{
				HugsLibController.Logger.Error("Failed to serialize setting \"{0}\" of type {1}. Setting value was reset. Exception was: {2}", base.Name, typeof(T).FullName, ex);
				try
				{
					Value = DefaultValue;
					return (DefaultValue != null) ? DefaultValue.ToString() : "";
				}
				catch
				{
					return "";
				}
			}
		}
		set
		{
			if (base.Validator != null && !base.Validator(value))
			{
				Value = DefaultValue;
				return;
			}
			try
			{
				if (Value is Enum)
				{
					Value = (T)Enum.Parse(typeof(T), value);
				}
				else if (typeof(SettingHandleConvertible).IsAssignableFrom(typeof(T)))
				{
					Value = CustomValueFromString(value);
				}
				else
				{
					Value = (T)Convert.ChangeType(value, typeof(T));
				}
			}
			catch (Exception)
			{
				Value = DefaultValue;
			}
		}
	}

	/// <summary>
	/// Returns the type of the handle Value property.
	/// </summary>
	public override Type ValueType => typeof(T);

	internal override bool ShouldBeSaved
	{
		get
		{
			SettingHandleConvertible convertible = Value as SettingHandleConvertible;
			return !base.Unsaved && !HasDefaultValue() && !SkipConvertibleSaving();
			bool SkipConvertibleSaving()
			{
				try
				{
					return convertible != null && !convertible.ShouldBeSaved;
				}
				catch (Exception e)
				{
					HugsLibController.Logger.ReportException(e);
					return false;
				}
			}
		}
	}

	/// <summary>
	/// Implicitly cast handles to the Value they carry.
	/// </summary>
	public static implicit operator T(SettingHandle<T> handle)
	{
		return handle.Value;
	}

	internal SettingHandle(string name)
	{
		base.Name = name;
	}

	/// <summary>
	/// Assigns the default value to the Value property.
	/// Ignores the <see cref="P:HugsLib.Settings.SettingHandle.CanBeReset" /> property.
	/// </summary>
	public override void ResetToDefault()
	{
		Value = DefaultValue;
	}

	/// <summary>
	/// Returns true if the handle is set to its default value.
	/// </summary>
	/// <returns></returns>
	public override bool HasDefaultValue()
	{
		return SafeEquals(Value, DefaultValue);
	}

	public override string ToString()
	{
		return StringValue;
	}

	private T CustomValueFromString(string stringValue)
	{
		try
		{
			T val = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
			if (val is SettingHandleConvertible settingHandleConvertible)
			{
				settingHandleConvertible.FromString(stringValue);
			}
			return val;
		}
		catch (Exception ex)
		{
			HugsLibController.Logger.Error("Failed to parse setting \"{0}\" as custom type {1}. Setting value was reset. Value was: \"{2}\". Exception was: {3}", base.Name, typeof(T).FullName, stringValue, ex);
			throw;
		}
	}

	private bool SafeEquals(T valueOne, T valueTwo)
	{
		return valueOne?.Equals(valueTwo) ?? (valueTwo == null);
	}
}
