using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

using ImageBoard.Parsers.Common;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Telerik.Windows.Controls;

using WP7_Mango_HtmlTextBlockControl;

namespace ImageBoardBrowser
{
    public partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.MenuNext = (ApplicationBarIconButton)this.ApplicationBar.Buttons[2];
            this.MenuPrevious = (ApplicationBarIconButton)this.ApplicationBar.Buttons[0];
        }

        public IEnumerable<Topic> Topics { get; set; }

        protected string ContextMenuText { get; set; }

        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.ViewModel.LastTopicRepliedTo = null;

            if (App.ViewModel.Topics == null)
            {
                if (this.NavigationContext.QueryString.ContainsKey("board"))
                {
                    string boardName = this.NavigationContext.QueryString["board"];

                    string site = this.NavigationContext.QueryString.ContainsKey("site") ? this.NavigationContext.QueryString["site"] : "4chan";

                    App.ViewModel.SetCurrentBoard(ViewModel.ExtractBoard(site));

                    App.ViewModel.Context.Board = App.ViewModel.Context.BoardManager.CreateBoard(boardName, string.Empty);
                }

                await this.RefreshPage();
            }

            try
            {
                if (e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
                {
                    if (this.ViewModel.Context.BoardManager.IsCatalogSupported)
                    {
                        var previousPage = this.NavigationService.BackStack.FirstOrDefault();

                        if (previousPage != null && previousPage.Source.ToString().StartsWith("/Catalog.xaml"))
                        {
                            this.NavigationService.RemoveBackEntry();
                        }
                    }
                    else
                    {
                        var catalog = this.Pivot.Items.OfType<PivotItem>().FirstOrDefault(i => i.Tag as string == "Catalog");

                        if (catalog != null)
                        {
                            this.Pivot.Items.Remove(catalog);
                        }
                    }
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private void PhoneApplicationPageLoaded(object sender, RoutedEventArgs e)
        {
        }

        private async Task RefreshPage()
        {
            var indicator = new ProgressIndicator();
            indicator.IsVisible = true;
            indicator.IsIndeterminate = true;
            indicator.Text = "Loading...";
            SystemTray.SetProgressIndicator(this, indicator);

            int currentPage = this.ViewModel.CurrentPage;

            var context = this.ViewModel.Context;

            var uri = context.BoardManager.BuildPageLink(App.ViewModel.Context.Board.Name, currentPage, ViewModel.GetNoCacheSeed());

            System.Diagnostics.Debug.WriteLine(uri);

            var handler = new HttpClientHandler();

            using (var webClient = Helper.CreateHttpClient(handler))
            {
                Exception exception = null;

                string result = null;

                try
                {
                    result = await webClient.GetStringAsync(uri);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (currentPage != this.ViewModel.CurrentPage)
                {
                    // Obsolete query
                    return;
                }

                if (exception != null)
                {
                    MessageBox.Show("An error occured while loading the picture, please check your network connectivity. \r\n\r\nError: " + exception);
                }
                else
                {
                    var progressIndicator = SystemTray.ProgressIndicator;

                    if (progressIndicator != null)
                    {
                        progressIndicator.Text = "Parsing...";
                    }

                    App.ViewModel.ParseBoard(result);

                    Helper.SetCookies(handler.CookieContainer, context.BoardManager.GetBaseUri());
                }

                this.UpdateApplicationBar();

                SystemTray.SetProgressIndicator(this, null);
            }
        }

        private void UpdateApplicationBar()
        {
            this.MenuPrevious.IsEnabled = App.ViewModel.CurrentPage > 0;

            this.MenuNext.IsEnabled = App.ViewModel.DisplayCurrentPage < App.ViewModel.DisplayPageCount;
        }

        private async void MenuPreviousClick(object sender, EventArgs e)
        {
            if (App.ViewModel.CurrentPage > 0)
            {
                App.ViewModel.CurrentPage--;
            }

            this.UpdateApplicationBar();

            App.ViewModel.ClearTopics();

            await this.RefreshPage();
        }

        private async void MenuNextClick(object sender, EventArgs e)
        {
            App.ViewModel.CurrentPage++;

            this.UpdateApplicationBar();

            App.ViewModel.ClearTopics();

            await this.RefreshPage();
        }

        private async void MenuRefreshClick(object sender, EventArgs e)
        {
            await this.RefreshPage();
            this.UpdateApplicationBar();
        }

        private void ListTopicsItemTap(object sender, ListBoxItemTapEventArgs e)
        {
            App.ViewModel.Context.Topic = (Topic)e.Item.DataContext;
            App.ViewModel.ClearMessages();

            this.NavigationService.Navigate("/ViewTopic.xaml");
        }

        private void HtmlTextBlockCompatibilityModeActivated(object sender, EventArgs e)
        {
            this.TextBoxCompatibility.Visibility = Visibility.Visible;
        }

        private void TextBlockCatalogTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.ClearMessages();
            App.ViewModel.ClearCatalog();
            this.NavigationService.Navigate("/Catalog.xaml");
        }

        private void TextBoxCompatibilityTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            const string Message = @"Compatibility mode is enabled.

What does it means?

For some reason, ImageBoard cannot parse one or more messages on this page. Therefore, the application has switched to compatibility mode. The messages below may be truncated or not displayed correctly. We aplogize for the inconvenience.";

            MessageBox.Show(Message);
        }

        private void MenuNewTopicClick(object sender, EventArgs e)
        {
            App.ViewModel.ClearReplyPage();
            this.NavigationService.Navigate("/ReplyMessage.xaml?type=topic");
        }

        private void MenuOpenWebBrowserClick(object sender, EventArgs e)
        {
            var context = this.ViewModel.Context;

            var link = context.BoardManager.GetBrowserPageLink(context, this.ViewModel.CurrentPage);

            Helper.OpenWebBrowser(link);
        }

        private void MenuFavoritesClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/Favorites.xaml");
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

        private void PivotSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var item = (PivotItem)e.AddedItems[0];

            if (item.Tag as string == "Catalog")
            {
                App.ViewModel.ClearMessages();
                App.ViewModel.ClearCatalog();

                this.NavigationService.Navigate("/Catalog.xaml");
            }
        }
    }
}