using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI.Views
{
    /// <summary>
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Window
    {
        private readonly DirectoryInfo _dir;
        private const string _ext = ".edb";
        private const string _searchPattern = "*" + _ext;
        public Authorization()
        {
            InitializeComponent();
            _dir = new DirectoryInfo("accs");
			if (!_dir.Exists)
			{
                _dir.Create();
			}
        }

        public static string ShowDialogAuth()
		{
            var window = new Authorization();
            window.ShowDialog();
            return null;
		}

		private void SignIn_Click(object sender, RoutedEventArgs e)
		{
            var name = UsernameAuth.Text;
            var pass = PasswordAuth.Password;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("No username!");
                return;
            }
            else if (string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("No password!");
                return;
            }
			else
			{
                name += _ext;
			}
            foreach (var f in _dir.EnumerateFiles(_searchPattern, SearchOption.TopDirectoryOnly))
            {
                if (f.Name == name)
                {
                    this.file = f;
                    this.password = pass;
                    this.Close();
                    return;
                }
            }
            MessageBox.Show(this, "User not found.");
        }

		private void CreateAcc_Click(object sender, RoutedEventArgs e)
		{
            var name = UsernameAuth.Text;
            var pass = PasswordAuth.Password;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("No username!");
                return;
            }
            else if (string.IsNullOrEmpty(pass))
			{
                MessageBox.Show("No password!");
                return;
			}
            foreach (var f in _dir.GetFiles(_searchPattern, SearchOption.TopDirectoryOnly))
            {
                if (f.Name == (name + _ext))
                {
                    MessageBox.Show(this, "Username already exists!");
                    return;
                }
            }
            this.file = new(System.IO.Path.Combine(_dir.FullName, name+_ext));
            this.password = pass;
            this.Close();
        }

        internal FileInfo file;
        internal string password;
	}
}
