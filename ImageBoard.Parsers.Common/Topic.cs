using System;
using System.Collections.Generic;

namespace ImageBoard.Parsers.Common
{
    public class Topic
    {
        public Topic()
        {
            this.AdditionalFields = new Dictionary<string, string>();
        }

        public string Id { get; set; }

        public string Content { get; set; }

        public string ImageLink { get; set; }

        public string ThumbImageLink { get; set; }

        public string Referer { get; set; }

        public string PosterName { get; set; }

        public string CountryFlag { get; set; }

        public DateTime? PostTime { get; set; }

        public string ReplyLink { get; set; }

        public int? NumberOfReplies { get; set; }

        public string Subject { get; set; }

        public Dictionary<string, string> AdditionalFields { get; set; }
    }
}
