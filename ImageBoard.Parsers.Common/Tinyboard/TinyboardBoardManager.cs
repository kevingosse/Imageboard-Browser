using System;

namespace ImageBoard.Parsers.Common.Tinyboard
{
    public abstract class TinyboardBoardManager : BoardManager
    {
        public abstract string BoardUriFormat { get; }

        public abstract string PostAddress { get; }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public override Uri BuildPageLink(string board, int page, string noCacheSeed)
        {
            var baseLink = string.Format(BoardUriFormat, board);

            if (page > 0)
            {
                baseLink += (page + 1) + ".html";
            }

            if (noCacheSeed != null)
            {
                baseLink += "?nocache=" + noCacheSeed;
            }

            return new Uri(baseLink);
        }

        public override Board CreateBoard(string name, string description)
        {
            return new Board(name, description, string.Format(BoardUriFormat, name));
        }

        public override Uri BuildCatalogLink(Board board)
        {
            return new Uri(string.Format(BoardUriFormat, board.Name) + "catalog.html");
        }

        public static DateTime? ConvertDate(string value)
        {
            DateTime result;

            if (DateTime.TryParse(value, out result))
            {
                return result;
            }

            return null;
        }
    }
}
