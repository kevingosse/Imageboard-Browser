using System;
using System.ComponentModel;

namespace ImageBoard.Parsers.Common
{
    public class CatalogEntry : INotifyPropertyChanged
    {
        private HistoryEntry history;

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime Date { get; set; }

        public int RepliesCount { get; set; }

        public int? ImagesCount { get; set; }

        public string Id { get; set; }
        
        public string ReplyLink { get; set; }

        public string Author { get; set; }

        public string ThumbImageLink { get; set; }
        
        public int? ImageWidth { get; set; }

        public int? ImageHeight { get; set; }

        public string Subject { get; set; }

        public string Description { get; set; }

        public HistoryEntry History
        {
            get
            {
                return this.history;
            }
            set
            {
                this.history = value;
                this.RaisePropertyChanged("History");
            }
        }

        public void RefreshHistory()
        {
            this.RaisePropertyChanged("History");
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
