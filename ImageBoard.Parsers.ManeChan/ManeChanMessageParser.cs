using System.Collections.Generic;
using System.Text.RegularExpressions;

using ImageBoard.Parsers.Common.Kusaba;

namespace ImageBoard.Parsers.ManeChan
{
    public class ManeChanMessageParser : KusabaMessageParser
    {
        public ManeChanMessageParser(bool injectReferer)
            : base(injectReferer)
        {
        }

        public override IEnumerable<Common.Message> ParsePage(Common.Context context, string content)
        {
            content = Regex.Replace(content, "<script type=\"text/template\" id=\"post-skeleton\">.*?</script>", string.Empty, RegexOptions.Singleline);

            return base.ParsePage(context, content);
        }
    }
}
