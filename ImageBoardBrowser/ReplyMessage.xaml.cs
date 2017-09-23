using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using ImageBoard.Parsers.Common;
using ImageBoard.Parsers.FourChan;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Windows.Media;

namespace ImageBoardBrowser
{
    public partial class ReplyMessage
    {
        public ReplyMessage()
        {
            this.InitializeComponent();
        }

        protected ApplicationBarIconButton SendButton
        {
            get
            {
                return (ApplicationBarIconButton)this.ApplicationBar.Buttons[0];
            }
        }

        protected Dictionary<string, object> Parameters { get; set; }

        protected byte[] ImageBytes { get; set; }

        protected string CaptchaChallenge { get; set; }

        protected string Boundary { get; set; }

        protected bool IsPostingNewTopic { get; set; }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (this.PopupCaptcha.IsOpen || this.PopupCaptcha2.IsOpen)
            {
                this.PopupCaptcha.IsOpen = false;
                this.PopupCaptcha2.IsOpen = false;
                this.SendButton.IsEnabled = true;
                e.Cancel = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.ViewModel.Context.BoardManager.IsReadOnly)
            {
                Helper.ShowMessageBox("Posting new messages on this board isn't supported yet. Sorry about that, we're working on it!");
                this.NavigationService.GoBack();
                return;
            }

            if (this.NavigationContext.QueryString.ContainsKey("type"))
            {
                string type = this.NavigationContext.QueryString["type"];

                this.IsPostingNewTopic = type == "topic";
            }
            else
            {
                this.IsPostingNewTopic = false;
            }

            if (this.IsPostingNewTopic && !this.ViewModel.Context.BoardManager.IsRedirectionSupported)
            {
                this.CheckBoxFavorite.IsChecked = false;
                this.CheckBoxFavorite.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.CheckBoxFavorite.Visibility = Visibility.Visible;

                if (e.NavigationMode != NavigationMode.Back)
                {
                    this.CheckBoxFavorite.IsChecked = this.ViewModel.AddReplyToFavorites;
                }   
            }

            this.TextTitle.Text = this.IsPostingNewTopic ? "NEW THREAD" : "REPLY";

            if (this.NavigationContext.QueryString.ContainsKey("id") && string.IsNullOrEmpty(App.ViewModel.ReplyComment))
            {
                string id = this.NavigationContext.QueryString["id"];

                App.ViewModel.ReplyComment = ">>" + id + Environment.NewLine;
            }
        }

        private static byte[] WriteMultipart(Dictionary<string, object> parameters, string boundary)
        {
            var encoding = Encoding.UTF8;

            using (var stream = new MemoryStream())
            {
                foreach (var kvp in parameters)
                {
                    var data = kvp.Value as byte[];

                    if (data != null)
                    {
                        string part = "--" + boundary + "\r\n";
                        part += "Content-Disposition: form-data; name=\"" + kvp.Key + "\"; filename=\"1.jpg\";\r\n";
                        part += "Content-Type: application/octet-stream\r\n\r\n";

                        Debug.WriteLine(part);

                        stream.Write(encoding.GetBytes(part), 0, part.Length);

                        stream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        string part = "--" + boundary + "\r\n";
                        part += "Content-Disposition: form-data; name=\"" + kvp.Key + "\"\r\n\r\n";
                        part += kvp.Value + "\r\n";

                        Debug.WriteLine(part);

                        stream.Write(encoding.GetBytes(part), 0, part.Length);
                    }
                }

                string end = "\r\n--" + boundary + "--\r\n";

                Debug.WriteLine(end);

                stream.Write(encoding.GetBytes(end), 0, end.Length);

                return stream.ToArray();
            }
        }

        private void ButtonSubmitClick(object sender, EventArgs e)
        {
            this.SendButton.IsEnabled = false;
            this.Focus();

            this.Dispatcher.BeginInvoke(this.Submit);
        }

        private async void Submit()
        {
            if (string.IsNullOrEmpty(App.ViewModel.ReplyComment))
            {
                Helper.ShowMessageBox("You have to type a comment");
                this.SendButton.IsEnabled = true;
                return;
            }

            string captchaChallengeKey = App.ViewModel.Context.BoardManager.GetCaptchaChallengeKey(this.IsPostingNewTopic);

            if (captchaChallengeKey == null)
            {
                this.Upload(null);
            }
            else
            {
                this.TextBoxCaptcha.Text = string.Empty;
                this.PanelCaptcha.Visibility = Visibility.Collapsed;
                this.PanelLoadingCaptcha.Visibility = Visibility.Visible;

                if (this.ViewModel.Context.BoardManager is FourChanBoardManager)
                {
                    this.PopupCaptcha2.IsOpen = true;
                    this.WebBrowser.Visibility = Visibility.Collapsed;

                    this.WebBrowser.Navigate(new Uri("https://boards.4chan.org/a/"));
                }
                else
                {
                    var handler = new HttpClientHandler();

                    using (var webClient = Helper.CreateHttpClient(handler))
                    {
                        handler.CookieContainer = Helper.GetCookieContainer();
                        webClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; Microsoft; Virtual)");

                        var baseUri = this.ViewModel.Context.BoardManager.GetBaseUri();

                        if (baseUri != null)
                        {
                            webClient.DefaultRequestHeaders.Referrer = baseUri;
                        }

                        string result;

                        try
                        {
                            result = await webClient.GetStringAsync(new Uri("https://www.google.com/recaptcha/api/fallback?k=" + captchaChallengeKey));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured while loading the verification image, please check your network connectivity. \r\n\r\nError: " + ex.Message);
                            this.PopupCaptcha.IsOpen = false;
                            this.SendButton.IsEnabled = true;
                            return;
                        }

                        var match = Regex.Match(result, "<img src=\"(?<image>\\S+)\"");

                        var image = match.Groups["image"].Value;

                        this.CaptchaChallenge = HttpUtility.HtmlDecode(image.Replace("/recaptcha/api2/payload?c=", string.Empty))
                            .Split('&').First();

                        Debug.WriteLine("Captcha challenge: {0}", this.CaptchaChallenge);

                        webClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/recaptcha/api/fallback?k=" + captchaChallengeKey);

                        try
                        {
                            string uri = string.Format("https://www.google.com/recaptcha/api2/payload?c={0}&k={1}", this.CaptchaChallenge, captchaChallengeKey);

                            var data = await webClient.GetByteArrayAsync(uri);

                            var captcha = new BitmapImage();

                            captcha.SetSource(new MemoryStream(data));

                            this.ImageCaptcha.Source = captcha;

                            this.PanelLoadingCaptcha.Visibility = Visibility.Collapsed;
                            this.PanelCaptcha.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured while loading the verification image, please check your network connectivity. \r\n\r\nError: " + ex.Message);
                            this.PopupCaptcha.IsOpen = false;
                            this.SendButton.IsEnabled = true;
                        }
                    }
                }
            }
        }

        private void PhotoChooserTaskCompleted(object sender, PhotoResult e)
        {
            try
            {
                if (e.ChosenPhoto == null)
                {
                    return;
                }

                bool animatedGif = false;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var fileName = e.OriginalFileName.Split('\\').Last();

                    if (fileName.EndsWith("_gif.jpg"))
                    {
                        fileName = fileName.Replace("_gif.jpg", ".gif");

                        string path = ViewModel.SavedPicturesDirectory + "/" + fileName;

                        if (isolatedStorage.FileExists(path))
                        {
                            using (var file = isolatedStorage.OpenFile(path, FileMode.Open))
                            {
                                using (var stream = new MemoryStream())
                                {
                                    file.CopyTo(stream);
                                    this.ImageBytes = stream.ToArray();

                                    animatedGif = true;
                                }
                            }
                        }
                    }
                }

                var image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);

                if (!animatedGif)
                {
                    using (var stream = new MemoryStream())
                    {
                        var bitmap = new WriteableBitmap(image);

                        bitmap.SaveJpeg(stream, image.PixelWidth, image.PixelHeight, 0, 85);

                        this.ImageBytes = stream.ToArray();
                    }
                }

                this.PickedPicture.Source = image;
            }
            catch (ArgumentNullException)
            {
            }
        }

        private async void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            var challenge = this.TextBoxCaptcha.Text;

            string captchaChallengeKey = App.ViewModel.Context.BoardManager.GetCaptchaChallengeKey(this.IsPostingNewTopic);

            using (var client = Helper.CreateHttpClient())
            {
                client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/recaptcha/api/fallback?k=" + captchaChallengeKey);
                
                var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("c", this.CaptchaChallenge),
                        new KeyValuePair<string, string>("response", challenge)
                    });

                var result = await client.PostAsync(new Uri("https://www.google.com/recaptcha/api/fallback?k=" + captchaChallengeKey), content);

                string html;

                try
                {
                    html = await result.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("A network error occured: " + ex);
                    return;
                }

                var match = Regex.Match(html, "<textarea dir=\"ltr\" readonly onclick=\"this\\.select\\(\\)\">(?<result>.+?)</textarea>");

                if (!match.Success)
                {
                    MessageBox.Show("You seem to have mistyped the verification");
                    this.PopupCaptcha.IsOpen = false;
                    this.SendButton.IsEnabled = true;
                    return;
                }

                string captcha = match.Groups["result"].Value;

                this.Upload(captcha);
            }
        }

        private void Upload(string captcha)
        {
            this.BusyIndicator.IsRunning = true;
            this.BusyIndicator.Visibility = Visibility.Visible;

            this.PopupCaptcha.IsOpen = false;
            this.PopupCaptcha2.IsOpen = false;

            var address = App.ViewModel.Context.BoardManager.GetPostUri(App.ViewModel.Context.Board, this.IsPostingNewTopic);

            var mapping = App.ViewModel.Context.BoardManager.GetMessageMapping();

            this.Parameters = new Dictionary<string, object>();

            if (mapping.Name != null)
            {
                this.Parameters.Add(mapping.Name, App.ViewModel.ReplyName);
            }

            if (mapping.Email != null)
            {
                this.Parameters.Add(mapping.Email, App.ViewModel.ReplyMail);
            }

            if (mapping.Subject != null)
            {
                this.Parameters.Add(mapping.Subject, App.ViewModel.ReplySubject);
            }

            if (mapping.Comment != null)
            {
                this.Parameters.Add(mapping.Comment, App.ViewModel.ReplyComment);
            }

            if (mapping.RecaptchaChallengeField != null)
            {
                this.Parameters.Add(mapping.RecaptchaChallengeField, this.CaptchaChallenge);
            }

            if (mapping.RecaptchaResponseField != null)
            {
                this.Parameters.Add(mapping.RecaptchaResponseField, captcha);
            }

            if (mapping.Password != null)
            {
                this.Parameters.Add(mapping.Password, App.ViewModel.ReplyPassword);
            }

            if (mapping.Image != null)
            {
                this.Parameters.Add(mapping.Image, (object)this.ImageBytes ?? string.Empty);
            }

            if (mapping.FileName != null)
            {
                this.Parameters.Add(mapping.FileName, DateTime.Now.Ticks + ".jpg");
            }

            App.ViewModel.Context.BoardManager.FillAdditionnalMessageFields(this.Parameters, App.ViewModel.Context, this.IsPostingNewTopic);

            if (!this.IsPostingNewTopic)
            {
                this.Parameters[mapping.TopicId] = App.ViewModel.Context.BoardManager.ExtractTopicIdFromUri(App.ViewModel.Context.Topic.ReplyLink);
            }

            this.Boundary = "-----------------------------" + DateTime.Now.Ticks;

            var request = WebRequest.CreateHttp(address);
            request.Headers[HttpRequestHeader.Referer] = App.ViewModel.Context.Board.Uri;
            request.ContentType = "multipart/form-data; boundary=" + this.Boundary;
            request.Method = "POST";
            request.UserAgent = string.Empty;

            var cookies = App.ViewModel.Context.BoardManager.GetCookies(App.ViewModel.Context, this.IsPostingNewTopic);

            if (cookies != null && cookies.Count > 0)
            {
                request.CookieContainer = new CookieContainer();

                foreach (var cookie in cookies)
                {
                    request.CookieContainer.Add(cookie.Key, cookie.Value);
                }
            }

            request.BeginGetRequestStream(this.RequestCallback, request);
        }

        private void RequestCallback(IAsyncResult result)
        {
            var request = (HttpWebRequest)result.AsyncState;
            var stream = request.EndGetRequestStream(result);

            var bytes = WriteMultipart(this.Parameters, this.Boundary);

            stream.Write(bytes, 0, bytes.Length);
            stream.Close();

            request.BeginGetResponse(this.Callback, request);
        }

        private void Callback(IAsyncResult result)
        {
            try
            {
                var request = (HttpWebRequest)result.AsyncState;

                var response = request.EndGetResponse(result);

                string rawResponse;

                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    rawResponse = stream.ReadToEnd();
                }

                string message = "An error occured while posting the message";

                string redirection = null;

                var postResult = App.ViewModel.Context.BoardManager.IsPostOk(rawResponse);

                if (postResult.IsOk)
                {
                    message = "Post successful!";

                    if (this.IsPostingNewTopic)
                    {
                        redirection = App.ViewModel.Context.BoardManager.ExtractRedirection(App.ViewModel.Context, rawResponse);
                    }
                }
                else
                {
                    if (postResult.ErrorMessage != null)
                    {
                        message += "\r\n\r\n" + postResult.ErrorMessage;
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.HideBusyIndicator();

                    MessageBox.Show(message);

                    if (postResult.IsOk)
                    {
                        App.ViewModel.ReplyComment = string.Empty;

                        if (redirection != null)
                        {
                            App.ViewModel.ClearMessages();
                            App.ViewModel.Context.Topic = new Topic { ReplyLink = redirection, Id = App.ViewModel.Context.BoardManager.ExtractTopicIdFromUri(redirection) };

                            if (this.CheckBoxFavorite.IsChecked == true)
                            {
                                App.ViewModel.LastTopicRepliedTo = App.ViewModel.Context.Topic.Id;
                            }

                            this.NavigationService.Navigate("/ViewTopic.xaml");
                        }
                        else
                        {
                            if (!this.IsPostingNewTopic && this.CheckBoxFavorite.IsChecked == true)
                            {
                                if (!App.ViewModel.IsFavorite(App.ViewModel.Context))
                                {
                                    App.ViewModel.AddToFavorites(App.ViewModel.Context);
                                }
                            }

                            if (this.NavigationService.CanGoBack)
                            {
                                this.NavigationService.GoBack();
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(() =>
                    {
                        this.HideBusyIndicator();

                        MessageBox.Show(
                            "An error occured, please check your network connectivity. You may also have mistyped the verification image.\r\n\r\nError: "
                            + ex.Message);
                    });
            }
        }

        private void HideBusyIndicator()
        {
            this.BusyIndicator.Visibility = Visibility.Collapsed;
            this.BusyIndicator.IsRunning = false;
            this.SendButton.IsEnabled = true;
        }

        private void ButtonPickFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var task = new PhotoChooserTask();

                task.Completed += this.PhotoChooserTaskCompleted;

                task.Show();
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private void WebBrowser_OnNavigating(object sender, NavigatingEventArgs e)
        {
            if (!e.Uri.ToString().StartsWith("https://boards.4chan.org"))
            {
                e.Cancel = true;
            }
        }

        private void WebBrowser_OnNavigated(object sender, NavigationEventArgs e)
        {
            
        }

        private void WebBrowser_OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            string theme = "dark";

            var themeVisibility = (Visibility)this.Resources["PhoneLightThemeVisibility"];

            if (themeVisibility == Visibility.Visible)
            {
                theme = "light";
            }
            
            this.WebBrowser.InvokeScript("eval", @"

var postForm = document.getElementById('postForm');
  
postForm.className = postForm.className.replace(' hideMobile', '');
postForm.style.display = 'block';

document.body.style.cssText = 'background:none;background-color:" + (themeVisibility == Visibility.Visible ? "#FFFFFF" : "#000000") + @" '
var captcha = document.getElementById('g-recaptcha');

var captchaHtml = captcha.outerHTML;

document.body.innerHTML = captchaHtml;

var intervalID = setInterval(function() { var r = grecaptcha.getResponse(); if (r != undefined && r != '') { clearInterval(intervalID); window.external.notify(r); } }, 200);

  var el = document.getElementById('g-recaptcha');
    
  if (!window.passEnabled && window.grecaptcha) {
    grecaptcha.render(el, {
      sitekey: window.recaptchaKey,
      theme: '" + theme + @"',
size:'compact',
      callback: function(response)
        {
            window.external.notify(response);
        }  
    });
  }

");

            this.WebBrowser.Visibility = Visibility.Visible;
        }

        private void WebBrowser_ScriptNotify(object sender, NotifyEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value) || e.Value.Length < 10)
            {
                return;
            }
            
            this.Upload(e.Value);
        }

        private void WebBrowser_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Debug.WriteLine("NavigationFailed: {0}", e.Exception);

            Helper.ShowMessageBox("An error occured while loading the captcha. Please check your network connectivity and try again.");

            this.PopupCaptcha2.IsOpen = false;
            this.SendButton.IsEnabled = true;
        }
    }
}