using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

using Coding4Fun.Phone.Controls;

using ImageBoard.Parsers.Common;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Telerik.Windows.Controls;

using WP7_Mango_HtmlTextBlockControl;

namespace ImageBoardBrowser
{
    public partial class ViewTopic : BasePage
    {
        public ViewTopic()
        {
            this.QuotesHistory = new Stack<Message>();

            this.InitializeComponent();
        }

        public List<Message> MessagesWithSameId { get; set; }

        public string SelectedId { get; set; }

        protected bool PopulateThread { get; set; }

        protected string ContextMenuText { get; set; }

        protected Stack<Message> QuotesHistory { get; private set; }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (this.QuotePopup.IsOpen)
            {
                e.Cancel = true;

                if (this.QuotesHistory.Count > 0)
                {
                    this.QuotePopup.DataContext = this.QuotesHistory.Pop();
                }
                else
                {
                    this.QuotePopup.IsOpen = false;
                }
            }
            else if (this.IdPopup.IsOpen)
            {
                this.IdPopup.IsOpen = false;
                e.Cancel = true;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var progressIndicator = SystemTray.GetProgressIndicator(this);

            if (progressIndicator != null)
            {
                progressIndicator.Text = App.ViewModel.Context.Board.Name;
            }

            var previousPage = this.NavigationService.BackStack.FirstOrDefault();

            if (previousPage != null && previousPage.Source.ToString().StartsWith("/ReplyMessage.xaml"))
            {
                this.NavigationService.RemoveBackEntry();
                this.PopulateThread = true;
            }

            this.SetFavoriteIcon();

            try
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    try
                    {
                        this.Dispatcher.BeginInvoke(() =>
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        });
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }

        private async void PhoneApplicationPageLoaded(object sender, RoutedEventArgs e)
        {
            this.SetSizes();

            if (App.ViewModel.Messages == null)
            {
                await this.RefreshPage();
            }
        }

        private async Task RefreshPage()
        {
            var indicator = new ProgressIndicator
            {
                IsVisible = true,
                IsIndeterminate = true,
                Text = "Loading..."
            };

            SystemTray.SetProgressIndicator(this, indicator);

            var handler = new HttpClientHandler();

            using (var webClient = Helper.CreateHttpClient(handler))
            {
                var context = this.ViewModel.Context;

                if (context?.Topic == null)
                {
                    return;
                }

                Uri uri;

                var success = Uri.TryCreate(
                    new Uri(context.Board.Uri),
                    App.ViewModel.Context.Topic.ReplyLink + "?nocache=" + ViewModel.GetNoCacheSeed(),
                    out uri);

                Debug.Assert(success, "Error creating the topic uri");

                Debug.WriteLine("Loading topic " + uri);

                string result;

                try
                {
                    result = await webClient.GetStringAsync(uri);
                }
                catch (Exception ex)
                {
                    this.LinkScrollBottom.Visibility = Visibility.Collapsed;
                    this.LinkScrollTop.Visibility = Visibility.Collapsed;
                    
                    MessageBox.Show("An error occured while loading the picture, please check your network connectivity. \r\n\r\nError: " + ex.Message);

                    SystemTray.SetProgressIndicator(this, null);

                    return;
                }

                Helper.SetCookies(handler.CookieContainer, context.BoardManager.GetBaseUri());

                this.TextBoxCompatibility.Visibility = Visibility.Collapsed;

                var progressIndicator = SystemTray.ProgressIndicator;

                if (progressIndicator != null)
                {
                    progressIndicator.Text = "Parsing...";
                }

                HistoryEntry historyEntry;

                var topicId = context.Topic.Id;

                // Grab the historyEntry before it's overwritten in the ParseMessages
                int oldRepliesCount = 0;

                if (topicId != null && this.ViewModel.History.TryGetValue(topicId, out historyEntry))
                {
                    oldRepliesCount = historyEntry.OldRepliesCount;
                }

                this.ViewModel.LoadMessages(context, result, true);

                // TODO: the "mark as read" should be here

                if (this.PopulateThread && this.ViewModel.Messages != null)
                {
                    var firstMessage = this.ViewModel.Messages.FirstOrDefault();

                    if (firstMessage != null)
                    {
                        context.Topic.ImageLink = firstMessage.ImageLink;
                        context.Topic.PosterName = firstMessage.PosterName;
                        context.Topic.PostTime = firstMessage.PostTime;
                        context.Topic.ThumbImageLink = firstMessage.ThumbImageLink;
                        context.Topic.Id = firstMessage.Id;

                        this.PopulateThread = false;

                        if (this.ViewModel.LastTopicRepliedTo != null)
                        {
                            if (this.ViewModel.LastTopicRepliedTo == context.Topic.Id)
                            {
                                if (!this.ViewModel.IsFavorite(context))
                                {
                                    this.ViewModel.AddToFavorites(context);
                                    this.SetFavoriteIcon();
                                }
                            }

                            this.ViewModel.LastTopicRepliedTo = null;
                        }
                    }
                }

                var messages = this.ViewModel.Messages;

                if (topicId != null && oldRepliesCount != 0)
                {
                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            message.IsLastReadMessage = false;
                        }

                        if (messages.Count > oldRepliesCount)
                        {
                            var lastReadMessage = messages.ElementAt(oldRepliesCount);

                            lastReadMessage.IsLastReadMessage = true;

                            this.ListBoxMessages.BringIntoView(lastReadMessage);
                        }
                    }
                }

                if (this.ListBoxMessages.ViewportItems.Length != context.Topic.NumberOfReplies + 1)
                {
                    this.LinkScrollBottom.Visibility = Visibility.Visible;
                    this.LinkScrollTop.Visibility = Visibility.Visible;
                }
            }

            SystemTray.SetProgressIndicator(this, null);
        }

        private void MenuReplyClick(object sender, EventArgs e)
        {
            App.ViewModel.ClearReplyPage();
            this.NavigationService.Navigate("/ReplyMessage.xaml");
        }

        private async void MenuRefreshClick(object sender, EventArgs e)
        {
            await this.RefreshPage();
        }

        private void HtmlTextBlockCompatibilityModeActivated(object sender, EventArgs e)
        {
            this.TextBoxCompatibility.Visibility = Visibility.Visible;
        }

        private void SetFavoriteIcon()
        {
            var favoriteMenuItem = (ApplicationBarIconButton)this.ApplicationBar.Buttons[2];

            if (App.ViewModel.IsFavorite(App.ViewModel.Context))
            {
                favoriteMenuItem.IconUri = new Uri("icons/appbar.favs.removefrom.rest.png", UriKind.Relative);
                favoriteMenuItem.Text = "Remove fav.";
            }
            else
            {
                favoriteMenuItem.IconUri = new Uri("icons/appbar.favs.addto.rest.png", UriKind.Relative);
                favoriteMenuItem.Text = "Add to fav.";
            }
        }

        private void TextBoxCompatibilityTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            const string Message = @"Compatibility mode is enabled.

What does it means?

For some reason, ImageBoard cannot parse one or more messages on this page. Therefore, the application has switched to compatibility mode. The messages below may be truncated or not displayed correctly. We aplogize for the inconvenience.";

            MessageBox.Show(Message);
        }

        private void HtmlTextBlockQuoteClicked(object sender, QuoteEventArgs e)
        {
            var quotedMessage = App.ViewModel.Messages.FirstOrDefault(m => m.Id == e.QuoteId);

            this.ShowQuote(quotedMessage);
        }

        private void ShowQuote(Message message)
        {
            if (message == null)
            {
                message = new Message
                {
                    Content = "No message found with this id."
                };
            }

            if (this.QuotePopup.IsOpen)
            {
                var previousMessage = this.QuotePopup.DataContext as Message;

                if (previousMessage != null)
                {
                    this.QuotesHistory.Push(previousMessage);
                }
            }
            else
            {
                this.QuotePopup.IsOpen = true;
            }

            this.QuotePopup.DataContext = message;
        }

        private void RadDataBoundListBoxSelectionChanging(object sender, SelectionChangingEventArgs e)
        {
            e.Cancel = true;
        }

        private void QuotePopupOpened(object sender, EventArgs e)
        {
            ((Popup)sender).Visibility = Visibility.Visible;
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            this.QuotePopup.IsOpen = false;
            this.QuotesHistory.Clear();
        }

        private void QuotePopupClosed(object sender, EventArgs e)
        {
            ((Popup)sender).Visibility = Visibility.Collapsed;
        }

        private void GridTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.Context.Message = (Message)((FrameworkElement)sender).DataContext;

            if (!string.IsNullOrEmpty(App.ViewModel.Context.Message.ImageLink))
            {
                this.NavigationService.Navigate("/ViewImage.xaml");
            }

            e.Handled = false;
        }

        private void ImageTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.Context.Message = (Message)((FrameworkElement)sender).DataContext;

            if (!string.IsNullOrEmpty(App.ViewModel.Context.Message.ImageLink))
            {
                this.NavigationService.Navigate("/ViewImage.xaml");
            }

            e.Handled = false;
        }

        private void ReplyMessageClick(object sender, RoutedEventArgs e)
        {
            var message = ((FrameworkElement)sender).DataContext as Message;

            if (message != null)
            {
                App.ViewModel.ClearReplyPage();
                this.NavigationService.Navigate(string.Format("/ReplyMessage.xaml?id={0}", message.Id));
            }
        }

        private void MenuHelpClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/TopicsHelpPage.xaml");
        }

        private void MenuFavoritesClick(object sender, EventArgs e)
        {
            string message;

            if (App.ViewModel.IsFavorite(App.ViewModel.Context))
            {
                App.ViewModel.RemoveFromFavorites(App.ViewModel.Context);
                message = "The thread has been removed from your favorites.";
            }
            else
            {
                App.ViewModel.AddToFavorites(App.ViewModel.Context);
                message = "The thread has been added to your favorites.";
            }

            this.SetFavoriteIcon();

            try
            {
                var toast = new ToastPrompt { Title = message };

                toast.Show();
            }
            catch (Exception)
            {
            }
        }

        private void LinkScrollBottomClick(object sender, RoutedEventArgs e)
        {
            var source = this.ListBoxMessages.ItemsSource as IEnumerable<Message>;

            if (source == null)
            {
                return;
            }

            var element = source.LastOrDefault();

            if (element != null)
            {
                this.ListBoxMessages.BringIntoView(element);
            }
        }

        private void LinkScrollTopClick(object sender, RoutedEventArgs e)
        {
            var source = this.ListBoxMessages.ItemsSource as IEnumerable<Message>;

            if (source == null)
            {
                return;
            }

            var element = source.FirstOrDefault();

            if (element != null)
            {
                this.ListBoxMessages.BringIntoView(element);
            }
        }

        private void HtmlTextBlockLinkClicked(object sender, LinkEventArgs e)
        {
            Uri uri;

            try
            {
                uri = new Uri(e.Link);
            }
            catch (Exception ex)
            {
                Helper.ShowMessageBox("An error occured while trying to parse the link: " + ex.Message);
                return;
            }

            Helper.OpenWebBrowser(uri);
        }

        private void MenuOpenWebBrowserClick(object sender, EventArgs e)
        {
            var context = this.ViewModel.Context;

            var uri = context.BoardManager.GetBrowserTopicLink(context);

            Helper.OpenWebBrowser(uri);
        }

        private void MenuConfigurationClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/Configuration.xaml");
        }

        private void RadContextMenuItemTapped(object sender, ContextMenuItemSelectedEventArgs e)
        {
            Clipboard.SetText(this.ContextMenuText);
        }

        private void RadContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            var textBlock = e.FocusedElement as HtmlTextBlock;

            if (textBlock != null)
            {
                this.ContextMenuText = textBlock.VisibleText;
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            this.SetSizes();
        }

        private void SetSizes()
        {
            if (this.ActualHeight == 0 || this.ActualWidth == 0)
            {
                return;
            }

            if (this.Orientation.IsPortrait())
            {
                this.PopupGrid.MaxHeight = this.ActualHeight - 30;
                this.PopupBorder.Width = this.ActualWidth;
            }
            else
            {
                this.PopupGrid.MaxHeight = this.ActualHeight - 30;
                this.PopupBorder.Width = this.ActualWidth - 40;
            }
        }

        private void TextQuoteTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var element = (FrameworkElement)sender;

            var quotedMessage = element.DataContext as Message;

            this.ShowQuote(quotedMessage);
        }

        private void ButtonCloseIdPopup(object sender, RoutedEventArgs e)
        {
            this.IdPopup.IsOpen = false;
        }
        
        private void IdTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var tappedMessage = element.DataContext as Message;

            if (tappedMessage == null)
            {
                return;
            }

            this.SelectedId = tappedMessage.PosterId;

            this.MessagesWithSameId = this.ViewModel.Messages.Where(m => m.PosterId == tappedMessage.PosterId).ToList();

            this.OnPropertyChanged("MessagesWithSameId");
            this.OnPropertyChanged("SelectedId");

            this.IdPopup.IsOpen = true;

            try
            {
                var unsubscribe = this.IdPopup.GetType().GetMethod("UnsubscribeFromPageUnloaded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                unsubscribe.Invoke(this.IdPopup, null);
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private void PopupIdClosing(object sender, CancelEventArgs e)
        {
            var window = (RadWindow)sender;

            if (window.CloseAnimation == RadAnimation.Empty)
            {
                e.Cancel = true;
            }
        }
    }
}