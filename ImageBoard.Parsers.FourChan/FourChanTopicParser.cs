using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

using Newtonsoft.Json.Linq;

namespace ImageBoard.Parsers.FourChan
{
    public class FourChanTopicParser : ITopicParser
    {
        public const string ThreadLinkFormat = "https://a.4cdn.org{0}thread/{1}.json";
       
        public IEnumerable<Topic> ParsePage(Context context, string content)
        {
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject(content) as JObject;

            if (result != null)
            {
                var threads = result["threads"];

                foreach (var thread in threads)
                {
                    var messages = thread["posts"];

                    if (messages != null && messages.First != null)
                    {
                        var message = messages.First;

                        yield return new Topic
                        {
                            Id = (string)message["no"],
                            ImageLink = FourChanBoardManager.ExtractImageLink(context, message),
                            Content = ExtractMessage(message),
                            PosterName = ExtractPosterName(message),
                            CountryFlag = FourChanBoardManager.ExtractCountryFlag(context, message),
                            PostTime = FourChanBoardManager.ExtractPostTime(message),
                            ReplyLink = ExtractReplyLink(context, message),
                            ThumbImageLink = FourChanBoardManager.ExtractThumbImageLink(context, message),
                            NumberOfReplies = (int)message["replies"],
                            Subject = ExtractSubject(message)
                        };
                    }
                }
            }
        }

        public int? ExtractPageCount(Context context, string html)
        {
            if (context.Board.Name.Equals("/b/", StringComparison.OrdinalIgnoreCase)
                || context.Board.Name.Equals("/r", StringComparison.OrdinalIgnoreCase))
            {
                return 16;
            }

            if (context.Board.Name.Equals("/vg/", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            return 11;
        }

        public IEnumerable<CatalogEntry> ParseCatalog(Context context, string content)
        {
            var pages = Newtonsoft.Json.JsonConvert.DeserializeObject(content) as JArray;

            if (pages != null)
            {
                foreach (var page in pages)
                {
                    var result = (JObject)page;

                    if (result != null)
                    {
                        var threads = result["threads"];

                        foreach (var thread in threads)
                        {
                            yield return new CatalogEntry
                            {
                                Date = FourChanBoardManager.ExtractPostTime(thread),
                                RepliesCount = (int)thread["replies"],
                                ImagesCount = (int)thread["images"],
                                Id = (string)thread["no"],
                                ReplyLink = ExtractReplyLink(context, thread),
                                Author = (string)thread["name"],
                                ThumbImageLink = FourChanBoardManager.ExtractThumbImageLink(context, thread),
                                ImageWidth = (int?)thread["tn_w"],
                                ImageHeight = (int?)thread["tn_h"],
                                Subject = (string)thread["sub"],
                                Description = ExtractMessage(thread)
                            };
                        }
                    }
                }
            }
        }

        private static string ExtractReplyLink(Context context, JToken thread)
        {
            return string.Format(ThreadLinkFormat, context.Board.Name, thread["no"]);
        }

        private static string ExtractSubject(JToken thread)
        {
            var subject = (string)thread["sub"];

            return subject == null ? null : BoardManager.StripHtml(subject, false);
        }

        private static string ExtractPosterName(JToken thread)
        {
            string name = (string)thread["name"] ?? string.Empty;

            return BoardManager.StripHtml(name.Replace("\r\n", string.Empty), false).Trim();
        }

        private static string ExtractMessage(JToken thread)
        {
            var content = (string)thread["com"];

            if (content == null)
            {
                return string.Empty;
            }

            var greenTextMatches = Regex.Matches(
                content,
                "<span class=\"quote\">(?<message>.+?)</span>");

            foreach (Match greenTextMatch in greenTextMatches)
            {
                content = content.Replace(greenTextMatch.Value, "|||g" + greenTextMatch.Groups["message"] + "g|||");
            }
            
            content = BoardManager.StripHtml(content, true);

            var matches = Regex.Matches(content, "\\|\\|\\|g(?<message>.+?)g\\|\\|\\|");

            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                string greenText = match.Groups["message"].Value;

                content = content.Replace(match.Value, "<greenText>" + greenText + "</greenText>");
            }

            return content;
        }
    }
}
