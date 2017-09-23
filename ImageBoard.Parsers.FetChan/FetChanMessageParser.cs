using System.Collections.Generic;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.FetChan
{
    public class FetChanMessageParser : KusabaMessageParser
    {
        public FetChanMessageParser(bool injectReferer)
            : base(injectReferer)
        {
        }

        public override IEnumerable<Common.Message> ParsePage(Common.Context context, string content)
        {
            if (content != null)
            {
                content += "</body>";
            }

            return base.ParsePage(context, content);
        }
    }
}
