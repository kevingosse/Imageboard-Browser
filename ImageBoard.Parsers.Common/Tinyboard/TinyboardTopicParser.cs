using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common.Tinyboard
{
    public class TinyboardTopicParser : ITopicParser
    {
        public TinyboardTopicParser(string boardUriFormat)
        {
            this.BoardUriFormat = boardUriFormat;
        }

        protected string BoardUriFormat { get; set; }

        public IEnumerable<Topic> ParsePage(Context context, string content)
        {
            int bodyStart = content.IndexOf("<body");
            int bodyEnd = content.LastIndexOf("</body>");

            Debug.Assert(bodyEnd > -1, "Error parsing page");

            if (bodyEnd == -1)
            {
                yield break;
            }

            string body = content.Substring(bodyStart, bodyEnd - bodyStart + 7);

            var messages = Regex.Split(body, "(?=<br class=\"clear\"/><hr/>)");

            // To skip the last topic
            Topic previousTopic = null;

            foreach (var message in messages.Skip(1))
            {
                if (previousTopic != null)
                {
                    yield return previousTopic;
                }

                previousTopic = new Topic
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
            }
        }

        public virtual IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            var messages = Regex.Split(content, "(?=<div class=\"catathread)");

            foreach (var message in messages.Skip(1))
            {
                var entry = new CatalogEntry();

                entry.Author = null;
                entry.Date = default(DateTime);

                var descriptionMatch = Regex.Match(message, "<div class=\"catapreview\">(?<message>.*?)</div>", RegexOptions.Singleline);

                entry.Description = descriptionMatch.Groups["message"].Value;

                entry.ImagesCount = null;

                var repliesMatch = Regex.Match(message, "<div class=\"catacount\">(?<replies>[0-9]+)");

                if (repliesMatch.Success)
                {
                    entry.RepliesCount = int.Parse(repliesMatch.Groups["replies"].Value);
                }

                var linkMatch = Regex.Match(message, "<a class=\"catalink\" href=\"(?<link>.*?)(#[0-9]+)*\"");

                entry.ReplyLink = string.Format(this.BoardUriFormat, linkMatch.Groups["link"].Value);

                entry.Id = entry.ReplyLink.Split('/').Last().Replace(".html", string.Empty);

                var subjectMatch = Regex.Match(message, "<span class=\"catasubject\">(?<subject>.*?)</span>");

                entry.Subject = subjectMatch.Groups["subject"].Value;

                var imageMatch = Regex.Match(message, "<img src=\"(?<image>.*?)\"");

                entry.ThumbImageLink = string.Format(this.BoardUriFormat, imageMatch.Groups["image"].Value);

                yield return entry;
            }
        }

        private int? ExtractNumberOfReplies(string message)
        {
            var match = Regex.Match(message, "<div class=\"omitted\">(?<messages>[0-9]+)", RegexOptions.Singleline);

            const string MessageBlock = "<div class=\"post reply";

            int total = 0;

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

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "id=\"thread_(?<id>[0-9]+)\">");

            return match.Groups["id"].Value;
        }

        protected virtual string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "<a class=\"threadviewlink\" href=\"(?<link>.+?)\"");

            return string.Format(this.BoardUriFormat, match.Groups["link"].Value);
        }

        private DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<time datetime=\"(?<date>.+?)\"");

            return TinyboardBoardManager.ConvertDate(match.Groups["date"].Value);
        }

        private string ExtractMessage(string message)
        {
            var match = Regex.Match(message, "<div class=\"body\">(?<message>.*?)</div>", RegexOptions.Singleline);

            string text = match.Groups["message"].Value;

            return BoardManager.StripHtml(text, true);
        }

        private string ExtractSubject(string message)
        {
            var match = Regex.Match(message, "<span class=\"subject\">(?<subject>.+?)</span>", RegexOptions.Singleline);

            return match.Groups["subject"].Value;
        }

        private string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"name\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "target=\"_blank\"><img class=\".+?\" src=\"(?<image>.+?)\"");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(this.BoardUriFormat, match.Groups["image"].Value);
        }

        private string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<p class=\"fileinfo\">.+?<a href=\"(?<image>.+?)>");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(this.BoardUriFormat, match.Groups["image"].Value);
        }

        public int? ExtractPageCount(Context context, string html)
        {
            var matches = Regex.Matches(html, "\\[<a href=\".+?\\.html\">[0-9]+</a>\\]");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count + 1;
        }
    }
}
