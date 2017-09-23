using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.SeventySevenChan
{
    public class SeventySevenChanMessageParser : KusabaMessageParser
    {
        public SeventySevenChanMessageParser(bool injectReferer)
            : base(injectReferer)
        {
        }

        protected override DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9]{2}/[0-9]{2}/[0-9]{4}\(\w+\)[0-9][0-9]:[0-9][0-9])", RegexOptions.Singleline);

            return SeventySevenChanBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
