using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Phone.Tasks;

namespace ImageBoardBrowser
{
    public partial class SelectSite
    {
        public SelectSite()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {
                try
                {
                    if (this.NavigationService.BackStack.Any())
                    {
                        this.NavigationService.RemoveBackEntry();
                    }
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
            }

            try
            {
                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isolatedStorage.FileExists("Error.txt"))
                    {
                        string exception;

                        using (var stream = new StreamReader(isolatedStorage.OpenFile("Error.txt", FileMode.Open)))
                        {
                            exception = stream.ReadToEnd();
                        }

                        isolatedStorage.DeleteFile("Error.txt");

                        var result =
                            MessageBox.Show(
                                "Sorry, it looks like this application crashed last time you used it. If you desire so, you can send us the error traces by e-mail, and we'll try our best to solve this issue in next version\r\nSend the traces by e-mail?",
                                "The application crashed!",
                                MessageBoxButton.OKCancel);

                        if (result == MessageBoxResult.OK)
                        {
                            var task = new EmailComposeTask();

                            task.Subject = "Error in " + App.ViewModel.ApplicationTitle;
                            task.To = "wp7dev@live.fr";
                            task.Body = "** Feel free to as much information as you can! (what you were doing when the crash occured, which board you were browsing... **"
                                + "\r\n\r\n\r\n\r\nError details:\r\n\r\n" +
                                exception;

                            task.Show();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var boardManager = ViewModel.ExtractBoard((string)e.AddedItems[0]);

                if (boardManager != null)
                {
                    App.ViewModel.SetCurrentBoard(boardManager);

                    this.NavigationService.Navigate("/ViewBoards.xaml?site=" + boardManager.Name);
                }
            }

            ((ListBox)sender).SelectedIndex = -1;
        }

        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;

            var site = element.DataContext as string;

            if (site != null)
            {
                App.ViewModel.DeleteSite(site);
            }
        }

        private void ContactButtonClick(object sender, RoutedEventArgs e)
        {
            var task = new EmailComposeTask
            {
                Subject = "Feedback on " + App.ViewModel.ApplicationTitle,
                To = "wp7dev@live.fr"
            };

            task.Show();
        }

        private void TwitterButtonClick(object sender, RoutedEventArgs e)
        {
            new WebBrowserTask { Uri = new Uri("https://twitter.com/ImageBoardBro", UriKind.Absolute) }.Show();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            string site = this.TextBoxSite.Text;

            var boardManager = ViewModel.ExtractBoard(site);

            if (boardManager != null)
            {
                if (App.ViewModel.Sites.Contains(boardManager.Name))
                {
                    MessageBox.Show(string.Format("The image board {0} is already in your list.", boardManager.Name));
                    return;
                }

                App.ViewModel.Sites.Add(boardManager.Name);
                App.ViewModel.SaveSites();
            }
            else if (site.Length > 2 && site.StartsWith("/") && site.EndsWith("/"))
            {
                MessageBox.Show("Looks like you typed the name of a 4chan board. In this screen, you have to type the website you want to browse (in this case: 4chan).");
            }
            else
            {
                MessageBox.Show("Sorry, this image board isn't supported at this time.\r\n\r\nFeel free to use the feedback feature to suggest it.");
            }
        }

        private void ButtonBuyClick(object sender, RoutedEventArgs e)
        {
            var marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void ButtonUservoiceClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var browser = new WebBrowserTask { Uri = new Uri("http://imageboard.uservoice.com") };
                browser.Show();
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Panorama.SelectedIndex == 2)
            {
                this.LoadingChangelog.Visibility = Visibility.Collapsed;
                this.Changelog.Visibility = Visibility.Visible;
            }
        }

        private void LicenseLink_OnClick(object sender, RoutedEventArgs e)
        {
            const string License = @"Copyright (c) 2015 Michael Craig McGee

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";

            MessageBox.Show(License);
        }
    }
}