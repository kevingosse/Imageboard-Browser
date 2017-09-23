using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.Ylilauta
{
    public class YlilautaMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            string body = content;

            var match = Regex.Match(body, "<div id=\"thread_(?<resto>\\S+)\">");

            string resto = match.Groups["resto"].Value;

            var anticaptchaMatch = Regex.Match(body, "<input name=\"anticaptcha\" id=\"anticaptcha\" value=\"(?<id>.+?)\" type=\"hidden\"/>");

            context.Board.AdditionalFields["anticaptcha"] = anticaptchaMatch.Groups["id"].Value;

            var uuidMatch = Regex.Match(body, "<input name=\"uuid\" id=\"uuid\" value=\"(?<id>.+?)\" type=\"hidden\"/>");

            context.Board.AdditionalFields["uuid"] = uuidMatch.Groups["uuid"].Value;

            var values = Regex.Split(body, "(?=<div class=\"postinfo\">)");

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
                    PostTime = ExtractPostTime(messageContent),
                    CountryFlag = ExtractFlag(messageContent),
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

        private static string ExtractFlag(string message)
        {
            var match = Regex.Match(message, "<img class=\"flag_[a-z]+\" src=\"(?<flag>.+?)\"");

            if (!match.Success)
            {
                return null;
            }

            return BoardManager.FixLink(match.Groups["flag"].Value, true);
        }

        private static string ExtractImageLink(string message)
        {
            var match = Regex.Match(message, "<div class=\"fileinfo\"><a href=\"(?<image>.+?)\" class=\"openblank\">");

            if (!match.Success)
            {
                return null;
            }

            var image = match.Groups["image"].Value;

            if (image.EndsWith(".mp4"))
            {
                return null;
            }

            return BoardManager.FixLink(image, true);
        }

        private static string ExtractThumbImageLink(string message)
        {
            var match = Regex.Match(message, "data-thumbfile=\"(?<image>.+?)\"", RegexOptions.Singleline);

            if (!match.Success)
            {
                return null;
            }

            if (match.Groups["image"].Value.EndsWith(".webm"))
            {
                return null;
            }

            return BoardManager.FixLink(BoardManager.StripHtml(match.Groups["image"].Value, true), true);
        }

        private static string ExtractMessage(string content, out List<string> quotes)
        {
            quotes = new List<string>();

            var messageMatch = Regex.Match(content, "<div class=\"post\"><p.+?>(?<message>.+?)</p></div>", RegexOptions.Singleline);

            var message = messageMatch.Groups["message"].Value;

            var greenTextMatches = Regex.Matches(content, "<span class=\"quote\">(?<message>.+?)</span>");

            foreach (Match greenTextMatch in greenTextMatches)
            {
                content = content.Replace(greenTextMatch.Value, "|||g" + greenTextMatch.Groups["message"] + "g|||");
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

            matches = Regex.Matches(content, "\\|\\|\\|g(?<message>.+?)g\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string greenText = match.Groups["message"].Value;

                content = content.Replace(match.Value, "<greenText>" + greenText + "</greenText>");
            }

            message = BoardManager.ExtractLinks(message);

            return message;
        }

        private static string ExtractId(string message)
        {
            var match = Regex.Match(message, "class=\"quotelink\">(?<id>[0-9]+)</a>");

            return match.Groups["id"].Value;
        }

        private static string ExtractPosterName(string message)
        {
            var match = Regex.Match(message, "<span class=\"postername.*?\">(?<name>.*?)</span>", RegexOptions.Singleline);

            return BoardManager.StripHtml(match.Groups["name"].Value, false);
        }

        private static DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, "<span class=\"posttime\">(?<date>.+?)</span>", RegexOptions.Singleline);

            return YlilautaBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
