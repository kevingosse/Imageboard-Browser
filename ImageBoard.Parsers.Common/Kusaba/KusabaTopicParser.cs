using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common.Kusaba
{
    public class KusabaTopicParser : ITopicParser
    {
        public KusabaTopicParser(string boardUriFormat, bool injectReferer)
        {
            this.BoardUriFormat = boardUriFormat;
            this.InjectReferer = injectReferer;
        }

        protected bool InjectReferer { get; set; }

        protected string BoardUriFormat { get; set; }

        public virtual IEnumerable<Topic> ParsePage(Context context, string content)
        {
            int bodyStart = content.IndexOf("<body");
            int bodyEnd = content.LastIndexOf("</body>");

            Debug.Assert(bodyEnd > -1, "Error parsing page");

            if (bodyEnd == -1)
            {
                yield break;
            }

            string body = content.Substring(bodyStart, bodyEnd - bodyStart + 7);

            var messages = Regex.Split(body, "(?=<a name=\"s[0-9]+\"></a>)");

            foreach (var message in messages.Skip(1))
            {
                    var topic = new Topic
                    {
                        ImageLink = ExtractImageLink(message),
                        Content = ExtractMessage(message),
                        PosterName = ExtractPosterName(message),
                        PostTime = ExtractPostTime(message),
                        ReplyLink = ExtractReplyLink(message),
                        ThumbImageLink = ExtractThumbImageLink(message),
                        NumberOfReplies = ExtractNumberOfReplies(message),
                        Subject = ExtractSubject(message),
                        Id = ExtractId(message)
                    };

                    if (this.InjectReferer)
                    {
                        topic.Referer = context.Board.Uri;
                    }

                    yield return topic;

            }
        }

        private static string ExtractSubject(string message)
        {
            var match = Regex.Match(message, "<span class=\"filetitle\">(?<subject>.+?)</span>", RegexOptions.Singleline);

            return match.Groups["subject"].Value;
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static int? ExtractNumberOfReplies(string message)
        {
            var match = Regex.Match(message, "<span class=\"omittedposts\">[\r\n]*(?<messages>[0-9]+)", RegexOptions.Singleline);

            const string MessageBlock = "<blockquote>";

            // The topic's message also starts with the pattern, so begin the replies counter at -1
            int total = -1;

            int index = -1;

            while ((index = message.IndexOf(MessageBlock, index + 1)) != -1)
            {
                total++;
            }

            if (match.Success)
            {
                int number;

                if (int.TryParse(match.Groups["messages"].Value, out number))
                {
                    return total + number;
                }

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }

            return total;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img src=\"(?<image>.+?)\" alt=\"[0-9]+\" class=\"thumb\"");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        protected virtual DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9][0-9]/[0-9][0-9]/[0-9][0-9]\(\w+\)[0-9][0-9]:[0-9][0-9])", RegexOptions.Singleline);

            return KusabaBoardManager.ConvertDate(match.Groups["date"].Value);
        }

        private string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "\\[<a href=\"(?<link>.+?)\">\\w+</a>\\]");

            return string.Format(this.BoardUriFormat, match.Groups["link"].Value);
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractMessage(string message)
        {
            var match = Regex.Match(message, "<blockquote>(?<message>.*?)</blockquote>", RegexOptions.Singleline);

            string text = match.Groups["message"].Value;

            return BoardManager.StripHtml(text, true);
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a href=\"(?<image>.+?)\" onclick=\"javascript:expandimg\\(");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        public int? ExtractPageCount(Context context, string html)
        {
            var matches = Regex.Matches(html, "&#91;<a href=\"/.+?/([0-9]+.html)*\">[0-9]+</a>&#93;");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count;
        }

        public virtual IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            throw new NotImplementedException();
        }
    }
}
