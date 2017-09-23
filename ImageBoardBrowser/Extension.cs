using System;
using System.Windows.Navigation;

namespace ImageBoardBrowser
{
    public static class Extension
    {
        public static void Navigate(this NavigationService navigationService, string pageName)
        {
            try
            {
                navigationService.Navigate(new Uri(pageName, UriKind.Relative));
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
        }
    }
}
