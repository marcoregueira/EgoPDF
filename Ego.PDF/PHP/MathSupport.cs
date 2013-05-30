namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods related to math functions.
    /// </summary>
    public class MathSupport
    {
        /// <summary>
        /// Converts a number in decimal base to any base.
        /// </summary>
        /// <param name="number">The number to be converted.</param>
        /// <param name="toBase">The new base. It can be in the range 2-36.</param>
        /// <returns>Returns the string representacion of the number in the new base.</returns>
        public static string DecToAny(double number, int toBase)
        {
            string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int digitValue;
            string result = "";

            //check base
            if (toBase < 2 || toBase > 36)
                throw new System.Exception("Invalid 'to Base'");
            else
            {
                //get the list of valid digits for the given base
                digits = digits.Substring(0, toBase);

                //convert to the other base
                while (number > 0)
                {
                    digitValue = (int) (number%(double) toBase);
                    number /= toBase;
                    result = digits.Substring(System.Convert.ToInt32(digitValue), 1) + result;
                }

                return result;
            }
        }

        /// <summary>
        /// Converts a number in any base to decimalbase.
        /// </summary>
        /// <param name="otherBaseNumber">The string representing the number to be converted.</param>
        /// <param name="fromBase">The base of the nuber to be converted.</param>
        /// <returns>Returns the representation of the number in decimal base.</returns>
        public static int AnyToDec(string otherBaseNumber, int fromBase)
        {
            string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int digitValue;
            int result = 0;

            //check Base
            if (fromBase < 2 || fromBase > 36)
                throw new System.Exception("Invalid 'from Base'");
            else
            {
                //get the list of valid digits for the given base
                digits = digits.Substring(0, fromBase);

                otherBaseNumber = otherBaseNumber.ToUpper();

                //convert to decimal
                for (int i = 0; i < otherBaseNumber.Length; i++)
                {
                    // get the digit's value
                    digitValue = digits.IndexOf(otherBaseNumber.Substring(i, 1), 0, digits.Length);
                    if (digitValue < 0)
                        throw new System.Exception("Invalid 'from Base'");
                    else
                    {
                        //add to result
                        result = result*fromBase + digitValue;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a string representing the given number in the 'toBase'.
        /// </summary>
        /// <param name="number">The string represening the number to be converted.</param>
        /// <param name="fromBase">The base of the given number.</param>
        /// <param name="toBase">The new base.</param>
        /// <returns>Returns the string representation of the number.</returns>
        public static string BaseConvert(string number, int fromBase, int toBase)
        {
            return (DecToAny(AnyToDec(number, fromBase), toBase).TrimStart(new char[] {'0'}));
        }

        /// <summary>
        /// Converts an angle in degrees to radians.
        /// </summary>
        /// <param name="angleInDegrees">The double value of angle in degrees to convert.</param>
        /// <returns>Returns the value of the angle in radians.</returns>
        public static double DegreesToRadians(double angleInDegrees)
        {
            double valueRadians = (2*System.Math.PI)/360;
            return angleInDegrees*valueRadians;
        }

        /// <summary>
        /// Converts an angle in radians to degrees.
        /// </summary>
        /// <param name="angleInRadians">The double value of angle in radians to convert.</param>
        /// <returns>Returns the value of the angle in degrees.</returns>
        public static double RadiansToDegrees(double angleInRadians)
        {
            double valueDegrees = 360/(2*System.Math.PI);
            return angleInRadians*valueDegrees;
        }

        /// <summary>
        /// Returns the floating point remainder of dividing x by y.
        /// </summary>
        /// <param name="x">The dividend.</param>
        /// <param name="y">The divisor.</param>
        /// <returns>Returns the floating point remainder of x/y.</returns>
        public static double FMod(double x, double y)
        {
            return x - (System.Math.Floor(x/y)*y);
        }

        /// <summary>
        /// Returns sqrt(x*x + y*y).
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>Returns sqrt(x*x + y*y).</returns>
        public static double Hypot(double x, double y)
        {
            return System.Math.Sqrt(x*x + y*y);
        }

        private static System.Random rand = new System.Random();

        /// <summary>
        /// Random number generator instance. The seed is initialized the first time MathSupport Class is used.
        /// </summary>
        public static System.Random Rand
        {
            get { return MathSupport.rand; }
        }

        /// <summary>
        /// Seeds the random number generator with the give seed.
        /// </summary>
        /// <param name="seed">The new seed.</param>
        public static void SeedRand(double seed)
        {
            MathSupport.rand = new System.Random((int) seed);
        }

        /// <summary>
        /// Returns the maximum or minimun value of the argument list.
        /// </summary>
        /// <param name="returnMax">The value that indicates whether to return the maximum value (true) or the minimum value (false) of the argument list.</param>
        /// <param name="args">The argument list that contains the elements to be tested.</param>
        /// <returns>The maximum or minimum value of the argument list.</returns>
        private static object _MaxMin(bool returnMax, params object[] args)
        {
            object result = null;
            if (args.Length > 0)
            {
                OrderedMap orderedMap = null;
                if (args[0] is OrderedMap)
                    orderedMap = (OrderedMap) args[0];
                else
                    orderedMap = new OrderedMap(args, false);

                OrderedMap.SortValue(ref orderedMap, OrderedMap.SORTREGULAR);
                int index = returnMax ? orderedMap.Count - 1 : 0;
                result = orderedMap.GetValueAt(index);
            }
            return result;
        }

        /// <summary>
        /// Returns the maximum value of the argument list.
        /// </summary>
        /// <param name="args">The argument list that contains the elements to be tested.</param>
        /// <returns>The maximum value of the argument list.</returns>
        public static object Max(params object[] args)
        {
            return _MaxMin(true, args);
        }

        /// <summary>
        /// Returns the minimum value of the argument list.
        /// </summary>
        /// <param name="args">The argument list that contains the elements to be tested.</param>
        /// <returns>The minimum value of the argument list.</returns>
        public static object Min(params object[] args)
        {
            return _MaxMin(false, args);
        }
    }
}