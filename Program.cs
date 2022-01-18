using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SOProfileCrawler
{
    class Program
    {
        static string apiKey { get { return Properties.Settings.Default.apiKey; } }
        static readonly string[] acceptableTypes = { "summary", "reputation", "answers", "questions", "comments" };
        static readonly string[] acceptableHelps = { "show", "save", "list", "key" };
        static readonly string listSeperator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        static StringHandler handler;
        static void Main(string[] args)
        {
            handler = new StringHandler();
            handler.WriteLine("startup");

            if (apiKey.Length > 1)
                Console.WriteLine(string.Format("Your current key is {0} - If you wish to change it, Run KEY command", apiKey));

            string cki;
            do
            {
                cki = GetCommand();
                cki = cki.ToLower();
                if (cki == "help" || cki.StartsWith("help "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    if(chars.Count() <= 1)
                    {
                        handler.WriteLine("help");
                    }
                    else
                    {
                        string helpType = chars[1];
                        if(Array.IndexOf(acceptableHelps, helpType) != -1)
                            handler.WriteLine(helpType);
                        else { handler.WriteLine("HELP_NOT_REC"); continue; }
                    }
                    
                }
                else if (cki.StartsWith("key "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    string newKey;
                    if (chars.Count() <= 1)
                    {
                        Console.WriteLine("Please enter your key! ");
                        continue;
                    }
                    newKey = chars[1];
                    Properties.Settings.Default.apiKey = newKey;
                    Properties.Settings.Default.Save();
                    Console.WriteLine("Key Saved!");
                }
                else if (cki.StartsWith("show ") || cki.StartsWith("save "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    string action = chars[0];
                    string userIdString;
                    int userId;
                    try
                    {
                        userIdString = chars[1];
                        if (!int.TryParse(userIdString, out userId)) { throw new Exception(""); }

                    }
                    catch { Console.WriteLine("UserId must be a Number! \n ====="); continue; }

                    string dataType;
                    try { dataType = chars[2].ToLower(); }
                    catch { dataType = "summary"; }

                    if (Array.IndexOf(acceptableTypes, dataType) == -1)
                    { Console.WriteLine("Data type not recognized! \n ====="); continue; }

                    string filePath = string.Empty;
                    if (action == "save")
                    {
                        try {
                            filePath = chars[3].ToLower();
                            filePath = cki;
                            filePath = cki.Substring(5).Substring(chars[1].Length + 1).Substring(chars[2].Length + 1);
                        }
                        catch { Console.WriteLine("Folder path must be provided! \n ====="); continue; }
                    }

                    ProgressBar progress = new ProgressBar();
                    Crawler cr = null;
                    if (dataType == "summary")
                        cr = new Crawler(PageType.summary, userId);
                    if (dataType == "reputation")
                        cr = new Crawler(PageType.reputation, userId);
                    if (dataType == "answers")
                        cr = new Crawler(PageType.answers, userId);
                    if (dataType == "questions")
                        cr = new Crawler(PageType.questions, userId);
                    if (dataType == "comments")
                        cr = new Crawler(PageType.comments, userId);

                    object response = cr.StartCrawl(progress);
                    progress.Report(1);
                    System.Threading.Thread.Sleep(500);
                    progress.Dispose();

                    if (response == null)
                    {
                        Console.WriteLine("There was no response OR There was a problem getting data! \n =====");
                        continue;
                    }

                    if (action == "show")
                    {
                        if (response.GetType() == typeof(PrSum))
                        {
                            PrSum summary = (PrSum)response;
                            Console.WriteLine(summary);
                        }
                        if (response is IList)
                        {
                            IList array = (IList)response;
                            foreach (var t in array)
                                Console.WriteLine(t.ToString());
                        }
                    }
                    else if (action == "save")
                    {
                        if (!Directory.Exists(filePath)) { Console.WriteLine("Folder path must be provided! \n ====="); continue; }
                        filePath = GetFileName(filePath, dataType, userId);
                        string csvContent = string.Empty;
                        string listSeperator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                        if (response.GetType() == typeof(PrSum))
                        {
                            PrSum summary = (PrSum)response;
                            csvContent = string.Format("Username{0}Reputation{0}Gold Badge{0}Silver Badge{0}Bronze Badge{0}Answer Count{0}Question Count\n", listSeperator);
                            csvContent += summary.ToCsv();
                        }
                        if (response is IList)
                        {
                            if (response.GetType() == typeof(List<Ans>))
                            {
                                csvContent = string.Format("Answer Date{0}Vote Count{0}Is Accepted\n", listSeperator);
                                foreach (var t in (List<Ans>)response)
                                    csvContent += t.ToCsv();
                            }
                            else if (response.GetType() == typeof(List<Qtn>))
                            {
                                csvContent = string.Format("Ask Date{0}Answer Count{0}Vote Count{0}Has Answer{0}View Count{0}Bookmark Count\n", listSeperator);
                                foreach (var t in (List<Qtn>)response)
                                    csvContent += t.ToCsv();
                            }
                            else if (response.GetType() == typeof(List<Rpt>))
                            {
                                csvContent = string.Format("Earned Date{0}Value\n", listSeperator);
                                foreach (var t in (List<Rpt>)response)
                                    csvContent += t.ToCsv();
                            }
                        }

                        try
                        {
                            using (var tw = new StreamWriter(filePath, true))
                                tw.Write(csvContent);
                            Console.WriteLine(string.Format("File saved in {0}", filePath));
                        }
                        catch
                        {
                            Console.WriteLine("Could not access file path! \n =====");
                        }
                    }
                }
                else if (cki.StartsWith("list "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    string listPath, dataType;

                    try
                    {
                        listPath = chars[2];
                        listPath = cki;
                        listPath = cki.Substring(5).Substring(chars[1].Length + 1);
                    }
                    catch { Console.WriteLine("Please provide a file path! \n ====="); continue; }

                    try { dataType = chars[1].ToLower(); }
                    catch { dataType = "summary"; }

                    if (Array.IndexOf(acceptableTypes, dataType) == -1)
                    { Console.WriteLine("Data type not recognized! \n ====="); continue; }

                    if (!File.Exists(listPath)) { Console.WriteLine("File is not accessible! \n ====="); continue; }
                    FileInfo info = new FileInfo(listPath);
                    if (info.Extension.ToLower() != ".csv") { Console.WriteLine("List must be a csv file! \n ====="); continue; }

                    List<int> userIds = new List<int>();
                    using (StreamReader sr = new StreamReader(listPath))
                    {
                        string currentLine;
                        while ((currentLine = sr.ReadLine()) != null)
                        {
                            string[] values = currentLine.Split(char.Parse(CultureInfo.CurrentCulture.TextInfo.ListSeparator));
                            int uid;
                            try
                            {
                                if (int.TryParse(values[0], out uid))
                                    userIds.Add(uid);
                            }
                            catch { continue; }
                        }
                    }

                    if (userIds.Count <= 0) { Console.WriteLine("Could not find any IDs in file! \n ====="); continue; }

                    StringBuilder stringBuilder = new StringBuilder();
                    string format;
                    string header;
                    switch (dataType)
                    {
                        case "summary":
                            format = "{0},{1},{2},{3},{4},{5},{6},{7},{8}\n";
                            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            header = string.Format(format, "Username", "Reputation", "Gold Badge Count", "Silver Badge Count",
                                "Bronze Badge Count", "Answer Count", "Question Count", "Age", "Signup Date");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "reputation":
                            format = "{0},{1}\n";
                            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            header = string.Format(format, "Earn Date", "Value");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "answers":
                            format = "{0},{1},{2},{3}\n";
                            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            header = string.Format(format, "Answer Date", "Upvote Count", "Downvote Count", "Is Accepted");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "questions":
                            format = "{0},{1},{2},{3},{4},{5},{6}\n";
                            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            header = string.Format(format, "Ask Date", "Answer Count", "Upvote Count", "Downvote Count", "Has Answer",
                                        "View Count", "Bookmark Count");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "comments":
                            format = "{0},{1}\n";
                            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            header = string.Format(format, "Post Date", "Score");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                    }

                    int count = 1;
                    foreach (int ui in userIds)
                    {
                        Console.Write("{2} : Getting {0} data for user {1} =>  ", dataType, ui, count);
                        count += 1;
                        ProgressBar progress = new ProgressBar();
                        Crawler cr = null;
                        if (dataType == "summary")
                            cr = new Crawler(PageType.summary, ui);
                        if (dataType == "reputation")
                            cr = new Crawler(PageType.reputation, ui);
                        if (dataType == "answers")
                            cr = new Crawler(PageType.answers, ui);
                        if (dataType == "questions")
                            cr = new Crawler(PageType.questions, ui);
                        if (dataType == "comments")
                            cr = new Crawler(PageType.comments, ui);

                        object response = cr.StartCrawl(progress);
                        progress.Report(1);
                        System.Threading.Thread.Sleep(100);
                        progress.Dispose();
                        if (response == null) { Console.Write("No data found!  "); }
                        else
                        {
                            if (response.GetType() == typeof(PrSum))
                            {
                                PrSum summary = (PrSum)response;
                                stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, summary.ToCsv()));
                            }
                            if (response is IList)
                            {
                                if (response.GetType() == typeof(List<Ans>))
                                    foreach (var t in (List<Ans>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<Qtn>))
                                    foreach (var t in (List<Qtn>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<Rpt>))
                                    foreach (var t in (List<Rpt>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<Cmt>))
                                    foreach (var t in (List<Cmt>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                            }
                        }
                        Console.Write(" Done!\n");
                    }

                    string filePath = GetFileName(info.Directory.FullName, dataType);

                    try
                    {
                        using (var tw = new StreamWriter(filePath, true))
                            tw.Write(stringBuilder.ToString());
                        Console.WriteLine(string.Format("File saved in {0}", filePath));
                    }
                    catch { Console.WriteLine("Could not access file path! \n ====="); }

                }
                else { Console.WriteLine("Command not recognized! \n ====="); }
            } while (cki != "exit");
            

        }
        static string GetCommand()
        {
            handler.Write("ENT_CMD");
            string line = Console.ReadLine();
            return line;
        }
        static string GetFileName(string directoryPath, string dataType, int userId = 0)
        {
            string filePath = string.Empty;
            if(userId == 0)
                filePath = string.Format("{0}\\{1}_{2}", directoryPath, userId, dataType).Replace("\\\\", "\\").Replace("\\\\", "\\");
            else
                filePath = string.Format("{0}\\{1}", directoryPath, dataType).Replace("\\\\", "\\").Replace("\\\\", "\\");

            while (File.Exists(filePath + ".csv"))
                filePath += Guid.NewGuid().ToString().ToUpper().Substring(0, 5);
            return filePath + ".csv";
        }
    }

    public class StringHandler
    {
        Strings data;
       
        public StringHandler()
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string jsonPath = string.Format("{0}/{1}",exePath, "strings.json");
            string jsonData = string.Empty;
            try
            {
                using (StreamReader sr = File.OpenText(jsonPath))
                    jsonData = sr.ReadToEnd();
                data = JsonConvert.DeserializeObject<Strings>(jsonData);
            }
            catch(Exception ex)
            { Console.WriteLine(string.Format("Error = {0} \n Press any key to terminate"), ex.Message); }
            
        }
        public void WriteLine(string what)
        {
            IList<string> init = null;
            init = data.all.Where(x => x.key == what).FirstOrDefault().values;
            foreach (string sx in init)
                Console.WriteLine(sx);
        }
        public void Write(string what)
        {
            IList<string> init = null;
            init = data.all.Where(x => x.key == what).FirstOrDefault().values;
            foreach (string sx in init)
                Console.Write(sx);
        }
    }
    public class All
    {
        public string key { get; set; }
        public IList<string> values { get; set; }
    }
    public class Strings
    {
        public IList<All> all { get; set; }
    }
}
