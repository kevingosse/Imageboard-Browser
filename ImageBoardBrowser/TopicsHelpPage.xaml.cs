using Microsoft.Phone.Controls;

namespace ImageBoardBrowser
{
    public partial class TopicsHelpPage : PhoneApplicationPage
    {
        public TopicsHelpPage()
        {
            this.InitializeComponent();

            this.DataContext = App.ViewModel;
        }
    }
}