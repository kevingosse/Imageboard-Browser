using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.FChan
{
    public class FChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://fchan.us{0}";

        public FChanBoardManager()
        {
            this.TopicParser = new FChanTopicParser();
            this.MessageParser = new FChanMessageParser();
        }

        public override string Name
        {
            get
            {
                return "fchan.us";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/c/";
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
            throw new NotImplementedException();
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            throw new NotImplementedException();
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            throw new NotImplementedException();
        }

        public override PostResult IsPostOk(string response)
        {
            throw new NotImplementedException();
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
                case "/f/":
                    return "Female";

                case "/m/":
                    return "Male";

                case "/h/":
                    return "Herm";

                case "/s/":
                    return "Straight & Bi";

                case "/toon/":
                    return "Toon";

                case "/a/":
                    return "Alternative";

                case "/ah/":
                    return "Alternative (hard)";

                case "/c/":
                    return "Clean";

                case "/artist/":
                    return "Artist";

                case "/crit/":
                    return "Critique";

                case "/b/":
                    return "Banner";

                case "/dis/":
                    return "Discussion";

                case "/req/":
                    return "Requests";

                case "/faq/":
                    return "Rule Clarification";

                default:
                    return null;
            }
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

            if (DateTime.TryParseExact(rawDate + "-05:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }
    }
}
