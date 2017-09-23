using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.FChan
{
    public class FChanMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            var match = Regex.Match(content, "<input type=\"hidden\" name=\"parent\" value=\"(?<resto>[0-9]+)\" />");

            string resto = match.Groups["resto"].Value;

            // First message is special
            var firstMessage = this.ParseFirstMessage(content);

            if (firstMessage == null)
            {
                yield break;
            }

            firstMessage.Resto = resto;
            firstMessage.Id = resto;

            var history = new Dictionary<string, Message>();

            history[firstMessage.Id] = firstMessage;

            yield return firstMessage;

            var values = Regex.Split(content, "(?=<table><tbody><tr>)");

            foreach (var messageContent in values.Skip(2))
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
                    Resto = resto,
                    Referer = string.Format(FChanBoardManager.BoardUriFormat, context.Board.Name)
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
            var match = Regex.Match(message, "<a target=\"_blank\" href=\"(?<image>.+?)\" rel=\"nofollow\">");

            var link = match.Groups["image"].Value;

            if (string.IsNullOrEmpty(link))
            {
                return link;
            }

            return string.Format(FChanBoardManager.BoardUriFormat, match.Groups["image"].Value);
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img id=\"img[0-9]+\" src=\"(?<image>.+?)\"");

            var link = match.Groups["image"].Value;

            if (string.IsNullOrEmpty(link))
            {
                return link;
            }

            return string.Format(FChanBoardManager.BoardUriFormat, link);
        }

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            const string BeginBlock = "<blockquote class=\"com_hide\">";
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
                "<a href=\".+?\" onclick=\"highlight\\([0-9]+\\)\">&gt;&gt;(?<id>[0-9]+)</a>");

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

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            if (match.Groups["id"].Value == "23833")
            {
                Debugger.Break();
            }

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"cpname\">(?<name>.*?)</span>", RegexOptions.Singleline);

            string name = BoardManager.StripHtml(match.Groups["name"].Value, false).Trim(new[] { '\t', '\n' });

            return string.IsNullOrEmpty(name) ? null : name;
        }

        private static DateTime? ExtractDate(string message)
        {
            var match = Regex.Match(message, "(?<date>[0-9]+/[0-9]+/[0-9]+\\(\\w+\\)[0-9]+:[0-9]+)");

            var rawDate = match.Groups["date"].Value;

            return FChanBoardManager.ConvertDate(rawDate);
        }

        protected Message ParseFirstMessage(string content)
        {
            const string End = "</blockquote>";

            int startIndex = content.IndexOf("<table><tbody><tr>");
            int endIndex = content.IndexOf("</tr></tbody></table>", startIndex);

            Debug.Assert(endIndex > -1, "Error parsing first message");

            if (endIndex == -1)
            {
                return null;
            }

            content = content.Substring(startIndex, endIndex - startIndex + End.Length);

            var match = Regex.Match(content, "<span class=\"postername\">(?<name>.*?)</span>", RegexOptions.Singleline);

            string name = BoardManager.StripHtml(match.Groups["name"].Value, false).Trim(new[] { '\t', '\n' });
            
            var message = new Message();

            message.ImageLink = ExtractImageLink(content);
            message.ThumbImageLink = ExtractThumbImageLink(content);
            message.PosterName = string.IsNullOrEmpty(name) ? null : name;

            List<string> quotes;
            message.Content = ExtractMessage(content, out quotes);

            message.PostTime = ExtractDate(content);

            return message;
        }
    }
}
