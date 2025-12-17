using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpVectors.Converters;
using SharpVectors.Renderers;
using WindowsInput;
using WindowsInput.Events;
using WindowsInput.Native;

namespace LeagueLogin;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        MasterPassword mp = new MasterPassword();
        if (!(mp.ShowDialog()??false))
        {
            Close();
        }
        App.master_password = mp.MasterPasswordText;
        
        InitializeComponent();
        
        AddAccountButton.Click += (_, _) =>
        {
            Add_Account add_account_dialog = new Add_Account();
            if (add_account_dialog.ShowDialog() ?? false == true)
            {
                App.accountManager.addAccount(add_account_dialog.alias, add_account_dialog.username, add_account_dialog.password);
                rerender();
            }
        };
        ChangeMasterPassword.Click += (_, _) =>
        {
            MasterPassword master_password = new MasterPassword();
            if (master_password.ShowDialog() ?? false)
            {
                App.accountManager.changeMasterPassword(master_password.MasterPasswordText);
                App.master_password = master_password.MasterPasswordText;
            }
        };
        populate_accounts();
    }

    private void populate_accounts()
    {
        foreach (var accountname in App.accountManager.getEntries())
        {
            AddEntry(new EntryData { name = accountname });
        }
    }
    private int children = 0;

    class EntryData
    {
        public string name;
    }
    private void AddEntry(EntryData data)
    {
        LoginGrid.RowDefinitions.Add(new RowDefinition());
        Grid element = new Grid();
        element.RowDefinitions.Add(new RowDefinition());
        element.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        element.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) });
        element.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });
        element.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Button b = new Button
        {
            Width = 190,
            Height = 30,
            Content = new TextBlock { Text = data.name }
        };
        b.Click += (sender, args) => use_acoustic_button_Click(data.name, sender, args);
        Image trashcan_image = new Image
        {
            Source = new BitmapImage(new Uri("/LeagueLogin;component/Resources/delete.png", UriKind.Relative)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
        };
        Button delete_entry_button = new Button
        {
            Content = trashcan_image,
            Width = 30,
            Height = 30,
            Background = Brushes.White,
            BorderBrush = Brushes.White,
        };
        delete_entry_button.Click += (_, _) =>
        {
            App.accountManager.removeAccount(data.name);
            rerender();
        };
        Grid.SetColumn(delete_entry_button, 2);
        element.Children.Add(delete_entry_button);
        Grid.SetColumn(b, 1);
        element.Children.Add(b);
        Grid.SetRow(element, children++);
        LoginGrid.Children.Add(element);
    }

    private void rerender()
    {
        children = 0;
        LoginGrid.RowDefinitions.Clear();
        LoginGrid.Children.Clear();
        populate_accounts();
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] static extern void SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    [StructLayout(LayoutKind.Sequential)] public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    private void use_acoustic_button_Click(string alias, object sender, RoutedEventArgs e)
    {
        var process = Process.GetProcessesByName("Riot Client").FirstOrDefault();
        if (process == null)
        {
            var startInfo = new ProcessStartInfo {
                FileName = @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                WorkingDirectory = @"C:\Riot Games\Riot Client",
                // ensures proper launching with working dir
            };
            Process.Start(startInfo);
            while (process == null)
            {
                Thread.Sleep(100);
                process = Process.GetProcessesByName("Riot Client").FirstOrDefault();
            }
            Thread.Sleep(5000);
        }
        IntPtr hWnd = process.MainWindowHandle;
        AccountManager.AccountManager.AccountData? account_data = App.accountManager.getAccount(alias);
        if (account_data == null) return;
        if (hWnd != IntPtr.Zero)
        {
            GetWindowRect(hWnd, out RECT rect);
            
            int x = rect.Right - 50;
            int y = (rect.Top + rect.Bottom) / 2;
            
            SetForegroundWindow(hWnd);
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
            Simulate.Events()
                .Click(KeyCode.Tab)
                .Click(KeyCode.Tab)
                .Click(KeyCode.Tab)
                .Click(account_data.username)
                .Click(KeyCode.Tab)
                .Click(account_data.password)
                .Click(KeyCode.Enter)
                .Invoke();
        }
    }
}