using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

using ImageBoard.Parsers.Common;

namespace ImageBoardBrowser
{
    public partial class Favorites
    {
        private bool networkError;

        public Favorites()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<Favorite> FavoriteList { get; private set; }

        public bool NetworkError
        {
            get
            {
                return this.networkError;
            }

            protected set
            {
                this.networkError = value;
                this.OnPropertyChanged("NetworkError");
            }
        }

        protected override async void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var favoriteList = App.ViewModel.Favorites
                .Where(f => f.SiteName == App.ViewModel.Context.BoardManager.Name)
                .ToList();

            foreach (var favorite in favoriteList)
            {
                if (favorite.Topic.Id == null)
                {
                    continue;
                }

                HistoryEntry historyEntry;

                if (!this.ViewModel.History.TryGetValue(favorite.Topic.Id, out historyEntry))
                {
                    historyEntry = new HistoryEntry(favorite.Board.Name, favorite.Topic.Id);
                    this.ViewModel.History[favorite.Topic.Id] = historyEntry;
                }

                favorite.History = historyEntry;
            }

            this.FavoriteList = new ObservableCollection<Favorite>(favoriteList);

            this.OnPropertyChanged("FavoriteList");

            if (e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                await this.RefreshFavorites();
            }
        }

        private async Task RefreshFavorites()
        {
            this.NetworkError = false;

            var context = this.ViewModel.Context;

            if (context.BoardManager.IsCatalogSupported)
            {
                foreach (var group in this.FavoriteList.GroupBy(f => f.Board))
                {
                    foreach (var favorite in group)
                    {
                        favorite.IsLoading = true;
                        favorite.NetworkError = false;
                    }

                    var boardContext = context.Clone();
                    boardContext.Board = group.Key;

                    var uri = App.ViewModel.Context.BoardManager.BuildCatalogLink(group.Key);

                    var webClient = Helper.CreateHttpClient();

                    try
                    {
                        var content = await webClient.GetStringAsync(uri);

                        this.CatalogDownloadCompleted(group, content, boardContext);
                    }
                    catch (Exception)
                    {
                        this.NetworkError = true;

                        foreach (var favorite in group)
                        {
                            favorite.IsLoading = false;
                            favorite.NetworkError = true;
                        }
                    }
                }
            }
            else
            {
                foreach (var favorite in this.FavoriteList)
                {
                    favorite.IsLoading = true;
                    favorite.NetworkError = false;

                    var webClient = Helper.CreateHttpClient();

                    Uri uri;

                    Uri.TryCreate(
                        new Uri(favorite.Board.Uri),
                        favorite.Topic.ReplyLink + "?nocache=" + ViewModel.GetNoCacheSeed(),
                        out uri);

                    var favoriteClosure = favorite;

                    await webClient.GetAsync(uri).ContinueWith(t => this.TopicDownloadCompleted(t, favoriteClosure));
                }
            }
        }

        private async void TopicDownloadCompleted(Task<HttpResponseMessage> task, Favorite favorite)
        {
            if (task.IsFaulted || !task.Result.IsSuccessStatusCode)
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    favorite.IsLoading = false;

                    if (!task.IsFaulted && task.Result.StatusCode == HttpStatusCode.NotFound && !string.IsNullOrEmpty(task.Result.ReasonPhrase))
                    {
                        favorite.IsDead = true;
                    }
                    else
                    {
                        this.NetworkError = true;
                        favorite.NetworkError = true;
                    }
                });
            }
            else
            {
                var context = this.ViewModel.Context.Clone();

                if (favorite.SiteName != context.BoardManager.Name)
                {
                    // Context has changed, request is outdated
                    return;
                }

                context.Board = favorite.Board;
                context.Topic = favorite.Topic;

                var content = await task.Result.Content.ReadAsStringAsync();

                App.ViewModel.ParseMessages(context, content, false);

                this.Dispatcher.BeginInvoke(() =>
                {
                    favorite.NetworkError = false;
                    favorite.IsLoading = false;

                    if (favorite.History == null)
                    {
                        HistoryEntry historyEntry;

                        if (this.ViewModel.History.TryGetValue(favorite.Topic.Id, out historyEntry))
                        {
                            favorite.History = historyEntry;
                        }
                    }
                    else
                    {
                        favorite.RefreshHistory();
                    }
                });
            }
        }

        private void CatalogDownloadCompleted(IGrouping<Board, Favorite> group, string content, Context context)
        {
            var catalog = App.ViewModel.ParseCatalog(context, content);

            foreach (var favorite in this.FavoriteList.Where(f => f.Board.Equals(group.Key)))
            {
                favorite.IsLoading = false;
                favorite.NetworkError = false;

                if (favorite.Topic.Id == null)
                {
                    continue;
                }

                if (favorite.History == null)
                {
                    HistoryEntry historyEntry;

                    if (this.ViewModel.History.TryGetValue(favorite.Topic.Id, out historyEntry))
                    {
                        favorite.History = historyEntry;
                    }
                }
                else
                {
                    favorite.RefreshHistory();
                }

                if (catalog.All(c => c.Id != favorite.Topic.Id))
                {
                    favorite.IsDead = true;
                }
            }
        }

        private void ListFavoritesItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var selectedFavorite = (Favorite)e.Item.DataContext;

            App.ViewModel.Context.Topic = selectedFavorite.Topic;
            App.ViewModel.Context.Board = selectedFavorite.Board;
            App.ViewModel.ClearMessages();

            this.NavigationService.Navigate("/ViewTopic.xaml");
        }

        private async void MenuRefreshClick(object sender, EventArgs e)
        {
            await this.RefreshFavorites();
        }

        private void MenuPruneClick(object sender, EventArgs e)
        {
            var result = MessageBox.Show("You are about to remove all dead threads. Do you want to continue?", "Clean up", MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            var deadFavorites = this.FavoriteList.Where(f => f.IsDead).ToList();

            foreach (var favorite in deadFavorites)
            {
                this.FavoriteList.Remove(favorite);
            }

            this.ViewModel.RemoveFromFavorites(deadFavorites);
        }
    }
}