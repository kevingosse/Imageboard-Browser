using System;

namespace WP7_Mango_HtmlTextBlockControl
{
    public class LinkEventArgs : EventArgs
    {
        public LinkEventArgs(string link)
        {
            this.Link = link;
        }

        public string Link { get; protected set; }
    }
}
