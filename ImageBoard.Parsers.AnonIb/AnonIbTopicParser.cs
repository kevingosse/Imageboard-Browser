using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.AnonIb
{
    public class AnonIbTopicParser : ITopicParser
    {
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

            var messages = Regex.Split(body, "(?=<div id=\"thread[0-9]+)");

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
                    Id = ExtractId(message)
                };
            }
        }

        public int? ExtractPageCount(Context context, string html)
        {
            var matches = Regex.Matches(html, "&#91;<a href=\"/.+?/([0-9])+\\.html\">([0-9])+</a>&#93;");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count + 1;
        }

        public virtual IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            var messages = Regex.Split(content, "(?=<td valign=\"top\" style=\"padding-top:20px\">)");

            foreach (var message in messages.Skip(1))
            {
                var entry = new CatalogEntry
                {
                    Author = null,
                    Date = default(DateTime),
                    Description = null,
                    ImagesCount = null
                };

                var repliesMatch = Regex.Match(message, "replies: <strong>(?<replies>[0-9]+)</strong>");

                if (repliesMatch.Success)
                {
                    entry.RepliesCount = int.Parse(repliesMatch.Groups["replies"].Value);
                }

                var linkMatch = Regex.Match(message, "<a href=\"(?<link>.*?)\"");

                entry.ReplyLink = string.Format(AnonIbBoardManager.BoardUriFormat, linkMatch.Groups["link"].Value);

                entry.Id = entry.ReplyLink.Split('/').Last().Replace(".html", string.Empty);

                var subjectMatch = Regex.Match(message, "title=\"(?<subject>.*?)\"");

                entry.Subject = subjectMatch.Groups["subject"].Value;

                var imageMatch = Regex.Match(message, "src=\"(?<image>.*?)\" alt=\"[0-9]+\"");

                entry.ThumbImageLink = imageMatch.Groups["image"].Value;

                if (string.IsNullOrEmpty(entry.ThumbImageLink))
                {
                    continue;
                }

                yield return entry;
            }
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "onclick=\"javascript:expandimg\\('[0-9]+', '(?<image>.+?)'");

            return match.Groups["image"].Value;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img style=\"float:none\" src=\"(?<image>.+?)\" alt=\"[0-9]+\" class=\"thumb\"");

            return match.Groups["image"].Value;
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<div id=\"thread(?<id>[0-9]+)");

            return match.Groups["id"].Value;
        }

        private static string ExtractSubject(string message)
        {
            message = message
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);

            var match = Regex.Match(message, "<span class=\"filetitle\">(?<subject>.*?)</span>");

            return BoardManager.StripHtml(match.Groups["subject"].Value, false);
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.*?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "(?<date>[0-9]+/[0-9]+/[0-9]+\\(\\w+\\)[0-9]+:[0-9]+)");

            var rawDate = match.Groups["date"].Value;

            return AnonIbBoardManager.ConvertDate(rawDate);
        }

        private static string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "\\[<a href=\"(?<link>.+?)\">Reply</a>\\]");

            return match.Groups["link"].Value;
        }

        private static string ExtractMessage(string message)
        {
            const string BeginBlock = "<blockquote>";
            const string EndBlock = "</blockquote>";

            int startIndex = message.IndexOf(BeginBlock) + BeginBlock.Length;
            int endIndex = message.IndexOf(EndBlock, startIndex);

            Debug.Assert(endIndex > -1, "Error extracting message content");

            if (endIndex == -1)
            {
                return string.Empty;
            }

            message = message.Substring(startIndex, endIndex - startIndex);

            message = Regex.Replace(message, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline);

            return BoardManager.StripHtml(message, true).Trim(new[] { '\n', '\t' });
        }

        private static int? ExtractNumberOfReplies(string content)
        {
            var match = Regex.Match(content, "<span class=\"omittedposts\">.*?(?<messages>[0-9]+).*?post", RegexOptions.Singleline);

            const string MessageBlock = "<blockquote";

            int total = -1;

            int index = -1;

            while ((index = content.IndexOf(MessageBlock, index + 1)) != -1)
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
    }
}
