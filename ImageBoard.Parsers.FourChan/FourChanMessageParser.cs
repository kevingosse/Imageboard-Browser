using System.Collections.Generic;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

using Newtonsoft.Json.Linq;

namespace ImageBoard.Parsers.FourChan
{
    public class FourChanMessageParser : IMessageParser
    {
        public IEnumerable<Message> ParsePage(Context context, string content)
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject(content) as JObject;
            
            if (result != null)
            {
                var messages = result["posts"];

                var history = new Dictionary<string, Message>();

                var messagesById = new Dictionary<string, List<Message>>();

                foreach (var message in messages)
                {
                    var resto = (int)message["resto"];
                    var id = (int)message["no"];

                    if (resto == 0)
                    {
                        // Resto stands for "responds to". For the first message of the topic, it's the id of the message
                        resto = id;
                    }

                    List<string> quotes;

                    var newMessage = new Message
                    {
                        Id = id.ToString(),
                        Content = ExtractMessage(message, out quotes),
                        ImageLink = FourChanBoardManager.ExtractImageLink(context, message),
                        ThumbImageLink = FourChanBoardManager.ExtractThumbImageLink(context, message),
                        PosterName = ExtractPosterName(message),
                        PostTime = FourChanBoardManager.ExtractPostTime(message),
                        CountryFlag = FourChanBoardManager.ExtractCountryFlag(context, message),
                        PosterId = (string)message["id"],
                        Resto = resto.ToString()
                    };

                    history[newMessage.Id] = newMessage;

                    if (newMessage.PosterId != null)
                    {
                        List<Message> previousMessages;

                        if (!messagesById.TryGetValue(newMessage.PosterId, out previousMessages))
                        {
                            previousMessages = new List<Message>();
                            messagesById[newMessage.PosterId] = previousMessages;
                        }

                        previousMessages.Add(newMessage);
                    }

                    foreach (var quote in quotes)
                    {
                        Message quotedMessage;

                        if (history.TryGetValue(quote, out quotedMessage))
                        {
                            if (quotedMessage.BackLinks == null)
                            {
                                quotedMessage.BackLinks = new List<Message>();
                            }

                            quotedMessage.BackLinks.Add(newMessage);
                        }
                    }

                    yield return newMessage;
                }

                foreach (var kvp in messagesById)
                {
                    var count = kvp.Value.Count;

                    foreach (var message in kvp.Value)
                    {
                        message.PreviousMessagesCount = count;
                    }
                }
            }
        }

        private static string ExtractMessage(JToken messageContent, out List<string> quotes)
        {
            quotes = new List<string>();

            var message = (string)messageContent["com"] ?? string.Empty;

            var quoteMatches = Regex.Matches(
                message,
                "<a href=\".+?\" class=\"quotelink\".*?>&gt;&gt;(?<id>[0-9]+)</a>");

            foreach (Match quoteMatch in quoteMatches)
            {
                message = message.Replace(quoteMatch.Value, "|||&gt;&gt;" + quoteMatch.Groups["id"].Value + "|||");

                quotes.Add(quoteMatch.Groups["id"].Value);
            }

            // <span class="quote">
            var greenTextMatches = Regex.Matches(
                message,
                "<span class=\"quote\">(?<message>.+?)</span>");

            foreach (Match greenTextMatch in greenTextMatches)
            {
                message = message.Replace(greenTextMatch.Value, "|||g" + greenTextMatch.Groups["message"] + "g|||");
            }

            var spoilerMatches = Regex.Matches(
                message,
                "<s>(?<message>.+?)</s>");

            foreach (Match spoilerMatch in spoilerMatches)
            {
                message = message.Replace(spoilerMatch.Value, "|||s" + spoilerMatch.Groups["message"] + "s|||");
            }

            message = BoardManager.StripHtml(message, true);

            var matches = Regex.Matches(message, "\\|\\|\\|&gt;&gt;(?<id>[0-9]+)\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string id = match.Groups["id"].Value;

                message = message.Replace("|||&gt;&gt;" + id + "|||", "<br/><quote>" + id + "</quote><br/>");
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

            matches = Regex.Matches(message, "\\|\\|\\|g(?<message>.+?)g\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string greenText = match.Groups["message"].Value;

                message = message.Replace(match.Value, "<greenText>" + greenText + "</greenText>");
            }

            message = BoardManager.ExtractLinks(message);

            return message;
        }

        private static string ExtractPosterName(JToken message)
        {
            var name = (string)message["name"] ?? string.Empty;

            name = Regex.Replace(name.Replace("\r\n", string.Empty), "<.*?>", string.Empty).Trim();

            return name;
        }
    }
}
