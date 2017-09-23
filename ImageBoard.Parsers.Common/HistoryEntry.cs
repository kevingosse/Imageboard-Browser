using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace ImageBoard.Parsers.Common
{
    public class HistoryEntry : INotifyPropertyChanged
    {
        private int repliesCount;

        private int imagesCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public HistoryEntry(string boardName, string threadId)
        {
            this.BoardName = boardName;
            this.ThreadId = threadId;
        }

        public int RepliesCount
        {
            get
            {
                return this.repliesCount;
            }
            set
            {
                this.repliesCount = value;
                this.RaisePropertyChanged("RepliesCount");
                this.RaisePropertyChanged("NewRepliesCount");
            }
        }

        public int ImagesCount
        {
            get
            {
                return this.imagesCount;
            }
            set
            {
                this.imagesCount = value;
                this.RaisePropertyChanged("ImagesCount");
                this.RaisePropertyChanged("NewImagesCount");
            }
        }

        public int OldRepliesCount { get; set; }
        public int OldImagesCount { get; set; }

        public string ThreadId { get; set; }
        public string BoardName { get; set; }

        public bool Visited { get; set; }

        public int? NewRepliesCount
        {
            get
            {
                var count = this.RepliesCount - this.OldRepliesCount;

                if (count == 0)
                {
                    return null;
                }

                return count;
            }
        }

        public int? NewImagesCount
        {
            get
            {
                var count = this.ImagesCount - this.OldImagesCount;

                if (count == 0)
                {
                    return null;
                }

                return count;
            }
        }

        public static HistoryEntry FromXml(XElement node)
        {
            return new HistoryEntry((string)node.Attribute("boardName"), (string)node.Attribute("threadId"))
            {
                ImagesCount = XmlConvert.ToInt32((string)node.Attribute("imagesCount")),
                OldImagesCount = XmlConvert.ToInt32((string)node.Attribute("oldImagesCount")),
                RepliesCount = XmlConvert.ToInt32((string)node.Attribute("repliesCount")),
                OldRepliesCount = XmlConvert.ToInt32((string)node.Attribute("oldRepliesCount")),
                Visited = node.Attribute("visited") != null && XmlConvert.ToBoolean((string)node.Attribute("visited"))
            };
        }

        public XElement ToXml()
        {
            return new XElement("historyEntry",
                new XAttribute("threadId", this.ThreadId),
                new XAttribute("boardName", this.BoardName),
                new XAttribute("imagesCount", this.ImagesCount),
                new XAttribute("oldImagesCount", this.OldImagesCount),
                new XAttribute("repliesCount", this.RepliesCount),
                new XAttribute("oldRepliesCount", this.OldRepliesCount),
                new XAttribute("visited", XmlConvert.ToString(this.Visited)));
        }

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
