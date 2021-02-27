using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Generic;

using AdaptiveCards;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace SS21.Examples.TeamBot.Helpers
{
    public static class AdaptiveCardFactory
    {
        public static AdaptiveCard ExecuteViaTemplate(Microsoft.Xrm.Sdk.Entity record, string jsonTemplate, string orgUrl)
        {
            try
            {
                if (record != null)
                {
                    jsonTemplate = ProcessDynamicContent(record, jsonTemplate);
                    jsonTemplate = MapAttributesToJsonTemplate(record, jsonTemplate, orgUrl);
                }

                AdaptiveCard ac = ExecuteViaJSON(jsonTemplate);

                return ac;
            }
            catch (Exception e)
            {
                throw new Exception("SS21.Examples.TeamBot.AdaptiveCardFactory.ExecuteViaTemplate :: ERROR :: Unable to process JSON Template into Adaptive Card object due to the following reason/s " + Environment.NewLine + e.Message);
            }
        }

        private static string MapAttributesToJsonTemplate(Microsoft.Xrm.Sdk.Entity record, string jsonTemplate, string orgUrl, bool doHyperlinks = true, bool doLinksAsAdaptive = true)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonTemplate))
                {
                    foreach (KeyValuePair<string, object> kvp in record.Attributes)
                    {
                        if (!jsonTemplate.Contains("{" + kvp.Key + "}"))
                        {
                            // move to next element if not detected within the template
                            continue;
                        }

                        string type = kvp.Value.GetType().Name.ToLower();
                        string value = kvp.Value.ToString();
                        string hyperlink = string.Empty;

                        switch (type)
                        {
                            case ("entityreference"):
                                {
                                    Microsoft.Xrm.Sdk.EntityReference refVal = (Microsoft.Xrm.Sdk.EntityReference)kvp.Value;
                                    value = refVal.Name;
                                    if (doHyperlinks) {
                                        hyperlink = orgUrl + "/main.aspx?forceUCI=1&etn=" + refVal.LogicalName + "&id=" + refVal.Id + "&pagetype=entityrecord";
                                    }
                                    break;
                                }
                            case ("optionsetvalue"):
                                {
                                    value = record.GetValueDisplayString(kvp.Key);
                                    break;
                                }
                            case ("datetime"):
                                {
                                    DateTime dtVal = (DateTime)kvp.Value;
                                    value = dtVal.ToString("dd/MM/yyyy");
                                    break;
                                }
                            case ("money"):
                                {
                                    Money mVal = (Money)kvp.Value;
                                    value = mVal.Value.ToString("#,###,##0");
                                    break;
                                }
                            case ("entitycollection"):
                                {
                                    if(kvp.Value != null)
                                    {
                                        Microsoft.Xrm.Sdk.EntityCollection collection = (Microsoft.Xrm.Sdk.EntityCollection)kvp.Value;
                                        value = collection.ToCommaSeparatedString();
                                    }
                                    else
                                    {
                                        value = "None";
                                    }
                                    break;
                                }
                            default:
                                {
                                    object o = kvp.Value;
                                    if ( o != null )
                                    {
                                        value = o.ToString();
                                    }
                                    else
                                    {
                                        value = "";
                                    }
                                    break;
                                }
                        }

                        if (!String.IsNullOrEmpty(hyperlink))
                        {
                            if (doLinksAsAdaptive)
                            {
                                // In AdaptiveCards, Links are formatted as [caption](url link)
                                jsonTemplate = jsonTemplate.Replace("{" + kvp.Key.ToLower() + "}", "[" + value + "](" + hyperlink + ")");
                            }
                            else
                            {
                                jsonTemplate = jsonTemplate.Replace("{" + kvp.Key.ToLower() + "}", hyperlink);
                            }
                        }
                        else
                        {
                            jsonTemplate = jsonTemplate.Replace("{" + kvp.Key.ToLower() + "}", value);
                        }
                        if (orgUrl != null)
                        {
                            jsonTemplate = jsonTemplate.Replace("{recordurl}", orgUrl + "/main.aspx?" + "etn=" + record.LogicalName + "&id=%7B" + record.Id + "%7D" + "&pagetype=entityrecord" + "&forceUCI=1");
                        }
                    }

                    // final step - remove all occuring strings by "{ and }" - effectively blanking the unmapped schemas
                    string result = jsonTemplate;
                    while (true)
                    {
                        int indexOfOpen = result.IndexOf("\"{");
                        int indexOfClose = result.IndexOf("}\"", indexOfOpen + 1);

                        if (indexOfOpen < 0 && indexOfClose < 0)
                        {
                            break;
                        }
                        result = result.Substring(0, indexOfOpen) + "\"" + result.Substring(indexOfClose + 1);
                    }

                    if (result != jsonTemplate)
                    {
                        jsonTemplate = result;
                    }
                }
                return jsonTemplate;
            }
            catch (Exception e)
            {
                throw new Exception("SS21.Examples.TeamBot.AdaptiveCardFactory.MapAttributesToJsonTemplate :: ERROR :: Unable to map attributes to JSON template due to the following reason/s " + Environment.NewLine + e.Message);
            }
        }

        public static string ProcessDynamicContent(Microsoft.Xrm.Sdk.Entity record, string jsonTemplate)
        {
            try
            {
                JObject jobject = JObject.Parse(jsonTemplate);

                JArray a = JArray.Parse(jobject["body"].ToString());
                JArray newArray = new JArray();

                foreach (JObject o in a.Children<JObject>())
                {
                    bool dynamicContent = false;
                    string schemaName = string.Empty;

                    foreach (JProperty p in o.Properties())
                    {
                        if (p.Name.Equals("dynamicContent"))
                        {
                            dynamicContent = true;
                        }
                        if (p.Name.Equals("text"))
                        {
                            // extract schema name between { } ex. {description}
                            // schemaName = description
                            schemaName = (string)o["text"];

                            if(!string.IsNullOrEmpty(schemaName))
                            {
                                schemaName = schemaName.Substring(1, schemaName.Length - 2);
                            }
                        }
                    }

                    // placeholder json for new textbox element
                    string jsonElement = @"{
                         ""type"": ""TextBlock"",
                         ""text"": """",
                         ""wrap"": true,
                         }";

                    if (!String.IsNullOrEmpty(schemaName))
                    {
                        // get value by schema name from Record
                        if (record.Contains(schemaName))
                        {
                            string fieldValue = (string)record[schemaName];

                            // remove html and perform string split
                            fieldValue = ConvertHTMLToText(fieldValue);

                            if (record.LogicalName == "email")
                            {
                                // remove any emails after the first detected email in the chain
                                int stringPosition = fieldValue.IndexOf("From:");

                                if (stringPosition != -1)
                                {
                                    fieldValue = fieldValue.Substring(0, stringPosition);
                                }

                                // attempt to remove signature by mapping from name
                                Microsoft.Xrm.Sdk.EntityCollection collection = (Microsoft.Xrm.Sdk.EntityCollection)record["from"];
                                string fromValue = collection.ToCommaSeparatedString();
                                stringPosition = fieldValue.IndexOf(fromValue);

                                if (stringPosition != -1)
                                {
                                    fieldValue = fieldValue.Substring(0, stringPosition);
                                }
                            }

                            // dodge - replace multiple line break characters with a unique break string
                            fieldValue = Regex.Replace(fieldValue, "[\r\n\f]", "/crmcs/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                            string[] fieldValues = fieldValue.Split(new string[] { "/crmcs/" }, StringSplitOptions.None);

                            for (int i = 0; i < fieldValues.Length; i++)
                            {
                                string value = fieldValues[i];

                                if (!string.IsNullOrEmpty(value))
                                {
                                    JObject newElement = null;

                                    if (i == 0)
                                    {
                                        // first value to replace current element
                                        newElement = o;
                                    }
                                    else
                                    {
                                        // next value set as new textbox element
                                        newElement = JObject.Parse(jsonElement);
                                    }
                                    newElement["text"] = value;
                                    newArray.Add(newElement);
                                }
                            }
                        }
                        else
                        {
                            // do nothing
                        }
                    }
                    else
                    {
                        newArray.Add(o);
                    }
                }

                jobject["body"] = newArray;

                // return formatted JSON Template as string to keep things consistent
                return jobject.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("ProcessDynamicContent :: " + ex.Message);
            }
        }

        public static AdaptiveCard ExecuteViaLocalTemplate(Microsoft.Xrm.Sdk.Entity record, string orgUrl)
        {
            try
            {
                string templateName = "Templates.default.json";

                switch (record.LogicalName)
                {
                    case ("task"):
                        {
                            templateName = "Templates.task.json";
                            break;
                        }
                    case ("incident"):
                        {
                            templateName = "Templates.incident.json";
                            break;
                        }
                }

                string resourceData = GlobalHelper.GetEmbeddedResource("SS21.Examples.TeamBot", templateName);

                resourceData = MapAttributesToJsonTemplate(record, resourceData, orgUrl);

                AdaptiveCard ac = ExecuteViaJSON(resourceData);

                return ac;
            }
            catch (Exception e)
            {
                throw new Exception("SS21.Examples.TeamBot.AdaptiveCardFactory.ExecuteViaLocalTemplate :: ERROR :: Unable to process JSON Template into Adaptive Card object due to the following reason/s " + Environment.NewLine + e.Message);
            }
        }
        public static AdaptiveCard ExecuteViaJSON(string json)
        {
            try
            {
                AdaptiveCard ac = JsonConvert.DeserializeObject<AdaptiveCard>(json);

                return ac;
            }
            catch (Exception e)
            {
                throw new Exception("SS21.Examples.TeamBot.AdaptiveCardFactory.ExecuteViaJSON :: ERROR :: Unable to deserialize JSON into Adaptive Card object due to the following reason/s " + Environment.NewLine + e.Message + Environment.NewLine + " JSON attempted: " + Environment.NewLine + json);
            }
        }
        public static string ConvertHTMLToText(string source)
        {

            //Dim result As String = Source
            string result = source;

            // Remove formatting that will prevent regex from running reliably
            // \r - Matches a carriage return \u000D.
            // \n - Matches a line feed \u000A.
            // \f - Matches a form feed \u000C.
            // For more details see http://msdn.microsoft.com/en-us/library/4edbef7e.aspx
            result = Regex.Replace(result, "[\r\n\f]", String.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // replace the most commonly used special characters:
            result = Regex.Replace(result, "&lt;", "<", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "&gt;", ">", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "&nbsp;", " ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "&quot;", @"""", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "&amp;", "&", RegexOptions.IgnoreCase);

            // Remove ASCII character code sequences such as &#nn; and &#nnn;
            result = Regex.Replace(result, "&#[0-9]{2,3};", String.Empty, RegexOptions.IgnoreCase);

            // Remove all other special characters. More can be added - see the following for more details:
            // http://www.degraeve.com/reference/specialcharacters.php
            // http://www.web-source.net/symbols.htm
            result = Regex.Replace(result, "&.{2,6};", String.Empty, RegexOptions.IgnoreCase);

            // Remove all attributes and whitespace from the <head> tag
            result = Regex.Replace(result, "< *head[^>]*>", "<head>", RegexOptions.IgnoreCase);
            // Remove all whitespace from the </head> tag
            result = Regex.Replace(result, "< */ *head *>", "</head>", RegexOptions.IgnoreCase);
            // Delete everything between the <head> and </head> tags
            result = Regex.Replace(result, "<head>.*</head>", String.Empty, RegexOptions.IgnoreCase);

            // Remove all attributes and whitespace from all <script> tags
            result = Regex.Replace(result, "< *script[^>]*>", "<script>", RegexOptions.IgnoreCase);
            // Remove all whitespace from all </script> tags
            result = Regex.Replace(result, "< */ *script *>", "</script>", RegexOptions.IgnoreCase);
            // Delete everything between all <script> and </script> tags
            result = Regex.Replace(result, "<script>.*</script>", String.Empty, RegexOptions.IgnoreCase);

            // Remove all attributes and whitespace from all <style> tags
            result = Regex.Replace(result, "< *style[^>]*>", "<style>", RegexOptions.IgnoreCase);
            // Remove all whitespace from all </style> tags
            result = Regex.Replace(result, "< */ *style *>", "</style>", RegexOptions.IgnoreCase);
            // Delete everything between all <style> and </style> tags
            result = Regex.Replace(result, "<style>.*</style>", String.Empty, RegexOptions.IgnoreCase);

            // Insert tabs in place of <td> tags
            result = Regex.Replace(result, "< *td[^>]*>", "\t", RegexOptions.IgnoreCase);

            // Insert single line breaks in place of <br> and <li> tags
            result = Regex.Replace(result, "< *br[^>]*>", Environment.NewLine, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "< *li[^>]*>", Environment.NewLine, RegexOptions.IgnoreCase);

            // Insert double line breaks in place of <p>, <div> and <tr> tags
            result = Regex.Replace(result, "< *div[^>]*>", Environment.NewLine + Environment.NewLine, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "< *tr[^>]*>", Environment.NewLine + Environment.NewLine, RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "< *p[^>]*>", Environment.NewLine + Environment.NewLine, RegexOptions.IgnoreCase);

            // Remove anything thats enclosed inside < >
            result = Regex.Replace(result, "<[^>]*>", String.Empty, RegexOptions.IgnoreCase);

            // Replace repeating spaces with a single space
            result = Regex.Replace(result, " +", " ");

            // Remove any trailing spaces and tabs from the end of each line
            result = Regex.Replace(result, "[ \t]+\r\n", Environment.NewLine);

            // Remove any leading whitespace characters
            result = Regex.Replace(result, @"^[\s]+", String.Empty);

            // Remove any trailing whitespace characters
            result = Regex.Replace(result, @"[\s]+$", String.Empty);

            // Remove extra line breaks if there are more than two in a row
            result = Regex.Replace(result, "\r\n\r\n(\r\n);+", Environment.NewLine + Environment.NewLine);

            // Thats it.
            return result;
        }
    }

    public static class EntityExtensions
    {
        /// <summary>
        /// Gets a value from a  entity
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="entity">Entity to get the value from</param>
        /// <param name="attributeName">Name of the property</param>
        /// <returns>Value, or null if not defined</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter",
            Justification = "Duly condsidered, there is an overload that doesn't")]
        public static T GetValue<T>(this Entity entity, string attributeName)
        {
            return GetValue<T>(entity, attributeName, default(T));
        }

        /// <summary>
        /// Gets a value from a  entity
        /// </summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="entity">Entity to get the value from</param>
        /// <param name="attributeName">Name of the property</param>
        /// <param name="defaultValue">Default value to return if not set</param>
        /// <returns>Value if defined, otherwise <paramref name="defaultValue"/></returns>
        public static T GetValue<T>(this Entity entity, string attributeName, T defaultValue)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            if (!entity.Contains(attributeName))
            {
                return defaultValue;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)GetValueDisplayString(entity, attributeName);
            }

            object value = entity[attributeName];

            if (value == null)
            {
                return defaultValue;
            }
            if (value is T)
            {
                return (T)value;
            }

            // remove alias wrapper
            AliasedValue aliasedValue = value as AliasedValue;
            if (aliasedValue != null)
            {
                value = aliasedValue.Value;
                if (value is T)
                {
                    return (T)value;
                }
            }

            OptionSetValue pickListValue = value as OptionSetValue;
            if (pickListValue != null)
            {
                if (typeof(T) == typeof(int)
                    || typeof(T) == typeof(int?))
                {
                    return (T)(object)pickListValue.Value;
                }
            }
            EntityReference reference = value as EntityReference;
            if (reference != null)
            {
                if (typeof(T) == typeof(Guid)
                    || typeof(T) == typeof(Guid?))
                {
                    return (T)(object)reference.Id;
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)reference.Name;
                }
            }

            Money money = value as Money;
            if (money != null)
            {
                if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                {
                    return (T)(object)money.Value;
                }
            }

            object underlyingValue = value;
            if (underlyingValue == null)
            {
                return defaultValue;
            }
            T result;
            if (TypeUtility.TryConvert(underlyingValue, out result))
            {
                return result;
            }

            DateTimeOffset? dateTimeOffsetValue = underlyingValue as DateTimeOffset?;
            if (dateTimeOffsetValue.HasValue)
            {
                if (typeof(T) == typeof(DateTime)
                    || typeof(T) == typeof(DateTime?))
                {
                    return (T)(object)dateTimeOffsetValue.Value.LocalDateTime;
                }
            }

            DateTime? dateTimeValue = underlyingValue as DateTime?;
            if (dateTimeValue.HasValue)
            {
                if (typeof(T) == typeof(DateTimeOffset)
                    || typeof(T) == typeof(DateTimeOffset?))
                {
                    return (T)(object)new DateTimeOffset(dateTimeValue.Value, TimeSpan.Zero);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the display string for a CRM property value
        /// </summary>
        /// <param name="entity">Entity to retreive the property from</param>
        /// <param name="attributeName">Name of the property</param>
        /// <returns></returns>
        public static string GetValueDisplayString(this Entity entity, string attributeName)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            if (!entity.Contains(attributeName))
            {
                return null;
            }

            string formattedValue = null;
            if (entity.FormattedValues.Contains(attributeName))
            {
                formattedValue = entity.FormattedValues[attributeName];
            }

            if (String.IsNullOrEmpty(formattedValue))
            {
                object valueObject = entity[attributeName];

                AliasedValue aliased = valueObject as AliasedValue;
                if (aliased != null)
                {
                    valueObject = aliased.Value;
                }

                if (valueObject != null)
                {
                    EntityReference reference = valueObject as EntityReference;
                    if (reference != null)
                    {
                        formattedValue = reference.Name;
                    }
                    else
                    {
                        formattedValue = valueObject.ToString();
                    }
                }
            }
            return formattedValue;
        }
    }

    /// <summary>
    /// Utility methods for type conversion
    /// </summary>
    public static class TypeUtility
    {
        /// <summary>
        /// Regular expression pattern that matches the string representation of a <see cref="Guid"/>.
        /// </summary>
        public const string GuidRegexPattern =
            @"^(\{|\(|)[0-9A-Fa-f]{8}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{12}(\}|\)|)$";
        private static readonly Regex guidRegex = new Regex(
            GuidRegexPattern, RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex isNumericRegex = new Regex(
            "^[0-9]*$", RegexOptions.Compiled | RegexOptions.Singleline);


        /// <summary>
        /// Tests if a string represents a Guid value
        /// </summary>
        /// <param name="text">Text represention of the ID</param>
        public static bool IsGuid(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return false;
            }
            return guidRegex.IsMatch(text);
        }

        /// <summary>
        /// Attempts to convert an object to a Guid
        /// </summary>
        /// <param name="value">Object value</param>
        /// <returns>Guid if known</returns>
        public static Guid? TryConvertGuid(object value)
        {
            if (value is Guid)
            {
                return (Guid)value;
            }
            if (value == null)
            {
                return null;
            }

            byte[] bytes = value as byte[];
            if (bytes != null && bytes.Length == 16)
            {
                return new Guid(bytes);
            }

            string stringValue = value.ToString();
            if (stringValue != null)
            {
                if (IsGuid(stringValue))
                {
                    return new Guid(stringValue);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to convert an object to a Guid
        /// </summary>
        /// <param name="value">Object value</param>
        /// <returns>Guid if known</returns>
        public static Guid? TryConvertGuid(string value)
        {
            if (IsGuid(value))
            {
                return new Guid(value);
            }
            return null;
        }

        /// <summary>
        /// Attempts to convert an object to the expected type
        /// </summary>
        /// <typeparam name="T">Type to attempt to convert the object to.</typeparam>
        /// <remarks>
        /// <para>The impelemntation will attempt to convert to the target type
        /// using various techniques. This is indetended for when an object is
        /// passed in for which the exact type is not known, but it is expected
        /// that it can be converted to a known type.</para>
        /// <para>Custom rules for converting types can be defined by adding a
        /// <see cref="TypeConverterAttribute"/> to the class, defining
        /// a custom <see cref="TypeConverter"/>. Note that this only allows
        /// for conversion between custom types, it cannot be used for
        /// conversion between framework types.</para>
        /// <para>The implementation is not able to perform multiple
        /// operations to covert the type, e.g. if type A can be converted
        /// to type B and type B to type C then <c>TryConvertType</c>
        /// cannot covert from type A to type C (the return value will be false)</para>
        /// <para>The following type conversion approaches are attempted, in the specified order
        /// <list type="bullet">
        ///     <item>If the item is already of the target type it is returned immediatly.</item>
        ///     <item>Null values are returned as null, regardless of orginal type.
        ///     This works the same way for nullable value types as it does for reference types.</item>
        ///     <item>If the target type is an <see cref="Enum"/> type and the <paramref name="value"/>
        ///     implements the <see cref="IConvertible"/> interface then that is used to convert
        ///     to the correct integer type. This means that conversion to enumerations
        ///     from integer values works the same way, and as efficent as, normal casts.</item>
        ///     <item>The <see cref="TypeConverter"/> defined for both the target type
        ///     and the <paramref name="value"/> are checked. If the target type defines
        ///     that it <see cref="TypeConverter.CanConvertFrom(ITypeDescriptorContext,Type)"/>
        ///     or the <paramref name="value"/> type defines that it 
        ///     <see cref="TypeConverter.CanConvertTo(ITypeDescriptorContext,Type)"/>
        ///     the target type then the type convert will be used.</item>
        ///     <item>Any implicit cast operators defined on either the target type
        ///     or the <paramref name="value"/> type will be used.</item>
        ///     <item>Any explicit cast operators defined on either the target type
        ///     or the <paramref name="value"/> type will be used.</item>
        /// </list>
        /// </para>
        /// <para>When converting to a nullable value type the implementation will attempt
        /// to convert to the underlying type. So this works the same way as converting to
        /// the underlying type.</para>
        /// </remarks>
        /// <param name="value">Value object</param>
        /// <param name="result">Converted value</param>
        /// <returns>True if converted successfully, false if not</returns>
        public static bool TryConvert<T>(object value, out T result)
        {
            // try direct casting
            if (value is T)
            {
                result = (T)value;
                return true;
            }

            object resultObject;
            if (TryConvert(value, typeof(T), out resultObject))
            {
                if (resultObject == null)
                {
                    result = default(T);
                    return true;
                }
                else if (resultObject is T)
                {
                    result = (T)resultObject;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Attempts to convert an object to the expected type
        /// </summary>
        /// <remarks>
        /// <para>The impelemntation will attempt to convert to the target type
        /// using various techniques. This is indetended for when an object is
        /// passed in for which the exact type is not known, but it is expected
        /// that it can be converted to a known type.</para>
        /// <para>Custom rules for converting types can be defined by adding a
        /// <see cref="TypeConverterAttribute"/> to the class, defining
        /// a custom <see cref="TypeConverter"/>. Note that this only allows
        /// for conversion between custom types, it cannot be used for
        /// conversion between framework types.</para>
        /// <para>The implementation is not able to perform multiple
        /// operations to covert the type, e.g. if type A can be converted
        /// to type B and type B to type C then <c>TryConvertType</c>
        /// cannot covert from type A to type C (the return value will be false)</para>
        /// <para>The following type conversion approaches are attempted, in the specified order
        /// <list type="bullet">
        ///     <item>If the item is already of the target type it is returned immediatly.</item>
        ///     <item>Null values are returned as null, regardless of orginal type.
        ///     This works the same way for nullable value types as it does for reference types.</item>
        ///     <item>If the target type is an <see cref="Enum"/> type and the <paramref name="value"/>
        ///     implements the <see cref="IConvertible"/> interface then that is used to convert
        ///     to the correct integer type. This means that conversion to enumerations
        ///     from integer values works the same way, and as efficent as, normal casts.</item>
        ///     <item>The <see cref="TypeConverter"/> defined for both the target type
        ///     and the <paramref name="value"/> are checked. If the target type defines
        ///     that it <see cref="TypeConverter.CanConvertFrom(ITypeDescriptorContext,Type)"/>
        ///     or the <paramref name="value"/> type defines that it 
        ///     <see cref="TypeConverter.CanConvertTo(ITypeDescriptorContext,Type)"/>
        ///     the target type then the type convert will be used.</item>
        ///     <item>Any implicit cast operators defined on either the target type
        ///     or the <paramref name="value"/> type will be used.</item>
        ///     <item>Any explicit cast operators defined on either the target type
        ///     or the <paramref name="value"/> type will be used.</item>
        /// </list>
        /// </para>
        /// <para>When converting to a nullable value type the implementation will attempt
        /// to convert to the underlying type. So this works the same way as converting to
        /// the underlying type.</para>
        /// </remarks>
        /// <param name="value">Value object</param>
        /// <param name="target">Type of object to return</param>
        /// <param name="result">Converted value</param>
        /// <returns>True if converted successfully, false if not</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public static bool TryConvert(object value, Type target, out object result)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            // handle nulls
            if (value == null)
            {
                result = null;
                if (target.IsClass) // ok for classes, remains null
                {
                    return true;
                }
                else // but struct's can't be null
                {
                    // except for Nullable<T>
                    if (target.IsGenericType &&
                        target.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        result = null;
                        return true;
                    }
                }
                return false;
            }

            if (target.IsAssignableFrom(value.GetType()))
            {
                result = value;
                return true;
            }

            // handle nullable target types, we've already checked for null
            if (target.IsGenericType &&
                target.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type targetType = target.GetGenericArguments()[0];
                bool methodResult = TryConvert(value, targetType, out result);
                return methodResult;
            }

            // handle enums
            bool tryEnumIConvertible = target.IsEnum && value is IConvertible;
            if (tryEnumIConvertible && value is string)
            {
                if (!isNumericRegex.IsMatch(value.ToString()))
                {
                    tryEnumIConvertible = false;
                }
            }
            if (tryEnumIConvertible)
            {
                if (TryConvertEnum(value, target, out result))
                {
                    return true;
                }
            }

            if (TryTypeConverter(value, target, out result))
            {
                return true;
            }

            // try implicit operators
            Type valueType = value.GetType();
            if (TryCastOperator("op_Implicit", value, target, target, out result))
            {
                return true;
            }
            if (TryCastOperator("op_Implicit", value, valueType, target, out result))
            {
                return true;
            }
            // try explicit operators
            if (TryCastOperator("op_Explicit", value, target, target, out result))
            {
                return true;
            }
            if (TryCastOperator("op_Explicit", value, valueType, target, out result))
            {
                return true;
            }

            result = null;
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "TryContert is not supposed to throw exceptions, so avoid them here, we do our best to convert in non-exception throwing ways")]
        private static bool TryTypeConverter(object value, Type target, out object result)
        {
            // try type converter
            TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
            if (converter != null)
            {
                if (converter.CanConvertTo(target))
                {
                    try
                    {
                        object converted = converter.ConvertTo(value, target);
                        if (converted != null && target.IsAssignableFrom(converted.GetType()))
                        {
                            result = converted;
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        // Trace.TraceError(ExceptionDetailManager.GetDetail(exception));
                        Trace.TraceError(exception.Message);
                    }
                }
            }
            // and the other way
            converter = TypeDescriptor.GetConverter(target);
            if (converter != null)
            {
                if (converter.CanConvertFrom(value.GetType()))
                {
                    try
                    {
                        object converted = converter.ConvertFrom(value);
                        if (converted != null && target.IsAssignableFrom(converted.GetType()))
                        {
                            result = converted;
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError(exception.Message);
                    }
                }
            }

            result = null;
            return false;
        }

        private static bool TryConvertEnum(object value, Type type, out object result)
        {
            Type underlyingType = Enum.GetUnderlyingType(type);
            IConvertible convertiable = value as IConvertible;

            try
            {
                if (underlyingType == typeof(Int32))
                {
                    result = Enum.ToObject(type, convertiable.ToInt32(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(Int64))
                {
                    result = Enum.ToObject(type, convertiable.ToInt64(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(Int16))
                {
                    result = Enum.ToObject(type, convertiable.ToInt16(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(Byte))
                {
                    result = Enum.ToObject(type, convertiable.ToByte(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(UInt32))
                {
                    result = Enum.ToObject(type, convertiable.ToUInt32(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(UInt64))
                {
                    result = Enum.ToObject(type, convertiable.ToUInt64(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(UInt16))
                {
                    result = Enum.ToObject(type, convertiable.ToUInt16(CultureInfo.CurrentCulture));
                    return true;
                }
                if (underlyingType == typeof(SByte))
                {
                    result = Enum.ToObject(type, convertiable.ToSByte(CultureInfo.CurrentCulture));
                    return true;
                }
            }
            catch (InvalidCastException)
            {
            }

            result = null;
            return false;
        }

        private static bool TryCastOperator(string operatorName, object value, Type type, Type targetType, out object result)
        {
            try
            {
                MethodInfo convertMethod = type.GetMethod(operatorName,
                    BindingFlags.Static | BindingFlags.Public, null,
                    new Type[] { value.GetType() }, null);
                if (convertMethod != null && convertMethod.ReturnType == targetType)
                {
                    result = convertMethod.Invoke(null, new object[] { value });
                    return true;
                }
                result = null;
                return false;
            }
            catch (Exception ex)
            {
                result = null;
                return false;
            }
        }
    }
}

