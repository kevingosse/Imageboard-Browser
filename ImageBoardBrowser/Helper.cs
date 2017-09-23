using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;

using Microsoft.Phone.Tasks;

namespace ImageBoardBrowser
{
    public static class Helper
    {
        private static Tuple<Uri, List<Cookie>> Cookies { get; set; }

        public static void OpenWebBrowser(Uri link)
        {
            try
            {
                new WebBrowserTask { Uri = link }.Show();
            }
            catch (Exception)
            {
            }
        }

        public static void ShowMessageBox(string message)
        {
            try
            {
                MessageBox.Show(message);
            }
            catch (Exception)
            {
            }
        }

        public static void SetCookies(CookieContainer container, Uri baseUri)
        {
            Cookies = baseUri == null ? null : Tuple.Create(baseUri, container.GetCookies(baseUri).OfType<Cookie>().ToList());
        }

        public static CookieContainer GetCookieContainer()
        {
            var container = new CookieContainer();

            var cookies = Cookies;

            if (cookies != null)
            {
                foreach (var cookie in cookies.Item2)
                {
                    container.Add(cookies.Item1, cookie);
                }
            }

            return container;
        }

        public static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();

            return CreateHttpClient(handler);
        }

        public static HttpClient CreateHttpClient(HttpClientHandler handler)
        {
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip;
            }

            return new HttpClient(handler, true);
        }
    }
}
