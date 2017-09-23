using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ImageBoard.Parsers.Common;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using Telerik.Windows.Controls;

namespace ImageBoardBrowser
{
    public partial class ViewBoards : BasePage
    {
        public ViewBoards()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.RefreshEmptyTextBoxVisibility();
        }

        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var board = (Board)e.AddedItems[0];

                this.ViewModel.Context.Board = board;
                this.ViewModel.ClearTopics();
                this.ViewModel.CurrentPage = 0;

                this.ViewModel.ClearCatalog();

                if (this.ViewModel.IsCatalogViewPerDefault && this.ViewModel.Context.BoardManager.IsCatalogSupported)
                {
                    this.NavigationService.Navigate("/Catalog.xaml");
                }
                else
                {
                    var uri = new Uri(string.Format("/MainPage.xaml?site={1}&board={0}", board.Name, App.ViewModel.Context.BoardManager.Name), UriKind.Relative);
                    this.NavigationService.Navigate(uri);
                }
            }

            ((ListBox)sender).SelectedIndex = -1;
        }

        private void ButtonAddBoardClick(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate("/AddNewBoard.xaml");
        }

        private void MenuItemDeleteClick(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;

            var board = element.DataContext as Board;

            if (board != null)
            {
                this.ViewModel.DeleteBoard(board);
                this.RefreshEmptyTextBoxVisibility();
            }
        }

        private void MenuItemPinClick(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;

            var board = element.DataContext as Board;

            if (board != null)
            {
                var uri = new Uri(string.Format("/MainPage.xaml?site={1}&board={0}", board.Name, App.ViewModel.Context.BoardManager.Name), UriKind.Relative);

                if (ShellTile.ActiveTiles.Any(t => t.NavigationUri == uri))
                {
                    MessageBox.Show("This board is already pinned to the home screen.");
                    return;
                }


                var smallContainer = new Grid { Height = 159, Width = 159, Background = new SolidColorBrush { Color = Colors.Transparent } };

                smallContainer.Children.Add(new TextBlock
                {
                    Foreground = new SolidColorBrush { Color = Colors.White },
                    Text = board.Name,
                    FontSize = 235 / board.Name.Length,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });

                //var tile = new RadIconicTileData
                //{
                //    Title = board.Name,
                //    SmallIconVisualElement = container,
                //    IconImage = new Uri("/Background.png", UriKind.Relative),
                //    WideContent1 = board.Description
                //};

                var mediumContainer = new Grid { Height = 336, Width = 336, Background = new SolidColorBrush { Color = Colors.Transparent } };

                mediumContainer.Children.Add(new Image
                {
                    Source = new BitmapImage(new Uri("/Background.png", UriKind.Relative)),
                    Stretch = Stretch.None,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(13)
                });

                var largeContainer = new Grid
                {
                    Height = 336,
                    Width = 691,
                    Background = new SolidColorBrush { Color = Colors.Transparent }
                };

                largeContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                largeContainer.ColumnDefinitions.Add(new ColumnDefinition());

                var image = new Image
                {
                    Source = new BitmapImage(new Uri("/Background.png", UriKind.Relative)),
                    Stretch = Stretch.None,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(30, 0, 30, 0)
                };

                Grid.SetColumn(image, 0);

                largeContainer.Children.Add(image);

                var panel = new StackPanel();

                panel.Children.Add(new TextBlock
                {
                    Foreground = new SolidColorBrush { Color = Colors.White },
                    Text = board.Name,
                    FontSize = 60,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 26, 0, 13)
                });

                panel.Children.Add(new TextBlock
                {
                    Foreground = new SolidColorBrush { Color = Colors.White },
                    FontSize = 40,
                    Text = board.Description
                });

                Grid.SetColumn(panel, 1);

                largeContainer.Children.Add(panel);

                var tile = new RadFlipTileData
                {
                    SmallVisualElement = smallContainer,
                    Title = board.Name,
                    VisualElement = mediumContainer,
                    WideVisualElement = largeContainer,
                    IsTransparencySupported = true
                };

                LiveTileHelper.CreateOrUpdateTile(tile, uri, true);
            }
        }

        private void RefreshEmptyTextBoxVisibility()
        {
            this.ScrollViewerEmptyList.Visibility = App.ViewModel.CustomBoards.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PhoneApplicationPageLoaded(object sender, RoutedEventArgs e)
        {
            if (ReviewBugger.IsTimeForReview())
            {
                try
                {
                    ReviewBugger.PromptUser();
                }
                catch (Exception)
                {
                }
            }
        }

        private void MenuFavoritesClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/Favorites.xaml");
        }

        private void MenuSettingsClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate("/Configuration.xaml");
        }
    }
}