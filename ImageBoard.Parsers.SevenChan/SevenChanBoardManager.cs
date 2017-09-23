using System;
using System.Globalization;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.SevenChan
{
    public class SevenChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://7chan.org{0}";
        public const string PostAddress = "https://7chan.org/board.php";
        protected const string CaptchaChallengeKey = "6LdVg8YSAAAAAOhqx0eFT1Pi49fOavnYgy7e-lTO";

        public SevenChanBoardManager()
        {
            this.TopicParser = new SevenChanTopicParser();
            this.MessageParser = new SevenChanMessageParser();
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/eh/";
            }
        }

        public override string Name
        {
            get
            {
                return "7chan.org";
            }
        }

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
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

        public override string GetCaptchaChallengeKey(bool isPostingNewTopic)
        {
            return isPostingNewTopic ? CaptchaChallengeKey : null;
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, ".+?/(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Subject = "subject",
                Comment = "message",
                Name = "name",
                Image = "imagefile",
                FileName = "filename",
                TopicId = "replythread",
                Email = "em",
                Password = "postpassword",
                RecaptchaChallengeField = "recaptcha_challenge_field",
                RecaptchaResponseField = "recaptcha_response_field"
            };
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));

            if (newTopic)
            {
                parameters.Add("replythread", 0);
            }

            parameters.Add("MAX_FILE_SIZE", newTopic ? 5242880 : 7340032);
            parameters.Add("embed", string.Empty);
            parameters.Add("email", string.Empty);
            parameters.Add("embedtype", "google");
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

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return PostAddress;
        }

        public override PostResult IsPostOk(string response)
        {
            if (response == "<meta http-equiv=\"refresh\" content=\"1;url=http://7chan.org\">")
            {
                return PostResult.Error(null);
            }

            if (response.Contains("<h1 style=\"font-size: 3em;\">Error</h1>"))
            {
                if (response.Contains("Incorrect captcha entered"))
                {
                    return PostResult.Error("Incorrect captcha entered");
                }

                var match = Regex.Match(response, "<h2 style=\"font-size: 2em;font-weight: bold;text-align: center;\">(?<error>.+?)</h2>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["error"].Value.Trim(new[] { '\r', '\n' }));
                }

                return PostResult.Error(null);
            }

            return PostResult.Ok();
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
                case "/7ch/":
                    return "Site discussion";

                case "/ch7/":
                    return "Channel7";

                case "/irc/":
                    return "Internet Relay Circlejerk";

                case "/777/":
                    return "/space/ - The Final Frontier";

                case "/b/":
                    return "Random";

                case "/banner/":
                    return "Banners";

                case "/fl/":
                    return "Flash";

                case "/gfx/":
                    return "Graphics Manipulation";

                case "/fail/":
                    return "Failure";

                case "/class/":
                    return "The Finer Things";

                case "/co/":
                    return "Comics and Cartoons";

                case "/eh/":
                    return "Particularly uninteresting conversation";

                case "/fit/":
                    return "Fitness & Health";

                case "/jew/":
                    return "Poor People";

                case "/lit/":
                    return "Literature";

                case "/phi/":
                    return "Philosophy";

                case "/pr/":
                    return "Programming";

                case "/rnb/":
                    return "Rage and Baww";

                case "/sci/":
                    return "Science & Technology";

                case "/tg/":
                    return "Tabletop Games";

                case "/w/":
                    return "Weapons";

                case "/zom/":
                    return "Zombies";

                case "/a/":
                    return "Anime & Manga";

                case "/grim/":
                    return "Cold, Grim & Miserable";

                case "/hi/":
                    return "History and Culture";

                case "/me/":
                    return "Media";

                case "/rx/":
                    return "Drugs";

                case "/vg/":
                    return "Video Games";

                case "/wp/":
                    return "Wallpapers";

                case "/x/":
                    return "Paranormal & Conspiracy";

                case "/be/":
                    return "Bestiality";

                case "/cd/":
                    return "Crossdressing";

                case "/di/":
                    return "Sexy Beautiful Traps";

                case "/fag/":
                    return "Men Discussion";

                case "/gif":
                    return "Animated GIFs";

                case "/men":
                    return "Sexy Beautiful Men";

                case "/s":
                    return "Sexy Beautiful Women";

                case "/ss/":
                    return "Straight Shotacon";

                case "/v/":
                    return "The Vineyard";

                case "/cake/":
                    return "Delicious";

                case "/d/":
                    return "Alternative Hentai";

                case "/elit":
                    return "Erotic Literature";

                case "/fur/":
                    return "Furry";

                case "/pco/":
                    return "Porn Comics";

                case "/sm/":
                    return "Shotacon";

                case "/unf/":
                    return "Uniforms";

                case "/gif/":
                    return "Animated GIFs";

                default:
                    return null;
            }
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }
    }
}
