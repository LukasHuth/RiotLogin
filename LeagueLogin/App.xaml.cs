using System.Configuration;
using System.Data;
using System.Windows;

namespace LeagueLogin;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static string? master_password = null;
    public static AccountManager.AccountManager accountManager = new();

}

