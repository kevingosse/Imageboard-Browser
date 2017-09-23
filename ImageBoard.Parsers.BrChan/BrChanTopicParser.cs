using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.BrChan
{
    public class BrChanTopicParser : KusabaTopicParser
    {
        public BrChanTopicParser(string boardUriFormat, bool injectReferer)
            : base(boardUriFormat, injectReferer)
        {
        }

        protected override DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9]{2}/[0-9]{2}/[0-9]{4} \(\w+\) as [0-9]{2}:[0-9]{2})", RegexOptions.Singleline);

            return BrChanBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
