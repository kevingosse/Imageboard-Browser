using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.KrautChan
{
    public class KrautChanTopicParser : ITopicParser
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

            var messages = body.Split(new[] { "<div class=\"thread\"" }, StringSplitOptions.None);

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
                    Id = ExtractId(message),
                    CountryFlag = KrautChanMessageParser.ExtractCountryFlag(message)
                };
            }
        }

        public int? ExtractPageCount(Context context, string html)
        {
            var matches = Regex.Matches(html, "<a href=\"/.+?/(([0-9])+\\.html)*\">([0-9])+</a>");

            int count = matches.OfType<Match>().Count(match => match.Success);

            return count + 1;
        }

        public IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            throw new NotImplementedException();
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "\\[<a href=\"(?<link>.+?)\">Antworten</a>\\]");

            if (!match.Groups["link"].Success)
            {
                match = Regex.Match(message, "\\[<a href=\"(?<link>.+?)\">Reply</a>\\]");
            }

            return match.Groups["link"].Value;
        }

        private static int? ExtractNumberOfReplies(string content)
        {
            var match = Regex.Match(content, "<span class=\"omittedinfo\">.+?(?<messages>[0-9]+).+?post", RegexOptions.Singleline);

            const string MessageBlock = "<blockquote>";

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

        private static string ExtractSubject(string message)
        {
            var match = Regex.Match(message, "<span class=\"postsubject\">(?<subject>.*?)</span>");

            return match.Groups["subject"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a href=\"(?<image>.+?)\" target=\"_blank\">");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(KrautChanBoardManager.BoardUriFormat, match.Groups["image"].Value);
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img style=\"display: block\" id='thumbnail_[0-9]+' src=(?<image>.+)\n");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(KrautChanBoardManager.BoardUriFormat, match.Groups["image"].Value);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<span class=\"postdate\">(?<date>.+?)</span>");

            var rawDate = match.Groups["date"].Value;

            return KrautChanBoardManager.ConvertDate(rawDate);
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

            return BoardManager.StripHtml(message, true);
        }
    }
}
