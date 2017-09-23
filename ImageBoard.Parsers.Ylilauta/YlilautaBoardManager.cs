using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.Ylilauta
{
    public class YlilautaBoardManager : BoardManager
    {
        public const string BoardUriFormat = "https://ylilauta.org{0}";
        public const string CatalogUriFormat = "https://ylilauta.org{0}threadlist";
        public const string PostAddress = "https://ylilauta.org/post";

        public YlilautaBoardManager()
        {
            this.TopicParser = new YlilautaTopicParser();
            this.MessageParser = new YlilautaMessageParser();
        }

        public override string Name
        {
            get
            {
                return "ylilauta.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/ruokajajuoma/";
            }
        }

        public override bool IsReadOnly
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

        public override bool IsCatalogSupported
        {
            get
            {
                return true;
            }
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Subject = "subject",
                Comment = "msg",
                Name = "postername",
                Image = "file",
                FileName = "filename",
                TopicId = "thread",
                Email = "email",
                Password = "postpassword",
                RecaptchaChallengeField = "recaptcha_challenge_field",
                RecaptchaResponseField = "recaptcha_response_field"
            };
        }

        public override void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));

            if (newTopic)
            {
                parameters.Add("anticaptcha", context.Board.AdditionalFields["anticaptcha"]);
                parameters.Add("uuid", context.Board.AdditionalFields["uuid"]);
            }
            else
            {
                parameters.Add("anticaptcha", context.Topic.AdditionalFields["anticaptcha"]);
                parameters.Add("uuid", context.Topic.AdditionalFields["uuid"]);
            }
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return PostAddress;
        }

        public override List<KeyValuePair<Uri, Cookie>> GetCookies(Context context, bool newTopic)
        {
            string uuid = newTopic ? context.Board.AdditionalFields["uuid"] : context.Topic.AdditionalFields["uuid"];

            return new List<KeyValuePair<Uri, Cookie>>
            {
                new KeyValuePair<Uri, Cookie>(new Uri("https://ylilauta.org"), new Cookie("uuid", uuid))
            };
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, ".+?/(?<id>[0-9]+)$");

            return match.Groups["id"].Value;
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("<h1>An error occurred</h1>"))
            {
                var match = Regex.Match(response, "<h2>(?<error>.+?)</h2>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["error"].Value.Trim(new[] { '\r', '\n' }));
                }

                return PostResult.Error(null);
            }

            return PostResult.Ok();
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(BoardUriFormat, board);

            if (page > 0)
            {
                baseLink = baseLink.Insert(baseLink.Length - 1, "-" + page);
            }

            if (noCacheSeed != null)
            {
                baseLink += "#nocache=" + noCacheSeed;
            }

            return new Uri(baseLink);
        }

        public override Uri BuildCatalogLink(Board board)
        {
            return new Uri(string.Format(CatalogUriFormat, board.Name));
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
                case "/ajoneuvot/":
                    return "Ajoneuvot";

                case "/arki/":
                    return "Arkipäiväinen";

                case "/arkisto/":
                    return "Arkisto";

                case "/assembly/":
                    return "Assembly Summer 2014";

                case "/deitti/":
                    return "Deitti";

                case "/jorma/":
                    return "Eräjorma";

                case "/erotiikka/":
                    return "Erotiikka";

                case "/ansat/":
                    return "Erotiikka / Ansat";

                case "/homostelu/":
                    return "Erotiikka / Homostelu";

                case "/rule34/":
                    return "Erotiikka / Rule34";

                case "/fobba/":
                    return "Fobban kirjanurkka";

                case "/harrastukset/":
                    return "Harrastukset ja liikunta";

                case "/hiekkalaatikko/":
                    return "Hiekkalaatikko";

                case "/hikky/":
                    return "Hikikomero";

                case "/ihmissuhteet/":
                    return "Ihmissuhteet";

                case "/int/":
                    return "International";

                case "/anime/":
                    return "Japanijutut";

                case "/seksuaalisuus/":
                    return "Keho ja seksuaalisuus";

                case "/kirjallisuus/":
                    return "Kirjallisuus, lehdet ja sarjakuvat";

                case "/opiskelu/":
                    return "Koulu ja opiskelu";

                case "/kuntosali/":
                    return "Kuntosali ja kehonrakennus";

                case "/luonto/":
                    return "Luonto ja eläimet";

                case "/masiinat/":
                    return "Masiinat";

                case "/matkustus/":
                    return "Matkustaminen";

                case "/muoti/":
                    return "Muoti ja pukeutuminen";

                case "/musiikki/":
                    return "Musiikki";

                case "/poni/":
                    return "My Little Pony";

                case "/ohjelmistot/":
                    return "Ohjelmistot ja ohjelmointi";

                case "/palaute/":
                    return "Palaute ja kehitysideat";

                case "/paranormaali/":
                    return "Paranormaali ja avaruuskulttuuri";

                case "/pelit/":
                    return "Pelit";

                case "/penkkiurheilu/":
                    return "Penkkiurheilu";

                case "/politiikka/":
                    return "Politiikka";

                case "/ruinaus/":
                    return "Ruinaus";

                case "/ruokajajuoma/":
                    return "Ruoka ja juoma";

                case "/televisio/":
                    return "Sarjat ja elokuvat";

                case "/satunnainen/":
                    return "Satunnainen";

                case "/sota/":
                    return "Sota ja armeija";

                case "/taide/":
                    return "Taide";

                case "/talous/":
                    return "Talous ja raha";

                case "/diy/":
                    return "Tee se itse";

                case "/terveys/":
                    return "Terveys";

                case "/tiede/":
                    return "Tiede, historia ja filosofia";

                case "/uskonnot/":
                    return "Uskonnot";

                case "/valokuvaus/":
                    return "Valo- ja videokuvaus";

                case "/nykto/":
                    return "Yölauta";

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
            const string DateFormat = "dd.MM.yyyy HH:mm:ssK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+02:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }
    }
}
