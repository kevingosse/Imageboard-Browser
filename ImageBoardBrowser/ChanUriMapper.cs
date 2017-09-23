using System;
using System.Windows;
using System.Windows.Navigation;

namespace ImageBoardBrowser
{
    public class ChanUriMapper : UriMapperBase
    {
        public const string StartPage = "SplashScreen";

        public override Uri MapUri(Uri uri)
        {
            try
            {
                var tempUri = System.Net.HttpUtility.UrlDecode(uri.ToString());

                if (tempUri.StartsWith("/Protocol?encodedLaunchUri="))
                {
                    return new Uri("/" + StartPage + ".xaml", UriKind.Relative);
                }

                if ((tempUri.Contains("RichMediaEdit")) && (tempUri.Contains("token")))
                {
                    // Redirect to RichMediaPage.xaml.
                    var mappedUri = tempUri.Replace(StartPage, "ViewGif");
                    return new Uri(mappedUri, UriKind.Relative);
                }

                if (!tempUri.Contains("SplashScreen.xaml"))
                {
                    var app = (App)Application.Current;

                    if (!app.IsInitialized)
                    {
                        ((App)Application.Current).Init();
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }

            return uri;
        }
    }
}
