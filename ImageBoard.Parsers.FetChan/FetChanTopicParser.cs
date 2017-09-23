using System.Collections.Generic;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.FetChan
{
    public class FetChanTopicParser : KusabaTopicParser
    {
        public FetChanTopicParser(string boardUriFormat, bool injectReferer)
            : base(boardUriFormat, injectReferer)
        {
        }

        public override IEnumerable<Common.Topic> ParsePage(Common.Context context, string content)
        {
            if (content != null)
            {
                content += "</body>";
            }

            return base.ParsePage(context, content);
        }
    }
}
