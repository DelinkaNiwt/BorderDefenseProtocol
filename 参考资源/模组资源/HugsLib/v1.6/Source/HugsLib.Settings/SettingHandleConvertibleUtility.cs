using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings;

/// <summary>
/// Utility methods for SettingHandleConvertible data objects.
/// These are useful for packing and unpacking your custom fields into a string without bothering with manual serialization.
/// </summary>
public static class SettingHandleConvertibleUtility
{
	/// <summary>
	/// Deserializes an XML string into an existing object instance.
	/// </summary>
	/// <param name="serializedValues">The serialized values to fill the object with</param>
	/// <param name="targetObject">The object to receive the deserialized values</param>
	public static void DeserializeValuesFromString(string serializedValues, object targetObject)
	{
		try
		{
			if (serializedValues.NullOrEmpty())
			{
				return;
			}
			DoSerializationChecks(targetObject);
			XmlSerializer xmlSerializer = new XmlSerializer(targetObject.GetType());
			using StringReader textReader = new StringReader(serializedValues);
			object source = xmlSerializer.Deserialize(textReader);
			CopySerializedMembersFromObject(source, targetObject);
		}
		catch (Exception arg)
		{
			throw new SerializationException($"Exception while serializing {targetObject?.GetType()}: {arg}");
		}
	}

	/// <summary>
	/// Serializes an object into a compact XML string.
	/// Whitespace and namespace declarations are omitted.
	/// Make sure the object is annotated with SerializableAttribute and the fields to serialize with XmlElementAttribute.
	/// </summary>
	/// <param name="targetObject">The object to serialize</param>
	public static string SerializeValuesToString(object targetObject)
	{
		try
		{
			DoSerializationChecks(targetObject);
			XmlSerializer xmlSerializer = new XmlSerializer(targetObject.GetType());
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = false,
				NewLineHandling = NewLineHandling.Entitize,
				OmitXmlDeclaration = true
			};
			XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces(new XmlQualifiedName[1] { XmlQualifiedName.Empty });
			using StringWriter stringWriter = new StringWriter();
			XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);
			xmlSerializer.Serialize(xmlWriter, targetObject, namespaces);
			return stringWriter.ToString();
		}
		catch (Exception arg)
		{
			throw new SerializationException($"Exception while deserializing {targetObject?.GetType()}: {arg}");
		}
	}

	private static void DoSerializationChecks(object targetObject)
	{
		if (targetObject == null)
		{
			throw new NullReferenceException("targetObject must be set");
		}
		if (targetObject.GetType().TryGetAttributeSafely<SerializableAttribute>() == null)
		{
			throw new SerializationException("targetObject must have the Serializable attribute");
		}
	}

	private static void CopySerializedMembersFromObject(object source, object destination)
	{
		if (source.GetType() != destination.GetType())
		{
			throw new Exception($"Mismatched types: {source.GetType()} vs {destination.GetType()}");
		}
		FieldInfo[] fields = source.GetType().GetFields(HugsLibUtility.AllBindingFlags);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.TryGetAttributeSafely<XmlElementAttribute>() != null)
			{
				fieldInfo.SetValue(destination, fieldInfo.GetValue(source));
			}
		}
		PropertyInfo[] properties = source.GetType().GetProperties(HugsLibUtility.AllBindingFlags);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.TryGetAttributeSafely<XmlElementAttribute>() != null)
			{
				propertyInfo.SetValue(destination, propertyInfo.GetValue(source, null), null);
			}
		}
	}
}
