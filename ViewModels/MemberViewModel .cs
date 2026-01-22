using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using WpfApp1_client.Models;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Drawing;
using System.CodeDom.Compiler;
using static WpfApp1_client.ViewModels.RelayCommand;

namespace WpfApp1_client.ViewModels
{

    public class ServersList
    {
        public string ServerID { get; set; }
        public string ServerName { get; set; }
        public string ServerColor { get; set; }
        public string ShortName { get; set; }
        public string Creator { get; set; }
        public int MaxRooms { get; set; }
        public List<Room> Rooms { get; set; }
        public int MaxUsers { get; set; }
        public List<MembersList> Users { get; set; }
        public bool IsPrivate { get; set; }
        public string Password { get; set; }
    }
    public class Room
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string Creator { get; set; }
        public int MaxUsers { get; set; }
        public List<MembersList> Users { get; set; }
        public string CurrentStreamer { get; set; }


    }
    public enum ClientStatus
    {
        Online,
        InCall,
        InRoom,
        Away,
        Streaming,
        Offline
    }
    public class MembersList : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public ClientStatus Status { get; set; }
        public List<string> ServerList { get; set; }
        public List<string> Friends { get; set; }
        public string Email { get; set; }
        public string UserColor { get; set; }
        public DateTime LastActivity { get; set; }
        public bool Microphone { get; set; }
        public string Initials { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public class RelayCommand_1<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public RelayCommand_1(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) =>
                _canExecute == null || _canExecute((T)parameter);

            public void Execute(object parameter) => _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MembersList> _members;
        private ObservableCollection<MembersList> _onlineMembers;
        private ObservableCollection<MembersList> _awayMembers;
        private ObservableCollection<MembersList> _offlineMembers;
        private ObservableCollection<ServersList> _servers;
        private ObservableCollection<Room> _rooms;

        private string _memberCountText;
        private string _serverIDText;
        private string _serverNameText;
        private string _onlineMembersHeader;
        private string _awayMembersHeader;
        private string _offlineMembersHeader;
        private string _serversHeader;

        private string _currentServerID;

        private string _inputTextCreateServer;
        private string _inputTextFindServer;
        private string _inputTextCreateRoom;
        private bool _isInputVisible;
        private bool _isInputVisibleCreateRoom;
        public Dictionary<string, ServersList> Servers = new Dictionary<string, ServersList>(); //список серверов пользователя
        private MembersList _currentUser; // переменная главного пользователя


        private VoiceChannelViewModel _currentChannel;


        private ObservableCollection<ChannelCategoryViewModel> _channelCategories;
        private int _channelCounter = 1;


        public ObservableCollection<ChannelCategoryViewModel> ChannelCategories
        {
            get => _channelCategories;
            set
            {
                if (_channelCategories != value)
                {
                    _channelCategories = value;
                    OnPropertyChanged();
                }
            }
        }


        public ICommand CreateVoiceChannelCommand { get; }
        public ICommand CreateServerCommand { get; }
        public ICommand FindServerCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand CreateRoomsCommand { get; }
        public ICommand FindCommand { get; }
        public ICommand JoinServerCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand CancelCommand { get; }

        public ObservableCollection<MembersList> Members
        {
            get => _members;
            set
            {
                if (_members != value)
                {
                    _members = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MembersList> OnlineMembers
        {
            get => _onlineMembers;
            set
            {
                if (_onlineMembers != value)
                {
                    _onlineMembers = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MembersList> AwayMembers
        {
            get => _awayMembers;
            set
            {
                if (_awayMembers != value)
                {
                    _awayMembers = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MembersList> OfflineMembers
        {
            get => _offlineMembers;
            set
            {
                if (_offlineMembers != value)
                {
                    _offlineMembers = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<ServersList> CurrentServers
        {
            get => _servers;
            set
            {
                if (_servers != value)
                {
                    _servers = value;
                    OnPropertyChanged();
                }
            }
        }  
        public ObservableCollection<Room> CurrentRooms
        {
            get => _rooms;
            set
            {
                if (_rooms != value)
                {
                    _rooms = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICollectionView OnlineMembersView { get; private set; }
        public ICollectionView AwayMembersView { get; private set; }
        public ICollectionView OfflineMembersView { get; private set; }
        public ICollectionView CurrentServersView { get; private set; }
        public ICollectionView CurrentRoomsView { get; private set; }

        public string MemberCountText
        {
            get => _memberCountText;
            set
            {
                if (_memberCountText != value)
                {
                    _memberCountText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ServerIDText
        {
            get => _serverIDText;
            set
            {
                if (_serverIDText != value)
                {
                    _serverIDText = value;
                    OnPropertyChanged();
                }
            }
        }public string ServerNameText
        {
            get => _serverNameText;
            set
            {
                if (_serverNameText != value)
                {
                    _serverNameText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OnlineMembersHeader
        {
            get => _onlineMembersHeader;
            set
            {
                if (_onlineMembersHeader != value)
                {
                    _onlineMembersHeader = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AwayMembersHeader
        {
            get => _awayMembersHeader;
            set
            {
                if (_awayMembersHeader != value)
                {
                    _awayMembersHeader = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OfflineMembersHeader
        {
            get => _offlineMembersHeader;
            set
            {
                if (_offlineMembersHeader != value)
                {
                    _offlineMembersHeader = value;
                    OnPropertyChanged();
                }
            }
        }
  

        public bool HasOnlineMembers => OnlineMembers?.Count > 0;
        public bool HasAwayMembers => AwayMembers?.Count > 0;
        public bool HasOfflineMembers => OfflineMembers?.Count > 0;
        public bool HasCurrentServers => CurrentServers?.Count > 0;
        public bool HasCurrentSRooms => CurrentRooms?.Count > 0;


        public ICommand ClearAllMembersCommand { get; }

        public ICommand ToggleCurrentUserStatusCommand { get; }


        //  public string CurrentUserStatus => CurrentUser?.Status == ClientStatus.Online ? "Онлайн" : "Оффлайн";

        public MembersList CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    OnPropertyChanged();
                    //   OnPropertyChanged(nameof(CurrentUserStatus));
                }
            }
        }
  
        public string InputTextCreateServer
        {
            get => _inputTextCreateServer;
            set
            {
                _inputTextCreateServer = value;
                OnPropertyChanged();
            }
        }

        public string InputTextFindServer
        {
            get => _inputTextFindServer;
            set
            {
                _inputTextFindServer = value;
                OnPropertyChanged();
            }
        }       
        public string InputTextCreateRoom
        {
            get => _inputTextCreateRoom;
            set
            {
                _inputTextCreateRoom = value;
                OnPropertyChanged();
            }
        }

        //отображение управлением серверами
        public bool IsInputVisible
        {
            get => _isInputVisible;
            set
            {
                _isInputVisible = value;
                OnPropertyChanged();
            }
        }
        public bool IsInputVisibleCreateRoom
        {
            get => _isInputVisible;
            set
            {
                _isInputVisible = value;
                OnPropertyChanged();
            }
        }
        // Текущий канал
        public VoiceChannelViewModel CurrentChannel
        {
            get => _currentChannel;
            set
            {
                if (_currentChannel != value)
                {
                    _currentChannel = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
            // Инициализация коллекций
            _members = new ObservableCollection<MembersList>();
            _onlineMembers = new ObservableCollection<MembersList>();
            _awayMembers = new ObservableCollection<MembersList>();
            _offlineMembers = new ObservableCollection<MembersList>();
            _servers = new ObservableCollection<ServersList>();
            _rooms = new ObservableCollection<Room>();

            // Инициализация представлений
            OnlineMembersView = CollectionViewSource.GetDefaultView(_onlineMembers);
            AwayMembersView = CollectionViewSource.GetDefaultView(_awayMembers);
            OfflineMembersView = CollectionViewSource.GetDefaultView(_offlineMembers);
            CurrentServersView = CollectionViewSource.GetDefaultView(_servers);
            CurrentRoomsView = CollectionViewSource.GetDefaultView(_rooms);

            // Создание команд

            // ToggleRandomMemberCommand = new RelayCommand(ToggleRandomMemberStatus);
            ClearAllMembersCommand = new RelayCommand(ClearAllMembers);


            ToggleCurrentUserStatusCommand = new RelayCommand(ToggleCurrentUserStatus);

           

            // Инициализация тестовых данных
            //  InitializeTestMembers();

            // Подписка на изменения
            _members.CollectionChanged += Members_CollectionChanged;

            foreach (var member in _members)
            {
                member.PropertyChanged += Member_PropertyChanged;
            }

            _channelCategories = new ObservableCollection<ChannelCategoryViewModel>();
           
            // Создаем начальную категорию
            var voiceCategory = new ChannelCategoryViewModel("ГОЛОСОВЫЕ КАНАЛЫ");

            //    voiceCategory.Channels.Add(new VoiceChannelViewModel("Общий"));

            ChannelCategories.Add(voiceCategory);

            // Команда создания канала
            CreateVoiceChannelCommand = new RelayCommand(CreateVoiceChannel);
            CreateServerCommand = new RelayCommand(CreateNewServer);
            FindServerCommand = new RelayCommand(FindNewServer);
            ApplyCommand = new RelayCommand(EnterServerName);
            CreateRoomCommand = new RelayCommand(CreateRoom);
            FindCommand = new RelayCommand(FindServerID);
            CreateRoomsCommand = new RelayCommand(CreateRooms);
            CancelCommand = new RelayCommand(CancelServerName);
            JoinServerCommand = new RelayCommand_1<ServersList>(ExecuteJoinServer);
            JoinRoomCommand = new RelayCommand_1<Room>(ExecuteJoinRoom);




        }
        private void ExecuteJoinServer(ServersList server) //вызывается при переключении серверов
        {
            var client = MainWindow.clientConnect;
          //  client.SendCommand($"/joint_server {server.ServerID}");
            client.SendCommand($"/server_list");
            _currentServerID = server.ServerID;
            ServerNameText = server.ServerName;
            ServerIDText = server.ServerID ;

            OfflineMembers.Clear();
            OnlineMembers.Clear();
            AwayMembers.Clear();
            Members.Clear();
            AddMember(_currentUser);
            CurrentRooms.Clear();
            foreach (var _rum in Servers[_currentServerID].Rooms)
            {
                CurrentRooms.Add(_rum);
            }



            // Обновляем счетчики и заголовки
            UpdateHeaders();
            UpdateMemberCount();

            // Уведомляем UI об изменении видимости секций
            OnPropertyChanged(nameof(HasOnlineMembers));
            OnPropertyChanged(nameof(HasAwayMembers));
            OnPropertyChanged(nameof(HasOfflineMembers));
            OnPropertyChanged(nameof(HasCurrentServers));
            OnPropertyChanged(nameof(HasCurrentSRooms));
        }  
        private void ExecuteJoinRoom(Room __room)
        {
            var client = MainWindow.clientConnect;
            client.SendCommand($"/joint_server {_currentServerID}");
            client.SendCommand($"/joint_room {__room.RoomId}");
            
           // CurrentServer = _currentServerID = server.ServerID;
            // Действия с сервером...
        }
        private void ToggleCurrentUserStatus() //функция переключения статуса пользователя
        {
            if (CurrentUser != null)
            {
                if (CurrentUser.Status != ClientStatus.Offline)
                {
                    // Если пользователь в канале, обновляем его статус там
                    if (CurrentChannel != null && CurrentChannel.ConnectedMembers.Contains(CurrentUser))
                    {
                        CurrentChannel.ConnectedMembers.Remove(CurrentUser);
                        CurrentChannel.ConnectedMembers.Add(CurrentUser);
                    }
                }
                else
                {
                    //CurrentUser.StatusText = "Не в сети";
                    //CurrentUser.IsOnline = false;
                    //_currentUser.IsOnline = false;
                }
                //AddToStatusCollection(CurrentUser);
                UpdateHeaders();
                UpdateMemberCount();


                OnPropertyChanged(nameof(HasOnlineMembers));
                OnPropertyChanged(nameof(HasAwayMembers));
                OnPropertyChanged(nameof(HasOfflineMembers));
                OnPropertyChanged(nameof(HasCurrentServers));
               OnPropertyChanged(nameof(HasCurrentSRooms));
            }
        }
        // Метод для обновления статуса текущего пользователя:
        public void AddMemberToChannel(string username, string channelName)
        {
            // Находим пользователя
            var member = FindMember(username);
            if (member == null)
            {
                // Если пользователь не найден

            }

            // Находим канал
            var channel = ChannelCategories
                .SelectMany(c => c.Channels)
                .FirstOrDefault(c => c.Name == channelName);

            if (channel == null)
            {
                // Если канал не найден, можно создать его

            }

            // Добавляем пользователя в канал
            if (!channel.ConnectedMembers.Contains(member))
            {
                channel.ConnectedMembers.Add(member);
            }
        }

        // Метод получения текущего пользователя
        public MembersList GetCurrentUser()
        {
            return _currentUser;
        }

        // Метод для отключения от всех каналов
        public void DisconnectFromAllChannels()
        {

            var client = MainWindow.clientConnect;
            client.SendCommand("/leave");
            foreach (var category in ChannelCategories)
            {
                foreach (var channel in category.Channels)
                {
                    channel.LeaveChannel();

                }
            }
        }

        // Метод для создания нового голосового канала
        private NetworkStream textStream;



        private void JointToCreatChannel(string Username, string roomname)
        {

            AddMemberToChannel(Username, roomname);
            //    CurrentUser.= roomname;
        }


        private void CancelServerName()
        {
            IsInputVisible = false;
        }
        private void CreateRoom()
        {
            IsInputVisibleCreateRoom = !IsInputVisibleCreateRoom;
        }

        private void EnterServerName()
        {
            var client = MainWindow.clientConnect;

            if (!string.IsNullOrWhiteSpace(InputTextCreateServer))
            {
                IsInputVisible = false;
                client.SendCommand($"/create_server {InputTextCreateServer}");
            }
        } 
        private void FindServerID()
        {
            var client = MainWindow.clientConnect;

            if (!string.IsNullOrWhiteSpace(InputTextFindServer))
            {
                IsInputVisible = false;
                client.SendCommand($"/joint_server {InputTextFindServer}");
            }
        }        
        private void CreateRooms()
        {
            var client = MainWindow.clientConnect;

            if (!string.IsNullOrWhiteSpace(InputTextCreateRoom))
            {
                IsInputVisibleCreateRoom = false;
                client.SendCommand($"/create_room {InputTextCreateRoom}");
            }
        }

        public void CreateNewServer()
        {

            IsInputVisible = true;
            InputTextCreateServer = string.Empty;

            //  client.SendCommand(command);

        }
        public void FindNewServer()
        {

            IsInputVisible = true;
            InputTextFindServer = string.Empty;

            //  client.SendCommand(command);

        }


        public void CreateVoiceChannel()
        {
            //  CreateRoom("4CB2B693");

            var channelName = $"Голосовой_{_channelCounter++}";
            var newChannel = new VoiceChannelViewModel(channelName, this);
            if (CurrentUser != null)
                DisconnectFromAllChannels();



            // Добавляем в первую категорию
            if (ChannelCategories.Any())
            {
                ChannelCategories.First().Channels.Add(newChannel);
            }
            else
            {
                var category = new ChannelCategoryViewModel("ГОЛОСОВЫЕ КАНАЛЫ");
                category.Channels.Add(newChannel);

                ChannelCategories.Add(category);

            }

            JointToCreatChannel(CurrentUser.Name, channelName);
            var client = MainWindow.clientConnect;
            var room = client.currentRoom;
            ;
        }

        public void RemoveVoiceChannel(string ChannelName)
        {
            if (ChannelName != null)
            {
                var category = new ChannelCategoryViewModel("ГОЛОСОВЫЕ КАНАЛЫ");
                var newChannel = new VoiceChannelViewModel(ChannelName, this);
                category.Channels.Remove(newChannel);
            }
        }
        //обновляем список серверов, комнат и пользователей

        public void UpdateServerList(Dictionary<string, ServersList> _Servers)
        {
 
            CurrentServers.Clear();
            foreach (var server in _Servers)
            {
                if (Servers.Keys.Contains(server.Key)) //если сервер у нас уже есть
                {
                    Servers.Remove(server.Key);
                    Servers.Add(server.Key, server.Value);
                    CurrentServers.Add(server.Value);
                   


                }
                else//если сервера еще нет
                {
                    Servers.Add(server.Key, server.Value);//добавляем сервер в список
                    CurrentServers.Add(server.Value);
                }

            }
            
            
           
            if (_currentServerID != null)//проверяем находимся ли мы на сервере
                if (Servers.Keys.Contains(_currentServerID)) // проверяем есть ли сервер в списке наших серверов
                    foreach (var user in Servers[_currentServerID].Users) // обновляем всех пользователей в текущей комнате
                    {
                        if (CurrentUser.ID != user.ID)
                        {
                            var mem = FindMember(user.ID);
                            if (mem != null)
                            {
                                
                                Members.Remove(mem);
                                Members.Add(user);
                                mem = null;
                            }
                            else
                            {
                                
                                Members.Add(user);
                            }

                            AddToStatusCollection(user);
                        }
                    }

         
            // Обновляем счетчики и заголовки
            UpdateHeaders();
            UpdateMemberCount();

            // Уведомляем UI об изменении видимости секций
            OnPropertyChanged(nameof(HasOnlineMembers));
            OnPropertyChanged(nameof(HasAwayMembers));
            OnPropertyChanged(nameof(HasOfflineMembers));
            OnPropertyChanged(nameof(HasCurrentServers));
            OnPropertyChanged(nameof(HasCurrentSRooms));
        }
        public void ServerAdded(IEnumerable<ServersList> Servers)
        {
            foreach (var server in Servers)
            {

            }

        }
        public void AddAllMember(IEnumerable<MembersList> member)
        { }
        public void UpdateCurrentServerID(string serverID) //при смене сервера получаем новый ID
        {
            
            _currentServerID = serverID;
            
                OfflineMembers.Clear();
                OnlineMembers.Clear();
                AwayMembers.Clear();
                Members.Clear();
                AddMember(_currentUser);
            CurrentRooms.Clear();
            foreach (var _rum in Servers[_currentServerID].Rooms)
            {
                CurrentRooms.Add(_rum);
            }
            


            // Обновляем счетчики и заголовки
            UpdateHeaders();
            UpdateMemberCount();

            // Уведомляем UI об изменении видимости секций
            OnPropertyChanged(nameof(HasOnlineMembers));
            OnPropertyChanged(nameof(HasAwayMembers));
            OnPropertyChanged(nameof(HasOfflineMembers));
            OnPropertyChanged(nameof(HasCurrentServers));
            OnPropertyChanged(nameof(HasCurrentSRooms));
        }
        // Основная функция добавления пользователя
        public void AddMember(MembersList member)
        {

            _currentUser = CurrentUser = member;
            //// Подписываемся на изменения статуса пользователя
            member.PropertyChanged += Member_PropertyChanged;

            //// Добавляем в основную коллекцию
            Members.Add(member);

            //// Добавляем в соответствующую коллекцию статусов
            AddToStatusCollection(member);

            // Обновляем счетчики и заголовки
            UpdateHeaders();
            UpdateMemberCount();

            // Уведомляем UI об изменении видимости секций
            OnPropertyChanged(nameof(HasOnlineMembers));
            OnPropertyChanged(nameof(HasAwayMembers));
            OnPropertyChanged(nameof(HasOfflineMembers));
            OnPropertyChanged(nameof(HasCurrentServers));
            OnPropertyChanged(nameof(HasCurrentSRooms));
        }
        private void AddToStatusCollection(MembersList member)
        {
            if (member.Status != ClientStatus.Offline)
            {
                if (member.Status == ClientStatus.Away)
                {
                    if (!AwayMembers.Contains(member))
                        AwayMembers.Add(member);
                    
                    // Удаляем из других коллекций
                    OnlineMembers.Remove(member);
                    OfflineMembers.Remove(member);
                }
                if (member.Status == ClientStatus.Online)
                {
                    // var te = OnlineMembers.Contains(member);
                    if (!OnlineMembers.Contains(member))
                        OnlineMembers.Add(member);

                    // Удаляем из других коллекций
                    AwayMembers.Remove(member);
                    OfflineMembers.Remove(member);
                }
            }
            else
            {

                OfflineMembers.Add(member);
                // Удаляем из других коллекций
                OnlineMembers.Remove(member);
                AwayMembers.Remove(member);
            }
        }
        private void Member_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is MembersList member)
            {
                if (e.PropertyName == nameof(ClientStatus.Offline))
                {
                    // Пересортировываем пользователя при изменении статуса
                    AddToStatusCollection(member);
                    UpdateHeaders();
                    UpdateMemberCount();

                    OnPropertyChanged(nameof(HasOnlineMembers));
                    OnPropertyChanged(nameof(HasAwayMembers));
                    OnPropertyChanged(nameof(HasOfflineMembers));
                    OnPropertyChanged(nameof(HasCurrentServers));
                    OnPropertyChanged(nameof(HasCurrentSRooms));
                }
            }
        }
        private void Members_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (MembersList member in e.OldItems)
                {
                    member.PropertyChanged -= Member_PropertyChanged;

                    // Удаляем из всех статусных коллекций
                    OnlineMembers.Remove(member);
                    AwayMembers.Remove(member);
                    OfflineMembers.Remove(member);
                }
            }

            if (e.NewItems != null)
            {
                foreach (MembersList member in e.NewItems)
                {
                    member.PropertyChanged += Member_PropertyChanged;
                }
            }

            UpdateHeaders();
            UpdateMemberCount();

            OnPropertyChanged(nameof(HasOnlineMembers));
            OnPropertyChanged(nameof(HasAwayMembers));
            OnPropertyChanged(nameof(HasOfflineMembers));
            OnPropertyChanged(nameof(HasCurrentServers));
            OnPropertyChanged(nameof(HasCurrentSRooms));
        }
        private void UpdateMemberCount()
        {
            var total = Members.Count;
            var online = OnlineMembers.Count + AwayMembers.Count;
            MemberCountText = $"УЧАСТНИКИ — {online}/{total}";
        }
        private void UpdateHeaders()
        {
            OnlineMembersHeader = $"В СЕТИ — {OnlineMembers.Count}";
            AwayMembersHeader = $"ЗАНЯТЫ — {AwayMembers.Count}";
            OfflineMembersHeader = $"НЕТ В СЕТИ — {OfflineMembers.Count}";
        }


        // Функция для поиска пользователя
        public MembersList FindMember(string username)
        {
            return Members.FirstOrDefault(m => m.ID == username);
        }

        // Функция для очистки всех пользователей
        public void ClearAllMembers()
        {
            Members.Clear();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}