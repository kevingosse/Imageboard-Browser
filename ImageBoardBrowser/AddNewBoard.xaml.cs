using System;
using System.Windows;

namespace ImageBoardBrowser
{
    public partial class AddNewBoard
    {
        public AddNewBoard()
        {
            this.InitializeComponent();
        }

        private bool IsCustomDescription { get; set; }

        private static string AddSlashes(string boardName)
        {
            return "/" + boardName.Trim('/') + "/";
        }

        private void ButtonAddClick(object sender, EventArgs e)
        {
            var name = AddSlashes(this.TextBoxName.Text);

            var description = this.TextBoxDescription.Text;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("You have to type a name.");
                return;
            }

            if (!name.StartsWith("/") || !name.EndsWith("/"))
            {
                MessageBox.Show("The name must start and end with /");
                return;
            }

            var board = App.ViewModel.Context.BoardManager.CreateBoard(name, description);

            App.ViewModel.CustomBoards.Add(board);
            App.ViewModel.SaveCustomBoards();

            this.NavigationService.GoBack();
        }

        private void FillDescription()
        {
            var description = App.ViewModel.Context.BoardManager.FillDescription(AddSlashes(this.TextBoxName.Text));

            this.TextBoxDescription.Text = description ?? string.Empty;
        }

        private void NameChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.IsCustomDescription)
            {
                this.FillDescription();
            }
        }

        private void NameHelpTap(object sender, EventArgs e)
        {
            Helper.ShowMessageBox("Enter the name of the board by using the format ck or /ck/");
        }

        private void TextBoxDescriptionKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            this.IsCustomDescription = this.TextBoxDescription.Text.Length > 0;
        }

        private void TextBoxDescriptionClearButtonTap(object sender, EventArgs e)
        {
            this.IsCustomDescription = false;
        }
    }
}