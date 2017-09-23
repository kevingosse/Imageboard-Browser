using System;
using System.Collections.Generic;

namespace ImageBoard.Parsers.Common
{
    public interface ITopicParser
    {
        IEnumerable<Topic> ParsePage(Context context, string content);
        int? ExtractPageCount(Context context, string html);

        IEnumerable<CatalogEntry> ParseCatalog(Context context, string content);
    }
}
