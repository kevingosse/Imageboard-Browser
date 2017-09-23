namespace ImageBoard.Parsers.Common
{
    public class PostResult
    {
        public bool IsOk { get; set; }

        public string ErrorMessage { get; set; }

        public static PostResult Ok()
        {
            return new PostResult { IsOk = true };
        }

        public static PostResult Error(string message)
        {
            return new PostResult { IsOk = false, ErrorMessage = message };
        }
    }
}
