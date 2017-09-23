﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.PonyChan
{
    public class PonyChanTopicParser : ITopicParser
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

            var ponyMatch = Regex.Match(body, "<input type=\"hidden\" name=\"how_much_pony_can_you_handle\" value=\"(?<pony>.+?)\" />");

            string pony = ponyMatch.Groups["pony"].Value;

            context.Board.AdditionalFields["pony"] = pony;

            var messages = Regex.Split(body, "(?=<a name=\"s[0-9]+\"></a>)");

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
            var matches = Regex.Matches(html, "&#91;<a href=\"http://www.ponychan.net/chan/.+?/([0-9])+\\.html\">([0-9])+</a>&#93");

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
            var match = Regex.Match(message, "\\[<a href=\"(?<link>.+?)\">View</a>\\]");

            string link = match.Groups["link"].Value;

            int index = link.IndexOf("res/");

            if (index == -1)
            {
                return link;
            }

            return link.Substring(index);
        }

        private static int? ExtractNumberOfReplies(string content)
        {
            var match = Regex.Match(content, "<span class=\"omittedposts\">.+?(?<messages>[0-9]+).+?post", RegexOptions.Singleline);

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
            message = message
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);

            var match = Regex.Match(message, "<span class=\"filetitle\">(?<subject>.*?)</span>");

            return match.Groups["subject"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "onclick=\"javascript:expandimg\\('[0-9]+', '(?<image>.+?)', '.+?'");

            return BoardManager.FixLink(match.Groups["image"].Value, false);
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "onclick=\"javascript:expandimg\\('[0-9]+', '.+?', '(?<image>.+?)'");

            return BoardManager.FixLink(match.Groups["image"].Value, false);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<span class=\"posttime\">(?<date>.+?)</span>");

            var rawDate = match.Groups["date"].Value;

            return PonyChanBoardManager.ConvertDate(rawDate);
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

            return BoardManager.StripHtml(message, true);
        }
    }
}
