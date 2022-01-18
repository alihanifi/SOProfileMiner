using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Globalization;
using System.Net;
using System.IO;
using StackExchange.StacMan;

namespace SOProfileCrawler
{
    public class Crawler
    {
        static int sleep = 300000;
        static int quotaMax = 1500;
        static int sleep2 = 1500;
        static string apiKey { get { return Properties.Settings.Default.apiKey; } }
        PageType crawlType;
        int profileId;
        public Crawler(PageType pageType, int prfId)
        {
            crawlType = pageType;
            profileId = prfId;
        }
        public object StartCrawl(ProgressBar progress)
        {
            List<int> profileIds = new List<int>();
            profileIds.Add(profileId);

            int page = 1;
            int pageSize = 100;
            int total = 0;
            switch (crawlType)
            {
                case PageType.summary:
                    var users = GetUsers(profileIds).Result.Data;
                    progress.Report(0.1);
                    if (users != null)
                    {
                        if(users.QuotaRemaining < quotaMax)
                            System.Threading.Thread.Sleep(sleep);

                        User u = users.Items.FirstOrDefault();
                        return new PrSum(u.DisplayName, u.Reputation, u.BadgeCounts.Gold, u.BadgeCounts.Silver,
                                                u.BadgeCounts.Bronze, u.AnswerCount, u.QuestionCount, u.Age, u.CreationDate);
                    }
                    else
                        return null;
                    
                case PageType.reputation:
                    var repResponse = GetReputation(profileIds, page);
                    progress.Report(0.1);
                    System.Threading.Thread.Sleep(sleep2 * 2);
                    List<Reputation> allReps = new List<Reputation>();
                    if (repResponse.Result.Data != null)
                    {
                        total = repResponse.Result.Data.Total;
                        CheckQuota_Reputation(repResponse.Result.Data);
                        allReps.AddRange(repResponse.Result.Data.Items);

                        while(repResponse.Result.Data.HasMore && total > pageSize)
                        {
                            repResponse = GetReputation(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(sleep2);
                            if (repResponse.Result.Data != null)
                            {
                                allReps.AddRange(repResponse.Result.Data.Items);
                                if (!CheckQuota_Reputation(repResponse.Result.Data))
                                    continue;
                                progress.Report((double)page * (90 / (double)total) + 0.1);
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    List<Rpt> reps = new List<Rpt>();
                    foreach (var t in allReps)
                        reps.Add(new Rpt(t.OnDate, t.ReputationChange));

                    return reps;
                case PageType.answers:
                    var ansResponse = GetAnswers(profileIds, page);
                    progress.Report(0.1);
                    System.Threading.Thread.Sleep(sleep2 * 2);
                    List<Answer> allAnswers = new List<Answer>();
                    if (ansResponse.Result.Data != null)
                    {
                        total = ansResponse.Result.Data.Total;
                        CheckQuota_Answer(ansResponse.Result.Data);
                        allAnswers.AddRange(ansResponse.Result.Data.Items);

                        while (ansResponse.Result.Data.HasMore && total > pageSize)
                        {
                            ansResponse = GetAnswers(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(sleep2);
                            if (ansResponse.Result.Data != null)
                            {
                                allAnswers.AddRange(ansResponse.Result.Data.Items);
                                if (!CheckQuota_Answer(ansResponse.Result.Data))
                                    continue;
                                progress.Report((double)page * (90 / (double)total) + 0.1);
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    List<Ans> answers = new List<Ans>();
                    foreach (var t in allAnswers)
                        answers.Add(new Ans(t.CreationDate, t.UpVoteCount, t.DownVoteCount, t.IsAccepted));

                    return answers;
                case PageType.questions:
                    var QtResponse = GetQuestions(profileIds, page);
                    progress.Report(0.1);
                    List<Question> allQs = new List<Question>();
                    System.Threading.Thread.Sleep(sleep2 * 2);
                    if (QtResponse.Result.Data != null)
                    {
                        total = QtResponse.Result.Data.Total;
                        CheckQuota_Question(QtResponse.Result.Data);
                        allQs.AddRange(QtResponse.Result.Data.Items);

                        while (QtResponse.Result.Data.HasMore && total > pageSize)
                        {
                            QtResponse = GetQuestions(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(sleep2);
                            if (QtResponse.Result.Data != null)
                            {
                                allQs.AddRange(QtResponse.Result.Data.Items);
                                if (!CheckQuota_Question(QtResponse.Result.Data))
                                    continue;
                                progress.Report((double)page * (90 / (double)total) + 0.1);
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    List<Qtn> questions = new List<Qtn>();
                    foreach (var t in allQs)
                        questions.Add(new Qtn(t.CreationDate, t.AnswerCount, t.UpVoteCount, t.DownVoteCount,
                                                t.IsAnswered, t.ViewCount, t.FavoriteCount));

                    return questions;
                
                case PageType.comments:
                    var CtResponse = GetComments(profileIds, page);
                    progress.Report(0.1);
                    List<Comment> allCs = new List<Comment>();
                    System.Threading.Thread.Sleep(sleep2 * 2);
                    if (CtResponse.Result.Data != null)
                    {
                        total = CtResponse.Result.Data.Total;
                        CheckQuota_Comment(CtResponse.Result.Data);
                        allCs.AddRange(CtResponse.Result.Data.Items);

                        while (CtResponse.Result.Data.HasMore && total > pageSize)
                        {
                            CtResponse = GetComments(profileIds, page, pageSize);
                            System.Threading.Thread.Sleep(sleep2);
                            if (CtResponse.Result.Data != null)
                            {
                                if(CtResponse.Result.Data.ErrorId == 502)
                                {
                                    Console.WriteLine("Trottle reached! Waiting 5 Minutes!");
                                    System.Threading.Thread.Sleep(300000);
                                    continue;
                                }
                                allCs.AddRange(CtResponse.Result.Data.Items);
                                if (!CheckQuota_Comment(CtResponse.Result.Data))
                                    continue;
                                progress.Report((double)page * (90 / (double)total) + 0.1);
                            }
                            else
                                break;

                            page += 1;
                        }
                    }
                    List<Cmt> comments = new List<Cmt>();
                    foreach (var t in allCs)
                        comments.Add(new Cmt(t.CreationDate, t.Score));

                    return comments;
            }
            return null;
        }
        private async Task<StacManResponse<User>> GetUsers(List<int> users)
        {
            var task = new StacManClient(key: apiKey, version: "2.1");
            var response = task.Users.GetByIds("stackoverflow", users, filter: "!*MZqiH2U.gWydEj0");
            return await response;
        }
        private async Task<StacManResponse<Answer>> GetAnswers(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: apiKey, version: "2.1");
            if(pagesize == 0)
                return await task.Users.GetAnswers("stackoverflow", users, filter: "!)sKEjRcyp9HIdKc6jcQY");
            else
                return await task.Users.GetAnswers("stackoverflow", users, filter: "!)sKEjRcyp9HIdKc6jcQY", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Question>> GetQuestions(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: apiKey, version: "2.1");
            if (pagesize == 0)
                return await task.Users.GetQuestions("stackoverflow", users, filter: "!LbL5qNr-de-k5H3NLZMi(S");
            else
                return await task.Users.GetQuestions("stackoverflow", users, filter: "!LbL5qNr-de-k5H3NLZMi(S", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Reputation>> GetReputation(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: apiKey, version: "2.1");
            if(pagesize == 0)
                return await task.Users.GetReputation("stackoverflow", users, filter: "!nKzQUR693x");
            else
                return await task.Users.GetReputation("stackoverflow", users, filter: "!nKzQUR693x", page: page, pagesize: pagesize);
        }
        private async Task<StacManResponse<Comment>> GetComments(List<int> users, int page, int pagesize = 0)
        {
            var task = new StacManClient(key: apiKey, version: "2.1");
            if (pagesize == 0)
                return await task.Users.GetComments("stackoverflow", users, filter: "!6VvPDzR2nLwf0");
            else
                return await task.Users.GetComments("stackoverflow", users, filter: "!6VvPDzR2nLwf0", page: page, pagesize: pagesize);
        }

        private bool CheckQuota_Answer(Wrapper<Answer> wrapper)
        {
            if (wrapper.QuotaRemaining < quotaMax)
            {
                Console.WriteLine(string.Format("Max Quota Reached ({0})!", wrapper.QuotaRemaining));
                System.Threading.Thread.Sleep(sleep);
                return false;
            }
            return true;
        }
        private bool CheckQuota_Reputation(Wrapper<Reputation> wrapper)
        {
            if (wrapper.QuotaRemaining < quotaMax)
            {
                Console.WriteLine(string.Format("Max Quota Reached ({0})!", wrapper.QuotaRemaining));
                System.Threading.Thread.Sleep(sleep);
                return false;
            }
            return true;
        }
        private bool CheckQuota_Question(Wrapper<Question> wrapper)
        {
            if (wrapper.QuotaRemaining < quotaMax)
            {
                Console.WriteLine(string.Format("Max Quota Reached ({0})!", wrapper.QuotaRemaining));
                System.Threading.Thread.Sleep(sleep);
                return false;
            }
            return true;
        }
        private bool CheckQuota_Comment(Wrapper<Comment> wrapper)
        {
            if (wrapper.QuotaRemaining < quotaMax)
            {
                Console.WriteLine(string.Format("Max Quota Reached ({0})!", wrapper.QuotaRemaining));
                System.Threading.Thread.Sleep(sleep);
                return false;
            }
            return true;
        }
    }
    public enum PageType { summary, answers, questions, reputation, comments}

    public class Rpt
    {
        public DateTime EarnedDate { get; set; }
        public int Value { get; set; }
        public Rpt(DateTime earnedDate, int value)
        {
            EarnedDate = earnedDate;
            Value = value;
        }
        public string ToCsv()
        {
            string format = "{0},{1}\n";
            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return string.Format(format, EarnedDate.ToString("yyyy/MM/dd"), Value);
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}", EarnedDate.ToString("yyyy/MM/dd"), Value);
        }
    }
    public class Cmt
    {
        public DateTime PostTime { get; set; }
        public int Score { get; set; }
        public Cmt(DateTime postTime, int score)
        {
            PostTime = postTime;
            Score = score;
        }
        public string ToCsv()
        {
            string format = "{0},{1}\n";
            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return string.Format(format, PostTime.ToString("yyyy/MM/dd"), Score);
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}", PostTime.ToString("yyyy/MM/dd"), Score);
        }
    }
    public class PrSum
    {
        public string Username { get; set; }
        public int Reputation { get; set; }
        public int GoldBadge { get; set; }
        public int SilverBadge { get; set; }
        public int BronzeBadge { get; set; }
        public int AnswerCount { get; set; }
        public int QuestionCount { get; set; }
        public int? Age { get; set; }
        public DateTime SignupDate { get; set; }
        public PrSum(string username, int reputation, int goldBadge, int silverBadge, int bronzeBadge, int answers, int questions,
            int? age, DateTime signUpDate)
        {
            this.Username = username;
            this.Reputation = reputation;
            this.GoldBadge = goldBadge;
            this.SilverBadge = silverBadge;
            this.BronzeBadge = bronzeBadge;
            this.AnswerCount = answers;
            this.QuestionCount = questions;
            this.Age = age;
            this.SignupDate = signUpDate;
        }
        public string ToCsv()
        {
            string format = "{0},{1},{2},{3},{4},{5},{6},{7},{8}\n";
            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return string.Format(format, Username, Reputation, GoldBadge, SilverBadge, BronzeBadge, AnswerCount, QuestionCount,
                Age, SignupDate.ToString("yyyy/MM/dd"));
        }
        public override string ToString()
        {
            string fr = "     Username: {0}\n   Reputation: {1}\nBadges earned: {2} Gold - {3} Silver - {4} Bronze\n      Answers: {5}\n    Questions: {6}\n";
            fr += "     Age: {7}\n  Signup Date: {8}";
            return string.Format(fr, Username, Reputation, GoldBadge, SilverBadge, BronzeBadge, AnswerCount, QuestionCount, Age, SignupDate.ToString("yyyy/MM/dd"));
        }
    }
    public class Ans
    {
        public DateTime AnswerDate { get; set; }
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public bool IsAccepted { get; set; }
        public Ans(DateTime answerDate, int upvoteCount, int downvoteCount, bool isAccepted)
        {
            AnswerDate = answerDate;
            UpvoteCount = upvoteCount;
            DownvoteCount = downvoteCount;
            IsAccepted = isAccepted;
        }
        public string ToCsv()
        {
            string format = "{0},{1},{2},{3}\n";
            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return string.Format(format, AnswerDate.ToString("yyyy/MM/dd"), UpvoteCount, DownvoteCount, IsAccepted);
        }
        public override string ToString()
        {
            return string.Format("{0} - Accepted: {1} - UpVote Count: {2} - DownVote Count: {3}",
                AnswerDate.ToString("yyyy/MM/dd"), IsAccepted, UpvoteCount, DownvoteCount);
        }
    }
    public class Qtn
    {
        public DateTime AskDate { get; set; }
        public int AnswerCount { get; set; }
        public int UpVoteCount { get; set; }
        public int DownVoteCount { get; set; }
        public bool HasAnswer { get; set; }
        public int ViewCount { get; set; }
        public int BookmarkCount { get; set; }
        public Qtn(DateTime askDate, int answerCount, int upVoteCount, int downVoteCount,
            bool hasAnswer, int viewCount, int bookmarkCount)
        {
            AskDate = askDate;
            AnswerCount = answerCount;
            UpVoteCount = upVoteCount;
            DownVoteCount = downVoteCount;
            HasAnswer = hasAnswer;
            ViewCount = viewCount;
            BookmarkCount = bookmarkCount;
        }
        public string ToCsv(bool header = false)
        {
            string format = "{0},{1},{2},{3},{4},{5},{6}\n";
            format = format.Replace(",", CultureInfo.CurrentCulture.TextInfo.ListSeparator);
            return string.Format(format, AskDate.ToString("yyyy/MM/dd"), AnswerCount, UpVoteCount,
                DownVoteCount, HasAnswer, ViewCount, BookmarkCount);
        }
        public override string ToString()
        {
            return string.Format("{0} - Answered: {1} - UpVote Count: {2} - DownVote Count: {3} - View Count: {4} - Bookmark Count: {5}",
                AskDate.ToString("yyyy/MM/dd"), HasAnswer, UpVoteCount, DownVoteCount, ViewCount, BookmarkCount);
        }
    }
}
