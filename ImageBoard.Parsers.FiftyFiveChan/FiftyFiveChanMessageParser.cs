using System;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.FiftyFiveChan
{
    public class FiftyFiveChanMessageParser : KusabaMessageParser
    {
        public FiftyFiveChanMessageParser(bool injectReferer)
            : base(injectReferer)
        {
        }

        protected override DateTime? ExtractPostTime(string message)
        {
            var match = Regex.Match(message, @"(?<date>[0-9][0-9]/[0-9][0-9]/[0-9][0-9]\(\w+\)[0-9][0-9]:[0-9][0-9])", RegexOptions.Singleline);

            return FiftyFiveChanBoardManager.ConvertDate(match.Groups["date"].Value);
        }
    }
}
