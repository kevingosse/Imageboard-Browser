using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common.Kusaba
{
    public abstract class KusabaBoardManager : BoardManager
    {
        public abstract string BoardUriFormat { get; }

        public abstract string PostAddress { get; }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Subject = "subject",
                Comment = "message",
                Name = "name",
                Image = "imagefile",
                FileName = "filename",
                TopicId = "replythread",
                Email = "em",
                Password = "postpassword"
            };
        }

        public override void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            if (newTopic)
            {
                parameters["replythread"] = 0;
            }

            parameters.Add("board", context.Board.Name.Trim('/'));
            parameters.Add("email", string.Empty);
            parameters.Add("MAX_FILE_SIZE", "10240000");
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return PostAddress;
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, ".+?/(?<id>[0-9]+).html$");

            return match.Groups["id"].Value;
        }

        public override PostResult IsPostOk(string response)
        {
            Debug.WriteLine(response);

            if (Regex.IsMatch(response, "<h1 style=\".+?\">Erro\\w*</h1>"))
            {
                var match = Regex.Match(response, "<h2 style=\".+?\">(?<error>.+?)</h2>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["error"].Value.Trim(new[] { '\r', '\n' }));
                }

                return PostResult.Error(null);
            }

            return PostResult.Ok();
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(BoardUriFormat, board);

            if (page > 0)
            {
                baseLink += page + ".html";
            }

            if (noCacheSeed != null)
            {
                baseLink += "?nocache=" + noCacheSeed;
            }

            return new Uri(baseLink);
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }

        public static DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }
            const string DateFormat = "yy/MM/dd(ddd)HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+02:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }
    }
}
