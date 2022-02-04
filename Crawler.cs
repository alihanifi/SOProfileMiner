using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using StackExchange.StacMan;

namespace SOProfileCrawler
{
    public class Crawler
    {
        static string APIKey { get { return Properties.Settings.Default.apiKey; } }
        static int ReqInterval { get { return Properties.Settings.Default.reqInterval; } }
        static int MaxSleep { get { return Properties.Settings.Default.maxSleep; } }

        PageType CrawlingType;
        int ProfileId;

        public Crawler(PageType pageType, int prfId)
        {
            CrawlingType = pageType;
            ProfileId = prfId;
        }
        public object StartCrawl(ProgressBar progress)
        {
            if (!CheckInternet())
                throw new AppEx(1);

            List<int> profileIds = new List<int> { ProfileId };

            int page = 1;
            int pageSize = 100;
            int total;
            switch (CrawlingType)
            {
                case PageType.summary:
                    var uResponse = GetUsers(profileIds);
                    progress.Report(0.1);
                    System.Threading.Thread.Sleep(ReqInterval * 2);
                    if (uResponse.Result.Success)
                    {
                        if(!CheckQuota(uResponse.Result.Data))
                            throw new AppEx(3);
                        User u = uResponse.Result.Data.Items.FirstOrDefault();
                        return new UserSummary(u.DisplayName, u.Reputation, u.BadgeCounts.Gold, u.BadgeCounts.Silver,
                                                u.BadgeCounts.Bronze, u.AnswerCount, u.QuestionCount, u.CreationDate);
                    }
                    else
                    {
                        throw new AppEx(2);
                    }
                    
                case PageType.reputation:
                    var repResponse = GetReputation(profileIds, page);
                    progress.Report(0.1);
                    System.Threading.Thread.Sleep(ReqInterval * 2);
                    List<Reputation> allReps = new List<Reputation>();
                    if (repResponse.Result.Success)
                    {
                        if (!CheckQuota(repResponse.Result.Data))
                            throw new AppEx(3);

                        total = repResponse.Result.Data.Total;
                        allReps.AddRange(repResponse.Result.Data.Items);

                        while(repResponse.Result.Data.HasMore && total > pageSize)
                        {
                            repResponse = GetReputation(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(ReqInterval);
                            if (repResponse.Result.Success)
                            {
                                if (!CheckQuota(repResponse.Result.Data))
                                    continue;
                                allReps.AddRange(repResponse.Result.Data.Items);
                                progress.Report(CalculateProgress(page, total, 0.1));
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    else
                    {
                        throw new AppEx(2);
                    }
                    List<UserReputation> reps = new List<UserReputation>();
                    foreach (var t in allReps)
                        reps.Add(new UserReputation(t.OnDate, t.ReputationChange));

                    return reps;
                case PageType.answers:
                    var ansResponse = GetAnswers(profileIds, page);
                    progress.Report(0.1);
                    System.Threading.Thread.Sleep(ReqInterval * 2);
                    List<Answer> allAnswers = new List<Answer>();
                    if (ansResponse.Result.Success)
                    {
                        if (!CheckQuota(ansResponse.Result.Data))
                            throw new AppEx(3);

                        total = ansResponse.Result.Data.Total;
                        allAnswers.AddRange(ansResponse.Result.Data.Items);

                        while (ansResponse.Result.Data.HasMore && total > pageSize)
                        {
                            ansResponse = GetAnswers(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(ReqInterval);
                            if (ansResponse.Result.Success)
                            {
                                if (!CheckQuota(ansResponse.Result.Data))
                                    continue;
                                allAnswers.AddRange(ansResponse.Result.Data.Items);
                                progress.Report(CalculateProgress(page, total, 0.1));
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    else
                    {
                        throw new AppEx(2);
                    }
                    List<UserAnswer> answers = new List<UserAnswer>();
                    foreach (var t in allAnswers)
                        answers.Add(new UserAnswer(t.AnswerId, t.CreationDate, t.UpVoteCount, t.DownVoteCount, t.IsAccepted, t.Body));

                    return answers;
                case PageType.questions:
                    var QtResponse = GetQuestions(profileIds, page);
                    progress.Report(0.1);
                    List<Question> allQs = new List<Question>();
                    System.Threading.Thread.Sleep(ReqInterval * 2);

                    if (QtResponse.Result.Success)
                    {
                        total = QtResponse.Result.Data.Total;
                        if (!CheckQuota(QtResponse.Result.Data))
                            throw new AppEx(3);

                        allQs.AddRange(QtResponse.Result.Data.Items);

                        while (QtResponse.Result.Data.HasMore && total > pageSize)
                        {
                            QtResponse = GetQuestions(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(ReqInterval);
                            if (QtResponse.Result.Success)
                            {
                                if (!CheckQuota(QtResponse.Result.Data))
                                    continue;
                                allQs.AddRange(QtResponse.Result.Data.Items);
                                progress.Report(CalculateProgress(page, total, 0.1));
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    else
                    {
                        throw new AppEx(2);
                    }
                    List<UserQuestion> questions = new List<UserQuestion>();
                    foreach (var t in allQs)
                        questions.Add(new UserQuestion(t.QuestionId ,t.CreationDate, t.AnswerCount, t.UpVoteCount, t.DownVoteCount,
                                                t.IsAnswered, t.ViewCount, t.Body));

                    return questions;
                
                case PageType.comments:
                    var CtResponse = GetComments(profileIds, page);
                    progress.Report(0.1);
                    List<Comment> allCs = new List<Comment>();
                    System.Threading.Thread.Sleep(ReqInterval * 2);
                    
                    if (CtResponse.Result.Success)
                    {
                        if(!CheckQuota(CtResponse.Result.Data))
                            throw new AppEx(3);

                        total = CtResponse.Result.Data.Total;
                        allCs.AddRange(CtResponse.Result.Data.Items);

                        while (CtResponse.Result.Data.HasMore && total > pageSize)
                        {
                            CtResponse = GetComments(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(ReqInterval);
                            if (CtResponse.Result.Success)
                            {
                                if (!CheckQuota(CtResponse.Result.Data))
                                    continue;
                                allCs.AddRange(CtResponse.Result.Data.Items);
                                progress.Report(CalculateProgress(page, total, 0.1));
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    else
                    {
                        throw new AppEx(2);
                    }
                    List<UserComment> comments = new List<UserComment>();
                    foreach (var t in allCs)
                        comments.Add(new UserComment(t.CommentId, t.Body, t.Score, t.CreationDate));

                    return comments;
            }
            return null;
        }
        private double CalculateProgress(int page, int total, double f)
        {
            return (double)page * ((100 - f * 100) / (double)total) + f;
        }
        private async Task<StacManResponse<User>> GetUsers(List<int> users)
        {
            var task = new StacManClient(key: APIKey, version: "2.1");
            var response = task.Users.GetByIds("stackoverflow", users, filter: "!*MZqiH2U.gWydEj0");
            return await response;
        }
        private async Task<StacManResponse<Answer>> GetAnswers(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: APIKey, version: "2.1");
            if(pagesize == 0)
                return await task.Users.GetAnswers("stackoverflow", users, filter: "!T1gn2_fTGr.5zKUrK*");
            else
                return await task.Users.GetAnswers("stackoverflow", users, filter: "!T1gn2_fTGr.5zKUrK*", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Question>> GetQuestions(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: APIKey, version: "2.1");
            if (pagesize == 0)
                return await task.Users.GetQuestions("stackoverflow", users, filter: "!T1gn2_ZGc-8J4jlhfl");
            else
                return await task.Users.GetQuestions("stackoverflow", users, filter: "!T1gn2_ZGc-8J4jlhfl", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Reputation>> GetReputation(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: APIKey, version: "2.1");
            if(pagesize == 0)
                return await task.Users.GetReputation("stackoverflow", users, filter: "!nKzQUR693x");
            else
                return await task.Users.GetReputation("stackoverflow", users, filter: "!nKzQUR693x", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Comment>> GetComments(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: APIKey, version: "2.1");
            if (pagesize == 0)
                return await task.Users.GetComments("stackoverflow", users, filter: "!6VvPDzRe9hsP4");
            else
                return await task.Users.GetComments("stackoverflow", users, filter: "!6VvPDzRe9hsP4", page: page, pagesize: pagesize);
        }
        private bool CheckInternet(int timeoutMs = 2000)
        {
            try
            {
                var stUrl = "http://stackoverflow.com/";
                var request = (HttpWebRequest)WebRequest.Create(stUrl);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch { return false; }
        }
        private bool CheckQuota(object wrapper)
        {
            int? backOff = null;
            int quotaRemaining = 0;
            if (wrapper.GetType() == typeof(Wrapper<Answer>))
            {
                Wrapper<Answer> wrp = (Wrapper<Answer>)wrapper;
                backOff = wrp.Backoff;
                quotaRemaining = wrp.QuotaRemaining;
            }
            else if (wrapper.GetType() == typeof(Wrapper<Question>))
            {
                Wrapper<Question> wrp = (Wrapper<Question>)wrapper;
                backOff = wrp.Backoff;
                quotaRemaining = wrp.QuotaRemaining;
            }
            else if (wrapper.GetType() == typeof(Wrapper<Reputation>))
            {
                Wrapper<Reputation> wrp = (Wrapper<Reputation>)wrapper;
                backOff = wrp.Backoff;
                quotaRemaining = wrp.QuotaRemaining;
            }
            else if (wrapper.GetType() == typeof(Wrapper<Comment>))
            {
                Wrapper<Comment> wrp = (Wrapper<Comment>)wrapper;
                backOff = wrp.Backoff;
                quotaRemaining = wrp.QuotaRemaining;
            }
            else if (wrapper.GetType() == typeof(Wrapper<User>))
            {
                Wrapper<User> wrp = (Wrapper<User>)wrapper;
                backOff = wrp.Backoff;
                quotaRemaining = wrp.QuotaRemaining;
            }

            if (backOff.HasValue)
            {
                StringHandler.WriteLine("BACKOFF", backOff.Value.ToString());
                System.Threading.Thread.Sleep(backOff.Value + 1000);
                return false;
            }
            if(quotaRemaining < 1)
            {
                StringHandler.WriteLine("QUOTA", new object[] { quotaRemaining, MaxSleep });
                System.Threading.Thread.Sleep(MaxSleep);
                return false;
            }
            return true;
        }
    }
    public enum PageType { summary, answers, questions, reputation, comments}

    public class UserReputation
    {
        public DateTime EarnedDate { get; set; }
        public int Value { get; set; }
        public UserReputation(DateTime earnedDate, int value)
        {
            EarnedDate = earnedDate;
            Value = value;
        }
        public string ToCsv()
        {
            return string.Format(StringHandler.BuildFormat(2, true), EarnedDate.ToString("yyyy/MM/dd"), Value);
        }
        public override string ToString()
        {
            return string.Format("Date:{0} | Point:{1}", EarnedDate.ToString("yyyy/MM/dd"), Value);
        }
    }
    public class UserSummary
    {
        public string Username { get; set; }
        public int Reputation { get; set; }
        public int GoldBadge { get; set; }
        public int SilverBadge { get; set; }
        public int BronzeBadge { get; set; }
        public int AnswerCount { get; set; }
        public int QuestionCount { get; set; }
        public DateTime SignupDate { get; set; }
        public UserSummary(string username, int reputation, int goldBadge, int silverBadge, int bronzeBadge,
            int answers, int questions, DateTime signUpDate)
        {
            Username = username;
            Reputation = reputation;
            GoldBadge = goldBadge;
            SilverBadge = silverBadge;
            BronzeBadge = bronzeBadge;
            AnswerCount = answers;
            QuestionCount = questions;
            SignupDate = signUpDate;
        }
        public string ToCsv()
        {
            return string.Format(StringHandler.BuildFormat(8, true), Username, Reputation, GoldBadge, SilverBadge,
                BronzeBadge, AnswerCount, QuestionCount, SignupDate.ToString("yyyy/MM/dd"));
        }
        public override string ToString()
        {
            string fr = "     Username: {0}\n   Reputation: {1}\nBadges earned: {2} Gold - {3} Silver - {4} Bronze\n";
            fr += "      Answers: {5}\n    Questions: {6}\n  Signup Date: {7}";
            return string.Format(fr, Username, Reputation, GoldBadge, SilverBadge, BronzeBadge,
                AnswerCount, QuestionCount, SignupDate.ToString("yyyy/MM/dd"));
        }
    }
    public class UserAnswer
    {
        public int ID { get; set; }
        public DateTime AnswerDate { get; set; }
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public bool IsAccepted { get; set; }
        public string RawContent { get; set; }
        public string Content { get; set; }
        public UserAnswer(int id, DateTime answerDate, int upvoteCount, int downvoteCount, bool isAccepted, string body)
        {
            ID = id;
            AnswerDate = answerDate;
            UpvoteCount = upvoteCount;
            DownvoteCount = downvoteCount;
            IsAccepted = isAccepted;
            RawContent = body;
            Content = StringHandler.StripHtmlTags(WebUtility.HtmlDecode(body));
            Content = Content.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            Content = Content.Replace(StringHandler.ListSeperator, " ");
        }
        public string ToCsv()
        {
            string format = StringHandler.BuildFormat(6, true);
            return string.Format(StringHandler.BuildFormat(6, true), ID, AnswerDate.ToString("yyyy/MM/dd"), UpvoteCount,
                DownvoteCount, IsAccepted, Content);
        }
        public override string ToString()
        {
            return string.Format("Answer ID: {0} | Date: {1} | Is Accepted: {2} - Upvotes: {3} | Downvotes: {4}\nContent: {5}",
                ID, AnswerDate.ToString("yyyy/MM/dd"), IsAccepted, UpvoteCount, DownvoteCount, Content);
        }
    }
    public class UserQuestion
    {
        public int ID { get; set; }
        public DateTime PostDate { get; set; }
        public int AnswerCount { get; set; }
        public int UpVoteCount { get; set; }
        public int DownVoteCount { get; set; }
        public bool IsAnswered { get; set; }
        public int ViewCount { get; set; }
        public string Content { get; set; }
        public string RawContent { get; set; }
        public UserQuestion(int id, DateTime askDate, int answerCount, int upVoteCount, int downVoteCount,
            bool hasAnswer, int viewCount, string body)
        {
            ID = id;
            PostDate = askDate;
            AnswerCount = answerCount;
            UpVoteCount = upVoteCount;
            DownVoteCount = downVoteCount;
            IsAnswered = hasAnswer;
            ViewCount = viewCount;
            RawContent = body;
            Content = StringHandler.StripHtmlTags(WebUtility.HtmlDecode(body));
            Content = Content.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            Content = Content.Replace(StringHandler.ListSeperator, " ");
        }
        public string ToCsv()
        {
            return string.Format(StringHandler.BuildFormat(8, true), ID, PostDate.ToString("yyyy/MM/dd"), AnswerCount, UpVoteCount,
                DownVoteCount, IsAnswered, ViewCount, Content);
        }
        public override string ToString()
        {
            string format = "Question ID: {0} | Is Answered: {1} | Date: {2} | Views: {3} | Upvotes: {4} | Downvotes: {5} | Answers: {6}";
            format += "\nContent: {8}";
            return string.Format(format, ID, IsAnswered, PostDate.ToString("yyyy/MM/dd"), ViewCount, UpVoteCount, DownVoteCount,
                AnswerCount, Content);
        }
    }

    public class UserComment
    {
        public int ID { get; set; }
        public string Content { get; set; }
        public string RawContent { get; set; }
        public DateTime PostDate { get; set; }
        public int Point { get; set; }
        public UserComment(int id, string body, int score, DateTime date)
        {
            ID = id;
            RawContent = body;
            Content = StringHandler.StripHtmlTags(WebUtility.HtmlDecode(body));
            Content = Content.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
            Content = Content.Replace(StringHandler.ListSeperator, " ");
            Point = score;
            PostDate = date;
        }
        public string ToCsv()
        {
            return string.Format(StringHandler.BuildFormat(4, true), ID, PostDate.ToString("yyyy/MM/dd"), Content, Point);
        }
        public override string ToString()
        {
            string format = "Comment ID: {0} | Score: {1} | Date: {2}\nContent: {3}";
            return string.Format(format, ID, Point, PostDate.ToString("yyyy/MM/dd"), Content);
        }
    }
}
