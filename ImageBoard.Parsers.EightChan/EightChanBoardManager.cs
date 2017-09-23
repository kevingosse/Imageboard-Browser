using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

using Newtonsoft.Json.Linq;

namespace ImageBoard.Parsers.EightChan
{
    public class EightChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "https://8ch.net{0}";

        internal const string PictureFormat = "https://8ch.net{0}src/{1}{2}";
        internal const string ThumbnailFormat = "https://8ch.net{0}thumb/{1}{2}";

        public EightChanBoardManager()
        {
            this.TopicParser = new EightChanTopicParser();
            this.MessageParser = new EightChanMessageParser();
        }

        public override string Name
        {
            get
            {
                return "8chan.co";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/v/";
            }
        }

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
        }

        public override bool IsCatalogSupported
        {
            get
            {
                return true;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public override Uri BuildCatalogLink(Board board)
        {
            return new Uri(string.Format(BoardUriFormat, board.Name) + "catalog.json?nocache=" + DateTime.UtcNow.Ticks);
        }

        public override Uri GetBrowserTopicLink(Context context)
        {
            return new Uri(string.Format("https://8ch.net{0}res/{1}.html", context.Board.Name, context.Topic.Id));
        }

        public override Uri GetBrowserPageLink(Context context, int currentPage)
        {
            if (currentPage == 0)
            {
                return new Uri(string.Format("https://8ch.net{0}", context.Board.Name));
            }

            return new Uri(string.Format("https://8ch.net{0}{1}.html", context.Board.Name, currentPage + 1));
        }

        public override MessageMapping GetMessageMapping()
        {
            return null;
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return null;
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            return null;
        }

        public override PostResult IsPostOk(string response)
        {
            return null;
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
            return null;
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
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
