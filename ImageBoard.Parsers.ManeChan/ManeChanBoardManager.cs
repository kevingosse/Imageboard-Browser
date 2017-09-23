using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.ManeChan
{
    public sealed class ManeChanBoardManager : KusabaBoardManager
    {
        public ManeChanBoardManager()
        {
            this.TopicParser = new KusabaTopicParser(this.BoardUriFormat, false);
            this.MessageParser = new ManeChanMessageParser(false);
        }

        public override string Name
        {
            get
            {
                return "manechan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/mlp/";
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
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
                case "/draw/":
                    return "Drawfags Oekaki";

                case "/mlp/":
                    return "4pone";

                case "/s/":
                    return "Site discussion";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://manechan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://manechan.org/board.php";
            }
        }
    }
}
