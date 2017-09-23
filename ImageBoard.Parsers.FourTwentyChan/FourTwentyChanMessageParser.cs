using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.FourTwentyChan
{
    public class FourTwentyChanMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            int bodyStart = content.IndexOf("<body");
            int bodyEnd = content.LastIndexOf("</body>");

            Debug.Assert(bodyEnd > -1, "Error parsing page");

            if (bodyEnd == -1)
            {
                yield break;
            }

            string body = content.Substring(bodyStart, bodyEnd - bodyStart + 7);

            var match = Regex.Match(body, "<input type=\"hidden\" name=\"parent\" value=\"(?<resto>\\S+)\" />");

            string resto = match.Groups["resto"].Value;

            var values = Regex.Split(body, "(?=<a id=\"[0-9]+\">)");

            var history = new Dictionary<string, Message>();

            foreach (var messageContent in values.Skip(1))
            {
                List<string> quotes;

                var message = new Message
                {
                    Id = ExtractId(messageContent),
                    Content = ExtractMessage(messageContent, out quotes),
                    ImageLink = ExtractImageLink(messageContent),
                    ThumbImageLink = ExtractThumbImageLink(messageContent),
                    PosterName = ExtractPosterName(messageContent),
                    PostTime = ExtractDate(messageContent),
                    Resto = resto
                };

                history[message.Id] = message;

                foreach (var quote in quotes)
                {
                    Message quotedMessage;

                    if (history.TryGetValue(quote, out quotedMessage))
                    {
                        if (quotedMessage.BackLinks == null)
                        {
                            quotedMessage.BackLinks = new List<Message>();
                        }

                        quotedMessage.BackLinks.Add(message);
                    }
                }

                yield return message;
            }
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a target=\"_blank\" href=\"(?<image>\\S+)\">");

            return match.Success ? string.Format(FourTwentyChanBoardManager.BoardUriFormat, match.Groups["image"].Value) : null;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img src=\"(?<image>\\S+)\" alt=\"[0-9]+\" class=\"thumb\" />");

            return match.Success ? string.Format(FourTwentyChanBoardManager.BoardUriFormat, match.Groups["image"].Value) : null;
        }

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            var messageMatch = Regex.Match(content, "<blockquote.+?>(?<message>.+?)</blockquote>");

            if (!messageMatch.Success)
            {
                return string.Empty;
            }

            var message = messageMatch.Groups["message"].Value;

            message = Regex.Replace(message, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline);

            message = BoardManager.StripHtml(message, true);

            var quoteMatches = Regex.Matches(
                message,
                "&gt;&gt;(?<id>[0-9]+)");

            foreach (Match match in quoteMatches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups["id"].Value;

                message = message.Replace("&gt;&gt;" + id, "<br/><quote>" + id + "</quote><br/>");

                quotes.Add(id);
            }

            message = BoardManager.ExtractLinks(message);

            return message;
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a id=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"\\w+?postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            string name = match.Groups["name"].Value;

            name = Regex.Replace(name.Replace("\r\n", string.Empty), "<.*?>", string.Empty).Trim();

            return name;
        }

        private static DateTime? ExtractDate(string message)
        {
            var match = Regex.Match(message, "<span class=\"\\w*?postername\">.+?</span>.*? - (?<date>.+?)<");

            string rawDate = match.Groups["date"].Value;

            rawDate = " - " + rawDate;

            // Index minus 5 to remove the timezone (for instance: " EST")
            int idIndex = rawDate.IndexOf("ID:") - 5;

            rawDate = rawDate.Substring(0, idIndex);

            return FourTwentyChanBoardManager.ConvertDate(rawDate);
        }
    }
}
