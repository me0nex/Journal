using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Journal
{
    public partial class MainWindow : Window
    {
        private readonly string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private string allJournalFile => Path.Combine(system32Path, "AllTheJournal.txt");
        private string clientFile => Path.Combine(system32Path, "client.txt");
        private string deletedExesFile => Path.Combine(system32Path, "DeletedExes.txt");
        private readonly DispatcherTimer statusTimer;

        private string selectedProcessNameUpper = "";
        private bool isUserMode = false;

        private string searchText = "";
        private Border searchOverlay;
        private TextBox searchTextBox;
        private List<JournalEntry> currentJournalEntries = new List<JournalEntry>();
        private int currentSearchIndex = -1;
        private List<int> searchResults = new List<int>();

        private Border pathSearchOverlay;
        private TextBox pathSearchTextBox;
        private ListBox pathResultsListBox;
        private ProgressBar pathSearchProgressBar;
        private TextBlock pathSearchStatusText;
        private Button pathSearchButton;
        private Button pathSearchCancelButton;
        private CancellationTokenSource pathSearchCancellationTokenSource;

        private readonly SolidColorBrush HighlightColor = new SolidColorBrush(Color.FromRgb(255, 255, 200));
        private readonly SolidColorBrush SearchColor = new SolidColorBrush(Color.FromRgb(144, 238, 144));
        private readonly SolidColorBrush CurrentSearchColor = new SolidColorBrush(Color.FromRgb(173, 216, 230));
        private readonly SolidColorBrush EvenRowColor = Brushes.White;
        private readonly SolidColorBrush OddRowColor = new SolidColorBrush(Color.FromRgb(236, 240, 241));

        private readonly Dictionary<string, string> reasonCodes = new Dictionary<string, string>
        {
            ["0x00000000"] = "Неизвестно",
            ["0x00000001"] = "Переименование",
            ["0x00000002"] = "Создание",
            ["0x00000003"] = "Переименование + Создание",
            ["0x00000004"] = "Изменение данных",
            ["0x00000005"] = "Переименование + Изменение",
            ["0x00000006"] = "Создание + Изменение",
            ["0x00000007"] = "Переименование + Создание + Изменение",
            ["0x00000008"] = "Изменение атрибутов",
            ["0x00000009"] = "Переименование + Изменение атрибутов",
            ["0x0000000A"] = "Создание + Изменение атрибутов",
            ["0x0000000B"] = "Переименование + Создание + Изменение атрибутов",
            ["0x0000000C"] = "Изменение данных + Изменение атрибутов",
            ["0x00000010"] = "Изменение безопасности",
            ["0x00000020"] = "Изменение владельца",
            ["0x00000040"] = "Переименование потока",
            ["0x00000080"] = "Добавление потока",
            ["0x00000100"] = "Создание файла",
            ["0x00000200"] = "Удаление",
            ["0x00000400"] = "Удаление потока",
            ["0x00000800"] = "Расширение",
            ["0x00001000"] = "Сжатие",
            ["0x00002000"] = "Шифрование",
            ["0x00004000"] = "Индексирование",
            ["0x00008000"] = "Закрытие",
            ["0x80000000"] = "Закрытие",
            ["0x80000001"] = "Переименование и закрытие",
            ["0x80000002"] = "Создание и закрытие",
            ["0x80000003"] = "Переименование + Создание и закрытие",
            ["0x80000004"] = "Изменение данных и закрытие",
            ["0x80000005"] = "Переименование + Изменение и закрытие",
            ["0x80000006"] = "Создание + Изменение и закрытие",
            ["0x80000007"] = "Переименование + Создание + Изменение и закрытие",
            ["0x80000008"] = "Изменение атрибутов и закрытие",
            ["0x80000009"] = "Переименование + Изменение атрибутов и закрытие",
            ["0x8000000A"] = "Создание + Изменение атрибутов и закрытие",
            ["0x8000000B"] = "Переименование + Создание + Изменение атрибутов и закрытие",
            ["0x8000000C"] = "Изменение данных + Изменение атрибутов и закрытие",
            ["0x80000010"] = "Изменение безопасности и закрытие",
            ["0x80000020"] = "Изменение владельца и закрытие",
            ["0x80000040"] = "Переименование потока и закрытие",
            ["0x80000080"] = "Добавление потока и закрытие",
            ["0x80000100"] = "Создание файла и закрытие",
            ["0x80000200"] = "Удаление и закрытие",
            ["0x80000400"] = "Удаление потока и закрытие",
            ["0x80000800"] = "Расширение и закрытие",
            ["0x80001000"] = "Сжатие и закрытие",
            ["0x80002000"] = "Шифрование и закрытие",
            ["0x80004000"] = "Индексирование и закрытие",
            ["0x0000A000"] = "Архивация",
            ["0x0000A001"] = "Переименование с архивацией",
            ["0x0000A002"] = "Создание с архивацией",
            ["0x0000A004"] = "Изменение с архивацией",
            ["0x0000A006"] = "Создание + Изменение с архивацией",
            ["0x0000A008"] = "Изменение атрибутов с архивацией",
            ["0x0000A800"] = "Системная операция",
            ["0x0000A801"] = "Переименование (системное)",
            ["0x0000A802"] = "Создание (системное)",
            ["0x0000A804"] = "Изменение (системное)",
            ["0x0000A806"] = "Создание + Изменение (системное)",
            ["0x8000A000"] = "Архивация и закрытие",
            ["0x8000A001"] = "Переименование с архивацией и закрытие",
            ["0x8000A002"] = "Создание с архивацией и закрытие",
            ["0x8000A004"] = "Изменение с архивацией и закрытие",
            ["0x8000A006"] = "Создание + Изменение с архивацией и закрытие",
            ["0x8000A008"] = "Изменение атрибутов с архивацией и закрытие",
            ["0x8000A800"] = "Системная операция и закрытие",
            ["0x8000A801"] = "Переименование (системное) и закрытие",
            ["0x8000A802"] = "Создание (системное) и закрытие",
            ["0x8000A804"] = "Изменение (системное) и закрытие",
            ["0x8000A806"] = "Создание + Изменение (системное) и закрытие",
            ["0x80000102"] = "Создание файла и закрытие",
            ["0x80000202"] = "Удаление и закрытие",
            ["0x80000106"] = "Изменение файла и закрытие",
            ["0x00000102"] = "Создание файла",
            ["0x00000202"] = "Удаление",
            ["0x00002002"] = "Создание (с шифрованием)",
            ["0x00002004"] = "Изменение (с шифрованием)",
            ["0x00002006"] = "Создание + Изменение (с шифрованием)",
            ["0x80002002"] = "Создание и закрытие (с шифрованием)",
            ["0x80002004"] = "Изменение и закрытие (с шифрованием)",
            ["0x80002006"] = "Создание + Изменение и закрытие (с шифрованием)",
            ["0x8000A800"] = "Системная операция и закрытие",
            ["0x0000A800"] = "Системная операция",
            ["0x8000A806"] = "Создание + Изменение (системное) и закрытие",
            ["0x0000A806"] = "Создание + Изменение (системное)"
        };

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                CheckUserMode();

                statusTimer = new DispatcherTimer();
                statusTimer.Interval = TimeSpan.FromSeconds(3);
                statusTimer.Tick += (s, e) => {
                    txtStatus.Text = "Готов к работе";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    statusTimer.Stop();
                };

                dgJournal.SelectionChanged += DgJournal_SelectionChanged;
                dgJournal.LoadingRow += DgJournal_LoadingRow;
                this.PreviewKeyDown += MainWindow_PreviewKeyDown;

                CreateSearchOverlay();
                CreatePathSearchOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreatePathSearchOverlay()
        {
            pathSearchOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 40, 40, 40)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 700,
                MaxHeight = 600,
                Visibility = Visibility.Collapsed,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerText = new TextBlock
            {
                Text = "ПОИСК ФАЙЛА НА ВСЕМ КОМПЬЮТЕРЕ",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerText, 0);

            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            var searchLabel = new TextBlock
            {
                Text = "Имя файла:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            pathSearchTextBox = new TextBox
            {
                Width = 350,
                Padding = new Thickness(5),
                Background = Brushes.White,
                Foreground = Brushes.Black
            };
            pathSearchTextBox.KeyDown += PathSearchTextBox_KeyDown;

            pathSearchButton = new Button
            {
                Content = "Найти",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                Foreground = Brushes.White,
                MinWidth = 80
            };
            pathSearchButton.Click += PathSearchButton_Click;

            pathSearchCancelButton = new Button
            {
                Content = "Отмена",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                Foreground = Brushes.White,
                MinWidth = 80,
                Visibility = Visibility.Collapsed
            };
            pathSearchCancelButton.Click += PathSearchCancelButton_Click;

            searchPanel.Children.Add(searchLabel);
            searchPanel.Children.Add(pathSearchTextBox);
            searchPanel.Children.Add(pathSearchButton);
            searchPanel.Children.Add(pathSearchCancelButton);

            Grid.SetRow(searchPanel, 1);

            var progressPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            pathSearchProgressBar = new ProgressBar
            {
                Height = 20,
                IsIndeterminate = true,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 5)
            };

            pathSearchStatusText = new TextBlock
            {
                Foreground = Brushes.White,
                Text = "",
                TextWrapping = TextWrapping.Wrap
            };

            progressPanel.Children.Add(pathSearchProgressBar);
            progressPanel.Children.Add(pathSearchStatusText);

            Grid.SetRow(progressPanel, 2);

            pathResultsListBox = new ListBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                Background = Brushes.White,
                MaxHeight = 300
            };

            var itemTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            var fileNameFactory = new FrameworkElementFactory(typeof(TextBlock));
            fileNameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            fileNameFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("FileName"));

            var pathFactory = new FrameworkElementFactory(typeof(TextBlock));
            pathFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
            pathFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            pathFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("FullPath"));

            stackPanelFactory.AppendChild(fileNameFactory);
            stackPanelFactory.AppendChild(pathFactory);

            itemTemplate.VisualTree = stackPanelFactory;
            pathResultsListBox.ItemTemplate = itemTemplate;

            pathResultsListBox.SelectionChanged += PathResultsListBox_SelectionChanged;
            pathResultsListBox.MouseDoubleClick += PathResultsListBox_MouseDoubleClick;
            Grid.SetRow(pathResultsListBox, 3);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var openFolderButton = new Button
            {
                Content = "Открыть папку",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                Foreground = Brushes.White
            };
            openFolderButton.Click += OpenFolderButton_Click;

            var copyPathButton = new Button
            {
                Content = "Копировать путь",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(155, 89, 182)),
                Foreground = Brushes.White
            };
            copyPathButton.Click += CopyPathButton_Click;

            var closeButton = new Button
            {
                Content = "Закрыть",
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                Foreground = Brushes.White
            };
            closeButton.Click += (s, e) => HidePathSearch();

            buttonPanel.Children.Add(openFolderButton);
            buttonPanel.Children.Add(copyPathButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 4);

            grid.Children.Add(headerText);
            grid.Children.Add(searchPanel);
            grid.Children.Add(progressPanel);
            grid.Children.Add(pathResultsListBox);
            grid.Children.Add(buttonPanel);

            pathSearchOverlay.Child = grid;

            ((Grid)this.Content).Children.Add(pathSearchOverlay);
        }

        private void ShowPathSearch()
        {
            pathSearchOverlay.Visibility = Visibility.Visible;
            pathSearchTextBox.Focus();
            pathSearchTextBox.SelectAll();

            if (dgJournal.SelectedItem is JournalEntry selectedEntry)
            {
                string fileName = GetFileNameOnly(selectedEntry.FileName);
                if (!string.IsNullOrEmpty(fileName))
                {
                    pathSearchTextBox.Text = fileName;
                    pathSearchTextBox.CaretIndex = fileName.Length;
                }
            }

            pathResultsListBox.ItemsSource = null;
            pathSearchStatusText.Text = "";
        }

        private void HidePathSearch()
        {
            if (pathSearchCancellationTokenSource != null)
            {
                pathSearchCancellationTokenSource.Cancel();
                pathSearchCancellationTokenSource = null;
            }

            pathSearchOverlay.Visibility = Visibility.Collapsed;
            pathSearchTextBox.Text = "";
            pathResultsListBox.ItemsSource = null;
            pathSearchProgressBar.Visibility = Visibility.Collapsed;
            pathSearchStatusText.Text = "";
            pathSearchButton.Visibility = Visibility.Visible;
            pathSearchCancelButton.Visibility = Visibility.Collapsed;
        }

        private void PathSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchName = pathSearchTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(searchName))
                {
                    StartPathSearch(searchName);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HidePathSearch();
                e.Handled = true;
            }
        }

        private void PathSearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchName = pathSearchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(searchName))
            {
                StartPathSearch(searchName);
            }
        }

        private void PathSearchCancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (pathSearchCancellationTokenSource != null)
            {
                pathSearchCancellationTokenSource.Cancel();
            }
        }

        private async void StartPathSearch(string searchName)
        {
            if (pathSearchCancellationTokenSource != null)
            {
                pathSearchCancellationTokenSource.Cancel();
                pathSearchCancellationTokenSource = null;
            }

            pathResultsListBox.ItemsSource = null;

            pathSearchProgressBar.Visibility = Visibility.Visible;
            pathSearchStatusText.Text = "Поиск...";
            pathSearchButton.Visibility = Visibility.Collapsed;
            pathSearchCancelButton.Visibility = Visibility.Visible;

            pathSearchCancellationTokenSource = new CancellationTokenSource();
            var token = pathSearchCancellationTokenSource.Token;

            try
            {
                var results = await Task.Run(() => SearchFileOnComputer(searchName, token), token);

                await Dispatcher.InvokeAsync(() =>
                {
                    pathResultsListBox.ItemsSource = results;
                    pathSearchProgressBar.Visibility = Visibility.Collapsed;
                    pathSearchStatusText.Text = results.Count > 0
                        ? $"Найдено {results.Count} файлов"
                        : "Файлы не найдены";
                    pathSearchButton.Visibility = Visibility.Visible;
                    pathSearchCancelButton.Visibility = Visibility.Collapsed;

                    if (results.Count > 0)
                    {
                        pathResultsListBox.SelectedIndex = 0;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    pathSearchProgressBar.Visibility = Visibility.Collapsed;
                    pathSearchStatusText.Text = "Поиск отменен";
                    pathSearchButton.Visibility = Visibility.Visible;
                    pathSearchCancelButton.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    pathSearchProgressBar.Visibility = Visibility.Collapsed;
                    pathSearchStatusText.Text = $"Ошибка: {ex.Message}";
                    pathSearchButton.Visibility = Visibility.Visible;
                    pathSearchCancelButton.Visibility = Visibility.Collapsed;
                });
            }
            finally
            {
                pathSearchCancellationTokenSource = null;
            }
        }

        private List<FilePathInfo> SearchFileOnComputer(string searchName, CancellationToken token)
        {
            var results = new List<FilePathInfo>();
            var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string searchNameUpper = searchName.ToUpper();

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType != DriveType.CDRom)
                .Select(d => d.RootDirectory.FullName)
                .ToList();

            int drivesScanned = 0;

            foreach (var drive in drives)
            {
                if (token.IsCancellationRequested)
                    break;

                Dispatcher.Invoke(() =>
                {
                    pathSearchStatusText.Text = $"Поиск на диске {drive}...";
                });

                try
                {
                    SearchDirectory(drive, searchNameUpper, results, uniquePaths, token);
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }

                drivesScanned++;
            }

            return results.OrderBy(x => x.FileName).ThenBy(x => x.FullPath).ToList();
        }

        private void SearchDirectory(string directory, string searchNameUpper, List<FilePathInfo> results, HashSet<string> uniquePaths, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            try
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string fileNameUpper = fileName.ToUpper();

                        if (fileNameUpper.Contains(searchNameUpper))
                        {
                            if (!uniquePaths.Contains(file))
                            {
                                uniquePaths.Add(file);

                                results.Add(new FilePathInfo
                                {
                                    FileName = fileName,
                                    FullPath = file,
                                    DirectoryPath = Path.GetDirectoryName(file)
                                });

                                if (results.Count >= 1000)
                                    return;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        SearchDirectory(subDir, searchNameUpper, results, uniquePaths, token);

                        if (results.Count >= 1000)
                            return;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private void PathResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pathResultsListBox.SelectedItem is FilePathInfo selectedFile)
            {
                txtStatus.Text = $"Выбран: {selectedFile.FullPath}";
                txtStatus.Foreground = Brushes.Blue;
            }
        }

        private void PathResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (pathResultsListBox.SelectedItem is FilePathInfo selectedFile)
            {
                try
                {
                    string directory = Path.GetDirectoryName(selectedFile.FullPath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        Process.Start("explorer.exe", $"/select,\"{selectedFile.FullPath}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (pathResultsListBox.SelectedItem is FilePathInfo selectedFile)
            {
                try
                {
                    string directory = Path.GetDirectoryName(selectedFile.FullPath);

                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        Process.Start("explorer.exe", $"\"{directory}\"");
                    }
                    else
                    {
                        MessageBox.Show($"Папка не существует: {directory}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии папки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (pathResultsListBox.SelectedItem is FilePathInfo selectedFile)
            {
                try
                {
                    Clipboard.SetText(selectedFile.FullPath);
                    txtStatus.Text = $"Путь скопирован: {selectedFile.FullPath}";
                    txtStatus.Foreground = Brushes.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при копировании: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateSearchOverlay()
        {
            searchOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 60, 20, 0),
                Visibility = Visibility.Collapsed,
                Width = 300
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            searchTextBox = new TextBox
            {
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Padding = new Thickness(5),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Margin = new Thickness(0, 0, 5, 0)
            };
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            searchTextBox.KeyDown += SearchTextBox_KeyDown;

            var nextButton = new Button
            {
                Content = "▼",
                Width = 30,
                Height = 25,
                Margin = new Thickness(2),
                ToolTip = "Найти далее (F3)"
            };
            nextButton.Click += (s, e) => FindNext();

            var prevButton = new Button
            {
                Content = "▲",
                Width = 30,
                Height = 25,
                Margin = new Thickness(2),
                ToolTip = "Найти ранее (Shift+F3)"
            };
            prevButton.Click += (s, e) => FindPrevious();

            var closeButton = new Button
            {
                Content = "✕",
                Width = 25,
                Height = 25,
                Margin = new Thickness(2, 2, 0, 2),
                Background = Brushes.Red,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            closeButton.Click += (s, e) => HideSearch();

            Grid.SetColumn(searchTextBox, 0);
            Grid.SetColumn(prevButton, 1);
            Grid.SetColumn(nextButton, 2);
            Grid.SetColumn(closeButton, 3);

            grid.Children.Add(searchTextBox);
            grid.Children.Add(prevButton);
            grid.Children.Add(nextButton);
            grid.Children.Add(closeButton);

            searchOverlay.Child = grid;

            ((Grid)this.Content).Children.Add(searchOverlay);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowSearch();
                e.Handled = true;
            }
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowPathSearch();
                e.Handled = true;
            }
            else if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.None)
            {
                FindNext();
                e.Handled = true;
            }
            else if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                FindPrevious();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (searchOverlay.Visibility == Visibility.Visible)
                {
                    HideSearch();
                    e.Handled = true;
                }
                else if (pathSearchOverlay.Visibility == Visibility.Visible)
                {
                    HidePathSearch();
                    e.Handled = true;
                }
            }
        }

        private void ShowSearch()
        {
            searchOverlay.Visibility = Visibility.Visible;
            searchTextBox.Focus();
            searchTextBox.SelectAll();

            if (dgJournal.SelectedItem is JournalEntry selectedEntry)
            {
                string fileName = GetFileNameOnly(selectedEntry.FileName);
                if (!string.IsNullOrEmpty(fileName))
                {
                    searchTextBox.Text = fileName;
                    searchTextBox.CaretIndex = fileName.Length;
                }
            }
        }

        private void HideSearch()
        {
            searchOverlay.Visibility = Visibility.Collapsed;
            searchTextBox.Text = "";
            searchText = "";
            ClearSearchHighlights();
            this.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = searchTextBox.Text.Trim();
            PerformSearch();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideSearch();
                e.Handled = true;
            }
        }

        private void PerformSearch()
        {
            searchResults.Clear();
            currentSearchIndex = -1;

            if (string.IsNullOrEmpty(searchText) || currentJournalEntries.Count == 0)
            {
                RefreshAllRows();
                UpdateSearchStatus();
                return;
            }

            string searchTextUpper = searchText.ToUpper();

            for (int i = 0; i < currentJournalEntries.Count; i++)
            {
                var entry = currentJournalEntries[i];
                string fileName = GetFileNameOnly(entry.FileName);
                string fileNameUpper = fileName.ToUpper();

                if (!string.IsNullOrEmpty(fileNameUpper) && fileNameUpper.Contains(searchTextUpper))
                {
                    searchResults.Add(i);
                }
            }

            if (searchResults.Count > 0)
            {
                currentSearchIndex = 0;
            }

            RefreshAllRows();
            UpdateSearchStatus();

            if (searchResults.Count > 0)
            {
                ScrollToSearchResult(currentSearchIndex);
            }
        }

        private void ClearSearchHighlights()
        {
            searchResults.Clear();
            currentSearchIndex = -1;
            RefreshAllRows();
        }

        private void FindNext()
        {
            if (searchResults.Count == 0) return;

            currentSearchIndex = (currentSearchIndex + 1) % searchResults.Count;
            RefreshAllRows();
            ScrollToSearchResult(currentSearchIndex);
            UpdateSearchStatus();
        }

        private void FindPrevious()
        {
            if (searchResults.Count == 0) return;

            currentSearchIndex--;
            if (currentSearchIndex < 0)
            {
                currentSearchIndex = searchResults.Count - 1;
            }
            RefreshAllRows();
            ScrollToSearchResult(currentSearchIndex);
            UpdateSearchStatus();
        }

        private void ScrollToSearchResult(int index)
        {
            if (index >= 0 && index < searchResults.Count)
            {
                int itemIndex = searchResults[index];
                if (itemIndex >= 0 && itemIndex < currentJournalEntries.Count)
                {
                    dgJournal.ScrollIntoView(currentJournalEntries[itemIndex]);
                    dgJournal.SelectedItem = currentJournalEntries[itemIndex];
                }
            }
        }

        private void UpdateSearchStatus()
        {
            if (searchResults.Count > 0)
            {
                txtStatus.Text = $"Поиск: найдено {searchResults.Count} совпадений (текущее: {currentSearchIndex + 1})";
                txtStatus.Foreground = Brushes.Blue;
            }
            else if (!string.IsNullOrEmpty(searchText))
            {
                txtStatus.Text = $"Поиск: совпадений не найдено";
                txtStatus.Foreground = Brushes.Red;
            }
            else
            {
                txtStatus.Text = "Готов к работе";
                txtStatus.Foreground = Brushes.Green;
            }
        }

        private void CheckUserMode()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isUserMode = !principal.IsInRole(WindowsBuiltInRole.Administrator);

                if (isUserMode)
                {
                    ApplyUserModeStyle();

                    txtStatus.Text = "Режим обычного пользователя (только просмотр)";
                    txtStatus.Foreground = Brushes.Orange;

                    btnUpdateAll.IsEnabled = false;
                    btnUpdateExe.IsEnabled = false;
                    btnUpdateDeleted.IsEnabled = false;

                    txtUserModeInfo.Text = "режим: только просмотр";
                    txtUserModeInfo.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                isUserMode = true;
                ApplyUserModeStyle();
            }
        }

        private void ApplyUserModeStyle()
        {
            this.Title = this.Title.ToLower();

            var headerText = FindName("HeaderText") as TextBlock;
            if (headerText != null)
            {
                headerText.Text = headerText.Text.ToLower();
                headerText.FontSize = 18;
                headerText.Opacity = 0.8;
            }

            ChangeTextToLower(this);
        }

        private void ChangeTextToLower(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBlock textBlock)
                {
                    textBlock.Text = textBlock.Text.ToLower();
                }
                else if (child is Button button)
                {
                    button.Content = button.Content.ToString().ToLower();
                }
                else if (child is RadioButton radioButton)
                {
                    radioButton.Content = radioButton.Content.ToString().ToLower();
                }
                else if (child is Label label)
                {
                    label.Content = label.Content.ToString().ToLower();
                }

                if (child is DependencyObject dependencyObject && VisualTreeHelper.GetChildrenCount(dependencyObject) > 0)
                {
                    ChangeTextToLower(child);
                }
            }
        }

        private void DgJournal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgJournal.SelectedItem is JournalEntry selectedEntry)
                {
                    string fileName = GetFileNameOnly(selectedEntry.FileName);
                    selectedProcessNameUpper = fileName.ToUpper();

                    RefreshAllRows();

                    int matchCount = CountMatchingProcesses(fileName);
                    txtStatus.Text = $"Выбран процесс: {fileName} (найдено совпадений: {matchCount})";
                    txtStatus.Foreground = Brushes.Blue;
                }
                else
                {
                    selectedProcessNameUpper = "";
                    RefreshAllRows();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при выборе: {ex.Message}");
            }
        }

        private void DgJournal_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                int index = e.Row.GetIndex();
                if (index >= 0 && index < currentJournalEntries.Count)
                {
                    ApplyRowStyle(e.Row, index);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в LoadingRow: {ex.Message}");
            }
        }

        private void ApplyRowStyle(DataGridRow row, int index)
        {
            if (index < 0 || index >= currentJournalEntries.Count) return;

            var entry = currentJournalEntries[index];

            string currentFileName = GetFileNameOnly(entry.FileName);
            string currentFileNameUpper = currentFileName.ToUpper();

            bool isSelectedProcess = !string.IsNullOrEmpty(selectedProcessNameUpper) &&
                                    !string.IsNullOrEmpty(currentFileNameUpper) &&
                                    currentFileNameUpper == selectedProcessNameUpper;

            bool isSearchResult = searchResults.Contains(index);
            bool isCurrentSearch = isSearchResult && currentSearchIndex >= 0 && index == searchResults[currentSearchIndex];

            if (isCurrentSearch)
            {
                row.Background = CurrentSearchColor;
                row.FontWeight = FontWeights.Bold;
            }
            else if (isSelectedProcess)
            {
                row.Background = HighlightColor;
                row.FontWeight = FontWeights.Bold;
            }
            else if (isSearchResult)
            {
                row.Background = SearchColor;
                row.FontWeight = FontWeights.Bold;
            }
            else
            {
                row.Background = index % 2 == 0 ? EvenRowColor : OddRowColor;
                row.FontWeight = FontWeights.Normal;
            }
        }

        private void RefreshAllRows()
        {
            if (dgJournal.ItemsSource == null) return;

            for (int i = 0; i < currentJournalEntries.Count; i++)
            {
                var row = dgJournal.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    ApplyRowStyle(row, i);
                }
            }
        }

        private string GetFileNameOnly(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return "";

            try
            {
                string cleaned = fullPath.Trim('"');
                string fileName = Path.GetFileName(cleaned);

                if (string.IsNullOrEmpty(fileName))
                {
                    return cleaned;
                }

                return fileName;
            }
            catch
            {
                return fullPath.Trim('"');
            }
        }

        private int CountMatchingProcesses(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;

            string processNameUpper = processName.ToUpper();

            try
            {
                int count = 0;
                foreach (JournalEntry entry in currentJournalEntries)
                {
                    string currentName = GetFileNameOnly(entry.FileName);
                    string currentNameUpper = currentName.ToUpper();

                    if (!string.IsNullOrEmpty(currentNameUpper) && currentNameUpper == processNameUpper)
                    {
                        count++;
                    }
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            dgJournal.SelectedItem = null;
            selectedProcessNameUpper = "";
            searchResults.Clear();
            currentSearchIndex = -1;
            searchText = "";
            RefreshAllRows();
            txtStatus.Text = "Выделение сброшено";
            txtStatus.Foreground = Brushes.Green;
        }

        private void BtnPathSearch_Click(object sender, RoutedEventArgs e)
        {
            ShowPathSearch();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadJournalDataAsync("AllTheJournal.txt");
        }

        private async void RbJournal_Checked(object sender, RoutedEventArgs e)
        {
            await LoadJournalDataAsync(GetCurrentJournalFile());
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadJournalDataAsync(GetCurrentJournalFile());
        }

        private async void BtnUpdateAll_Click(object sender, RoutedEventArgs e)
        {
            if (isUserMode)
            {
                MessageBox.Show("В режиме обычного пользователя обновление журналов недоступно.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string command = $"fsutil usn readjournal C: csv > \"{allJournalFile}\"";
            await UpdateJournalAsync("AllTheJournal.txt", command);
        }

        private async void BtnUpdateExe_Click(object sender, RoutedEventArgs e)
        {
            if (isUserMode)
            {
                MessageBox.Show("В режиме обычного пользователя обновление журналов недоступно.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string command = $"fsutil usn readjournal C: csv | findstr /i \".exe\" > \"{clientFile}\"";
            await UpdateJournalAsync("client.txt", command);
        }

        private async void BtnUpdateDeleted_Click(object sender, RoutedEventArgs e)
        {
            if (isUserMode)
            {
                MessageBox.Show("В режиме обычного пользователя обновление журналов недоступно.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string command = $"fsutil usn readjournal C: csv | findstr /i \".exe\" | findstr \"0x80000200\" > \"{deletedExesFile}\"";
            await UpdateJournalAsync("DeletedExes.txt", command);
        }

        private async Task UpdateJournalAsync(string journalName, string command)
        {
            try
            {
                txtStatus.Text = $"Обновление {journalName}...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                var success = await Task.Run(() =>
                {
                    try
                    {
                        string testFile = Path.Combine(system32Path, "test_write.tmp");
                        try
                        {
                            File.WriteAllText(testFile, "test");
                            File.Delete(testFile);
                        }
                        catch
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show("Нет прав на запись в папку System32.\nЗапустите программу от имени администратора.",
                                "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {command}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true
                        };

                        using (Process process = Process.Start(psi))
                        {
                            if (process != null)
                            {
                                string output = process.StandardOutput.ReadToEnd();
                                string error = process.StandardError.ReadToEnd();
                                process.WaitForExit(30000);

                                if (!string.IsNullOrEmpty(error))
                                {
                                    Dispatcher.Invoke(() =>
                                        MessageBox.Show($"Ошибка команды: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error));
                                }

                                return process.ExitCode == 0 && File.Exists(Path.Combine(system32Path, journalName));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Ошибка выполнения команды: {ex.Message}"));
                    }
                    return false;
                });

                if (success)
                {
                    txtStatus.Text = $"{journalName} успешно обновлен";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    MessageBox.Show($"Журнал {journalName} успешно создан в папке {system32Path}",
                                   "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadJournalDataAsync(journalName);
                }
                else
                {
                    txtStatus.Text = $"Ошибка при обновлении {journalName}";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;

                    string filePath = Path.Combine(system32Path, journalName);
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new System.IO.FileInfo(filePath);
                        if (fileInfo.Length > 0)
                        {
                            txtStatus.Text = $"Файл существует, но возможно неполный ({fileInfo.Length} байт)";
                            await LoadJournalDataAsync(journalName);
                        }
                    }
                }

                statusTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCurrentJournalFile()
        {
            if (rbAllJournal.IsChecked == true)
                return "AllTheJournal.txt";
            else if (rbExeJournal.IsChecked == true)
                return "client.txt";
            else
                return "DeletedExes.txt";
        }

        private async Task LoadJournalDataAsync(string journalName)
        {
            try
            {
                txtStatus.Text = $"Загрузка {journalName}...";
                dgJournal.ItemsSource = null;
                currentJournalEntries.Clear();

                string filePath = Path.Combine(system32Path, journalName);

                if (!File.Exists(filePath))
                {
                    txtFileInfo.Text = $"Журнал: {journalName} (файл не найден)";
                    txtCountInfo.Text = "Записей: 0";
                    txtStatus.Text = "Файл не найден. Нажмите кнопку обновления.";
                    return;
                }

                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    txtFileInfo.Text = $"Журнал: {journalName} (пустой файл)";
                    txtCountInfo.Text = "Записей: 0";
                    txtStatus.Text = "Файл пуст. Обновите журнал.";
                    return;
                }

                var lines = await Task.Run(() => File.ReadAllLines(filePath, Encoding.Default));

                txtStatus.Text = $"Обработка {lines.Length} записей...";

                var journalEntries = new List<JournalEntry>();

                await Task.Run(() =>
                {
                    int count = 0;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.StartsWith("USN", StringComparison.OrdinalIgnoreCase)) continue;

                        var parts = SplitCsvLine(line);
                        if (parts.Length < 6) continue;

                        string fileName = CleanFileName(parts.Length > 1 ? parts[1] : "");
                        string reasonCode = parts.Length > 3 ? parts[3].Trim() : "";

                        var entry = new JournalEntry
                        {
                            USN = parts[0]?.Trim('"') ?? "",
                            FileName = fileName,
                            Reason = DecodeReason(reasonCode),
                            ReasonCode = reasonCode,
                            Timestamp = ParseDate(parts.Length > 5 ? parts[5] : "")
                        };

                        lock (journalEntries)
                        {
                            journalEntries.Add(entry);
                        }

                        count++;
                        if (count % 1000 == 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                txtStatus.Text = $"Обработано {count} записей...";
                            });
                        }
                    }
                });

                journalEntries = journalEntries.OrderByDescending(x => x.Timestamp).ToList();
                currentJournalEntries = journalEntries;

                dgJournal.ItemsSource = journalEntries;
                txtFileInfo.Text = $"Журнал: {journalName}";
                txtCountInfo.Text = $"Записей: {journalEntries.Count}";
                txtStatus.Text = $"Загружено {journalEntries.Count} записей";

                selectedProcessNameUpper = "";
                searchResults.Clear();
                currentSearchIndex = -1;
                searchText = "";

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RefreshAllRows();
                }), DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке журнала: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string DecodeReason(string reasonCode)
        {
            if (string.IsNullOrEmpty(reasonCode)) return "Неизвестно";

            reasonCode = reasonCode.Trim();

            if (reasonCodes.TryGetValue(reasonCode, out string? description))
            {
                return description;
            }

            if (reasonCode.Contains("|"))
            {
                var codes = reasonCode.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var descriptions = new List<string>();

                foreach (var code in codes)
                {
                    string trimmedCode = code.Trim();
                    if (reasonCodes.TryGetValue(trimmedCode, out string? desc))
                    {
                        descriptions.Add(desc);
                    }
                    else
                    {
                        string maskedDesc = DecodeByMask(trimmedCode);
                        descriptions.Add(maskedDesc);
                    }
                }

                return string.Join(" + ", descriptions.Distinct());
            }

            return DecodeByMask(reasonCode);
        }

        private string DecodeByMask(string code)
        {
            foreach (var kvp in reasonCodes)
            {
                if (kvp.Key.Length >= 4 && code.Length >= 4)
                {
                    string keyPrefix = kvp.Key.Substring(0, Math.Min(6, kvp.Key.Length));
                    string codePrefix = code.Substring(0, Math.Min(6, code.Length));

                    if (keyPrefix == codePrefix)
                    {
                        return kvp.Value + " (вариация)";
                    }
                }
            }

            try
            {
                if (code.StartsWith("0x"))
                {
                    string hexValue = code.Substring(2);
                    if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int value))
                    {
                        List<string> operations = new List<string>();

                        if ((value & 0x01) != 0) operations.Add("Переименование");
                        if ((value & 0x02) != 0) operations.Add("Создание");
                        if ((value & 0x04) != 0) operations.Add("Изменение");
                        if ((value & 0x08) != 0) operations.Add("Изменение атрибутов");
                        if ((value & 0x40) != 0) operations.Add("Переименование потока");
                        if ((value & 0x80) != 0) operations.Add("Добавление потока");
                        if ((value & 0x100) != 0) operations.Add("Создание файла");
                        if ((value & 0x200) != 0) operations.Add("Удаление");
                        if ((value & 0x800) != 0) operations.Add("Расширение");
                        if ((value & 0x1000) != 0) operations.Add("Сжатие");
                        if ((value & 0x2000) != 0) operations.Add("Шифрование");
                        if ((value & 0x4000) != 0) operations.Add("Индексирование");
                        if ((value & 0x8000) != 0) operations.Add("Закрытие");
                        if ((value & 0x80000000) != 0) operations.Add("Закрытие");

                        if (operations.Count > 0)
                        {
                            return string.Join(" + ", operations.Distinct());
                        }
                    }
                }
            }
            catch { }

            return code;
        }

        private string CleanFileName(string fileName)
        {
            return fileName.Trim('"', ' ', '\t');
        }

        private DateTime ParseDate(string dateStr)
        {
            try
            {
                dateStr = dateStr.Trim('"');
                if (DateTime.TryParse(dateStr, out DateTime result))
                    return result;
            }
            catch { }
            return DateTime.MinValue;
        }

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var current = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }

    public class JournalEntry
    {
        public string USN { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string ReasonCode { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public string FormattedTime => Timestamp != DateTime.MinValue ?
            Timestamp.ToString("dd.MM.yyyy HH:mm:ss") : "Неизвестно";
    }

    public class FilePathInfo
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string DirectoryPath { get; set; } = "";
    }
}