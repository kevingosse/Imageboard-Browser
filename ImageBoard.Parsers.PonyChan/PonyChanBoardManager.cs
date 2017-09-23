using System;
using System.Globalization;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.PonyChan
{
    public class PonyChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://www.ponychan.net/chan{0}";

        public PonyChanBoardManager()
        {
            this.TopicParser = new PonyChanTopicParser();
            this.MessageParser = new PonyChanMessageParser();
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/gala/";
            }
        }

        public override string Name
        {
            get
            {
                return "ponychan.net";
            }
        }

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
        }

        public static DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }

            // Sun, May 20th, 2012 20:13
            rawDate = rawDate
                .Replace("st", string.Empty)
                .Replace("rd", string.Empty)
                .Replace("th", string.Empty);

            const string DateFormat = "ddd, MMM d, yyyy HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "-05:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return "http://www.ponychan.net/chan/board.php";
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, "res/(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("<h1 style=\"font-size: 3em;\">Error</h1>"))
            {
                var match = Regex.Match(response, "<h2 style=\"font-size: 2em;font-weight: bold;text-align: center;\">(?<reason>.+?)</h2>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["reason"].Value);
                }
            }

            return PostResult.Ok();
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Email = "em",
                Name = "name",
                Subject = "subject",
                Image = "imagefile",
                FileName = "filename",
                Password = "postpassword",
                TopicId = "replythread",
                Comment = "message"
            };
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));

            parameters.Add("quickreply", string.Empty);
            parameters.Add("MAX_FILE_SIZE", "4194304");
            parameters.Add("email", string.Empty);
            parameters.Add("postform", "postform");

            string pony;

            if (newTopic)
            {
                pony = context.Board.AdditionalFields["pony"];
            }
            else
            {
                pony = context.Topic.AdditionalFields["pony"];
                parameters.Add("replythread", "0");
            }

            parameters.Add("how_much_pony_can_you_handle", pony);
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

        public override string FillDescription(string boardName)
        {
            if (string.IsNullOrEmpty(boardName))
            {
                return null;
            }

            boardName = boardName.ToLower();

            switch (boardName)
            {
                case "/meta/":
                    return "Site Issues";

                case "/arch/":
                    return "Twilight's Library";

                case "/show/":
                    return "Friendship is Magic";

                case "/merch/":
                    return "Merchandise";

                case "/oat/":
                    return "Pony General";

                case "/art/":
                    return "Art";

                case "/fic/":
                    return "Fanfics";

                case "/media/":
                    return "Music/video";

                case "/collab/":
                    return "Projects";

                case "/rp/":
                    return "Roleplay";

                case "/ooc/":
                    return "Roleplay Lounge";

                case "/good/":
                    return "Forum o’ Good";

                case "/phoenix/":
                    return "Shipping";

                case "/pic/":
                    return "Pictures";

                case "/vinyl":
                    return "Music";

                case "/g/":
                    return "Games";

                case "/dis/":
                    return "Discussion";

                case "/chat/":
                    return "Chat";

                case "/gala/":
                    return "Gala";

                case "/int/":
                    return "World";

                case "/pony/":
                    return "Show Discussion";

                default:
                    return null;
            }
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }
    }
}
