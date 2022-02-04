using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SOProfileCrawler
{
    public static class StringHandler
    {
        static Strings data;
        public static string ListSeperator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        public static void Setup()
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string jsonPath = string.Format("{0}/{1}", exePath, "strings.json");
            string jsonData = string.Empty;
            try
            {
                using (StreamReader sr = File.OpenText(jsonPath))
                    jsonData = sr.ReadToEnd();
                data = JsonConvert.DeserializeObject<Strings>(jsonData);
            }
            catch (Exception ex)
            { Console.WriteLine(string.Format("Error = {0} \n Press any key to terminate"), ex.Message); }

        }
        public static void WriteLine(string what, object format = null)
        {
            IList<string> init = null;
            init = data.all.Where(x => x.key == what).FirstOrDefault().values;
            string colorString = data.all.Where(x => x.key == what).FirstOrDefault().color;
            ConsoleColor color = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colorString);
            Console.ForegroundColor = color;
            string[] fmt = null;
            if (format != null)
            {
                if (typeof(object[]) == format.GetType())
                    fmt = Array.ConvertAll((object[])format, ConvertObjectToString);
                else
                    fmt = new string[] { format.ToString() };
            }
            
            foreach (string sx in init)
                if (fmt == null)
                    Console.WriteLine(sx);
                else 
                    Console.WriteLine(sx, fmt);
        }
        public static void Write(string what, object format = null)
        {
            IList<string> init = null;
            init = data.all.Where(x => x.key == what).FirstOrDefault().values;
            ConsoleColor color = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), data.all.Where(x => x.key == what).FirstOrDefault().color);
            Console.ForegroundColor = color;
            string[] fmt = null;
            if (format != null)
            {
                if (typeof(object[]) == format.GetType())
                    fmt = Array.ConvertAll((object[])format, ConvertObjectToString);
                else
                    fmt = new string[] { format.ToString() };
            }
            foreach (string sx in init)
                if (fmt == null)
                    Console.Write(sx);
                else
                    Console.Write(sx, fmt);
        }
        public static void WriteLog(string what, ConsoleColor color, bool lb = false)
        {
            Console.ForegroundColor = color;
            Console.Write(what);
            if (lb)
                Console.Write("\n");
        }
        public static string GetCSV(string what)
        {
            string csv = string.Empty;
            IList<string> init = null;
            init = data.all.Where(x => x.key == what).FirstOrDefault().values;
            foreach (string sx in init)
                csv += sx.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return csv;
        }
        public static string BuildFormat(int count, bool slashN = false)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < count; i++)
                stringBuilder.Append(string.Format("{0}{1}", "{" + i.ToString() + "}", ListSeperator));
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            if (slashN)
                stringBuilder.Append("\n");
            return stringBuilder.ToString();
        }
        public static string StripHtmlTags(string source)
        {
            return Regex.Replace(source, "<.*?>|&.*?;", string.Empty);
        }
        private static string ConvertObjectToString(object obj)
        {
            return obj?.ToString() ?? string.Empty;
        }
    }
    public class All
    {
        public string key { get; set; }
        public IList<string> values { get; set; }
        public string color { get; set; }
    }
    public class Strings
    {
        public IList<All> all { get; set; }
    }
}
