using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common.Tinyboard
{
    public class TinyboardMessageParser : IMessageParser
    {
        public TinyboardMessageParser(string boardUriFormat)
        {
            this.BoardUriFormat = boardUriFormat;            
        }

        protected string BoardUriFormat { get; set; }

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

            var match = Regex.Match(body, "<input type=\"hidden\" name=\"thread\" value=\"(?<resto>\\S+)\">");

            string resto = match.Groups["resto"].Value;

            var history = new Dictionary<string, Message>();

            foreach (var messageContent in this.GetMessages(body))
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

                yield return message;
            }
        }

        protected virtual IEnumerable<string> GetMessages(string content)
        {
            var messages = Regex.Split(content, "(?=<div class=\"postContainer)");

            return messages.Skip(1);
        }

        protected virtual string ExtractId(string messageContent)
        {
            var match = Regex.Match(messageContent, "id=\"reply_(?<id>[0-9]+)\">");

            return match.Groups["id"].Value;
        }

        private string ExtractMessage(string messageContent, out List<string> quotes)
        {
            quotes = new List<string>();

            var messageMatch = Regex.Match(messageContent, "<div class=\"body\">(?<message>.*?)</div>", RegexOptions.Singleline);

            var message = messageMatch.Groups["message"].Value;

            var spoilerMatches = Regex.Matches(
                message,
                "<span class=\"spoiler\">(?<message>.+?)</span>");

            foreach (Match spoilerMatch in spoilerMatches)
            {
                message = message.Replace(spoilerMatch.Value, "|||s" + spoilerMatch.Groups["message"] + "s|||");
            }

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

            matches = Regex.Matches(message, "\\|\\|\\|s(?<message>.+?)s\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string spoiler = match.Groups["message"].Value;

                message = message.Replace(match.Value, "<s>" + spoiler + "</s>");
            }

            message = BoardManager.ExtractLinks(message);

            return message;
        }

        private string ExtractThumbImageLink(string messageContent)
        {
            var match = Regex.Match(messageContent, "target=\"_blank\"><img class=\".+?\" src=\"(?<image>.+?)\"");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(this.BoardUriFormat, match.Groups["image"].Value);
        }

        private string ExtractImageLink(string messageContent)
        {
            var match = Regex.Match(messageContent, "<p class=\"fileinfo\">.+?<a href=\"(?<image>.+?)\">");

            if (!match.Success)
            {
                return null;
            }

            return string.Format(this.BoardUriFormat, match.Groups["image"].Value);
        }

        private string ExtractPosterName(string messageContent)
        {
            var match = Regex.Match(messageContent, "<span class=\"name\">(?<name>.+?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private DateTime? ExtractPostTime(string messageContent)
        {
            var match = Regex.Match(messageContent, "<time datetime=\"(?<date>.+?)\"");

            return TinyboardBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
