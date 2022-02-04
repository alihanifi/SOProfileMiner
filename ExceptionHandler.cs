using System;

namespace SOProfileCrawler
{
    public static class ExceptionHandler
    {
        public static void Handle(Exception exception)
        {
            if (typeof(AppEx) == exception.GetType())
            {
                AppEx appEx = (AppEx)exception;
                switch (appEx.Code)
                {
                    case 1:
                        StringHandler.WriteLine("NO_INTERNET");
                        break;
                    case 2:
                        StringHandler.WriteLine("NO_RESPONSE");
                        break;
                    case 3:
                        StringHandler.WriteLine("NO_DATA");
                        break;
                    default:
                        break;
                }
            }
            else
            {
                StringHandler.WriteLog(exception.Message, ConsoleColor.Red);
            }
        }
    }
    public class AppEx : ApplicationException
    {
        public int Code { get; set; }
        public AppEx(int code)
        {
            Code = code;
        }
    }
}
