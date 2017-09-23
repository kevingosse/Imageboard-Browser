using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common.Kusaba
{
    public class KusabaMessageParser : IMessageParser
    {
        public KusabaMessageParser(bool injectReferer)
        {
            this.InjectReferer = injectReferer;
        }

        protected bool InjectReferer { get; set; }

        public virtual IEnumerable<Message> ParsePage(Context context, string content)
        {
            int bodyStart = content.IndexOf("<body");
            int bodyEnd = content.LastIndexOf("</body>");

            Debug.Assert(bodyEnd > -1, "Error parsing page");

            if (bodyEnd == -1)
            {
                yield break;
            }

            string body = content.Substring(bodyStart, bodyEnd - bodyStart + 7);

            var match = Regex.Match(body, "<input type=\"hidden\" name=\"replythread\" value=\"(?<resto>\\S+)\" />");

            string resto = match.Groups["resto"].Value;

            var messages = Regex.Split(body, "(?=<td class=\"doubledash\">)");

            var history = new Dictionary<string, Message>();

            foreach (var messageContent in messages)
            {
                List<string> quotes;

                var message = new Message
                {
                    Id = ExtractId(messageContent),
                    Content = ExtractMessage(messageContent, out quotes),
                    ImageLink = ExtractImageLink(messageContent),
                    ThumbImageLink = ExtractThumbImageLink(messageContent),
                    PosterName = ExtractPosterName(messageContent),
                    PostTime = ExtractPostTime(messageContent),
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

                if (this.InjectReferer)
                {
                    message.Referer = context.Board.Uri;
                }

                yield return message;
            }
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<a href=\"(?<image>.+?)\" onclick=\"javascript:expandimg\\(");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "<img src=\"(?<image>.+?)\" alt=\"[0-9]+\" class=\"thumb");

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["image"].Value;
        }

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            var messageMatch = Regex.Match(content, "<blockquote>(?<message>.*?)</blockquote>", RegexOptions.Singleline);

            var message = messageMatch.Groups["message"].Value;

            message = BoardManager.StripHtml(message, true);

            var matches = Regex.Matches(message, "&gt;&gt;(?<id>[0-9]+)");

            foreach (Match match in matches)
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
            var match = Regex.Match(message, "<a name=\"(?<id>[0-9]+)\"></a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        protected virtual DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9][0-9]/[0-9][0-9]/[0-9][0-9]\(\w+\)[0-9][0-9]:[0-9][0-9])", RegexOptions.Singleline);

            return KusabaBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
