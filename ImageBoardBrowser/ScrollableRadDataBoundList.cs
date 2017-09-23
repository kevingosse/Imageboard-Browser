using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

using Telerik.Windows.Controls;

namespace ImageBoardBrowser
{
    public class ScrollableRadDataBoundList : RadDataBoundListBox
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.ScrollBar = this.GetTemplateChild("PART_VerticalScrollbar") as ScrollBar;

            if (this.ScrollBar != null)
            {
                this.ScrollBar.ValueChanged += ScrollBar_ValueChanged;
            }
        }

        public event RoutedPropertyChangedEventHandler<double> ScrollValueChanged;

        protected ScrollBar ScrollBar { get; set; }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var eventHandler = this.ScrollValueChanged;

            if (eventHandler != null)
            {
                eventHandler(sender, e);
            }
        }
    }
}
