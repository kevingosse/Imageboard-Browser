using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.KrautChan
{
    public class KrautChanMessageParser : IMessageParser
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

            var match = Regex.Match(body, "<input type=\"hidden\" name=\"redirect\" value=\"(?<resto>[0-9]+)\">");

            string resto = match.Groups["resto"].Value;

            var values = body.Split(new[] { "<div class=\"postheader\">" }, StringSplitOptions.None);

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
                    CountryFlag = ExtractCountryFlag(messageContent),
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

        public static string ExtractCountryFlag(string message)
        {
            var match = Regex.Match(message, "<img src=\"/images/balls/(?<country>[a-z]+)\\.png");

            if (!match.Success)
            {
                return null;
            }

            return string.Format("http://krautchan.net/images/balls/{0}.png", match.Groups["country"]);
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

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            var messageMatch = Regex.Match(content, "<blockquote>(?<message>.+?)</blockquote>", RegexOptions.Singleline);

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
            var match = Regex.Match(message, "<input name=\"post_(?<id>[0-9]+)\" value=\"delete\" type=\"checkbox\">");

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername\">(?<name>.+?)</span>");

            string name = match.Groups["name"].Value;

            name = Regex.Replace(name, "<.*?>", string.Empty).Trim();

            return name;
        }

        private static DateTime? ExtractDate(string message)
        {
            var match = Regex.Match(message, "<span class=\"postdate\">(?<date>.+?)</span>");

            var rawDate = match.Groups["date"].Value;

            return KrautChanBoardManager.ConvertDate(rawDate);
        }
    }
}
