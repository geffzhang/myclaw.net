using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MyClaw.Uno;

/// <summary>
/// Main chat window for MyClaw
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatMessage> _messages = new();
    private IWebUIService? _webUIService;
    
    // UI Elements
    private ScrollViewer? _messagesScrollViewer;
    private ListView? _messagesList;
    private TextBox? _messageInput;
    private Button? _sendButton;
    private TextBlock? _connectionStatus;
    private TextBlock? _statusText;

    public MainWindow()
    {
        Title = "MyClaw";
        BuildUI();
        
        // Welcome message
        AddMessage("MyClaw", "Hello! I'm MyClaw, your personal AI assistant. How can I help you today?", false);
    }

    private void BuildUI()
    {
        // Main grid
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var header = new Border
        {
            Background = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"],
            Padding = new Thickness(20, 12, 20, 12)
        };
        Grid.SetRow(header, 0);

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        headerGrid.Children.Add(new TextBlock 
        { 
            Text = "🤖", 
            FontSize = 24,
            VerticalAlignment = VerticalAlignment.Center 
        });

        var titleText = new TextBlock
        {
            Text = "MyClaw",
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 1);
        headerGrid.Children.Add(titleText);

        var statusPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        Grid.SetColumn(statusPanel, 2);

        _connectionStatus = new TextBlock
        {
            Text = "●",
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusPanel.Children.Add(_connectionStatus);

        _statusText = new TextBlock
        {
            Text = "Ready",
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusPanel.Children.Add(_statusText);
        headerGrid.Children.Add(statusPanel);

        header.Child = headerGrid;
        grid.Children.Add(header);

        // Messages area
        _messagesScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(20, 10, 20, 10)
        };
        Grid.SetRow(_messagesScrollViewer, 1);

        _messagesList = new ListView
        {
            ItemsSource = _messages
        };
        _messagesScrollViewer.Content = _messagesList;
        grid.Children.Add(_messagesScrollViewer);

        // Input area
        var inputBorder = new Border
        {
            Background = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 224, 224, 224)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 12, 20, 12)
        };
        Grid.SetRow(inputBorder, 2);

        var inputGrid = new Grid();
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _messageInput = new TextBox
        {
            PlaceholderText = "Type a message...",
            FontSize = 14,
            Padding = new Thickness(16, 12, 16, 12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 224, 224, 224)),
            CornerRadius = new CornerRadius(24),
            VerticalAlignment = VerticalAlignment.Center,
            AcceptsReturn = false
        };
        _messageInput.KeyDown += MessageInput_KeyDown;

        _sendButton = new Button
        {
            Content = "Send",
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(24, 12, 24, 12),
            Background = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"],
            Foreground = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(24)
        };
        _sendButton.Click += SendButton_Click;
        Grid.SetColumn(_sendButton, 1);

        inputGrid.Children.Add(_messageInput);
        inputGrid.Children.Add(_sendButton);
        inputBorder.Child = inputGrid;
        grid.Children.Add(inputBorder);

        Content = grid;
    }

    /// <summary>
    /// Sets the web UI service for sending messages
    /// </summary>
    public void SetWebUIService(IWebUIService service)
    {
        _webUIService = service;
        _webUIService.OnMessageReceived += OnMessageReceived;
        UpdateConnectionStatus(true);
    }

    /// <summary>
    /// Adds a message to the chat
    /// </summary>
    public void AddMessage(string sender, string content, bool isUser)
    {
        var message = new ChatMessage
        {
            Sender = sender,
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.Now.ToString("HH:mm")
        };

        _messages.Add(message);
        ScrollToBottom();
    }

    /// <summary>
    /// Receive message from agent
    /// </summary>
    private void OnMessageReceived(object? sender, string message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AddMessage("MyClaw", message, false);
        });
    }

    /// <summary>
    /// Send button click handler
    /// </summary>
    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    /// <summary>
    /// Key down handler for input box
    /// </summary>
    private void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SendMessage();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Send message to agent
    /// </summary>
    private void SendMessage()
    {
        var text = _messageInput?.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        // Add user message to UI
        AddMessage("You", text, true);
        _messageInput!.Text = "";

        // Send to service
        if (_webUIService != null)
        {
            _ = _webUIService.SendMessageAsync(text);
        }
        else
        {
            AddMessage("System", "Not connected to agent service.", false);
        }
    }

    /// <summary>
    /// Scroll messages to bottom
    /// </summary>
    private void ScrollToBottom()
    {
        if (_messagesScrollViewer != null)
        {
            _messagesScrollViewer.ChangeView(null, _messagesScrollViewer.ExtentHeight, null);
        }
    }

    /// <summary>
    /// Update connection status indicator
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_connectionStatus != null)
            {
                _connectionStatus.Foreground = connected
                    ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
                    : new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54));
            }
            if (_statusText != null)
            {
                _statusText.Text = connected ? "Connected" : "Disconnected";
            }
        });
    }
}

/// <summary>
/// Chat message model
/// </summary>
public class ChatMessage
{
    public string Sender { get; set; } = "";
    public string Content { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public bool IsUser { get; set; }
}
