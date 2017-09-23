using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.SevenChan
{
    public class SevenChanTopicParser : ITopicParser
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

            var messages = Regex.Split(body, "(?=<div class=\"op\".+?>)");

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
            var matches = Regex.Matches(html, "&#91;<a href=\"/.+?/[0-9]+.html\">[0-9]+</a>&#93;");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count + 1;
        }

        public IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            throw new NotImplementedException();
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<div id=(?<id>[0-9]+) class=\"post\">");

            return match.Groups["id"].Value;
        }

        private static string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "<span class=\"replylinks\">.*?\\[<a href=\"(?<link>.+?)\">.+?</a>\\].*?</span>", RegexOptions.Singleline);

            return match.Groups["link"].Value;
        }

        private static int? ExtractNumberOfReplies(string content)
        {
            var match = Regex.Match(content, "<span class=\"omittedposts\">.+?(?<messages>[0-9]+).+?post", RegexOptions.Singleline);

            const string MessageBlock = "<div class=\"reply\"";

            int total = 0;

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

        private static string ExtractSubject(string message)
        {
            var match = Regex.Match(message, "<span class=\"subject\">(?<subject>.*?)</span>");

            return match.Groups["subject"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a href=\"(?<image>.+?)\" id=\"expandimg_.+?\">");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img src=\"(?<image>.+)\" alt=\".+?\" class=\"thumb\"");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "\n\n\n\n(?<date>[0-9]+/[0-9]+/[0-9]+(.+?)[0-9]+:[0-9]+)\n\n", RegexOptions.Singleline);

            var rawDate = match.Groups["date"].Value;

            return SevenChanBoardManager.ConvertDate(rawDate);
        }

        private static string ExtractMessage(string message)
        {
            const string BeginBlock = "<p class=\"message\">";
            const string EndBlock = "</p>";

            int startIndex = message.IndexOf(BeginBlock) + BeginBlock.Length;
            int endIndex = message.IndexOf(EndBlock, startIndex);

            Debug.Assert(endIndex > -1, "Error extracting message content");

            if (endIndex == -1)
            {
                return string.Empty;
            }

            message = message.Substring(startIndex, endIndex - startIndex);

            return BoardManager.StripHtml(message, true);
        }
    }
}
