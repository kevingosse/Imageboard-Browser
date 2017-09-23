using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.BrChan
{
    public sealed class BrChanBoardManager : KusabaBoardManager
    {
        public BrChanBoardManager()
        {
            this.TopicParser = new BrChanTopicParser(this.BoardUriFormat, false);
            this.MessageParser = new BrChanMessageParser(false);
        }

        public override string Name
        {
            get
            {
                return "brchan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/mod/";
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
                case "/b/":
                    return "Random";

                case "/bairro/":
                    return "Bairrismo";

                case "/d/":
                    return "Discussões Aleatórias";

                case "/mod/":
                    return "Moderação";

                case "/jo/":
                    return "Jogos";

                case "/jp/":
                    return "Japão e Cultura Otaku";

                case "/bs/":
                    return "Brainstorming";

                case "/cbm/":
                    return "Companhia Brasileira de Mineração";

                case "/cine/":
                    return "Sétima Arte";

                case "/cmx/":
                    return "Comix Zone";

                case "/efm/":
                    return "Educação Física e Mental";

                case "/est/":
                    return "Estudos";

                case "/fut/":
                    return "Esportes";

                case "/lit/":
                    return "Literatura";

                case "/mu/":
                    return "Música";

                case "/prog/":
                    return "Programação";

                case "/sagan/":
                    return "Pale blue dot";

                case "/tech/":
                    return "Software, Hardware e Tecnologias";

                case "/tv/":
                    return "Televisão e Seriados";

                case "/ve/":
                    return "Veículos";

                case "/x/":
                    return "Sobrenatural";

                case "/g/":
                    return "Gay Porn";

                case "/p/":
                    return "Porn";

                case "/tr/":
                    return "Traps";

                case "/int/":
                    return "International";

                case "/proj/":
                    return "Projetos";

                case "/temp/":
                    return "Assuntos Temporários (ENEM)";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "http://www.brchan.org{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "http://www.brchan.org/forum.php";
            }
        }

        public static new DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }
            const string DateFormat = "dd/MM/yyyy (ddd) \\a\\s HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+02:00", DateFormat, new CultureInfo("pt-BR"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        public override List<KeyValuePair<Uri, Cookie>> GetCookies(Common.Context context, bool newTopic)
        {
            return new List<KeyValuePair<Uri, Cookie>>
            {
                new KeyValuePair<Uri, Cookie>(new Uri("http://www.brchan.org"), new Cookie("brchanrules", "1")),
                new KeyValuePair<Uri, Cookie>(new Uri("http://www.brchan.org"), new Cookie("postpassword", "oOCsbOUl")),
                new KeyValuePair<Uri, Cookie>(new Uri("http://www.brchan.org"), new Cookie("PHPSESSID", "qe7ptu727b3f6892eqq6g5bv6")),
                new KeyValuePair<Uri, Cookie>(new Uri("http://www.brchan.org"), new Cookie("__cfduid", "d0f34283cb47871bfd972cd199a5f02531366056498")),
                new KeyValuePair<Uri, Cookie>(new Uri("http://www.brchan.org"), new Cookie("testSessionCookie", "Enabled"))
            };
        }
    }
}
