using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.Ylilauta
{
    public class YlilautaTopicParser : ITopicParser
    {
        public IEnumerable<Topic> ParsePage(Context context, string content)
        {
            string body = content;

            var anticaptchaMatch = Regex.Match(body, "<input name=\"anticaptcha\" id=\"anticaptcha\" value=\"(?<id>.+?)\" type=\"hidden\"/>");

            context.Board.AdditionalFields["anticaptcha"] = anticaptchaMatch.Groups["id"].Value;

            var uuidMatch = Regex.Match(body, "<input name=\"uuid\" id=\"uuid\" value=\"(?<id>.+?)\" type=\"hidden\"/>");

            context.Board.AdditionalFields["uuid"] = uuidMatch.Groups["uuid"].Value;

            var messages = Regex.Split(body, "(?=<div id=\"thread_[0-9]+\")");

            foreach (var message in messages.Skip(1))
            {
                yield return new Topic
                {
                    ImageLink = ExtractImageLink(message),
                    Content = ExtractMessage(message),
                    PosterName = ExtractPosterName(message),
                    PostTime = ExtractPostTime(message),
                    ReplyLink = ExtractReplyLink(message),
                    ThumbImageLink = ExtractThumbImageLink(message),
                    NumberOfReplies = ExtractNumberOfReplies(message),
                    Subject = ExtractSubject(message),
                    CountryFlag = ExtractFlag(message),
                    Id = ExtractId(message)
                };
            }
        }

        public int? ExtractPageCount(Context context, string html)
        {
            const string PaginationBlock = "<div class=\"pagination\">";

            int blockStartIndex = html.IndexOf(PaginationBlock);

            if (blockStartIndex == -1)
            {
                return null;
            }

            int blockEndIndex = html.IndexOf("</div>", blockStartIndex);

            var content = html.Substring(blockStartIndex, blockEndIndex - blockStartIndex);

            var matches = Regex.Matches(content, ">[0-9]+<");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count;
        }

        public IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            var messages = Regex.Split(content, "(?=<div class=\"tlist_thread\")");

            foreach (var message in messages.Skip(1))
            {
                var entry = new CatalogEntry
                {
                    Author = null,
                    Date = default(DateTime),
                    ImagesCount = null
                };

                var repliesMatch = Regex.Match(message, "<span class=\"tlist_replycount\">(?<replies>[0-9]+)");

                if (repliesMatch.Success)
                {
                    entry.RepliesCount = int.Parse(repliesMatch.Groups["replies"].Value);
                }

                var linkMatch = Regex.Match(message, "<a href=\"(?<link>.*?)\"");

                entry.ReplyLink = BoardManager.FixLink(linkMatch.Groups["link"].Value, true);

                entry.Id = entry.ReplyLink.Split('/').Last();

                var subjectMatch = Regex.Match(message, "<span class=\"postsubject\">(?<subject>.*?)</span>");

                entry.Subject = subjectMatch.Groups["subject"].Value;

                var descriptionMatch = Regex.Match(message, "<span class=\"tlist_replycount\">[0-9]+ replies</span><br />(?<description>.*?)</p></div>", RegexOptions.Singleline);

                entry.Description = BoardManager.StripHtml(descriptionMatch.Groups["description"].Value, true);

                var imageMatch = Regex.Match(message, "data-original=\"(?<image>.+?)\"", RegexOptions.Singleline);

                entry.ThumbImageLink = BoardManager.FixLink(imageMatch.Groups["image"].Value, true);

                if (string.IsNullOrEmpty(entry.ThumbImageLink))
                {
                    continue;
                }

                yield return entry;
            }
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<div id=\"thread_(?<id>[0-9]+)\">");

            return match.Groups["id"].Value;
        }

        private static string ExtractSubject(string message)
        {
            var match = Regex.Match(message, "<span class=\"postsubject\"><a href=\".+?\">(?<subject>.+?)</a>");

            return match.Groups["subject"].Value;
        }

        private static int? ExtractNumberOfReplies(string message)
        {
            var match = Regex.Match(message, "<div class=\"omitted\">(?<messages>[0-9]+)", RegexOptions.Singleline);

            const string MessageBlock = "<div class=\"post\">";

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
            var match = Regex.Match(message, "data-thumbfile=\"(?<image>.+?)\"", RegexOptions.Singleline);

            return BoardManager.FixLink(BoardManager.StripHtml(match.Groups["image"].Value, true), true);
        }

        private static string ExtractFlag(string message)
        {
            var match = Regex.Match(message, "<img class=\"flag_[a-z]+\" src=\"(?<flag>.+?)\"");

            if (!match.Success)
            {
                return null;
            }

            return BoardManager.FixLink(match.Groups["flag"].Value, true);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<span class=\"posttime\">(?<date>.+?)</span>", RegexOptions.Singleline);

            return YlilautaBoardManager.ConvertDate(match.Groups["date"].Value);
        }

        private static string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "<div class=\"postsubject\"><a href=\"(?<message>.+?)\">");

            return BoardManager.FixLink(match.Groups["message"].Value, true);
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername.*?\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractMessage(string message)
        {
            var messageMatch = Regex.Match(message, "<div class=\"post\"><p.+?>(?<message>.+?)</p></div>", RegexOptions.Singleline);

            var content = messageMatch.Groups["message"].Value;

            var greenTextMatches = Regex.Matches(content, "<span class=\"quote\">(?<message>.+?)</span>");

            foreach (Match greenTextMatch in greenTextMatches)
            {
                content = content.Replace(greenTextMatch.Value, "|||g" + greenTextMatch.Groups["message"] + "g|||");
            }

            content = BoardManager.StripHtml(content, true);
            
            var matches = Regex.Matches(content, "\\|\\|\\|g(?<message>.+?)g\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string greenText = match.Groups["message"].Value;

                content = content.Replace(match.Value, "<greenText>" + greenText + "</greenText>");
            }

            return content;
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<div class=\"fileinfo\"><a href=\"(?<image>.+?)\" class=\"openblank\">");

            if (!match.Success)
            {
                return null;
            }

            return BoardManager.FixLink(match.Groups["image"].Value, true);
        }
    }
}
