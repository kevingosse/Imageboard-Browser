using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.SeventySevenChan
{
    public class SeventySevenChanTopicParser : KusabaTopicParser
    {
        public SeventySevenChanTopicParser(string boardUriFormat, bool injectReferer)
            : base(boardUriFormat, injectReferer)
        {
        }

        protected override DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9]{2}/[0-9]{2}/[0-9]{4} [0-9]{2}:[0-9]{2})", RegexOptions.Singleline);

            return SeventySevenChanBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
