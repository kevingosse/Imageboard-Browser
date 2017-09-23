using System;
using System.Globalization;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.FiftyFiveChan
{
    public sealed class FiftyFiveChanBoardManager : KusabaBoardManager
    {

        public FiftyFiveChanBoardManager()
        {
            this.TopicParser = new FiftyFiveChanTopicParser(this.BoardUriFormat, true);
            this.MessageParser = new FiftyFiveChanMessageParser(true);
        }

        public override string Name
        {
            get
            {
                return "55ch.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/a/";
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public static new DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }
            const string DateFormat = "dd/MM/yy(ddd)HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+03:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Common.Context context, bool newTopic)
        {
            base.FillAdditionnalMessageFields(parameters, context, newTopic);
            parameters["captcha"] = "epic";
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
                case "/a/":
                    return "C. Jews";

                case "/b/":
                    return "Random";

                case "/d/":
                    return "Disputatio";

                case "/mod/":
                    return "Moderação";

                case "/cri/":
                    return "Criação";

                case "/c/":
                    return "Copi-cola";

                case "/an/":
                    return "Assuntos Nipônicos";

                case "/lit/":
                    return "Livros, quadrinhos, etc.";

                case "/mu/":
                    return "Música";

                case "/tv/":
                    return "Filmes, séries, programas de TV";

                case "/jo/":
                    return "Jogos";

                case "/lan/":
                    return "Jogatina conjunta";

                case "/cb/":
                    return "Comes e bebes";

                case "/comp/":
                    return "Computaria em geral";

                case "/help/":
                    return "Sem tempo para dor.";

                case "/pol/":
                    return "Politica.";

                case "/UF55/":
                    return "Universidade Federal do 55chan";

                case "/sch/":
                    return "Scholar";

                case "/34/":
                    return "Pornografia 2d de tudo e todos.";

                case "/pr0n/":
                    return "*fapfapfap*";

                case "/pinto/":
                    return "Ai que delicia, cara";

                case "/tr/":
                    return "Pintos Femininos";

                case "/esp/":
                    return "Esportes.";

                case "/o/":
                    return "Ocultismo, religiões e outras crenças sem provas.";

                case "/high/":
                    return "Drogas, drogas e drogas.";

                case "/mimimi/":
                    return "ain bê que dor no coraçaum";

                case "/gtk/":
                    return "Creepypasta, mindfuck, aliens, e viadagens relacionadas.";

                case "/$/":
                    return "Dinheiro";

                case "/fit/":
                    return "Fitness";

                case "/pfiu/":
                    return "Automóveis";

                case "/vs/":
                    return "Versus";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://55ch.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://55ch.org/board.php";
            }
        }
    }
}
