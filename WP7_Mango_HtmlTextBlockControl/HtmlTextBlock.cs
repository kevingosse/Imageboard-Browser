using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace WP7_Mango_HtmlTextBlockControl
{
    public class HtmlTextBlock : Control
    {
        protected const string ShowSpoilersText = "Show spoilers";
        protected const string HideSpoilersText = "Hide spoilers";

        protected const string ElementA = "A";
        protected const string ElementB = "B";
        protected const string ElementBR = "BR";
        protected const string ElementEM = "EM";
        protected const string ElementI = "I";
        protected const string ElementP = "P";
        protected const string ElementQuote = "QUOTE";
        protected const string ElementLink = "LINK";
        protected const string ElementStrong = "STRONG";
        protected const string ElementU = "U";
        protected const string ElementSpoiler = "S";

        protected const string ElementGreenText = "GREENTEXT";

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            "TextWrapping",
            typeof(TextWrapping),
            typeof(HtmlTextBlock),
            new PropertyMetadata((o, e) => { ((HtmlTextBlock)o).TextBlock.TextWrapping = (TextWrapping)e.NewValue; }));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(HtmlTextBlock),
            new PropertyMetadata((o, e) => ((HtmlTextBlock)o).ParseAndSetText((string)e.NewValue)));

        public static readonly DependencyProperty GreenTextForegroundProperty = DependencyProperty.Register(
            "GreenTextForeground",
            typeof(Brush),
            typeof(HtmlTextBlock),
            new PropertyMetadata(Application.Current.Resources["PhoneForegroundBrush"]));

        public HtmlTextBlock()
        {
            this.Spoilers = new List<Run>();

            // Initialize Control by creating a template with a TextBlock
            // TemplateBinding is used to associate Control-based properties
            this.Template = XamlReader.Load(
                "<ControlTemplate " +
                    "xmlns='http://schemas.microsoft.com/client/2007' " +
                    "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
                    "<RichTextBox " +
                        "x:Name=\"TextBlock\" " +
                    "/>" +
                "</ControlTemplate>") as ControlTemplate;
            this.ApplyTemplate();
        }

        public event EventHandler CompatibilityModeActivated;
        public event EventHandler<QuoteEventArgs> QuoteClicked;
        public event EventHandler<LinkEventArgs> LinkClicked;

        // Returns the visible text (i.e., without the HTML tags)
        public string VisibleText
        {
            get
            {
                string rawText = this.Text
                    .Replace("<br>", Environment.NewLine)
                    .Replace("<br/>", Environment.NewLine)
                    .Replace("<br />", Environment.NewLine);

                string text = Regex.Replace(rawText, "<.*?>", string.Empty)
                    .Trim(new[] { '\r', '\n' });

                return HttpUtility.HtmlDecode(text);
            }
        }

        // Specifies whether the browser DOM can be used to attempt to parse invalid XHTML
        // Note: Deliberately not a DependencyProperty because setting this has security implications
        public bool UseDomAsParser { get; set; }

        public InlineCollection Inlines
        {
            get { return Paragraph.Inlines; }
        }

        public Brush GreenTextForeground
        {
            get
            {
                return (Brush)this.GetValue(GreenTextForegroundProperty);
            }

            set
            {
                this.SetValue(GreenTextForegroundProperty, value);
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        protected List<Run> Spoilers { get; private set; }

        protected bool AreSpoilersVisible { get; private set; }

        protected Paragraph Paragraph
        {
            get
            {
                if (this.TextBlock.Blocks.Count == 0)
                {
                    this.TextBlock.Blocks.Add(new Paragraph());
                }

                return (Paragraph)this.TextBlock.Blocks[0];
            }
        }

        protected RichTextBox TextBlock { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get a reference to the embedded TextBlock
            this.TextBlock = this.GetTemplateChild("TextBlock") as RichTextBox;
        }

        protected void ParseAndSetText(string text)
        {
            if (text == null)
            {
                Debugger.Break();
                return;
            }

            this.Spoilers.Clear();

            // Sanitize input
            var matches = Regex.Matches(text, "&\\w{0,8}?;");

            if (matches.Count > 0)
            {
                foreach (var match in matches.OfType<Match>().Select(m => m.Value).Distinct())
                {
                    if (match == "&lt;" || match == "&gt;" || match == "&amp;")
                    {
                        continue;
                    }

                    var newValue = HttpUtility.HtmlDecode(match);

                    if (newValue != match)
                    {
                        text = text.Replace(match, newValue);
                    }
                }
            }

            // Try for a valid XHTML representation of text
            var success = false;

            try
            {
                // Try to parse as-is
                this.ParseAndSetSpecifiedText(text, false);
                success = true;
            }
            catch (Exception ex)
            {
                // Invalid XHTML
                Debug.WriteLine("Error parsing as XML: " + ex);
            }

            if (!success && this.UseDomAsParser)
            {
                try
                {
                    // Try to parse
                    this.ParseAndSetSpecifiedText(text, true);
                    success = true;
                }
                catch (Exception ex)
                {
                    // Still invalid XML
                    Debug.WriteLine("Error parsing as text: " + ex);

                    try
                    {
                        Paragraph.Inlines.Clear();
                        Paragraph.Inlines.Add(new Run { Text = "Error: Could not parse the message.", Foreground = new SolidColorBrush { Color = Colors.Red } });
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (!success)
            {
                // Invalid, unfixable XHTML; display the supplied text as-is
                Paragraph.Inlines.Clear();
                Paragraph.Inlines.Add(new Run { Text = text });
            }
        }

        protected void RaiseLinkClickedEvent(string link)
        {
            var eventHandler = this.LinkClicked;

            if (eventHandler != null)
            {
                eventHandler(this, new LinkEventArgs(link));
            }
        }

        protected void RaiseQuoteClickedEvent(string quote)
        {
            var eventHandler = this.QuoteClicked;

            if (eventHandler != null)
            {
                eventHandler(this, new QuoteEventArgs(quote));
            }
        }

        private void ParseAndSetSpecifiedText(string text, bool lastChance)
        {
            var paragraph = Paragraph;

            if (lastChance)
            {
                Paragraph.Inlines.Clear();

                text = HttpUtility.HtmlDecode(text);

                text = Regex.Replace(text, "<quote>(?<id>[0-9]+)</quote>", ">>${id}");

                var values = text.Split(new[] { "<br/>" }, StringSplitOptions.None);

                foreach (var val in values)
                {
                    Paragraph.Inlines.Add(new Run { Text = val });
                    Paragraph.Inlines.Add(new LineBreak());
                }

                var eventHandler = this.CompatibilityModeActivated;

                if (eventHandler != null)
                {
                    eventHandler(this, EventArgs.Empty);
                }

                return;
            }

            // Clear the collection of Inlines
            paragraph.Inlines.Clear();

            // Wrap the input in a <div> (so even plain text becomes valid XML)
            using (var stringReader = new StringReader(string.Concat("<div>", text, "</div>")))
            {
                // Read the input
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    // State variables
                    var bold = 0;
                    var italic = 0;
                    var underline = 0;
                    string link = null;
                    var lastElement = ElementP;

                    string quote = null;
                    string httpLink = null;

                    string spoiler = null;
                    string greenText = null;

                    // Read the entire XML DOM...
                    while (xmlReader.Read())
                    {
                        var nameUpper = xmlReader.Name.ToUpper();
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                // Handle the begin element
                                switch (nameUpper)
                                {
                                    case ElementQuote:
                                        quote = string.Empty;
                                        break;

                                    case ElementSpoiler:
                                        spoiler = string.Empty;
                                        break;

                                    case ElementGreenText:
                                        greenText = string.Empty;
                                        break;

                                    case ElementLink:
                                        httpLink = string.Empty;
                                        break;

                                    case ElementA:
                                        link = string.Empty;

                                        // Look for the HREF attribute (can't use .MoveToAttribute because it's case-sensitive)
                                        if (xmlReader.MoveToFirstAttribute())
                                        {
                                            do
                                            {
                                                if ("HREF" == xmlReader.Name.ToUpper())
                                                {
                                                    // Store the link target
                                                    link = xmlReader.Value;
                                                    break;
                                                }
                                            }
                                            while (xmlReader.MoveToNextAttribute());
                                        }

                                        break;

                                    case ElementB:
                                    case ElementStrong:
                                        bold++;
                                        break;

                                    case ElementI:
                                    case ElementEM:
                                        italic++;
                                        break;

                                    case ElementU:
                                        underline++;
                                        break;

                                    case ElementBR:
                                        Paragraph.Inlines.Add(new LineBreak());
                                        break;

                                    case ElementP:
                                        // Avoid double-space for <p/><p/>
                                        if (lastElement != ElementP)
                                        {
                                            Paragraph.Inlines.Add(new LineBreak());
                                            Paragraph.Inlines.Add(new LineBreak());
                                        }

                                        break;
                                }

                                lastElement = nameUpper;
                                break;

                            case XmlNodeType.EndElement:
                                // Handle the end element
                                switch (nameUpper)
                                {
                                    case ElementLink:
                                        {
                                            var run = new Run { Text = httpLink, FontSize = 24 };

                                            var hyperlink = new Hyperlink();

                                            hyperlink.Inlines.Add(run);

                                            string closureLink = httpLink;

                                            hyperlink.Click += (sender, e) => this.RaiseLinkClickedEvent(closureLink);
                                            hyperlink.Command = new DummyCommand();

                                            Paragraph.Inlines.Add(hyperlink);

                                            quote = null;
                                            httpLink = null;
                                            break;
                                        }

                                    case ElementSpoiler:
                                        {
                                            var run = new Run { Text = spoiler, Foreground = new SolidColorBrush(Colors.Transparent) };

                                            paragraph.Inlines.Add(run);
                                            spoiler = null;

                                            this.Spoilers.Add(run);

                                            break;
                                        }

                                    case ElementGreenText:
                                    {
                                        var run = new Run { Text = greenText };

                                        var binding = new Binding("GreenTextForeground");
                                        binding.Source = this;

                                        BindingOperations.SetBinding(run, Run.ForegroundProperty, binding);
                                        
                                        paragraph.Inlines.Add(run);
                                        greenText = null;

                                        break;
                                    }

                                    case ElementQuote:
                                        {
                                            var run = new Run { Text = ">>" + quote, FontSize = 24 };

                                            var quoteLink = new Hyperlink();

                                            quoteLink.Inlines.Add(run);

                                            string closureQuote = quote;

                                            quoteLink.Click += (sender, e) => this.RaiseQuoteClickedEvent(closureQuote);
                                            quoteLink.Command = new DummyCommand();

                                            Paragraph.Inlines.Add(quoteLink);

                                            quote = null;
                                            break;
                                        }

                                    case ElementA:
                                        link = null;
                                        break;

                                    case ElementB:
                                    case ElementStrong:
                                        bold--;
                                        break;

                                    case ElementI:
                                    case ElementEM:
                                        italic--;
                                        break;

                                    case ElementU:
                                        underline--;
                                        break;

                                    case ElementBR:
                                        Paragraph.Inlines.Add(new LineBreak());
                                        break;

                                    case ElementP:
                                        Paragraph.Inlines.Add(new LineBreak());
                                        Paragraph.Inlines.Add(new LineBreak());
                                        break;
                                }

                                lastElement = nameUpper;
                                break;

                            case XmlNodeType.Text:
                            case XmlNodeType.Whitespace:
                                // Create a Run for the visible text
                                // Collapse contiguous whitespace per HTML behavior
                                if (quote != null)
                                {
                                    quote += xmlReader.Value;
                                    break;
                                }

                                if (spoiler != null)
                                {
                                    spoiler += xmlReader.Value;
                                    break;
                                }

                                if (greenText != null)
                                {
                                    greenText += xmlReader.Value;
                                    break;
                                }

                                if (httpLink != null)
                                {
                                    httpLink += xmlReader.Value;
                                    break;
                                }

                                var builder = new StringBuilder(xmlReader.Value.Length);
                                var last = '\0';

                                foreach (var c in xmlReader.Value.Replace('\n', ' '))
                                {
                                    if (' ' != last || ' ' != c)
                                    {
                                        builder.Append(c);
                                    }

                                    last = c;
                                }

                                // Trim leading whitespace if following a <P> or <BR> element per HTML behavior
                                var builderString = builder.ToString();

                                if ((ElementP == lastElement) || (ElementBR == lastElement))
                                {
                                    builderString = builderString.TrimStart();
                                }

                                // If any text remains to display...
                                if (0 < builderString.Length)
                                {
                                    // Create a Run to display it
                                    var run = new Run { Text = builderString };

                                    // Style the Run appropriately
                                    if (0 < bold)
                                    {
                                        run.FontWeight = FontWeights.Bold;
                                    }

                                    if (0 < italic)
                                    {
                                        run.FontStyle = FontStyles.Italic;
                                    }

                                    if (0 < underline)
                                    {
                                        run.TextDecorations = TextDecorations.Underline;
                                    }

                                    if (null != link)
                                    {
                                        // Links get styled and display their HREF since Run doesn't support MouseLeftButton* events
                                        run.TextDecorations = TextDecorations.Underline;
                                        run.Foreground = new SolidColorBrush { Color = Colors.Blue };
                                        run.Text = link;
                                    }

                                    // Add the Run to the collection
                                    Paragraph.Inlines.Add(run);
                                    lastElement = null;
                                }

                                break;
                        }
                    }
                }
            }

            if (this.Spoilers.Count > 0)
            {
                var run = new Run { Text = ShowSpoilersText };

                var showSpoilersLink = new Hyperlink();

                showSpoilersLink.Inlines.Add(run);

                showSpoilersLink.Click += this.ShowSpoilersLinkClick;
                showSpoilersLink.Command = new DummyCommand();
                
                this.Paragraph.Inlines.Insert(0, showSpoilersLink);
                this.Paragraph.Inlines.Insert(1, new LineBreak());
            }
        }

        private void ShowSpoilersLinkClick(object sender, RoutedEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;

            this.AreSpoilersVisible = !this.AreSpoilersVisible;
            
            var linkText = (Run)hyperlink.Inlines[0];

            linkText.Text = this.AreSpoilersVisible ? HideSpoilersText : ShowSpoilersText;

            var spoilerForeground = this.AreSpoilersVisible ? hyperlink.Foreground : new SolidColorBrush(Colors.Transparent);

            foreach (var run in this.Spoilers)
            {
                run.Foreground = spoilerForeground;
            }
        }
    }

    /// <summary>
    /// Workaround for the hyperlink click issue. What the hell is going on?
    /// </summary>
    public class DummyCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
        }

        public event EventHandler CanExecuteChanged;
    }
}
