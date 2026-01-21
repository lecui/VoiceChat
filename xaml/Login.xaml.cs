using System;
using System.Collections.Generic;
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
using WpfApp1_client.ViewModels;

namespace WpfApp1_client.xaml
{
    /// <summary>
    /// Логика взаимодействия для Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public string UserName { get; private set; }

        public Login()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Фокус на текстовое поле при загрузке
            UserNameTextBox.Focus();

            // Загружаем сохраненное имя, если есть
       /*     if (!string.IsNullOrEmpty(Properties.Settings.Default.UserName))
            {
                UserNameTextBox.Text = Properties.Settings.Default.UserName;
                UserNameTextBox.SelectAll();
            }*/
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите имя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            UserContext.Initialize(UserNameTextBox.Text);

            // clientModel.Username= UserNameTextBox.Text;


            var mainWindow = new MainWindow();
            mainWindow.Show();


            Close();
        }
    }
}
