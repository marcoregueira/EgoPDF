namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods related to miscellaneous functions.
    /// </summary>
    public class MiscSupport
    {
        /// <summary>
        /// Searches (using reflection) for the value of the specified string as a defined constant.
        /// </summary>
        /// <param name="constant">The name of the constant to search.</param>
        /// <param name="mb">The current method, required for reflection.</param>
        /// <returns>Returns the value of the constant, or an empty string if not defined.</returns>
        public static string Constant(string constant, System.Reflection.MethodBase method)
        {
            System.Reflection.FieldInfo field = method.DeclaringType.GetField(constant);
            return ((field == null) ? System.String.Empty : field.GetValue(field.GetType()).ToString());
        }

        /// <summary>
        /// Displays the contents of the specified file as HTML code.
        /// </summary>
        /// <param name="filename">The relative path of the file.</param>
        public static void ShowSourceFile(string filename)
        {
            try
            {
                using (
                    System.IO.StreamReader sr =
                        System.IO.File.OpenText(
                            System.Web.HttpContext.Current.Request.MapPath(
                                System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        System.Web.HttpContext.Current.Response.Write(
                            System.Web.HttpContext.Current.Server.HtmlEncode(line) + "<br>");
                }
            }
            catch
            {
                System.Web.HttpContext.Current.Response.Write(
                    System.Web.HttpContext.Current.Request.MapPath(
                        System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename +
                    ", Failed to Open Stream.");
            }
        }

        /// <summary>
        /// Returns the contents of the specified file as HTML code.
        /// </summary>
        /// <param name="filename">The relative path of the file.</param>
        /// <returns>Returns the contents of the specified file.</returns>
        public static string GetSourceFile(string filename)
        {
            string ret = "";
            try
            {
                using (
                    System.IO.StreamReader sr =
                        System.IO.File.OpenText(
                            System.Web.HttpContext.Current.Request.MapPath(
                                System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        ret += (System.Web.HttpContext.Current.Server.HtmlEncode(line) + "<br>");
                }
            }
            catch
            {
                ret +=
                    (System.Web.HttpContext.Current.Request.MapPath(
                        System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename +
                     ", Failed to Open Stream.");
            }
            return ret;
        }

        /// <summary>
        /// Returns an OrderedMap with the browser information the user was expecting.
        /// </summary>
        /// <returns>Returns an OrderedMap with browser information.</returns>
        public static OrderedMap GetBrowserInfo()
        {
            OrderedMap browserInfo = new OrderedMap();
            browserInfo.Add("browser_name_pattern",
                            System.Web.HttpContext.Current.Request.Browser.Browser +
                            System.Web.HttpContext.Current.Request.Browser.Version);
            browserInfo.Add("browser", System.Web.HttpContext.Current.Request.Browser.Browser);
            browserInfo.Add("version", System.Web.HttpContext.Current.Request.Browser.Version);
            browserInfo.Add("majorver", System.Web.HttpContext.Current.Request.Browser.MajorVersion.ToString());
            browserInfo.Add("minorver", System.Web.HttpContext.Current.Request.Browser.MinorVersion.ToString());
            browserInfo.Add("frames", System.Web.HttpContext.Current.Request.Browser.Frames ? "1" : "0");
            browserInfo.Add("tables", System.Web.HttpContext.Current.Request.Browser.Tables ? "1" : "0");
            browserInfo.Add("cookies", System.Web.HttpContext.Current.Request.Browser.Cookies ? "1" : "0");
            browserInfo.Add("backgroundsounds",
                            System.Web.HttpContext.Current.Request.Browser.BackgroundSounds ? "1" : "0");
            browserInfo.Add("vbscript", System.Web.HttpContext.Current.Request.Browser.VBScript ? "1" : "0");
            browserInfo.Add("javascript", System.Web.HttpContext.Current.Request.Browser.JavaScript ? "1" : "0");
            browserInfo.Add("javaapplets", System.Web.HttpContext.Current.Request.Browser.JavaApplets ? "1" : "0");
            browserInfo.Add("activexcontrols",
                            System.Web.HttpContext.Current.Request.Browser.ActiveXControls ? "1" : "0");
            browserInfo.Add("cdf", System.Web.HttpContext.Current.Request.Browser.CDF ? "1" : "0");
            browserInfo.Add("aol", System.Web.HttpContext.Current.Request.Browser.AOL ? "1" : "0");
            browserInfo.Add("beta", System.Web.HttpContext.Current.Request.Browser.Beta ? "1" : "0");
            browserInfo.Add("win16", System.Web.HttpContext.Current.Request.Browser.Win16 ? "1" : "0");
            browserInfo.Add("crawler", System.Web.HttpContext.Current.Request.Browser.Crawler ? "1" : "0");
            browserInfo.Add("netclr", System.Web.HttpContext.Current.Request.Browser.ClrVersion.ToString());

            return browserInfo;
        }

        /// <summary>
        /// Prints out the specified message and halts the execution of the page.
        /// </summary>
        /// <param name="message">The message to print out.</param>
        /// <returns>Always returns false.</returns>
        /// <remarks>This method is only used when the original 'exitt' function is used in an expression.</remarks>
        public static bool End(string message)
        {
            System.Web.HttpContext.Current.Response.Write(message);
            System.Web.HttpContext.Current.Response.End();
            return false;
        }

        /// <summary>
        /// Halts the execution of the page.
        /// </summary>
        /// <returns>Always returns false.</returns>
        /// <remarks>This method is only used when the original 'exitt' function is used in an expression.</remarks>
        public static bool End()
        {
            System.Web.HttpContext.Current.Response.End();
            return false;
        }
    }
}