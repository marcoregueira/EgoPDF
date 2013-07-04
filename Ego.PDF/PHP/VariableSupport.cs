namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods related to variable functions.
    /// </summary>
    public class VariableSupport
    {
        /// <summary>
        /// Returns 'true' if the given parameter is empty/null or has a zero-value.
        /// Note that objects will return false at all times.
        /// </summary>
        /// <param name="obj">The object of any type.</param>
        /// <returns>Returns false if object has a non-empty or non-zero value, true otherwise.</returns>
        public static bool Empty(object obj)
        {
            if (obj != null)
            {
                if (obj is System.String)
                    return ((string) obj == System.String.Empty || (string) obj == "0");
                else if (obj is System.Int32)
                    return ((int) obj == 0);
                else if (obj is System.Double)
                    return ((double) obj == 0);
                else if (obj is System.Boolean)
                    return ((bool) obj == false);
                else if (obj.GetType() == typeof (OrderedMap))
                    return (((OrderedMap) obj).Count == 0);
                else //objects with [empty] properties returns false at all times.
                    return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Returns the float representation of the specified object.
        /// If the float value could not be retrieved, then the method will return zero.
        /// </summary>
        /// <param name="obj">The object as string.</param>
        /// <returns>Returns the float matched value.</returns>
        public static double FloatVal(object obj)
        {
            double result = 0;
            if (obj != null)
            {
                System.Text.RegularExpressions.Match match = null;
                try
                {
                    match = System.Text.RegularExpressions.Regex.Match(obj.ToString(),
                                                                       "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?");
                }
                catch (System.FormatException)
                {
                }
                result = (match.Success ? System.Convert.ToDouble(match.Value) : 0);
            }
            return result;
        }

        /// <summary>
        /// Returns the integer representation of the specified object in the specified base. 
        /// If the integer value could not be retrieved, then the method will return zero.
        /// It will work on bases 2, 8, 10 and 16 only.
        /// </summary>
        /// <param name="obj">The object as string.</param>
        /// <param name="_base">The numerical base, can be 2, 8, 10 or 16.</param>
        /// <returns>The integer matched value.</returns>
        public static int IntVal(object obj, int _base)
        {
            int result = 0;
            if (obj != null)
            {
                System.Text.RegularExpressions.Match match = null;
                try
                {
                    switch (_base)
                    {
                        case 2:
                            match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-1]+");
                            break;
                        case 8:
                            match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-7]+");
                            break;
                        case 10:
                            match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-9]+");
                            break;
                        case 16:
                            match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[a-fA-F0-9]+");
                            break;
                    }
                }
                catch (System.FormatException)
                {
                }
                if (match.Success)
                {
                    if (match.Value.IndexOf('-') != -1)
                        result = (0 - System.Convert.ToInt32(match.Value.Substring(1), _base));
                    else
                        result = System.Convert.ToInt32(match.Value, _base);
                }
                else
                    result = 0;
            }
            return result;
        }

        /// <summary>
        /// Makes no distinction between a number or a numerical string; returns true for any of these.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>Returns true if object contains a numerical value, false otherwise.</returns>
        public static bool IsNumeric(object obj)
        {
            bool result = false;
            if (obj != null)
            {
                result = System.Text.RegularExpressions.Regex.IsMatch(obj.ToString(),
                                                                      "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?$");
            }
            return result;
        }

        /// <summary>
        /// Returns true if the object passed as parameter is a scalar type, false otherwise.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>Returns true if the object is scalar, false otherwise.</returns>
        public static bool IsScalar(object obj)
        {
            return (obj is System.String ||
                    obj is System.Int32 ||
                    obj is System.Int64 ||
                    obj is System.Double ||
                    obj is System.Boolean);
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the specified value is a method or not.
        /// </summary>
        /// <param name="method">The value that may contain a method name (string) or an OrderedMap.
        /// If the value is an OrderedMap, then it should have to entries: a class instance and a method name.</param>
        /// <param name="callableName">The referenced string value used to return the method name.</param>
        /// <param name="declaringType">The method declaring type. This parameter is used when the first parameter is a string.</param>
        /// <returns>Returns a boolean value that indicates whther the specified value is a method or not.</returns>
        public static bool IsCallable(object method, ref string callableName, System.Type declaringType)
        {
            bool result = false;
            try
            {
                System.Reflection.MethodInfo methodInfo = null;

                if (method is System.String)
                {
                    callableName = method.ToString();
                    methodInfo = declaringType.GetMethod(method.ToString());
                    if (methodInfo != null)
                    {
                        callableName = methodInfo.Name;
                        result = true;
                    }
                }
                else if (method is OrderedMap)
                {
                    OrderedMap typeMethod = (OrderedMap) method;
                    callableName = typeMethod.ToString();
                    methodInfo = typeMethod.GetValueAt(0).GetType().GetMethod(typeMethod.GetValueAt(1).ToString());
                    if (methodInfo != null)
                    {
                        callableName = typeMethod.GetValueAt(0).GetType().Name + ":" + methodInfo.Name;
                        result = true;
                    }
                }
                else
                {
                    callableName = method.ToString();
                }
            }
            catch
            {
            }

            return result;
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the specified value is a method or not.
        /// </summary>
        /// <param name="method">The value that may contain a method name (string) or an OrderedMap.
        /// If the value is an OrderedMap, then it should have to entries: a class instance and a method name.</param>
        /// <param name="declaringType">The method declaring type. This parameter is used when the first parameter is a string.</param>
        /// <returns>Returns a boolean value that indicates whther the specified value is a method or not.</returns>
        public static bool IsCallable(object method, System.Type declaringType)
        {
            string callableName = "";
            return IsCallable(method, ref callableName, declaringType);
        }

        /// <summary>
        /// Returns true if the specified object is not a scalar type nor an OrderedMap.
        /// </summary>
        /// <param name="var">The object to test.</param>
        /// <returns>Returns true if the specified object is not a scalar type nor an OrderedMap.</returns>
        public static bool IsObject(object var)
        {
            return !VariableSupport.IsScalar(var) && !(var is OrderedMap);
        }

        /// <summary>
        /// Prints human-readable representation of the specified object array.
        /// </summary>
        /// <param name="vars">The object array to be printed.</param>
        public static void PrintHumanReadable(params object[] vars)
        {
            for (int index = 0; index < vars.Length; index++)
            {
                PrintHumanReadable(vars[index], false);
            }
        }

        /// <summary>
        /// Prints or returns a human-readable representation of the specified object.
        /// </summary>
        /// <param name="var">The object to be printed or returned.</param>
        /// <param name="returnValue">A boolean value that indicates whether the value string representation should be printed out or returned.</param>
        /// <returns>Returns a string value, if <code>returnValue</code> is true, then the human-readable representation is returned, otherwise returns 1.</returns>
        public static string PrintHumanReadable(object var, bool returnValue)
        {
            int indentLevel = 0;
            return _PrintHumanReadable(var, returnValue, ref indentLevel);
        }

        /// <summary>
        /// Prints or returns a human-readable representation of the specified object.
        /// </summary>
        /// <param name="var">The object to be printed or returned.</param>
        /// <param name="returnValue">A boolean value that indicates whether the value string representation should be printed out or returned</param>
        /// <param name="indentLevel">This argument is used for indentation.</param>
        /// <returns>Returns a string value, if <code>returnValue</code> is true, then the human-readable representation is returned, otherwise returns 1.</returns>
        private static string _PrintHumanReadable(object var, bool returnValue, ref int indentLevel)
        {
            string result = "";
            if (var != null)
            {
                if (!IsObject(var))
                {
                    if (var is OrderedMap)
                    {
                        int orderedMapLevel = indentLevel == 0 ? indentLevel : indentLevel + 1;
                        result = ((OrderedMap) var).ToStringContents(ref orderedMapLevel).TrimEnd() + "\r\n";
                    }
                    else
                    {
                        if (var is System.Boolean)
                            result = (bool) var ? "1" : "";
                        else
                            result = var.ToString();
                    }
                }
                else
                {
                    System.Type theType = var.GetType();

                    indentLevel++;
                    string indentSpaces = new string(' ', (indentLevel - 1)*4);
                    string classFields = theType.Name.ToLower() + " Object\r\n" + indentSpaces + "(\r\n";

                    System.Reflection.FieldInfo[] theFields = theType.GetFields();
                    foreach (System.Reflection.FieldInfo field in theFields)
                    {
                        indentSpaces = new string(' ', indentLevel*4);

                        object fieldValue = field.GetValue(var);
                        string fieldValueString = "";
                        if (fieldValue != null)
                        {
                            if (IsObject(fieldValue))
                            {
                                indentLevel++;
                                fieldValueString = _PrintHumanReadable(fieldValue, true, ref indentLevel);
                                indentLevel--;
                            }
                            else
                            {
                                fieldValueString = _PrintHumanReadable(fieldValue, true, ref indentLevel);
                            }
                        }

                        classFields += indentSpaces + "[" + field.Name + "] => " + fieldValueString + "\r\n";
                    }

                    indentSpaces = new string(' ', (indentLevel - 1)*4);
                    indentLevel--;
                    classFields += indentSpaces + ")\r\n";
                    result = classFields;
                }
            }
            if (!returnValue)
            {
                System.Web.HttpContext.Current.Response.Write(result);
                result = "1";
            }

            return result;
        }
    }
}