using System.Collections.Generic;

namespace ImageBoard.Parsers.Common
{
    public class Board
    {
        public Board(string name, string description, string uri)
        {
            this.Name = name;
            this.Uri = uri;
            this.Description = description;
            this.AdditionalFields = new Dictionary<string, string>();
        }

        public string Name { get; set; }

        public string Uri { get; private set; }

        public string Description { get; set; }

        public Dictionary<string, string> AdditionalFields { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Board)obj);
        }

        public override int GetHashCode()
        {
            int hashCode = (this.Name != null ? this.Name.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (this.Uri != null ? this.Uri.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (this.Description != null ? this.Description.GetHashCode() : 0);
            return hashCode;
        }

        protected bool Equals(Board other)
        {
            return string.Equals(this.Name, other.Name) && string.Equals(this.Uri, other.Uri) && string.Equals(this.Description, other.Description);
        }
    }
}
