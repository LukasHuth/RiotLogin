using System.Configuration;
using System.Data;
using System.Windows;

namespace LeagueLogin;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static AccountManager accountManager = new AccountManager();
}

