using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

using ImageBoard.Parsers.Common;

using Microsoft.Phone.Controls;

namespace ImageBoardBrowser
{
    public partial class Catalog : BasePage, INotifyPropertyChanged
    {
        private bool isLoading;

        public Catalog()
        {
            // Force IsLoading to true from the very beginning, to avoid the "Refresh" button to be displayed
            this.IsLoading = true;

            this.InitializeComponent();

            this.ListEntries.UseOptimizedManipulationRouting = true;
        }

        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }

            set
            {
                this.isLoading = value;
                this.OnPropertyChanged("IsLoading");
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                this.ViewModel.CatalogFilter = null;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
                {
                    var previousPage = this.NavigationService.BackStack.FirstOrDefault();

                    if (previousPage != null && previousPage.Source.ToString().StartsWith("/MainPage.xaml"))
                    {
                        this.NavigationService.RemoveBackEntry();
                    }
                }
                else
                {
                    var selectedEntry = this.SlideViewDetail.SelectedItem as CatalogEntry;

                    if (selectedEntry != null)
                    {
                        selectedEntry.RefreshHistory();
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

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.CatalogEntries == null)
            {
                await this.RefreshPage();
            }
            else
            {
                this.IsLoading = false;
            }
        }

        private async Task RefreshPage()
        {
            this.IsLoading = true;

            var handler = new HttpClientHandler();

            using (var webClient = Helper.CreateHttpClient(handler))
            {
                var context = this.ViewModel.Context;

                var uri = context.BoardManager.BuildCatalogLink(App.ViewModel.Context.Board);

                Exception exception = null;

                bool success = false;

                string result = null;

                try
                {
                    result = await webClient.GetStringAsync(uri);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    MessageBox.Show("An error occured while loading the picture, please check your network connectivity. \r\n\r\nError: " + exception.Message);
                }
                else
                {
                    App.ViewModel.ParseCatalog(context, result);

                    success = true;

                    Helper.SetCookies(handler.CookieContainer, context.BoardManager.GetBaseUri());
                }

                this.IsLoading = false;

                this.ListEntries.StopPullToRefreshLoading(true, success);
            }
        }

        private void RadDataBoundListBoxItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var entry = (CatalogEntry)e.Item.DataContext;

            this.SlideViewDetail.SelectedItem = entry;

            this.PopupDetail.IsOpen = true;
        }

        private void ButtonOpenTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var button = (FrameworkElement)sender;

            var entry = (CatalogEntry)button.DataContext;

            App.ViewModel.ClearMessages();

            App.ViewModel.Context.Topic = new Topic
            {
                Id = entry.Id,
                ReplyLink = entry.ReplyLink,
                ThumbImageLink = entry.ThumbImageLink,
                Content = entry.Description,
                NumberOfReplies = entry.RepliesCount,
                PosterName = entry.Author,
                PostTime = entry.Date
            };

            this.NavigationService.Navigate("/ViewTopic.xaml");
        }

        private async void RefreshRequested(object sender, EventArgs e)
        {
            await this.RefreshPage();
        }

        private async void ButtonRefreshClick(object sender, RoutedEventArgs e)
        {
            await this.RefreshPage();
        }

        private void PivotSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            var item = (PivotItem)e.AddedItems[0];

            if (item.Tag as string == "Topics")
            {
                App.ViewModel.CatalogFilter = null;
                this.NavigationService.Navigate("/MainPage.xaml");
            }
        }

        private void TextBoxSearchChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.ViewModel.CatalogFilter = this.TextBoxSearch.Text;
        }

        private void TextBoxSearchKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.SearchExpander.IsExpanded = false;
            }
        }

        private void SearchExpanderStateChanged(object sender, Telerik.Windows.Controls.ExpandedStateChangedEventArgs e)
        {
            if (e.IsExpanded)
            {
                this.TextBoxSearch.Focus();
            }
            else
            {
                this.ListEntries.Focus();
            }
        }

        private void ListEntries_SelectionChanging(object sender, Telerik.Windows.Controls.SelectionChangingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}