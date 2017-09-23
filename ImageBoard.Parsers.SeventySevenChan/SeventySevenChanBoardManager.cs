using System;
using System.Collections.Generic;
using System.Globalization;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.SeventySevenChan
{
    public sealed class SeventySevenChanBoardManager : KusabaBoardManager
    {
        public SeventySevenChanBoardManager()
        {
            this.TopicParser = new SeventySevenChanTopicParser(this.BoardUriFormat, false);
            this.MessageParser = new SeventySevenChanMessageParser(false);
        }

        public override string Name
        {
            get
            {
                return "77chan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/a/";
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://77chan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://77chan.org/board.php";
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public override void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Common.Context context, bool newTopic)
        {
            base.FillAdditionnalMessageFields(parameters, context, newTopic);
            parameters["recaptcha_challenge_field"] = "doqe";
            parameters["recaptcha_response_field"] = "manual_challenge";
        }

        public static new DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }
            const string DateFormat = "dd/MM/yyyy HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+03:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
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
                case "/b/":
                    return "Random";

                case "/mod/":
                    return "Moderação";

                case "/a/":
                    return "Anime";

                case "/art/":
                    return "Criatividade e desenvolvimento artístico";

                case "/bro/":
                    return "BROTIPS";

                case "/conf/":
                    return "Confraria";

                case "/epic/":
                    return "Epic";

                case "/feels/":
                    return "Sentimentos";

                case "/high/":
                    return "Discussão sobre entorpecentes";

                case "/int/":
                    return "International";

                case "/jo/":
                    return "Jogos";

                case "/mu/":
                    return "Música";

                case "/r/":
                    return "Requests";

                case "/rpg/":
                    return "Role Playing Game";

                case "/share/":
                    return "Share";

                case "/stdio/":
                    return "int main( int argc char *argv )";

                case "/x/":
                    return "Sobrenatural";

                case "/df/":
                    return "Desenvolvimento Físico";

                case "/dm/":
                    return "Desenvolvimento Mental";

                case "/est/":
                    return "Estudos";

                case "/jew/":
                    return "Finanças";

                case "/lit/":
                    return "Literatura";

                case "/fet/":
                    return "Fetiches";

                case "/p/":
                    return "Internet is for porn";

                case "/tr/":
                    return "Traps";

                default:
                    return null;
            }
        }
    }
}
