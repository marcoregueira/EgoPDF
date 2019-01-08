using System;
using System.Reflection;

namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods related to Type casting
    /// </summary>
    public class TypeSupport
    {
        #region ToDouble

        /// <summary>
        /// Converts a string to its numeric double representation.
        /// </summary>
        /// <param name="stringValue">The string value to convert.</param>
        /// <returns>Returns the numeric double representation of the string.</returns>
        public static double ToDouble(string stringValue)
        {
            double doubleValue = 0;
            if (stringValue != null)
            {
                string regStr = "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?";
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(regStr);
                if (reg.IsMatch(stringValue))
                {
                    System.Text.RegularExpressions.Match match = reg.Match(stringValue);
                    try
                    {
                        doubleValue = System.Convert.ToDouble(match.Value);
                    }
                    catch
                    {
                        doubleValue = 0;
                    }
                }
            }
            return doubleValue;
        }

        /// <summary>
        /// Converts an object to its numeric double representation.
        /// </summary>
        /// <param name="instance">The object value to convert.</param>
        /// <returns>Returns the numeric double representation of the object.</returns>
        public static double ToDouble(object instance)
        {
            double result = 0;
            if (instance != null)
            {
                if (instance is int || instance is long || instance is double || instance is bool || instance is UInt32 || instance is UInt16)
                {
                    result = System.Convert.ToDouble(instance);
                }
                else if (instance is string)
                {
                    result = ToDouble((string)instance);
                }
                else if (instance is OrderedMap)
                {
                    OrderedMap orderedMap = (OrderedMap)instance;
                    result = orderedMap.Count == 0 ? 0 : 1;
                }
                else
                {
                    result = 1;
                }
            }
            return result;
        }

        #endregion

        #region ToInt32

        /// <summary>
        /// Converts a string to its numeric integer representation.
        /// </summary>
        /// <param name="stringValue">The string value to convert.</param>
        /// <returns>Returns the numeric integer representation of the string.</returns>
        public static int ToInt32(string stringValue)
        {
            int intValue = 0;
            if (stringValue != null)
            {
                string regStr = "^[-+]?[0-9]+";
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(regStr);
                if (reg.IsMatch(stringValue))
                {
                    System.Text.RegularExpressions.Match match = reg.Match(stringValue);
                    try
                    {
                        intValue = System.Convert.ToInt32(match.Value);
                    }
                    catch
                    {
                        intValue = 0;
                    }
                }
            }
            return intValue;
        }

        /// <summary>
        /// Converts an object to its numeric int representation.
        /// </summary>
        /// <param name="instance">The object value to convert.</param>
        /// <returns>Returns the numeric int representation of the object.</returns>
        public static int ToInt32(object instance)
        {
            int result = 0;
            if (instance != null)
            {
                if (instance is int || instance is long || instance is double || instance is bool || instance is UInt32 || instance is UInt16)
                {
                    result = System.Convert.ToInt32(instance);
                }
                else if (instance is string)
                {
                    result = ToInt32((string)instance);
                }
                else if (instance is OrderedMap)
                {
                    OrderedMap orderedMap = (OrderedMap)instance;
                    result = orderedMap.Count == 0 ? 0 : 1;
                }
                else
                {
                    result = 1;
                }
            }

            return result;
        }


        #endregion

        #region ToBoolean

        /// <summary>
        /// Obtains a boolean value from a string.
        /// </summary>
        /// <param name="stringValue">The string value to convert.</param>
        /// <returns>If the string is empty or "0" returns false, otherwise returns true.</returns>
        public static bool ToBoolean(string stringValue)
        {
            bool result = false;
            if (stringValue != null)
            {
                if ((stringValue == "") || (stringValue == "0"))
                {
                    result = false;
                }
                else
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Obtains a boolean value from an object.
        /// </summary>
        /// <param name="objectValue">The object to convert to boolean.</param>
        /// <returns>Returns the boolean representation of the object.</returns>
        public static bool ToBoolean(object objectValue)
        {
            bool result = false;
            if (objectValue != null)
            {
                if (objectValue is int || objectValue is long || objectValue is double || objectValue is bool)
                {
                    result = System.Convert.ToBoolean(objectValue);
                }
                else if (objectValue is string)
                {
                    result = ToBoolean((string)objectValue);
                }
                else if (objectValue is OrderedMap)
                {
                    OrderedMap orderedMap = (OrderedMap)objectValue;
                    result = orderedMap.Count == 0 ? false : true;
                }
                else
                {
                    result = true;
                }
            }
            return result;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Obtains a string value from an object.
        /// </summary>
        /// <param name="objectValue">The object to convert to string.</param>
        /// <returns>Returns the string representation of the object.</returns>
        public static string ToString(object objectValue)
        {
            string result = "";
            if (objectValue != null)
            {
                if (objectValue is System.Text.StringBuilder)
                {
                    result = objectValue.ToString();
                }
                else if (objectValue is double || objectValue is long || objectValue is int || objectValue is string)
                {
                    result = objectValue.ToString();
                }
                else if (objectValue is bool)
                {
                    result = (bool)objectValue ? "1" : "";
                }
                else if (objectValue is OrderedMap)
                {
                    result = "Array";
                }
                else if (objectValue is Enum)
                {
                    result = ((Enum)objectValue).ToString();
                }
                else
                {
                    result = "Object";
                }
            }
            return result;
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Converts an object into an OrderedMap
        /// </summary>
        /// <param name="obj">The object to convert to OrderedMap.</param>
        /// <returns>Returns an OrderedMap representation of the object.</returns>
        public static OrderedMap ToArray(object obj)
        {
            OrderedMap result = new OrderedMap();

            if (obj != null)
            {
                if (obj is OrderedMap)
                    result = new OrderedMap((OrderedMap)obj, false);
                else if (obj is string || obj is int || obj is long || obj is double || obj is bool)
                    result = new OrderedMap(obj);
                else
                {
                    System.Reflection.FieldInfo[] fields = obj.GetType().GetTypeInfo().GetFields();
                    for (int index = 0; index < fields.Length; index++)
                        result[fields[index].Name] = fields[index].GetValue(obj);
                }
            }
            return result;
        }

        #endregion
    }
}