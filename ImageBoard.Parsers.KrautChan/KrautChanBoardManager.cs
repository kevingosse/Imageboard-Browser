using System;
using System.Globalization;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;

namespace ImageBoard.Parsers.KrautChan
{
    public class KrautChanBoardManager : BoardManager
    {
        public const string BoardUriFormat = "http://krautchan.net{0}";

        public KrautChanBoardManager()
        {
            this.TopicParser = new KrautChanTopicParser();
            this.MessageParser = new KrautChanMessageParser();
        }

        public override string SampleBoardName
        {
            get
            {
                return "/int/";
            }
        }

        public override string Name
        {
            get
            {
                return "krautchan.net";
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

            // 2012-06-15 00:28:49.297672
            const string DateFormat = "yyyy-MM-dd HH:mm:ssK";

            rawDate = rawDate.Substring(0, rawDate.IndexOf('.'));

            DateTime date;

            if (DateTime.TryParseExact(rawDate + "+02:00", DateFormat, new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, "http://krautchan.net/\\w+?/thread-(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Subject = "internal_s",
                Comment = "internal_t",
                FileName = "filename",
                Image = "file_0",
                Password = "password",
                TopicId = "parent"
            };
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            parameters.Add("board", context.Board.Name.Trim('/'));
            parameters.Add("postform", "postform");
            parameters.Add("email", string.Empty);
            parameters.Add("e-mail", string.Empty);
            parameters.Add("subject", string.Empty);
            parameters.Add("name", string.Empty);
            parameters.Add("comment", string.Empty);
            parameters.Add("text", string.Empty);
            parameters.Add("captcha_name", string.Empty);
            parameters.Add("captcha_secret", string.Empty);
            parameters.Add("forward", "thread");
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
            return "http://krautchan.net/post";
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
                case "/b/":
                    return "Der Prozess";

                case "/int/":
                    return "International";

                case "/vip/":
                    return "Beste der Besten";

                case "/a/":
                    return "Anime & Manga";

                case "/c/":
                    return "Computer";

                case "/co/":
                    return "Comics & Cartoons";

                case "/d/":
                    return "Drogen";

                case "/e/":
                    return "Essen & Trinken";

                case "/f/":
                    return "Fahrzeuge";

                case "/fb/":
                    return "Frag Bernd";

                case "/fit/":
                    return "Fitness";

                case "/jp/":
                    return "Otakuhimmel";

                case "/k/":
                    return "Kreatives";

                case "/l/":
                    return "Literatur";

                case "/li/":
                    return "Lifestyle";

                case "/m/":
                    return "Musik";

                case "/n/":
                    return "Natur & Tierwelt";

                case "/p/":
                    return "Politik & News";

                case "/ph/":
                    return "Philosophie";

                case "/sp/":
                    return "Spielzeug";

                case "/t/":
                    return "Technik";

                case "/tv/":
                    return "Film & Fernsehen";

                case "/v/":
                    return "Videospiele";

                case "/w/":
                    return "Wissenschaft";

                case "/we/":
                    return "Weltschmerz";

                case "/wp/":
                    return "Wallpaper";

                case "/x/":
                    return "Paranormales";

                case "/z/":
                    return "Zeichnen";

                case "/zp/":
                    return "MS Paint";

                case "/ng/":
                    return "Geld & Finanz";

                case "/prog/":
                    return "/prog/";

                case "/wk/":
                    return "Waffen & Krieg";

                case "/h/":
                    return "Hentai";

                case "/s/":
                    return "Sexy Frauen";

                case "/kc/":
                    return "Krautchan";

                case "/rfk/":
                    return "Radio Freies Krautchan";

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
