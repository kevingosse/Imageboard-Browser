using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.OperatorChan
{
    public sealed class OperatorChanBoardManager : KusabaBoardManager
    {
        public OperatorChanBoardManager()
        {
            this.TopicParser = new KusabaTopicParser(this.BoardUriFormat, false);
            this.MessageParser = new KusabaMessageParser(false);
        }

        public override string Name
        {
            get
            {
                return "operatorchan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/t/";
            }
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
                case "/k/":
                    return "Weapons";

                case "/g/":
                    return "Gear";

                case "/v/":
                    return "Vehicles";

                case "/w/":
                    return "Warfare, Military & Militaria";

                case "/st/":
                    return "Strip Club";

                case "/s/":
                    return "Survival, Outdoors & Hunting";

                case "/stem/":
                    return "Science, Technology, Engineering and Mathematics";

                case "/pt/":
                    return "Fitness & Sports";

                case "/a/":
                    return "Airsoft & Paintball";

                case "/vg/":
                    return "Vidya & Games";

                case "/n/":
                    return "News";

                case "/t/":
                    return "Talk";

                case "/m/":
                    return "Media, Photography & Wallpapers";

                case "/trade/":
                    return "Buy, Sell & Trade";

                case "/meet/":
                    return "Meetings & Shoots";

                case "/pasta/":
                    return "Copypasta";

                case "/z/":
                    return "Files";

                case "/sug/":
                    return "Suggestions & Feedback";

                case "/arch/":
                    return "Archive";

                case "/pbe/":
                    return "PBE Store";

                case "/clw/":
                    return "Cyclone Leadworks";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://www.operatorchan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://www.operatorchan.org/board.php";
            }
        }
    }
}
