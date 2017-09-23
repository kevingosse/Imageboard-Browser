using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ImageBoard.Parsers.Common
{
    public class Message : INotifyPropertyChanged
    {
        private bool isLastReadMessage;

        private List<Message> backLinks;

        private int previousMessagesCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Id { get; set; }

        public string PosterName { get; set; }

        public string PosterId { get; set; }

        public int PreviousMessagesCount
        {
            get
            {
                return this.previousMessagesCount;
            }
            set
            {
                this.previousMessagesCount = value;
                this.RaisePropertyChanged("PreviousMessagesCount");
            }
        }

        public DateTime? PostTime { get; set; }

        public string Content { get; set; }

        public string CountryFlag { get; set; }

        public string ImageLink { get; set; }

        public string ThumbImageLink { get; set; }

        public string Resto { get; set; }

        public string Referer { get; set; }

        public bool IsLastReadMessage
        {
            get
            {
                return this.isLastReadMessage;
            }
            set
            {
                this.isLastReadMessage = value;
                this.RaisePropertyChanged("IsLastReadMessage");
            }
        }

        public bool IsIconVisible
        {
            get
            {
                return !string.IsNullOrEmpty(this.ImageLink);
            }
        }

        public List<Message> BackLinks
        {
            get
            {
                return this.backLinks;
            }
            set
            {
                this.backLinks = value;
                this.RaisePropertyChanged("BackLinks");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Message))
            {
                return false;
            }

            return this.Id == ((Message)obj).Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
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
