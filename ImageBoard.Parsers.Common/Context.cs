using System.ComponentModel;

namespace ImageBoard.Parsers.Common
{
    public class Context : INotifyPropertyChanged
    {
        private Board board;
        private Topic topic;
        private Message message;

        public event PropertyChangedEventHandler PropertyChanged;

        public BoardManager BoardManager { get; set; }

        public Board Board
        {
            get
            {
                return this.board;
            }

            set
            {
                this.board = value;
                this.RaisePropertyChanged("Board");
            }
        }

        public Topic Topic
        {
            get
            {
                return this.topic;
            }

            set
            {
                this.topic = value;
                this.RaisePropertyChanged("Topic");
            }
        }

        public Message Message
        {
            get
            {
                return this.message;
            }

            set
            {
                this.message = value;
                this.RaisePropertyChanged("Message");
            }
        }

        public Context Clone()
        {
            return new Context
            {
                Board = this.Board,
                BoardManager = this.BoardManager,
                Message = this.Message,
                Topic = this.Topic
            };
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;

            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
