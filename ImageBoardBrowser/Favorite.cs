using System.ComponentModel;

using ImageBoard.Parsers.Common;

namespace ImageBoardBrowser
{
    public class Favorite : INotifyPropertyChanged
    {
        private HistoryEntry history;

        private bool isDead;

        private bool isLoading;

        private bool networkError;

        public event PropertyChangedEventHandler PropertyChanged;

        public string SiteName { get; set; }

        public Board Board { get; set; }

        public Topic Topic { get; set; }

        public bool IsDead
        {
            get
            {
                return this.isDead;
            }
            set
            {
                this.isDead = value;
                this.RaisePropertyChanged("IsDead");
            }
        }

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

        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }
            set
            {
                this.isLoading = value;
                this.RaisePropertyChanged("IsLoading");
            }
        }

        public bool NetworkError
        {
            get
            {
                return this.networkError;
            }
            set
            {
                this.networkError = value;
                this.RaisePropertyChanged("NetworkError");
            }
        }

        public void RefreshHistory()
        {
            this.RaisePropertyChanged("History");
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
