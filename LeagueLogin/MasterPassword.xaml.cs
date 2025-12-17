using System.Windows;

namespace LeagueLogin;

public partial class MasterPassword : Window
{
    public string MasterPasswordText { get; set; } = "";
    public MasterPassword()
    {
        InitializeComponent();
        ok_button.Click += (_, _) =>
        {
            MasterPasswordText = Password.Password;
            DialogResult = true;
            Close();
        };
        cancel_button.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
    }
}