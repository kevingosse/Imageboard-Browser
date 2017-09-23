using System;
using System.Globalization;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.AnonIb
{
    public class AnonIbBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://www.anonib.com{0}";

        protected const string CaptchaChallengeKey = "6LcmgsYSAAAAAKlgf5y5a_PP-KarRsFkMWei-9lc";

        public AnonIbBoardManager()
        {
            this.TopicParser = new AnonIbTopicParser();
            this.MessageParser = new AnonIbMessageParser();
        }

        public override string Name
        {
            get
            {
                return "anonib.com";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/sf/";
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

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
        }

        public override string GetCaptchaChallengeKey(bool isPostingNewTopic)
        {
            return CaptchaChallengeKey;
        }

        public override Uri BuildCatalogLink(Board board)
        {
            return new Uri(string.Format(BoardUriFormat, board.Name) + "catalog.html");
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Name = "name",
                Subject = "subject",
                Image = "imagefile",
                FileName = "filename",
                Password = "postpassword",
                TopicId = "replythread",
                Comment = "message",
                RecaptchaChallengeField = "recaptcha_challenge_field",
                RecaptchaResponseField = "recaptcha_response_field"
            };
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));

            parameters.Add("MAX_FILE_SIZE", "5120000");
            parameters.Add("email", string.Empty);
            parameters.Add("savePost", "Submit");
            parameters.Add("fileurl", string.Empty);

            if (newTopic)
            {
                parameters.Add("replythread", "0");
            }
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return "http://www.anonib.com/board.php";
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, "res/(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("<h1 style=\"font-size: 3em;\">Error</h1>"))
            {
                var match = Regex.Match(response, "<h2 style=\"font-size: 2em;font-weight: bold;text-align: center;\">(?<reason>.+?)</h2>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["reason"].Value);
                }
            }

            return PostResult.Ok();
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
                case "/1afan/":
                    return "Anonib Fansigns";

                case "/3d/":
                    return "3d Porn";

                case "/a/":
                    return "Amateurs";

                case "/aes/":
                    return "Amputee ES";

                case "/an/":
                    return "Accidental Nude";

                case "/anal/":
                    return "Anal";

                case "/ass/":
                    return "Ass";

                case "/azn/":
                    return "Azn Chikz";

                case "/b/":
                    return "/b/";

                case "/bdsm/":
                    return "BDSM";

                case "/bi/":
                    return "Bisexual";

                case "/bt/":
                    return "BIG TITS";

                case "/c/":
                    return "Celebs";

                case "/cb/":
                    return "College Bitches";

                case "/cf/":
                    return "Celeb Fakes";

                case "/ci/":
                    return "Creepy Pictures";

                case "/coe/":
                    return "Cum on Everything";

                case "/cof/":
                    return "Cum on Food";

                case "/cosp/":
                    return "Cosplay";

                case "/cuck/":
                    return "Cuckold";

                case "/cut/":
                    return "Amputee";

                case "/di/":
                    return "Diaper";

                case "/dr/":
                    return "Drunk/Passed out";

                case "/eb/":
                    return "Ebony";

                case "/et/":
                    return "Ethnic/Indian";

                case "/ex/":
                    return "ExGF";

                case "/fa/":
                    return "Fakes/Xray";

                case "/fba/":
                    return "Furry Bara";

                case "/fn/":
                    return "Fuck or not";

                case "/ft/":
                    return "Feet";

                case "/ftj/":
                    return "Face the Jury";

                case "/g/":
                    return "1GayChan";

                case "/g3+/":
                    return "Group";

                case "/ga/":
                    return "Gardevoir";

                case "/gfba/":
                    return "Gay Furry";

                case "/gif/":
                    return "Animated GIFs";

                case "/gin/":
                    return "Gingers/Redheads";

                case "/gwg/":
                    return "G.W.G.";

                case "/hb/":
                    return "Hentai Bestiality";

                case "/hy/":
                    return "Hairy";

                case "/hyp/":
                    return "Hypnosis/Mind";

                case "/ir/":
                    return "Interacial";

                case "/les/":
                    return "Lesbians";

                case "/lt/":
                    return "Latina";

                case "/mast/":
                    return "Masturbation";

                case "/mfnu/":
                    return "Movie/Film Nudes";

                case "/mgt/":
                    return "Midget";

                case "/mh/":
                    return "Monster hentai";

                case "/mil/":
                    return "Military";

                case "/milf/":
                    return "Milfs";

                case "/mod/":
                    return "Models";

                case "/mu/":
                    return "Muscle";

                case "/musfest/":
                    return "Music Festival";

                case "/orals/":
                    return "Oral Sex";

                case "/panty/":
                    return "Panty";

                case "/pb/":
                    return "PB";

                case "/pe/":
                    return "Pee girls";

                case "/pg/":
                    return "Paragirls";

                case "/pl/":
                    return "Plump";

                case "/preg/":
                    return "Pregnant";

                case "/pro/":
                    return "Adult models and porn stars";

                case "/rav/":
                    return "Raver Girls";

                case "/sb/":
                    return "SSBBW";

                case "/scv4/":
                    return "Scene v4";

                case "/sg/":
                    return "Sleeping girls";

                case "/sk/":
                    return "Skinny";

                case "/snoca/":
                    return "shit no one cares about";

                case "/soci/":
                    return "Social Networks Sites - Myspace/Facebook etc";

                case "/sq/":
                    return "Squirt";

                case "/stol/":
                    return "Obtained Pictures";

                case "/t/":
                    return "Teen (18+)";

                case "/tatt/":
                    return "Tattooed";

                case "/tblr/":
                    return "Tumbl.R";

                case "/toons/":
                    return "Toons";

                case "/tr/":
                    return "Traps";

                case "/uf/":
                    return "Uniform fetish";

                case "/v/":
                    return "Peeping Toms (No Fakes!)";

                case "/wc/":
                    return "Wincest";

                case "/we/":
                    return "Brides/Weddings";

                case "/whl/":
                    return "Wheelchair";

                case "/ygwbt/":
                    return "YGWBT";

                case "/ytb/":
                    return "Youtube Sluts";

                case "/ak/":
                    return "Alaska";

                case "/alb/":
                    return "Alabama";

                case "/ar/":
                    return "Arkansas";

                case "/az/":
                    return "Arizona";

                case "/brazil/":
                    return "Brazil";

                case "/cal/":
                    return "California";

                case "/co/":
                    return "Colorado";

                case "/ct/":
                    return "Connecticut";

                case "/dc/":
                    return "District of Columbia";

                case "/dw/":
                    return "Delaware";

                case "/fl/":
                    return "Florida";

                case "/grga/":
                    return "Georgia";

                case "/ha/":
                    return "Hawaii";

                case "/idaho/":
                    return "Idaho";

                case "/il/":
                    return "Illinois";

                case "/in/":
                    return "Indiana";

                case "/io/":
                    return "Iowa";

                case "/kan/":
                    return "Kentucky";

                case "/ks/":
                    return "Kansas";

                case "/lou/":
                    return "Louisiana";

                case "/ma/":
                    return "Maryland";

                case "/mass/":
                    return "Massachusetts/Capecod";

                case "/mi/":
                    return "Michigan";

                case "/miss/":
                    return "Mississippi";

                case "/mn/":
                    return "Maine";

                case "/mo/":
                    return "Missouri";

                case "/mont/":
                    return "Montana";

                case "/ms/":
                    return "Minnesota";

                case "/nc/":
                    return "North Carolina";

                case "/nd/":
                    return "North Dakota";

                case "/ne/":
                    return "Nebraska";

                case "/nh/":
                    return "New Hampshire";

                case "/nj/":
                    return "New Jersey";

                case "/nm/":
                    return "New Mexico";

                case "/nv/":
                    return "Nevada";

                case "/ny/":
                    return "New York";

                case "/oh/":
                    return "Ohio";

                case "/ok/":
                    return "Oklahoma";

                case "/or/":
                    return "Oregon";

                case "/pa/":
                    return "Pennsylvania";

                case "/ri/":
                    return "Rhode Island";

                case "/ric/":
                    return "Costa Rica";

                case "/sd/":
                    return "South Dakota";

                case "/socar/":
                    return "South Carolina";

                case "/tn/":
                    return "Tennessee";

                case "/tx/":
                    return "Texas";

                case "/ut/":
                    return "Utah";

                case "/va/":
                    return "Virginia";

                case "/vt/":
                    return "Vermont";

                case "/wa/":
                    return "Washington";

                case "/wi/":
                    return "Wisconsin";

                case "/wv/":
                    return "West Virginia";

                case "/wy/":
                    return "Wyoming";

                case "/al/":
                    return "Alberta";

                case "/bc/":
                    return "British Columbia";

                case "/mar/":
                    return "The Maritimes";

                case "/nt/":
                    return "The Northern Territories";

                case "/on/":
                    return "Ontario";

                case "/pr/":
                    return "The Prairies";

                case "/qbc/":
                    return "Quebec";

                case "/au/":
                    return "Aussie Sluts";

                case "/bel/":
                    return "België, Belgique, Belgien";

                case "/ch/":
                    return "Switzerland";

                case "/dk/":
                    return "Denmark";

                case "/exyu/":
                    return "EX-YU";

                case "/finl/":
                    return "Finland";

                case "/fr/":
                    return "France";

                case "/ger/":
                    return "Germany";

                case "/it/":
                    return "Italy";

                case "/nl/":
                    return "Netherlands";

                case "/nor/":
                    return "Norway";

                case "/plc/":
                    return "Poland";

                case "/roman/":
                    return "Romania";

                case "/russia/":
                    return "Russia";

                case "/sp/":
                    return "Spain";

                case "/sw/":
                    return "Sweden";

                case "/uk/":
                    return "UK";

                case "/btv/":
                    return "blogTV";

                case "/c4/":
                    return "Cam4";

                case "/cams/":
                    return "The Cam Board";

                case "/ce/":
                    return "Epic";

                case "/cr/":
                    return "Cam Requests";

                case "/sc/":
                    return "Stickam Girls and Camwhores";

                case "/ac/":
                    return "Advice and Confessions";

                case "/book/":
                    return "Bookchan";

                case "/cns/":
                    return "Conspiracy";

                case "/film/":
                    return "Film Discussion";

                case "/h/":
                    return "hikikomori";

                case "/o/":
                    return "420";

                case "/sf/":
                    return "SO FUNNY";

                case "/r/":
                    return "Requests";

                case "/sug/":
                    return "Suggestions";

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

            const string DateFormat = "dd/MM/yy(ddd)HH:mmK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "-05:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }
    }
}
