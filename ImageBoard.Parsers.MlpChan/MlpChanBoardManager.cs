using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common;
using ImageBoard.Parsers.Common.Tinyboard;

namespace ImageBoard.Parsers.MlpChan
{
    public sealed class MlpChanBoardManager : TinyboardBoardManager
    {
        public MlpChanBoardManager()
        {
            this.TopicParser = new TinyboardTopicParser(this.BoardUriFormat);
            this.MessageParser = new TinyboardMessageParser(this.BoardUriFormat);
        }

        public override string Name
        {
            get
            {
                return "mlpchan.net";
            }
        }

        public override string SampleBoardName
        {
            get
            {
                return "/pony/";
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

        public override MessageMapping GetMessageMapping()
        {
            return new MessageMapping
            {
                Email = "email",
                Name = "name",
                Subject = "subject",
                Image = "file",
                FileName = "filename",
                Password = "password",
                TopicId = "thread",
                Comment = "body"
            };
        }

        public override string GetPostUri(Board selectedBoard, bool newTopic)
        {
            return "https://mlpchan.net/post.php";
        }

        public override string ExtractTopicIdFromUri(string uri)
        {
            var match = Regex.Match(uri, "res/(?<id>[0-9]+).html");

            return match.Groups["id"].Value;
        }

        public override void FillAdditionnalMessageFields(System.Collections.Generic.Dictionary<string, object> parameters, Context context, bool newTopic)
        {
            base.FillAdditionnalMessageFields(parameters, context, newTopic);

            parameters.Add("board", context.Board.Name.Trim('/'));

            if (newTopic)
            {
                parameters.Add("post", "New Reply");
            }

            parameters.Add("q", context.Topic.AdditionalFields["q"]);
            parameters.Add("hash", context.Topic.AdditionalFields["hash"]);
            parameters.Add("url", context.Topic.AdditionalFields["url"]);
            parameters.Add("username", context.Topic.AdditionalFields["username"]);
            parameters.Add("lastname", context.Topic.AdditionalFields["lastname"]);
            parameters.Add("firstname", context.Topic.AdditionalFields["firstname"]);
        }

        public override PostResult IsPostOk(string response)
        {
            if (response.Contains("<h1>Error</h1>"))
            {
                var match = Regex.Match(response, "<div class=\"subtitle\">(?<reason>.+?)</div>", RegexOptions.Singleline);

                if (match.Success)
                {
                    return PostResult.Error(match.Groups["reason"].Value);
                }
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
                case "/site/":
                    return "Site Issues";

                case "/arch/":
                    return "The Library";

                case "/pony/":
                    return "The Show";

                case "/oat/":
                    return "Off Topic";

                case "/anon/":
                    return "Anything Goes";

                case "/fic/":
                    return "Fanfiction";

                case "/rp/":
                    return "Roleplaying";

                case "/art/":
                    return "Art & Fanwork";

                default:
                    return null;
            }
        }

        public override string BoardUriFormat
        {
            get
            {
                return "https://mlpchan.net{0}";
            }
        }

        public override string PostAddress
        {
            get
            {
                return "https://mlpchan.net/post.php";
            }
        }

        public override bool IsCatalogSupported
        {
            get
            {
                return true;
            }
        }
    }
}
