using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml;
using System.Xml.Linq;

using ImageBoard.Controls;
using ImageBoard.Parsers.AnonIb;
using ImageBoard.Parsers.Common;
using ImageBoard.Parsers.EightChan;
using ImageBoard.Parsers.FChan;
using ImageBoard.Parsers.FetChan;
using ImageBoard.Parsers.FiftyFiveChan;
using ImageBoard.Parsers.FourChan;
using ImageBoard.Parsers.FourTwentyChan;
using ImageBoard.Parsers.KrautChan;
using ImageBoard.Parsers.ManeChan;
using ImageBoard.Parsers.MlpChan;
using ImageBoard.Parsers.NewFapChan;
using ImageBoard.Parsers.OperatorChan;
using ImageBoard.Parsers.PonyChan;
using ImageBoard.Parsers.SevenChan;
using ImageBoard.Parsers.SeventySevenChan;
using ImageBoard.Parsers.WizardChan;
using ImageBoard.Parsers.Ylilauta;

using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;

using Telerik.Windows.Controls;

namespace ImageBoardBrowser
{
    public class ViewModel : INotifyPropertyChanged
    {
        public const string SavedPicturesDirectory = "SavedPictures";

        public const string TempDirectory = "Temp";

        protected const string CustomBoardsFileName = "CustomBoards_{0}.xml";
        protected const string SitesFileName = "Sites.xml";
        protected const string FavoritesFileName = "Favorites.xml";
        protected const string HistoryFileName = "History_{0}.xml";

        private object historyLock = new object();

        private string catalogFilter;
        private string replyComment;
        private string replySubject;
        private int currentPage;
        private int? pageCount;

        static ViewModel()
        {
            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));
            InteractionEffectManager.AllowedTypes.Add(typeof(ListBoxItem));
            InteractionEffectManager.AllowedTypes.Add(typeof(EmbeddedImage));
        }

        public ViewModel()
        {
            this.History = new Dictionary<string, HistoryEntry>();

            this.Context = new Context();
            this.LoadSites();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Context Context { get; private set; }

        public string LastTopicRepliedTo { get; set; }

        public int? PageCount
        {
            get
            {
                return this.pageCount;
            }

            set
            {
                this.pageCount = value;
                this.RaisePropertyChanged("PageCount");
                this.RaisePropertyChanged("DisplayPageCount");
            }
        }

        public int? DisplayPageCount
        {
            get
            {
                if (this.pageCount == null)
                {
                    return null;
                }

                return Math.Max(this.DisplayCurrentPage, this.pageCount.Value);
            }
        }

        public string ApplicationTitle
        {
            get
            {
                return "Imageboard Browser";
            }
        }

        public string Version
        {
            get
            {
                return App.Version;
            }
        }

        public IEnumerable<Topic> Topics { get; set; }

        public ObservableCollection<Message> Messages { get; set; }

        public IEnumerable<Message> FilteredMessages
        {
            get
            {
                if (this.Messages == null)
                {
                    return new Message[0];
                }

                return this.Messages.Where(m => !string.IsNullOrEmpty(m.ImageLink));
            }
        }

        public IEnumerable<CatalogEntry> CatalogEntries { get; set; }

        public IEnumerable<CatalogEntry> FilteredCatalogEntries
        {
            get
            {
                var catalogEntries = this.CatalogEntries;

                if (catalogEntries == null)
                {
                    return null;
                }

                var filter = this.CatalogFilter;

                if (string.IsNullOrEmpty(filter))
                {
                    return catalogEntries;
                }

                return catalogEntries.Where(c => c.Description.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1
                    || (c.Subject != null && c.Subject.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1));
            }
        }

        public string CatalogFilter
        {
            get
            {
                return this.catalogFilter;
            }

            set
            {
                this.catalogFilter = value;
                this.RaisePropertyChanged("CatalogFilter");
                this.RaisePropertyChanged("FilteredCatalogEntries");
            }
        }

        public ObservableCollection<string> Sites { get; set; }

        public ObservableCollection<Favorite> Favorites { get; set; }

        public ObservableCollection<Board> CustomBoards { get; set; }

        public string ReplyName { get; set; }

        public string ReplyMail { get; set; }

        public string ReplySubject
        {
            get
            {
                return this.replySubject;
            }

            set
            {
                this.replySubject = value;
                this.RaisePropertyChanged("ReplySubject");
            }
        }

        public string ReplyComment
        {
            get
            {
                return this.replyComment;
            }

            set
            {
                this.replyComment = value;
                this.RaisePropertyChanged("ReplyComment");
            }
        }

        public string ReplyPassword { get; set; }

        public int DisplayCurrentPage
        {
            get
            {
                return this.CurrentPage + 1;
            }
        }

        public int CurrentPage
        {
            get
            {
                return this.currentPage;
            }

            set
            {
                this.currentPage = value;
                this.RaisePropertyChanged("CurrentPage");
                this.RaisePropertyChanged("DisplayCurrentPage");
            }
        }

        public bool AddReplyToFavorites
        {
            get
            {
                return GetSetting("AddReplyToFavorites", false);
            }

            set
            {
                SetSetting("AddReplyToFavorites", value);
            }
        }

        public int SlideShowDelay
        {
            get
            {
                return GetSetting("SlideShowDelay", 5);
            }

            set
            {
                SetSetting("SlideShowDelay", value);
            }
        }

        public bool IsSlideShowLoopActivated
        {
            get
            {
                return GetSetting("IsSlideShowLoopActivated", false);
            }

            set
            {
                SetSetting("IsSlideShowLoopActivated", value);
            }
        }

        public bool GreenTextIsGreen
        {
            get
            {
                return GetSetting("GreenTextIsGreen", true);
            }

            set
            {
                SetSetting("GreenTextIsGreen", value);
                this.RaisePropertyChanged("GreenTextIsGreen");
            }
        }

        public bool PersistHistory
        {
            get
            {
                return GetSetting("PersistHistory", true);
            }

            set
            {
                SetSetting("PersistHistory", value);
            }
        }

        public bool IsCatalogViewPerDefault
        {
            get
            {
                return GetSetting("IsCatalogViewPerDefault", false);
            }

            set
            {
                SetSetting("IsCatalogViewPerDefault", value);
            }
        }

        public bool IsOrientationLockActivated
        {
            get
            {
                return GetSetting("IsOrientationLockActivated", false);
            }

            set
            {
                SetSetting("IsOrientationLockActivated", value);
            }
        }

        public bool IsGifStreamingEnabled
        {
            get
            {
                return GetSetting("IsGifStreamingEnabled", true);
            }

            set
            {
                SetSetting("IsGifStreamingEnabled", value);
            }
        }

        public bool IsOrientationLockActivatedForPictures { get; set; }

        public Dictionary<string, HistoryEntry> History { get; set; }

        public static string GetNoCacheSeed()
        {
            var rnd = new Random();

            var number = rnd.Next(100000000).ToString();

            return number.Replace('0', '1');
        }

        public static IEnumerable<string> DumpDirectory(string directory)
        {
            yield return "<dir> " + directory;

            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (var file in isolatedStorage.GetFileNames(directory + "/*"))
                {
                    long size = -1;

                    try
                    {
                        using (var stream = isolatedStorage.OpenFile(file, FileMode.Open, FileAccess.Read, FileShare.Delete))
                        {
                            size = stream.Length;
                        }
                    }
                    catch (Exception)
                    {

                    }

                    yield return directory + "/" + file + " " + size;
                }

                foreach (var subDirectory in isolatedStorage.GetDirectoryNames(directory + "/*"))
                {
                    foreach (var entry in DumpDirectory(directory + "/" + subDirectory))
                    {
                        yield return entry;
                    }
                }
            }
        }

        public static void ClearFiles(string directory, string filter)
        {
            try
            {
                string prefix = directory == null ? string.Empty : directory + "/";

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var files = isolatedStorage.GetFileNames(prefix + filter);

                    foreach (var file in files)
                    {
                        isolatedStorage.DeleteFile(prefix + file);
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

        public static void CleanPictures(List<string> files)
        {
            if (files == null || files.Count == 0)
            {
                return;
            }

            try
            {
                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var library = new MediaLibrary())
                    {
                        foreach (var picture in library.SavedPictures)
                        {
                            if (files.Contains(picture.Name))
                            {
                                files.Remove(picture.Name);

                                if (files.Count == 0)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var file in files)
                    {
                        isolatedStorage.DeleteFile(SavedPicturesDirectory + "/" + file);
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

        public static void ClearShellContent()
        {
            int failures = 0;

            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var files = isolatedStorage.GetFileNames("/Shared/ShellContent/*");

                foreach (var file in files)
                {
                    try
                    {
                        isolatedStorage.DeleteFile(file);
                    }
                    catch (Exception)
                    {
                        failures++;
                    }
                }

                files = isolatedStorage.GetFileNames("*.jpg");

                foreach (var file in files)
                {
                    try
                    {
                        isolatedStorage.DeleteFile(file);
                    }
                    catch (Exception)
                    {
                        failures++;
                    }
                }

                MessageBox.Show(string.Format("{0} files deleted out of {1}", files.Length - failures, files.Length));
            }
        }

        public static BoardManager ExtractBoard(string name)
        {
            if (name.Equals("showall", StringComparison.InvariantCultureIgnoreCase))
            {
                MessageBox.Show(@"The supported boards are:
 4chan
 Ponychan
 Krautchan
 7chan
 8chan
 420chan
 Operatorchan
 Ylilauta
 Mlpchan
 AnonIB
 WizardChan
 55ch
 77chan
 Fetchan
 Newfapchan
 Fchan");
                return null;
            }

            if (name.Equals("debugstorage", StringComparison.InvariantCultureIgnoreCase))
            {
                string result = string.Empty;

                foreach (var entry in DumpDirectory(""))
                {
                    result += entry + "\r\n";
                }

                MessageBox.Show(result);

                return null;
            }

            if (name.Equals("debugclearstorage", StringComparison.InvariantCultureIgnoreCase))
            {
                ClearShellContent();

                return null;
            }

            if (name.IndexOf("4chan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new FourChanBoardManager();
            }

            if (name.IndexOf("ponychan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new PonyChanBoardManager();
            }

            if (name.IndexOf("krautchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new KrautChanBoardManager();
            }

            if (name.IndexOf("420chan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new FourTwentyChanBoardManager();
            }

            if (name.IndexOf("fchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new FChanBoardManager();
            }

            if (name.IndexOf("77chan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new SeventySevenChanBoardManager();
            }

            if (name.IndexOf("7chan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new SevenChanBoardManager();
            }

            if (name.IndexOf("8ch", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new EightChanBoardManager();
            }

            if (name.IndexOf("ylilauta", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new YlilautaBoardManager();
            }

            if (name.IndexOf("fapchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new NewFapChanBoardManager();
            }

            if (name.IndexOf("operatorchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new OperatorChanBoardManager();
            }

            if (name.IndexOf("fetchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new FetChanBoardManager();
            }

            if (name.IndexOf("manechan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new ManeChanBoardManager();
            }

            if (name.IndexOf("mlpchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new MlpChanBoardManager();
            }

            if (name.IndexOf("wizardchan", StringComparison.InvariantCultureIgnoreCase) != -1
                || name.IndexOf("wizchan", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new WizardChanBoardManager();
            }

            if (name.IndexOf("anonib", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new AnonIbBoardManager();
            }

            if (name.IndexOf("55ch", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                return new FiftyFiveChanBoardManager();
            }

            return null;
        }

        public void SetCurrentBoard(BoardManager board)
        {
            var context = new Context { BoardManager = board };

            this.Context = context;
            this.LoadCustomBoards(context);
            this.LoadFavorites(context);
            this.LoadHistory(context);
        }

        public bool IsFavorite(Context context)
        {
            return this.Favorites.Any(f => f.Board.Uri == context.Board.Uri && f.Topic.ReplyLink == context.Topic.ReplyLink);
        }

        public void AddToFavorites(Context context)
        {
            if (this.IsFavorite(context))
            {
                return;
            }

            this.Favorites.Add(new Favorite { SiteName = context.BoardManager.Name, Board = context.Board, Topic = context.Topic });

            HistoryEntry history;

            if (!this.History.TryGetValue(context.Topic.Id, out history))
            {
                history = new HistoryEntry(context.Board.Name, context.Topic.Id);
                this.History[context.Topic.Id] = history;

                var messages = this.Messages;

                if (messages != null && messages.Count > 0)
                {
                    var repliesCount = messages.Skip(1).Count();
                    var imagesCount = messages.Skip(1).Count(m => !string.IsNullOrEmpty(m.ThumbImageLink));

                    history.RepliesCount = repliesCount;
                    history.OldRepliesCount = repliesCount;

                    history.ImagesCount = imagesCount;
                    history.OldImagesCount = imagesCount;

                    history.Visited = true;
                }
            }

            this.SaveFavorites();
            this.SaveHistory(context);
        }

        public void RemoveFromFavorites(IEnumerable<Favorite> favorites)
        {
            foreach (var favorite in favorites)
            {
                var favoriteToRemove = this.Favorites.FirstOrDefault(f => f.Board.Uri == favorite.Board.Uri && f.Topic.ReplyLink == favorite.Topic.ReplyLink);

                if (favoriteToRemove != null)
                {
                    if (favorite.Topic.Id != null)
                    {
                        this.History.Remove(favorite.Topic.Id);
                    }

                    this.Favorites.Remove(favorite);
                }
            }

            this.SaveFavorites();
            this.SaveHistory(this.Context);
        }

        public void RemoveFromFavorites(Context context)
        {
            var favorite = this.Favorites.FirstOrDefault(f => f.Board.Uri == context.Board.Uri && f.Topic.ReplyLink == context.Topic.ReplyLink);

            if (favorite != null)
            {
                this.Favorites.Remove(favorite);

                if (favorite.Topic.Id != null)
                {
                    this.History.Remove(favorite.Topic.Id);
                }
            }

            this.SaveFavorites();
            this.SaveHistory(context);
        }

        public void ClearMessages()
        {
            this.Messages = null;
            this.RaisePropertyChanged("Messages");
        }

        public void ClearCatalog()
        {
            this.CatalogEntries = null;
            this.RaisePropertyChanged("CatalogEntries");
        }

        public void ClearTopics()
        {
            this.Topics = null;
            this.RaisePropertyChanged("Topics");
        }

        public void ClearReplyPage()
        {
            this.ReplyComment = string.Empty;
            this.ReplySubject = string.Empty;
        }

        public void ParseBoard(string content)
        {
            try
            {
                this.Topics = this.Context.BoardManager.TopicParser.ParsePage(this.Context, content).ToList();
                this.PageCount = this.Context.BoardManager.TopicParser.ExtractPageCount(this.Context, content);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured, please check your network connectivity.\r\n\r\nError: " + ex.Message);
            }

            this.RaisePropertyChanged("Topics");
        }

        public List<Message> ParseMessages(Context context, string content, bool markAsRead)
        {
            var newMessages = context.BoardManager.MessageParser.ParsePage(context, content).ToList();

            if (newMessages.Count > 0)
            {
                var repliesCount = newMessages.Skip(1).Count();
                var imagesCount = newMessages.Skip(1).Count(m => !string.IsNullOrEmpty(m.ThumbImageLink));

                if (context.Topic.Id == null)
                {
                    // Probably a legacy favorite
                    var resto = newMessages[0].Resto;

                    if (resto != null)
                    {
                        context.Topic.Id = resto;
                        this.SaveFavorites();
                    }
                }

                HistoryEntry historyEntry = null;

                // Retrieve the history, but don't create it if catalog isn't supported
                if (context.Topic.Id != null && !this.History.TryGetValue(context.Topic.Id, out historyEntry) && context.BoardManager.IsCatalogSupported)
                {
                    historyEntry = new HistoryEntry(context.Board.Name, context.Topic.Id);
                    this.History[historyEntry.ThreadId] = historyEntry;
                }

                if (historyEntry != null)
                {
                    if (markAsRead)
                    {
                        historyEntry.OldImagesCount = imagesCount;
                        historyEntry.OldRepliesCount = repliesCount;
                    }

                    historyEntry.ImagesCount = imagesCount;
                    historyEntry.RepliesCount = repliesCount;

                    historyEntry.Visited = true;

                    this.SaveHistory(context);
                }
            }

            return newMessages;
        }

        public void LoadMessages(Context context, string content, bool markAsRead)
        {
            try
            {
                var collection = this.Messages;

                var newMessages = this.ParseMessages(context, content, markAsRead);

                if (collection == null || collection.Count == 0)
                {
                    this.Messages = new ObservableCollection<Message>(newMessages);
                    this.RaisePropertyChanged("Messages");
                }
                else
                {
                    lock (collection)
                    {
                        var lastMessage = collection.LastOrDefault();

                        bool areNewMessages = lastMessage == null;

                        foreach (var message in newMessages)
                        {
                            if (!areNewMessages)
                            {
                                var originalMessage = collection.FirstOrDefault(m => m.Id == message.Id);

                                if (originalMessage != null)
                                {
                                    originalMessage.BackLinks = message.BackLinks;
                                    originalMessage.PreviousMessagesCount = message.PreviousMessagesCount;
                                }

                                if (message.Id == lastMessage.Id)
                                {
                                    areNewMessages = true;
                                }
                            }
                            else
                            {
                                collection.Add(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured, please check your network connectivity.\r\n\r\nError: " + ex.Message);
            }
        }

        public List<CatalogEntry> ParseCatalog(Context context, string content)
        {
            List<CatalogEntry> catalogEntries = null;

            try
            {
                catalogEntries = context.BoardManager.TopicParser.ParseCatalog(context, content).ToList();

                var newHistory = new Dictionary<string, HistoryEntry>();

                foreach (var historyEntry in this.History.Values.Where(h => h.BoardName != context.Board.Name))
                {
                    newHistory[historyEntry.ThreadId] = historyEntry;
                }

                foreach (var entry in catalogEntries)
                {
                    HistoryEntry historyEntry;

                    if (!this.History.TryGetValue(entry.Id, out historyEntry))
                    {
                        historyEntry = new HistoryEntry(context.Board.Name, entry.Id);
                    }

                    historyEntry.ImagesCount = entry.ImagesCount ?? 0;
                    historyEntry.RepliesCount = entry.RepliesCount;

                    entry.History = historyEntry;

                    newHistory[historyEntry.ThreadId] = historyEntry;
                }

                this.CatalogEntries = catalogEntries;

                this.History = newHistory;

                this.SaveHistory(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured, please check your network connectivity.\r\n\r\nError: " + ex.Message);
            }

            this.RaisePropertyChanged("CatalogEntries");
            this.RaisePropertyChanged("FilteredCatalogEntries");

            return catalogEntries;
        }

        public void LoadSites()
        {
            var result = new ObservableCollection<string>();

            try
            {
                string content;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new StreamReader(isolatedStorage.OpenFile(SitesFileName, FileMode.OpenOrCreate)))
                    {
                        content = stream.ReadToEnd();
                    }
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var document = XDocument.Parse(content);

                    foreach (var node in document.Descendants("site"))
                    {
                        result.Add(node.Value);
                    }
                }
                else
                {
                    result.Add("4chan.org");
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }

            this.Sites = result;
            this.RaisePropertyChanged("Sites");
        }

        public void LoadCustomBoards(Context context)
        {
            var result = new ObservableCollection<Board>();

            string fileName = string.Format(CustomBoardsFileName, context.BoardManager.Name);

            try
            {
                string content;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new StreamReader(isolatedStorage.OpenFile(fileName, FileMode.OpenOrCreate)))
                    {
                        content = stream.ReadToEnd();
                    }
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var document = XDocument.Parse(content);

                    foreach (var node in document.Descendants("board"))
                    {
                        result.Add(context.BoardManager.CreateBoard((string)node.Attribute("name"), (string)node.Attribute("description")));
                    }
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }

            this.CustomBoards = result;
            this.RaisePropertyChanged("CustomBoards");
        }

        public void LoadFavorites(Context context)
        {
            var result = new ObservableCollection<Favorite>();

            try
            {
                string content;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new StreamReader(isolatedStorage.OpenFile(FavoritesFileName, FileMode.OpenOrCreate)))
                    {
                        content = stream.ReadToEnd();
                    }
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var settings = new XmlReaderSettings { CheckCharacters = false, DtdProcessing = DtdProcessing.Parse };

                    using (var textReader = new StringReader(content))
                    {
                        using (var reader = XmlReader.Create(textReader, settings))
                        {
                            var document = XDocument.Load(reader);

                            foreach (var node in document.Descendants("favorite"))
                            {
                                result.Add(new Favorite
                                {
                                    SiteName = (string)node.Attribute("site") ?? "4chan.org",
                                    Topic = new Topic
                                    {
                                        Content = (string)node.Attribute("content"),
                                        Id = (string)node.Attribute("topicId"),
                                        ImageLink = (string)node.Attribute("imageLink"),
                                        ThumbImageLink = (string)node.Attribute("ThumbImageLink"),
                                        PosterName = (string)node.Attribute("PosterName"),
                                        PostTime = string.IsNullOrEmpty((string)node.Attribute("PostTime")) ? null : (DateTime?)XmlConvert.ToDateTime((string)node.Attribute("PostTime"), XmlDateTimeSerializationMode.Local),
                                        ReplyLink = (string)node.Attribute("ReplyLink")
                                    },
                                    Board = context.BoardManager.CreateBoard((string)node.Attribute("boardName"), (string)node.Attribute("boardDescription"))
                                });
                            }
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

            this.Favorites = result;
            this.RaisePropertyChanged("Favorites");
        }

        public void LoadHistory(Context context)
        {
            var result = new Dictionary<string, HistoryEntry>();

            string fileName = string.Format(HistoryFileName, context.BoardManager.Name);

            try
            {
                string content;

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new StreamReader(isolatedStorage.OpenFile(fileName, FileMode.OpenOrCreate)))
                    {
                        content = stream.ReadToEnd();
                    }
                }

                if (!string.IsNullOrEmpty(content))
                {
                    var document = XDocument.Parse(content);

                    foreach (var entry in document.Descendants("historyEntry").Select(HistoryEntry.FromXml))
                    {
                        result[entry.ThreadId] = entry;
                    }
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }

            this.History = result;
        }

        public void DeleteBoard(Board board)
        {
            this.CustomBoards.Remove(board);
            this.SaveCustomBoards();
        }

        public void DeleteSite(string site)
        {
            this.Sites.Remove(site);
            this.SaveSites();
        }

        public void SaveSites()
        {
            var document = new XDocument();

            document.Add(
                new XElement(
                    "sites",
                    this.Sites
                        .OrderBy(a => a)
                        .Select(a => new XElement("site", a))));

            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isolatedStorage.DeleteFile(SitesFileName);

                using (var stream = isolatedStorage.OpenFile(SitesFileName, FileMode.OpenOrCreate))
                {
                    document.Save(stream);
                }
            }
        }

        public void SaveCustomBoards()
        {
            string fileName = string.Format(CustomBoardsFileName, this.Context.BoardManager.Name);

            var document = new XDocument();

            document.Add(
                new XElement(
                    "boards",
                    this.CustomBoards
                        .OrderBy(a => a.Name)
                        .Select(
                            a => new XElement(
                                "board",
                                new XAttribute("name", a.Name),
                                new XAttribute("description", a.Description)))));

            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isolatedStorage.DeleteFile(fileName);

                using (var stream = isolatedStorage.OpenFile(fileName, FileMode.OpenOrCreate))
                {
                    document.Save(stream);
                }
            }
        }

        public void SaveHistory(Context context)
        {
            if (!this.PersistHistory)
            {
                return;
            }

            lock (historyLock)
            {
                var history = this.History;

                string fileName = string.Format(HistoryFileName, context.BoardManager.Name);

                var document = new XDocument();

                document.Add(new XElement("history", history.Values.Select(a => a.ToXml())));

                using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    isolatedStorage.DeleteFile(fileName);

                    using (var stream = isolatedStorage.OpenFile(fileName, FileMode.OpenOrCreate))
                    {
                        document.Save(stream);
                    }
                }
            }
        }

        public void SaveFavorites()
        {
            var document = new XDocument();

            document.Add(
                new XElement(
                    "favorites",
                    this.Favorites
                        .Select(f => new XElement(
                            "favorite",
                            new XAttribute("site", f.SiteName ?? string.Empty),
                            new XAttribute("content", f.Topic.Content ?? string.Empty),
                            new XAttribute("imageLink", f.Topic.ImageLink ?? string.Empty),
                            new XAttribute("topicId", f.Topic.Id ?? string.Empty),
                            new XAttribute("ThumbImageLink", f.Topic.ThumbImageLink ?? string.Empty),
                            new XAttribute("PosterName", f.Topic.PosterName ?? string.Empty),
                            new XAttribute("PostTime", f.Topic.PostTime == null ? string.Empty : XmlConvert.ToString(f.Topic.PostTime.Value, XmlDateTimeSerializationMode.Local)),
                            new XAttribute("ReplyLink", f.Topic.ReplyLink),
                            new XAttribute("boardName", f.Board.Name),
                            new XAttribute("boardDescription", f.Board.Description)))));

            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isolatedStorage.DeleteFile(FavoritesFileName);

                var settings = new XmlWriterSettings { CheckCharacters = false };

                if (document.Declaration != null && !string.IsNullOrEmpty(document.Declaration.Encoding))
                {
                    try
                    {
                        settings.Encoding = Encoding.GetEncoding(document.Declaration.Encoding);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                using (var stream = isolatedStorage.OpenFile(FavoritesFileName, FileMode.OpenOrCreate))
                {
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        document.Save(writer);
                    }
                }
            }
        }

        public void SaveState()
        {
            var service = PhoneApplicationService.Current;

            var context = this.Context;

            if (context != null && context.BoardManager != null)
            {
                service.State["SelectedBoardManagerName"] = context.BoardManager.Name;
            }

            if (this.Context.Board != null)
            {
                service.State["SelectedBoardName"] = this.Context.Board.Name;
                service.State["SelectedBoardDescription"] = this.Context.Board.Description;

                if (this.Context.Board.AdditionalFields.ContainsKey("pony"))
                {
                    service.State["SelectedBoardPony"] = this.Context.Board.AdditionalFields["pony"];
                }

                if (this.Context.Board.AdditionalFields.ContainsKey("anticaptcha"))
                {
                    service.State["SelectedBoardAnticaptcha"] = this.Context.Board.AdditionalFields["anticaptcha"];
                }

                if (this.Context.Board.AdditionalFields.ContainsKey("uuid"))
                {
                    service.State["SelectedBoardUuid"] = this.Context.Board.AdditionalFields["uuid"];
                }
            }

            if (this.Context.Topic != null)
            {
                service.State["SelectedTopicContent"] = this.Context.Topic.Content;
                service.State["SelectedTopicImageLink"] = this.Context.Topic.ImageLink;
                service.State["SelectedTopicPosterName"] = this.Context.Topic.PosterName;
                service.State["SelectedTopicPostTime"] = this.Context.Topic.PostTime;
                service.State["SelectedTopicReplyLink"] = this.Context.Topic.ReplyLink;
                service.State["SelectedTopicThumbImageLink"] = this.Context.Topic.ThumbImageLink;
                service.State["SelectedTopicId"] = this.Context.Topic.Id;

                if (this.Context.Topic.AdditionalFields.ContainsKey("pony"))
                {
                    service.State["SelectedTopicPony"] = this.Context.Topic.AdditionalFields["pony"];
                }

                if (this.Context.Topic.AdditionalFields.ContainsKey("anticaptcha"))
                {
                    service.State["SelectedTopicAnticaptcha"] = this.Context.Topic.AdditionalFields["anticaptcha"];
                }

                if (this.Context.Topic.AdditionalFields.ContainsKey("uuid"))
                {
                    service.State["SelectedTopicUuid"] = this.Context.Topic.AdditionalFields["uuid"];
                }
            }

            if (this.Context.Message != null)
            {
                service.State["SelectedMessageContent"] = this.Context.Message.Content;
                service.State["SelectedMessageId"] = this.Context.Message.Id;
                service.State["SelectedMessageImageLink"] = this.Context.Message.ImageLink;
                service.State["SelectedMessagePosterName"] = this.Context.Message.PosterName;
                service.State["SelectedMessagePostTime"] = this.Context.Message.PostTime;
                service.State["SelectedMessageResto"] = this.Context.Message.Resto;
                service.State["SelectedMessageThumbImageLink"] = this.Context.Message.ThumbImageLink;
            }

            service.State["CurrentPage"] = this.CurrentPage;
            service.State["ReplyComment"] = this.ReplyComment;
            service.State["ReplyMail"] = this.ReplyMail;
            service.State["ReplyName"] = this.ReplyName;
            service.State["ReplyPassword"] = this.ReplyPassword;
            service.State["ReplySubject"] = this.ReplySubject;
        }

        public void LoadState()
        {
            var service = PhoneApplicationService.Current;

            if (service.State.ContainsKey("SelectedBoardManagerName"))
            {
                var boardManager = ExtractBoard((string)service.State["SelectedBoardManagerName"]);

                this.SetCurrentBoard(boardManager);
            }

            if (service.State.ContainsKey("SelectedBoardName"))
            {
                this.Context.Board = this.Context.BoardManager.CreateBoard((string)service.State["SelectedBoardName"], (string)service.State["SelectedBoardDescription"]);

                if (service.State.ContainsKey("SelectedBoardPony"))
                {
                    this.Context.Board.AdditionalFields["pony"] = (string)service.State["SelectedBoardPony"];
                }

                if (service.State.ContainsKey("SelectedBoardAnticaptcha"))
                {
                    this.Context.Board.AdditionalFields["anticaptcha"] = (string)service.State["SelectedBoardAnticaptcha"];
                }


                if (service.State.ContainsKey("SelectedBoardUuid"))
                {
                    this.Context.Board.AdditionalFields["uuid"] = (string)service.State["SelectedBoardUuid"];
                }
            }

            if (service.State.ContainsKey("SelectedTopicContent"))
            {
                this.Context.Topic = new Topic
                {
                    Content = (string)service.State["SelectedTopicContent"],
                    ImageLink = (string)service.State["SelectedTopicImageLink"],
                    PosterName = (string)service.State["SelectedTopicPosterName"],
                    PostTime = (DateTime?)service.State["SelectedTopicPostTime"],
                    ReplyLink = (string)service.State["SelectedTopicReplyLink"],
                    ThumbImageLink = (string)service.State["SelectedTopicThumbImageLink"],
                    Id = (string)service.State["SelectedTopicId"]
                };

                if (service.State.ContainsKey("SelectedTopicPony"))
                {
                    this.Context.Topic.AdditionalFields["pony"] = (string)service.State["SelectedTopicPony"];
                }

                if (service.State.ContainsKey("SelectedBoardAnticaptcha"))
                {
                    this.Context.Topic.AdditionalFields["anticaptcha"] = (string)service.State["SelectedBoardAnticaptcha"];
                }

                if (service.State.ContainsKey("SelectedBoardUuid"))
                {
                    this.Context.Topic.AdditionalFields["uuid"] = (string)service.State["SelectedBoardUuid"];
                }
            }

            if (service.State.ContainsKey("SelectedMessageContent"))
            {
                this.Context.Message = new Message
                {
                    Content = (string)service.State["SelectedMessageContent"],
                    Id = (string)service.State["SelectedMessageId"],
                    ImageLink = (string)service.State["SelectedMessageImageLink"],
                    PosterName = (string)service.State["SelectedMessagePosterName"],
                    PostTime = (DateTime?)service.State["SelectedMessagePostTime"],
                    Resto = (string)service.State["SelectedMessageResto"],
                    ThumbImageLink = (string)service.State["SelectedMessageThumbImageLink"]
                };
            }

            this.CurrentPage = (int)service.State["CurrentPage"];
            this.ReplyComment = (string)service.State["ReplyComment"];
            this.ReplyMail = (string)service.State["ReplyMail"];
            this.ReplyName = (string)service.State["ReplyName"];
            this.ReplyPassword = (string)service.State["ReplyPassword"];
            this.ReplySubject = (string)service.State["ReplySubject"];
        }

        protected static T GetSetting<T>(string settingName, T defaultValue)
        {
            T result;

            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(settingName, out result))
            {
                return result;
            }

            return defaultValue;
        }

        protected static void SetSetting<T>(string settingName, T value)
        {
            IsolatedStorageSettings.ApplicationSettings[settingName] = value;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;

            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
