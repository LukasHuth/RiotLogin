using System.Windows;

namespace LeagueLogin;

public partial class Add_Account : Window
{
    public string alias;
    public string username;
    public string password;
    public Add_Account()
    {
        InitializeComponent();
        ok_button.Click += (sender, args) =>
        {
            username = Username.Text;
            password = Password.Password;
            alias = Alias.Text;
            this.DialogResult = true;
            this.Close();
        };
        cancel_button.Click += (sender, args) =>
        {
            this.DialogResult = false;
            this.Close();
        };
    }
}