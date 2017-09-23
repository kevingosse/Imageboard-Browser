using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.AnonIb
{
    public class AnonIbMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            var match = Regex.Match(content, "<input type=\"hidden\" name=\"replythread\" value=\"(?<resto>[0-9]+)\" />");

            string resto = match.Groups["resto"].Value;

            var history = new Dictionary<string, Message>();

            var values = Regex.Split(content, "(?=<table>\\r\\n<tbody>\\r\\n<tr>)");

            foreach (var messageContent in values)
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

        private static DateTime? ExtractDate(string message)
        {
            var match = Regex.Match(message, "(?<date>[0-9]+/[0-9]+/[0-9]+\\(\\w+\\)[0-9]+:[0-9]+)");

            var rawDate = match.Groups["date"].Value;

            return AnonIbBoardManager.ConvertDate(rawDate);
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            if (match.Groups["id"].Value == "23833")
            {
                Debugger.Break();
            }

            return match.Groups["id"].Value;
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a href=\"(?<image>.+?)\" onclick=\"javascript:expandimg");

            var image = match.Groups["image"].Value;

            const string Prefix = "/img.php?path=";

            if (image.StartsWith(Prefix))
            {
                image = image.Remove(0, Prefix.Length);
            }

            return image;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "src=\"(?<image>.+?)\" alt=\"[0-9]+\"");

            return match.Groups["image"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.*?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            const string BeginBlock = "<blockquote>";
            const string EndBlock = "</blockquote>";

            int startIndex = content.IndexOf(BeginBlock) + BeginBlock.Length;
            int endIndex = content.IndexOf(EndBlock, startIndex);

            Debug.Assert(endIndex > -1, "Error extracting message content");

            if (endIndex == -1)
            {
                return string.Empty;
            }

            var message = content.Substring(startIndex, endIndex - startIndex);

            var quoteMatches = Regex.Matches(
                message,
                "<a href=\".+?\" onclick=\"return highlight\\('[0-9]+', .+\\);\" class=\".+\">&gt;&gt;(?<id>[0-9]+)</a>");
                
            foreach (Match quoteMatch in quoteMatches)
            {
                var id = quoteMatch.Groups["id"].Value;

                message = message.Replace(quoteMatch.Value, "|||&gt;&gt;" + id + "|||");

                quotes.Add(id);
            }

            message = Regex.Replace(message, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline);

            message = BoardManager.StripHtml(message, true);

            var matches = Regex.Matches(message, "\\|\\|\\|&gt;&gt;(?<id>[0-9]+)\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups["id"].Value;

                message = message.Replace("|||&gt;&gt;" + id + "|||", "<quote>" + id + "</quote>");
            }

            message = BoardManager.ExtractLinks(message);

            return message.Trim(new[] { '\n', '\t', ' ' });
        }
    }
}
