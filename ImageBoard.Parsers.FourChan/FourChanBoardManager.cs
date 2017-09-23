using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

using Newtonsoft.Json.Linq;

namespace ImageBoard.Parsers.FourChan
{
    public class FourChanBoardManager : BoardManager
    {
        internal const string PictureFormat = "https://images.4chan.org{0}src/{1}{2}";
        internal const string ThumbnailFormat = "https://thumbs.4chan.org{0}thumb/{1}s.jpg";

        protected const string BoardUriFormat = "https://boards.4chan.org{0}";

        protected const string PageUriFormat = "https://a.4cdn.org{0}{1}.json";

        protected const string CatalogUriFormat = "https://a.4cdn.org{0}catalog.json";
        
        protected const string ReplyMessageAddressFormat = "https://sys.4chan.org{0}post";
        protected const string NewTopicAddressFormat = "https://sys.4chan.org{0}post";
        protected const string CaptchaChallengeKey = "6Ldp2bsSAAAAAAJ5uyx_lx34lJeEpTLVkP5k04qc";

        public FourChanBoardManager()
        {
            this.TopicParser = new FourChanTopicParser();
            this.MessageParser = new FourChanMessageParser();
        }

        public override bool IsCatalogSupported
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
                return true;
            }
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
                return "/ck/";
            }
        }

        public override string Name
        {
            get
            {
                return "4chan.org";
            }
        }

        public static DateTime? ConvertDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
            {
                return null;
            }

            double seconds;

            if (!double.TryParse(rawDate, out seconds))
            {
                return null;
            }

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return epoch.AddSeconds(seconds).ToLocalTime();
        }

        public override string GetCaptchaChallengeKey(bool isPostingNewTopic)
        {
            return CaptchaChallengeKey;
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            var format = newTopic ? NewTopicAddressFormat : ReplyMessageAddressFormat;

            return string.Format(format, selectedBoard.Name);
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            return uri.Split('/').Last().Replace(".json", string.Empty);
        }

        public override Uri GetBaseUri()
        {
            return new Uri("https://www.4chan.org");
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Name = "name",
                Email = "email",
                Subject = "sub",
                Comment = "com",
                //RecaptchaChallengeField = "recaptcha_challenge_field",
                RecaptchaResponseField = "g-recaptcha-response",
                Password = "pwd",
                Image = "upfile",
                FileName = "filename",
                TopicId = "resto"
            };
        }

        public override void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("MAX_FILE_SIZE", "2097152");
            parameters.Add("mode", "regist");

            if (newTopic)
            {
                parameters.Add("com_submit", "com_submit");
            }
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(PageUriFormat, board, page + 1);

            if (noCacheSeed != null)
            {
                baseLink += "?nocache=" + noCacheSeed;
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
                case "/a/":
                    return "Anime & Manga";

                case "/c/":
                    return "Anime/Cute";

                case "/w/":
                    return "Anime/Wallpapers";

                case "/m/":
                    return "Mecha";

                case "/cgl/":
                    return "Cosplay & EGL";

                case "/cm/":
                    return "Cute/Male";

                case "/f/":
                    return "Flash";

                case "/n/":
                    return "Transportation";

                case "/jp/":
                    return "Otaku Culture";

                case "/vp/":
                    return "Pokémon";

                case "/v/":
                    return "Video Games";

                case "/vg/":
                    return "Video Game Generals";

                case "/co/":
                    return "Comics & Cartoons";

                case "/g/":
                    return "Technology";

                case "/tv/":
                    return "Television & Film";

                case "/k/":
                    return "Weapons";

                case "/o/":
                    return "Auto";

                case "/an/":
                    return "Animals & Nature";

                case "/tg/":
                    return "Traditional Games";

                case "/sp/":
                    return "Sports";

                case "/sci/":
                    return "Science & Math";

                case "/int/":
                    return "International";

                case "/i/":
                    return "Oekaki";

                case "/po/":
                    return "Papercraft & Origami";

                case "/p/":
                    return "Photography";

                case "/ck/":
                    return "Food & Cooking";

                case "/ic/":
                    return "Artwork/Critique";

                case "/wg/":
                    return "Wallpapers/General";

                case "/mu/":
                    return "Music";

                case "/fa/":
                    return "Fashion";

                case "/toy/":
                    return "Toys";

                case "/3/":
                    return "3DCG";

                case "/diy/":
                    return "Do-It-Yourself";

                case "/trv/":
                    return "Travel";

                case "/fit/":
                    return "Health & Fitness";

                case "/x/":
                    return "Paranormal";

                case "/lit/":
                    return "Literature";

                case "/s/":
                    return "Sexy Beautiful Women";

                case "/hc/":
                    return "Hardcore";

                case "/h/":
                    return "Hentai";

                case "/e/":
                    return "Ecchi";

                case "/u/":
                    return "Yuri";

                case "/d/":
                    return "Hentai/Alternative";

                case "/y/":
                    return "Yaoi";

                case "/t/":
                    return "Torrents";

                case "/hr/":
                    return "High Resolution";

                case "/gif/":
                    return "Animated GIF";

                case "/b/":
                    return "Random";

                case "/r/":
                    return "Request";

                case "/r9k/":
                    return "ROBOT9001";

                case "/pol/":
                    return "Politically Incorrect";

                case "/soc/":
                    return "Social";

                case "/mlp/":
                    return "Pony";

                case "/wsg/":
                    return "Worksafe GIF";

                default:
                    return null;
            }
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("Post successful"))
            {
                return PostResult.Ok();
            }

            if (response.Contains("mistyped"))
            {
                return PostResult.Error("You seem to have mistyped the verification");
            }

            return PostResult.Error(null);
        }

        public override string ExtractRedirection(Context context, string content)
        {
            var match = Regex.Match(content, "<!-- thread:[0-9]+,no:(?<id>[0-9]+) -->");

            return match.Success
                ? string.Format(FourChanTopicParser.ThreadLinkFormat, context.Board.Name, match.Groups["id"].Value) 
                : null;
        }

        public override Uri GetBrowserTopicLink(Context context)
        {
            return new Uri(string.Format("https://boards.4chan.org{0}res/{1}", context.Board.Name, context.Topic.Id));
        }

        public override Uri GetBrowserPageLink(Context context, int currentPage)
        {
            return new Uri(string.Format("https://boards.4chan.org{0}{1}", context.Board.Name, currentPage));
        }

        internal static string ExtractImageLink(Context context, JToken message)
        {
            var tim = (string)message["tim"];
            var ext = (string)message["ext"];

            if (tim == null || ext == null)
            {
                return null;
            }

            return string.Format(PictureFormat, context.Board.Name, tim, ext);
        }

        internal static string ExtractThumbImageLink(Context context, JToken thread)
        {
            var tim = (string)thread["tim"];

            if (tim == null)
            {
                return null;
            }

            return string.Format(ThumbnailFormat, context.Board.Name, tim);
        }

        internal static DateTime ExtractPostTime(JToken thread)
        {
            return new DateTime(1970, 1, 1).AddSeconds((int)thread["time"]);
        }

        internal static string ExtractCountryFlag(Context context, JToken message)
        {
            var flagCode = (string)message["country"];

            if (flagCode == null)
            {
                return null;
            }

            var format = context.Board.Name == "/pol/" ? "https://s.4cdn.org/image/country/troll/{0}.gif" : "https://s.4cdn.org/image/country/{0}.gif";

            return string.Format(format, flagCode.ToLower());
        }
    }
}
