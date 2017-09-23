using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.PonyChan
{
    public class PonyChanMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            var match = Regex.Match(content, "<input type=\"hidden\" name=\"replythread\" value=\"(?<resto>[0-9]+)\" />");

            string resto = match.Groups["resto"].Value;

            var ponyMatch = Regex.Match(content, "<input type=\"hidden\" name=\"how_much_pony_can_you_handle\" value=\"(?<pony>.+?)\" />");

            string pony = ponyMatch.Groups["pony"].Value;

            context.Topic.AdditionalFields["pony"] = pony;

            // First message is special
            var firstMessage = this.ParseFirstMessage(content);

            if (firstMessage == null)
            {
                yield break;
            }

            firstMessage.Resto = resto;

            var history = new Dictionary<string, Message>();

            history[firstMessage.Id] = firstMessage;

            yield return firstMessage;

            var values = Regex.Split(content, "(?=<td class=\"reply\")");

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

        protected Message ParseFirstMessage(string content)
        {
            const string End = "</blockquote>";

            int startIndex = content.IndexOf("<a name=\"s\"></a>");
            int endIndex = content.LastIndexOf(End);

            Debug.Assert(endIndex > -1, "Error parsing first message");

            if (endIndex == -1)
            {
                return null;
            }

            content = content.Substring(startIndex, endIndex - startIndex + End.Length);

            var message = new Message();

            message.Id = ExtractId(content);

            message.ImageLink = ExtractImageLink(content);

            message.ThumbImageLink = ExtractThumbImageLink(content);

            message.PosterName = ExtractPosterName(content);

            List<string> quotes;

            message.Content = ExtractMessage(content, out quotes);

            message.PostTime = ExtractDate(content);

            return message;
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
                "<span class=\"quote\"><a href=\".+?\" class=\"quotelink\".*?>&gt;&gt;(?<id>[0-9]+)</a></span>");

            foreach (Match quoteMatch in quoteMatches)
            {
                var id = quoteMatch.Groups["id"].Value;

                message = message.Replace(quoteMatch.Value, "|||&gt;&gt;" + id + "|||");
            }

            message = Regex.Replace(message, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline);
            
            message = BoardManager.StripHtml(message, true);

            var matches = Regex.Matches(message, "&gt;&gt;(?<id>[0-9]+)");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups["id"].Value;

                message = message.Replace("&gt;&gt;" + id, "<quote>" + id + "</quote>");

                quotes.Add(id);
            }

            message = BoardManager.ExtractLinks(message);

            return message;
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static DateTime? ExtractDate(string message)
        {
            var match = Regex.Match(message, "<span class=\"posttime\">(?<date>.+?)</span>");

            var rawDate = match.Groups["date"].Value;

            return PonyChanBoardManager.ConvertDate(rawDate);
        }
    }
}
