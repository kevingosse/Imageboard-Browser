using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ImageBoard.Parsers.Common
{
    public abstract class BoardManager
    {
        public ITopicParser TopicParser { get; protected set; }

        public IMessageParser MessageParser { get; protected set; }

        public abstract string Name { get; }

        public abstract string SampleBoardName { get; }

        public abstract bool IsRedirectionSupported { get; }

        public virtual bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public virtual bool IsCatalogSupported
        {
            get
            {
                return false;
            }
        }

        public static string ExtractLinks(string content)
        {
            var matches = Regex.Matches(
                content,
                "http(s*)://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?",
                RegexOptions.IgnoreCase);

            foreach (Match match in matches.OfType<Match>().OrderByDescending(m => m.Index))
            {
                content = content.Insert(match.Index + match.Value.Length, "</link>");
                content = content.Insert(match.Index, "<link>");
            }

            return content;
        }

        public static string FixLink(string link, bool https)
        {
            if (string.IsNullOrEmpty(link) || link.StartsWith("http://") || link.StartsWith("https://"))
            {
                return link;
            }

            return (https ? "https://" : "http://") + link.TrimStart('/');
        }

        public static string StripHtml(string content, bool keepEndOfLines)
        {
            if (keepEndOfLines)
            {
                content = content
                    .Replace("<br/>", "##ENDOFLINE##")
                    .Replace("<br>", "##ENDOFLINE##")
                    .Replace("<br />", "##ENDOFLINE##");
            }

            content = Regex.Replace(content, "<.*?>", string.Empty);

            if (keepEndOfLines)
            {
                content = content.Replace("##ENDOFLINE##", "<br/>");
            }

            return content;
        }

        public virtual Uri GetBaseUri()
        {
            return null;
        }

        public abstract MessageMapping GetMessageMapping();

        public abstract string GetPostUri(Board selectedBoard, bool newTopic);

        public abstract string ExtractTopicIdFromUri(string uri);

        public virtual void FillAdditionnalMessageFields(Dictionary<string, object> parameters, Context context, bool newTopic)
        {
        }

        public virtual List<KeyValuePair<Uri, Cookie>> GetCookies(Context context, bool newTopic)
        {
            return null;
        }

        public abstract PostResult IsPostOk(string response);

        public abstract Uri BuildPageLink(string board, int page, string noCacheSeed);

        public virtual Uri BuildCatalogLink(Board board)
        {
            return null;
        }

        public virtual string ExtractRedirection(Context context, string content)
        {
            return null;
        }

        public abstract string FillDescription(string boardName);

        public abstract Board CreateBoard(string name, string description);

        public virtual string GetCaptchaChallengeKey(bool isPostingNewTopic)
        {
            return null;
        }

        public virtual Uri GetBrowserTopicLink(Context context)
        {
            Uri uri;

            Uri.TryCreate(
                new Uri(context.Board.Uri),
                context.Topic.ReplyLink,
                out uri);

            return uri;
        }

        public virtual Uri GetBrowserPageLink(Context context, int currentPage)
        {
            return this.BuildPageLink(context.Board.Name, currentPage, null);
        }
    }
}
