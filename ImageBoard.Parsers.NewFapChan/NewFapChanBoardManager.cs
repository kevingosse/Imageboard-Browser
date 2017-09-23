using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.NewFapChan
{
    public sealed class NewFapChanBoardManager : KusabaBoardManager
    {
        public override string BoardUriFormat
        {
            get
            {
                return "http://www.newfapchan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://www.newfapchan.org/board.php";
            }
        }

        public NewFapChanBoardManager()
        {
            this.TopicParser = new KusabaTopicParser(this.BoardUriFormat, true);
            this.MessageParser = new KusabaMessageParser(true);
        }

        public override string Name
        {
            get
            {
                return "newfapchan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/vg/";
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
                case "/dev/":
                    return "Development";

                case "/mu/":
                    return "Music";

                case "/b/":
                    return "Random";

                case "/vg/":
                    return "Vidya Gaems";

                case "/am/":
                    return "Amateur";

                case "/an/":
                    return "Anal";

                case "/asi/":
                    return "Asian";

                case "/cd/":
                    return "Crossdressing";

                case "/cs/":
                    return "Cumshots";

                case "/babby/":
                    return "Diaper Fetishism & Ageplay";

                case "/di/":
                    return "Dickgirls";

                case "/fat/":
                    return "Fatties";

                case "/fe/":
                    return "Fetish";

                case "/gay/":
                    return "Gay";

                case "/gp/":
                    return "General Porno";

                case "/girly/":
                    return "Girly Fetishism & Feminization";

                case "/hypno/":
                    return "Hypnosis Fetishism";

                case "/mat/":
                    return "Mature";

                case "/ni/":
                    return "Nigras";

                case "/smb/":
                    return "S&M / Bondage";

                case "/men/":
                    return "Sexy Beautiful Men";

                case "/s/":
                    return "Sexy Beautiful Women";

                case "/cm/":
                    return "Comics";

                case "/do/":
                    return "Doujinshi";

                case "/fur/":
                    return "Furfags";

                case "/fn/":
                    return "Futanari";

                case "/gfur/":
                    return "Gay Furfags";

                case "/gu/":
                    return "Guro";

                case "/d/":
                    return "Hentai: Alternative";

                case "/h/":
                    return "Hentai: General";

                case "/34/":
                    return "RULE 34";

                case "/y/":
                    return "Yaoi";

                case "/gif/":
                    return "Animated GIFs";

                case "/f/":
                    return "Flash";

                case "/r/":
                    return "Request";

                case "/w/":
                    return "Wallpaper";

                default:
                    return null;
            }
        }
    }
}
