using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1_client.xaml;

namespace WpfApp1_client
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Показываем окно логина
            var loginWindow = new Login();
            loginWindow.ShowDialog();

            // После закрытия окна логина, проверяем результат
            if (loginWindow.DialogResult == true && !string.IsNullOrEmpty(loginWindow.UserName))
            {
            //    // Сохраняем имя
              //  Properties.Settings.Default.UserName = loginWindow.UserName;
             //   Properties.Settings.Default.Save();

                // Открываем главное окно
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
               
            }
            else
            {
                // Закрываем приложение, если логин отменен
                Shutdown();
            }
        }
    }
}
