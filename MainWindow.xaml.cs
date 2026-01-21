using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfApp1_client.Models;
using WpfApp1_client.ViewModels;
using WpfApp1_client.xaml;

namespace WpfApp1_client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

        public static Client_Connect clientConnect { get; private set; }

        private void LeftPanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = this.FindResource("LeftPanelContextMenu") as ContextMenu;
            if (contextMenu != null && sender is UIElement element)
            {
                contextMenu.PlacementTarget = element;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
        private string GetInitialsFromUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return "??";

            var parts = username.Split(' ');
            if (parts.Length >= 2)
            {
                // Если есть имя и фамилия
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                // Если только одно слово
                return parts[0].Substring(0, 2).ToUpper();
            }

            return "??";
        }
        //добавление нового пользователя
        public void AddMember(MembersList User)
        {

            string ShortName = GetInitialsFromUsername(User.Name);
            var viewModel = DataContext as MainViewModel;
            viewModel.AddMember(User);
        }
        public void UpdateCurrentServerID(string serverID)
        {
            //  string ShortName = GetInitialsFromUsername(User.Name);
            var viewModel = DataContext as MainViewModel;
            viewModel.UpdateCurrentServerID(serverID);
        }
        public void ServerAdded(IEnumerable<ServersList> Server)
        {
            //  string ShortName = GetInitialsFromUsername(User.Name);
            var viewModel = DataContext as MainViewModel;
            viewModel.ServerAdded(Server);
        }        
        public void UpdateServerList(Dictionary<string, ServersList> Server)
        {
            //  string ShortName = GetInitialsFromUsername(User.Name);
            var viewModel = DataContext as MainViewModel;
            viewModel.UpdateServerList(Server);
        }
        public void ChangeStatus(string username, bool isOnline, string statusText = "")
        {
            var viewModel = DataContext as MainViewModel;

            // viewModel.UpdateMemberStatus(username, isOnline, statusText);
        }
        public void RemoveChannel(string channelName)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel.RemoveVoiceChannel(channelName);
        }

        public MainWindow()
        {
            InitializeComponent();



            string serverIP = "127.0.0.1";


            var viewModel = DataContext as MainViewModel;
            // viewModel.SetMainUsers(UserContext.UserName, GetInitialsFromUsername(UserContext.UserName)); //создаем главного пользователя

            //   AddCustomMember(UserContext.UserName,true);
            //   AddCustomMember("Zedefen");
            //  ChangeStstus("Leshyi",true, "В сети");





            
            //viewModel.AddMemberToChannel("Zedefen", "Голосовой 1");



            try
            {
                // Client client = new Client();
                //   client.Connect(serverIP, );
                clientConnect = new Client_Connect();

                // Подписываемся на события
                clientConnect.UserAdded += OnUserAdded;
                clientConnect.UpdateCurrentServerID += OnUpdateCurrentServerID;
                clientConnect.UpdateServerList += OnUpdateServerList;
                clientConnect.UserStatusChanged += OnUserStatusChanged;
                clientConnect.RemoveChannel += OnRemoveChannel;


                clientConnect.Connect(serverIP, UserContext.UserName);
               // viewModel.CreateVoiceChannel();
            }
            catch (Exception ex)
            {
                ;
            }


        }

        private void ClientConnect_RemoveChannel(object sender, string e)
        {
            throw new NotImplementedException();
        }

        // Обработчики событий
        private void OnUserAdded(object sender, MembersList username)
        {
            // Вызываем в UI потоке, так как это изменение интерфейса
            Dispatcher.Invoke(() =>
            {
                AddMember(username);
            });
        }
        private void OnUpdateCurrentServerID(object sender, string serverID)
        {
            // Вызываем в UI потоке, так как это изменение интерфейса
            Dispatcher.Invoke(() =>
            {
                UpdateCurrentServerID(serverID);
            });
        }
        private void OnServerAdded(object sender, IEnumerable<ServersList> server)
        {
            // Вызываем в UI потоке, так как это изменение интерфейса
            Dispatcher.Invoke(() =>
            {
                ServerAdded(server);
            });
        }       
        private void OnUpdateServerList(object sender, Dictionary<string, ServersList> server)
        {
            // Вызываем в UI потоке, так как это изменение интерфейса
            Dispatcher.Invoke(() =>
            {
                UpdateServerList(server);
            });
        }
        private void OnUserStatusChanged(object sender, UserStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ChangeStatus(e.Username, e.IsOnline, e.StatusText);
            });
        }
        private void OnRemoveChannel(object sender, string channelName)
        {
            Dispatcher.Invoke(() =>
            {
                RemoveChannel(channelName);
            });
        }


    }
}
