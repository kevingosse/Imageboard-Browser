using System.Collections.Generic;

namespace ImageBoard.Parsers.Common
{
    public interface IMessageParser
    {
        IEnumerable<Message> ParsePage(Context context, string content);
    }
}
