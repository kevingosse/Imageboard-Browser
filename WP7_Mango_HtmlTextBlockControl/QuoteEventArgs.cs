using System;

namespace WP7_Mango_HtmlTextBlockControl
{
    public class QuoteEventArgs : EventArgs
    {
        public QuoteEventArgs(string quoteId)
        {
            this.QuoteId = quoteId;
        }

        public string QuoteId { get; protected set; }
    }
}
