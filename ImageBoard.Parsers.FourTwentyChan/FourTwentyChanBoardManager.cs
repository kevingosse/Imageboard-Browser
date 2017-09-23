using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.FourTwentyChan
{
    public class FourTwentyChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://boards.420chan.org{0}";
        public const string PostAddressFormat = "http://boards.420chan.org{0}taimaba.pl";

        public FourTwentyChanBoardManager()
        {
            this.TopicParser = new FourTwentyChanTopicParser();
            this.MessageParser = new FourTwentyChanMessageParser();
        }

        public override string Name
        {
            get
            {
                return "420chan.org";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/pss/";
            }
        }

        public override bool IsRedirectionSupported
        {
            get
            {
                return false;
            }
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                //Subject = "field3",
                Comment = "field4",
                Image = "file",
                //FileName = "filename",
                TopicId = "parent"
            };
        }

        public override void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));
            parameters.Add("task", "post");
            parameters.Add("name", string.Empty);
            parameters.Add("link", string.Empty);
            parameters.Add("password", "pI45ce3R");

            if (newTopic)
            {
                parameters.Add("com_submit", "com_submit");
            }
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return string.Format(PostAddressFormat, selectedBoard.Name);
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, ".+?/(?<id>[0-9]+).php");

            return match.Groups["id"].Value;
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(BoardUriFormat, board);

            if (page > 0)
            {
                baseLink += page + ".php";
            }

            if (noCacheSeed != null)
            {
                baseLink += "?nocache=" + noCacheSeed;
            }

            return new Uri(baseLink);
        }

        public override PostResult IsPostOk(string response)
        {
            throw new NotImplementedException();
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
                case "/weed/":
                    return "Cannabis";

                case "/hooch/":
                    return "Alcohol";

                case "/mdma/":
                    return "Ecstasy";

                case "/psy/":
                    return "Psychedelic";

                case "/stim/":
                    return "Stimulant";

                case "/dis/":
                    return "Dissociative";

                case "/opi/":
                    return "Opiate";

                case "/smoke/":
                    return "Tobacco";

                case "/benz/":
                    return "Benzodiazepine";

                case "/del/":
                    return "Deliriant";

                case "/other/":
                    return "Other Drugs";

                case "/jenk/":
                    return "Jenkem";

                case "/detox/":
                    return "Detox";

                case "/qq/":
                    return "Personnal Issues";

                case "/dr/":
                    return "Dreams";

                case "/ana/":
                    return "Fitness";

                case "/nom/":
                    return "Food & Munchies";

                case "/vroom/":
                    return "Travel & Transportation";

                case "/st/":
                    return "Style & Fashion";

                case "/nra/":
                    return "Weapons";

                case "/sd/":
                    return "Sexuality";

                case "/cd/":
                    return "Transgender";

                case "/art/":
                    return "Art & Oekaki";

                case "/sagan/":
                    return "Space & Astronomy";

                case "/math/":
                    return "Mathematics";

                case "/chem/":
                    return "Science & Chemistry";

                case "/his/":
                    return "History";

                case "/crops/":
                    return "Growing & Botany";

                case "/howto/":
                    return "Guides & Tutorials";

                case "/law/":
                    return "Law Discussion";

                case "/lit/":
                    return "Books & Literature";

                case "/med/":
                    return "Medicine & Health";

                case "/pss/":
                    return "Philosophy & Social Sciences";

                case "/pol/":
                    return "Politics";

                case "/tech/":
                    return "Computers & Technology";

                case "/prog/":
                    return "Programming";

                case "/1701/":
                    return "Star Trek";

                case "/sport/":
                    return "Sports";

                case "/2/":
                    return "World of Warcraft & MMO";

                case "/mtv/":
                    return "Movies & Television";

                case "/f/":
                    return "Flash";

                case "/m/":
                    return "Music & Production";

                case "/mma/":
                    return "Mixed Martial Arts";

                case "/616/":
                    return "Comics";

                case "/wooo/":
                    return "Pro Wrestling";

                case "/n/":
                    return "World News";

                case "/vg/":
                    return "Video Games";

                case "/po/":
                    return "Pokémon";

                case "/tg/":
                    return "Traditional Games";

                case "/420/":
                    return "420chan Discussion";

                case "/b/":
                    return "Random & High Stuff";

                case "/spooky/":
                    return "Paranormal";

                case "/dino/":
                    return "Dinosaurs";

                case "/ani/":
                    return "Animals";

                case "/nj/":
                    return "Netjester AI";

                case "/nc/":
                    return "Net Characters";

                case "/tinfoil/":
                    return "Conspiracy Theories";

                case "/w/":
                    return "Desktop Wallpapers";

                case "/ga/":
                    return "Adult (Gay)";

                case "/sa/":
                    return "Adult (Straight)";

                case "/h/":
                    return "Hentai";

                case "/wc/":
                    return "Wildcard (Futurism)*";

                case "/tesla/":
                    return "Engineering";

                case "/fo/":
                    return "Post-apocalyptic";

                case "/lang/":
                    return "World Languages";

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

            //  - Thu, 02 Aug 2012 11:56:45 EST
            const string DateFormat = " - ddd, dd MMM yyyy HH:mm:ssK";

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+02:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }
    }
}
