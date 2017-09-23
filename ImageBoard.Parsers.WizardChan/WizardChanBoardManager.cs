using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

using Newtonsoft.Json.Linq;

namespace ImageBoard.Parsers.WizardChan
{
    public sealed class WizardChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://wizchan.org{0}";

        internal const string PictureFormat = "https://wizchan.org{0}src/{1}{2}";

        // Thumbnails are jpg stored as .gif. Adding a ? to trick the EmbeddedImage algorithm into not recognizing a gif
        internal const string ThumbnailFormat = "https://wizchan.org{0}thumb/{1}.gif?";
        //internal const string ThumbnailFormat = "https://wizchan.org{0}thumb/{1}{2}";

        public WizardChanBoardManager()
        {
            this.TopicParser = new WizardChanTopicParser();
            this.MessageParser = new WizardChanMessageParser();
        }

        public override string Name
        {
            get
            {
                return "wizardchan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/wiz/";
            }
        }

        public override bool IsCatalogSupported
        {
            get
            {
                return true;
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
                Email = "email",
                Name = "name",
                Subject = "subject",
                Image = "file",
                FileName = "filename",
                Password = "password",
                TopicId = "thread",
                Comment = "body"
            };
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return "http://wizchan.org/post.php";
        }

        public override Uri BuildCatalogLink(Board board)
        {
            return new Uri(string.Format(BoardUriFormat, board.Name) + "catalog.json?nocache=" + DateTime.UtcNow.Ticks);
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, "res/(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("<h1>Error</h1>"))
            {
                var match = Regex.Match(response, "<div class=\"subtitle\">(?<reason>.+?)</div>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["reason"].Value);
                }
            }

            return PostResult.Ok();
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(BoardUriFormat, board) + page + ".json";

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
                case "/wiz/":
                    return "General";

                case "/hob/":
                    return "Hobbies";

                case "/meta/":
                    return "Meta";

                case "/v9k/":
                    return "Virgin9000";

                case "/b/":
                    return "Random";

                default:
                    return null;
            }
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }

        public string PostAddress
        {
            get
            {
                return "http://wizchan.org/post.php";
            }
        }

        internal static string ExtractImageLink(Context context, JToken message)
        {
            var tim = (string)message["tim"];
            var ext = (string)message["ext"];

            if (tim == null || ext == null)
            {
                return null;
            }

            return string.Format(PictureFormat, context.Board.Name, tim, ext);
        }

        internal static string ExtractThumbImageLink(Context context, JToken thread)
        {
            var tim = (string)thread["tim"];
            var ext = (string)thread["ext"];

            if (tim == null || ext == null)
            {
                var embed = (string)thread["embed"];

                if (embed != null && embed.Contains("https://www.youtube.com/watch?v="))
                {
                    var youtubeMatch = Regex.Match(embed, @"""https:\/\/www.youtube.com\/watch\?v=(?<id>[a-zA-Z]+)""");

                    if (youtubeMatch.Success)
                    {
                        return string.Format("http://img.youtube.com/vi/{0}/0.jpg", youtubeMatch.Groups["id"].Value);
                    }
                }

                return null;
            }

            if (ext == ".webm")
            {
                ext = ".jpg";
            }

            return string.Format(ThumbnailFormat, context.Board.Name, tim, ext);
        }

        internal static DateTime ExtractPostTime(JToken thread)
        {
            return new DateTime(1970, 1, 1).AddSeconds((int)thread["time"]);
        }

        internal static string ExtractCountryFlag(Context context, JToken message)
        {
            var flagCode = (string)message["country"];

            if (flagCode == null)
            {
                return null;
            }

            var format = "https://s.4cdn.org/image/country/{0}.gif";

            return string.Format(format, flagCode.ToLower());
        }
    }
}
