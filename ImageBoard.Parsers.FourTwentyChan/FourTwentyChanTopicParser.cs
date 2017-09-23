using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.FourTwentyChan
{
    public class FourTwentyChanTopicParser : ITopicParser
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

            var messages = Regex.Split(body, "(?=<div id=\"\\w+?thread[0-9]+?\")");

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
            var matches = Regex.Matches(html, "<a href=\"/.+?/[0-9]+\\.php\\\">[0-9]+</a>");

            int count = matches.OfType<Match>().Count(match => match.Success);

            if (Regex.IsMatch(html, "<a href=\"/.+?/index.php\">0</a>"))
            {
                count++;
            }

            return count + 1;
        }

        public IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            throw new NotImplementedException();
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a id=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractReplyLink(string message)
        {
            var match = Regex.Match(message, "<a class=\"fg-button ui-state-default fg-button-icon-left ui-corner-all\" href=\"(?<link>.+?)\"> <span class=\"ui-icon ui-icon-comment\"></span>Reply </a>");

            return match.Groups["link"].Value;
        }

        private static int? ExtractNumberOfReplies(string content)
        {
            var match = Regex.Match(content, "<span class=\"omittedposts\">.+?(?<messages>[0-9]+).+?post", RegexOptions.Singleline);

            int total = Regex.Matches(content, "<blockquote ( class=\"\\w+?[0-9]+\")*>").Count;

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
            var match = Regex.Match(message, "<a onclick=\"return imageExpansion\\(this\\)\" class=\"img[0-9]+?\" target=\"_blank\" href=\"(?<image>.+?)\"");

            return string.Format(FourTwentyChanBoardManager.BoardUriFormat, match.Groups["image"].Value);
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img src=\"(?<image>[\\w/\\.]+?)\" alt=\"[0-9]+\" class=\"thumb\" />");

            return string.Format(FourTwentyChanBoardManager.BoardUriFormat, match.Groups["image"].Value);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">.+?</span>.+?<span class=\"inbetween\">(?<date>.+?)</span>");

            var rawDate = match.Groups["date"].Value;

            // Index minus 5 to remove the timezone (for instance: " EST")
            int idIndex = rawDate.IndexOf("ID:") - 5;

            rawDate = rawDate.Substring(0, idIndex);

            return FourTwentyChanBoardManager.ConvertDate(rawDate);
        }

        private static string ExtractMessage(string message)
        {
            var match = Regex.Match(message, "<blockquote class=\"opcomment *\\w*?[0-9]*?\">(?<message>.+?)</blockquote>");

            if (!match.Success)
            {
                return string.Empty;
            }

            message = match.Groups["message"].Value;

            message = Regex.Replace(message, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline);

            return BoardManager.StripHtml(message, true);
        }
    }
}
