using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SOProfileCrawler
{
    class Program
    {
        static string apiKey { get { return Properties.Settings.Default.apiKey; } }
        static readonly string[] acceptableTypes = { "summary", "reputation", "answers", "questions", "comments" };
        static readonly string[] acceptableHelps = { "show", "save", "list", "key", "slpt", "nett", "reqt" };
        static readonly string listSeperator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

        static void Main(string[] args)
        {
            StringHandler.Setup();
            StringHandler.WriteLine("startup");

            if (apiKey.Length > 1)
                StringHandler.WriteLine("STARTUP_KEY", apiKey);

            object[] times = new object[3];
            times[0] = Properties.Settings.Default.reqInterval;
            times[1] = Properties.Settings.Default.maxSleep;
            times[2] = Properties.Settings.Default.netTimeout;
            
            Console.WriteLine("");
            StringHandler.WriteLine("TIMES_CFG", times);

            string cki;
            do
            {
                cki = GetCommand();
                cki = cki.ToLower();
                if (cki == "help" || cki.StartsWith("help "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    if (chars.Count() <= 1)
                    {
                        StringHandler.WriteLine("help");
                    }
                    else
                    {
                        string helpType = chars[1];
                        if (Array.IndexOf(acceptableHelps, helpType) != -1)
                            StringHandler.WriteLine(helpType);
                        else {
                            StringHandler.WriteLine("HELP_NOT_REC");
                            continue;
                        }
                    }

                }
                else if (cki.StartsWith("key "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    string newKey;
                    if (chars.Count() <= 1)
                    {
                        StringHandler.WriteLine("ENTER_KEY");
                        continue;
                    }
                    newKey = chars[1];
                    Properties.Settings.Default.apiKey = newKey;
                    Properties.Settings.Default.Save();
                    StringHandler.WriteLine("KEY_SAVE");
                }
                else if (cki.StartsWith("nett ") || cki.StartsWith("slpt ") || cki.StartsWith("reqt "))
                {
                    string[] chars = cki.Split(char.Parse(" "));
                    string newTime;
                    if (chars.Count() <= 1)
                    {
                        StringHandler.WriteLine("ENTER_TIME");
                        continue;
                    }
                    newTime = chars[1];
                    int nt;
                    if(!int.TryParse(newTime, out nt))
                    {
                        StringHandler.WriteLine("ENTER_TIME");
                        continue;
                    }
                    if(nt < 0)
                    {
                        StringHandler.WriteLine("ENTER_TIME");
                        continue;
                    }
                    switch(chars[0])
                    {
                        case "nett":
                            Properties.Settings.Default.netTimeout = nt;
                            break;
                        case "slpt":
                            Properties.Settings.Default.maxSleep = nt;
                            break;
                        case "reqt":
                            Properties.Settings.Default.reqInterval = nt;
                            break;
                    }
                    Properties.Settings.Default.Save();
                    StringHandler.WriteLine("TIME_SAVE");
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
                    catch
                    {
                        StringHandler.WriteLine("USER_ID_ERROR");
                        continue;
                    }

                    string dataType;
                    try { dataType = chars[2].ToLower(); }
                    catch { dataType = "summary"; }

                    if (Array.IndexOf(acceptableTypes, dataType) == -1)
                    {
                        StringHandler.WriteLine("DATA_TYPE_NOT_REC");
                        continue;
                    }

                    string filePath = string.Empty;
                    if (action == "save")
                    {
                        try
                        {
                            filePath = chars[3].ToLower();
                            filePath = cki;
                            filePath = cki.Substring(5).Substring(chars[1].Length + 1).Substring(chars[2].Length + 1);
                        }
                        catch
                        {
                            StringHandler.WriteLine("FOLDER_PATH_NOT_PROV");
                            continue;
                        }
                    }

                    ProgressBar progress = new ProgressBar();
                    Crawler crawler;
                    PageType pageType = (PageType)Enum.Parse(typeof(PageType), dataType);
                    crawler = new Crawler(pageType, userId);
                    object response = null;
                    try
                    {
                        response = crawler.StartCrawl(progress);
                        progress.Report(1);
                        System.Threading.Thread.Sleep(500);
                        progress.Dispose();
                    }
                    catch (Exception ex)
                    {
                        progress.Dispose();
                        ExceptionHandler.Handle(ex);
                        continue;
                    }

                    if (response == null) {
                        StringHandler.WriteLine("NO_RESPONSE");
                        continue;
                    }

                    if (action == "show")
                    {
                        if (response.GetType() == typeof(UserSummary))
                        {
                            UserSummary summary = (UserSummary)response;
                            StringHandler.WriteLog(summary.ToString(), ConsoleColor.DarkCyan);
                        }
                        if (response is IList)
                        {
                            IList array = (IList)response;
                            foreach (var t in array)
                                StringHandler.WriteLog(t.ToString(), ConsoleColor.DarkCyan, true);
                        }
                    }
                    else if (action == "save")
                    {
                        if (!Directory.Exists(filePath))
                        {
                            StringHandler.WriteLine("FOLDER_PATH_NOT_PROV");
                            continue;
                        }
                        filePath = GetFileName(filePath, dataType, userId);
                        string csvContent = string.Empty;
                        if (response.GetType() == typeof(UserSummary))
                        {
                            UserSummary summary = (UserSummary)response;
                            csvContent = StringHandler.GetCSV("SUMMARY_HEADER");
                            csvContent += summary.ToCsv();
                        }
                        if (response is IList)
                        {
                            if (response.GetType() == typeof(List<UserAnswer>))
                            {
                                csvContent = StringHandler.GetCSV("ANSWER_HEADER");
                                foreach (var t in (List<UserAnswer>)response)
                                    csvContent += t.ToCsv();
                            }
                            else if (response.GetType() == typeof(List<UserQuestion>))
                            {
                                csvContent = StringHandler.GetCSV("QUESTION_HEADER");
                                foreach (var t in (List<UserQuestion>)response)
                                    csvContent += t.ToCsv();
                            }
                            else if (response.GetType() == typeof(List<UserReputation>))
                            {
                                csvContent = StringHandler.GetCSV("REP_HEADER");
                                foreach (var t in (List<UserReputation>)response)
                                    csvContent += t.ToCsv();
                            }
                            else if (response.GetType() == typeof(List<UserComment>))
                            {
                                csvContent = StringHandler.GetCSV("COMMENT_HEADER");
                                foreach (var t in (List<UserComment>)response)
                                    csvContent += t.ToCsv();
                            }
                        }

                        try
                        {
                            using (var tw = new StreamWriter(filePath, true))
                                tw.Write(csvContent);
                            StringHandler.WriteLine("FILE_SAVED", filePath);
                        }
                        catch
                        {
                            StringHandler.WriteLine("FILE_NO_ACCESS");
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
                    catch
                    {
                        StringHandler.WriteLine("PROVIDE_FILE_PATH");
                        continue;
                    }

                    try
                    {
                        dataType = chars[1].ToLower();
                    }
                    catch
                    {
                        dataType = "summary";
                    }

                    if (Array.IndexOf(acceptableTypes, dataType) == -1)
                    {
                        StringHandler.WriteLine("DATA_TYPE_NOT_REC");
                        continue;
                    }

                    if (!File.Exists(listPath))
                    {
                        StringHandler.WriteLine("FILE_NO_ACCESS");
                        continue;
                    }
                    FileInfo info = new FileInfo(listPath);
                    if (info.Extension.ToLower() != ".csv")
                    {
                        StringHandler.WriteLine("LIST_MUST_CSV");
                        continue;
                    }

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
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    if (userIds.Count <= 0)
                    {
                        StringHandler.WriteLine("CANT_FIND_ID");
                        continue;
                    }

                    StringBuilder stringBuilder = new StringBuilder();
                    string header;
                    switch (dataType)
                    {
                        case "summary":
                            header = StringHandler.GetCSV("SUMMARY_HEADER");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "reputation":
                            header = StringHandler.GetCSV("REP_HEADER");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "answers":
                            header = StringHandler.GetCSV("ANSWER_HEADER");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "questions":
                            header = StringHandler.GetCSV("QUESTION_HEADER");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                        case "comments":
                            header = StringHandler.GetCSV("COMMENT_HEADER");
                            stringBuilder.Append(string.Format("User ID{0}{1}", listSeperator, header));
                            break;
                    }

                    int count = 1;
                    PageType pageType = (PageType)Enum.Parse(typeof(PageType), dataType);
                    foreach (int ui in userIds)
                    {
                        StringHandler.Write("GETTING_DATA", new object[] { dataType, ui, count });
                        count += 1;
                        ProgressBar progress = new ProgressBar();
                        Crawler cr = null;
                        cr = new Crawler(pageType, ui);

                        object response = null;

                        try
                        {
                            response = cr.StartCrawl(progress);
                            progress.Report(1);
                            System.Threading.Thread.Sleep(100);
                            progress.Dispose();
                        }
                        catch(Exception ex)
                        {
                            progress.Dispose();
                            ExceptionHandler.Handle(ex);
                        }
                        
                       
                        if (response == null)
                        {
                            StringHandler.Write("NO_DATA");
                        }
                        else
                        {
                            if (response.GetType() == typeof(UserSummary))
                            {
                                UserSummary summary = (UserSummary)response;
                                stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, summary.ToCsv()));
                            }
                            if (response is IList)
                            {
                                if (response.GetType() == typeof(List<UserAnswer>))
                                    foreach (var t in (List<UserAnswer>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<UserQuestion>))
                                    foreach (var t in (List<UserQuestion>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<UserReputation>))
                                    foreach (var t in (List<UserReputation>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                                else if (response.GetType() == typeof(List<UserComment>))
                                    foreach (var t in (List<UserComment>)response)
                                        stringBuilder.Append(string.Format("{0}{1}{2}", ui, listSeperator, t.ToCsv()));
                            }
                        }
                        StringHandler.Write("DONE");
                    }

                    string filePath = GetFileName(info.Directory.FullName, dataType);

                    try
                    {
                        using (var tw = new StreamWriter(filePath, true))
                            tw.Write(stringBuilder.ToString());
                        StringHandler.WriteLine("FILE_SAVED", filePath);
                    }
                    catch
                    {
                        StringHandler.WriteLine("FILE_NO_ACCESS");
                    }

                }
                else { StringHandler.WriteLine("CMD_NOT_REC"); }
            } while (cki != "exit");


        }
        static string GetCommand()
        {
            Console.WriteLine("");
            StringHandler.Write("ENT_CMD");
            string line = Console.ReadLine();
            return line;
        }
        static string GetFileName(string directoryPath, string dataType, int userId = 0)
        {
            string filePath = string.Empty;
            if (userId != 0)
                filePath = string.Format("{0}\\{1}_{2}", directoryPath, userId, dataType).Replace("\\\\", "\\").Replace("\\\\", "\\");
            else
                filePath = string.Format("{0}\\{1}", directoryPath, dataType).Replace("\\\\", "\\").Replace("\\\\", "\\");

            while (File.Exists(filePath + ".csv"))
                filePath += "_" + Guid.NewGuid().ToString().ToUpper().Substring(0, 5);
            return filePath + ".csv";
        }
    }



}
