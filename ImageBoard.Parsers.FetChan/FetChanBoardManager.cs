using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.FetChan
{
    public sealed class FetChanBoardManager : KusabaBoardManager
    {
        public FetChanBoardManager()
        {
            this.TopicParser = new FetChanTopicParser(this.BoardUriFormat, false);
            this.MessageParser = new FetChanMessageParser(false);
        }

        public override string Name
        {
            get
            {
                return "fetchan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/sug/";
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
                case "/s/":
                    return "Sexy and Beautiful Bondage";

                case "/f/":
                    return "Femdom";

                case "/m/":
                    return "Maledom";

                case "/g/":
                    return "Gay / Trap";

                case "/enf/":
                    return "Enforced/Embarrassed Nude Females";

                case "/a/":
                    return "Hentai/Animu & Art";

                case "/ft/":
                    return "Feet";

                case "/cfnm/":
                    return "Clothed Female Naked Male";

                case "/cfnf/":
                    return "Clothed Female Naked Female";

                case "/cmfn/":
                    return "Clothed Male Naked Female";

                case "/mc/":
                    return "Mindcontrol & Hypnosis";

                case "/wtf/":
                    return "Gross / Bestiality / WTF";

                case "/b/":
                    return "Random";

                case "/lit/":
                    return "Literature";

                case "/sug/":
                    return "Suggestions";

                case "/sb/":
                    return "Selfbondage";

                case "/c/":
                    return "Contacts & Ads";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://fetchan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://fetchan.org/board.php";
            }
        }
    }
}
