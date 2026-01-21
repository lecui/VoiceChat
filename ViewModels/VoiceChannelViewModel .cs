using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfApp1_client.Models;

namespace WpfApp1_client.ViewModels
{
    // Модель для голосового канала
    public class VoiceChannelViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _channelForeground = "#8e9297";
        private ObservableCollection<MembersList> _connectedMembers;
        private bool _isCurrentUserConnected;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ChannelForeground
        {
            get => _channelForeground;
            set
            {
                if (_channelForeground != value)
                {
                    _channelForeground = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MembersList> ConnectedMembers
        {
            get => _connectedMembers;
            set
            {
                if (_connectedMembers != value)
                {
                    _connectedMembers = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        public bool IsActive => ConnectedMembers?.Count > 0;


        public ICommand JoinChannelCommand { get; }
        public ICommand LeaveChannelCommand { get; }



        public MainViewModel MainViewModel { get; set; }

        public VoiceChannelViewModel(string name, MainViewModel mainViewModel = null)
        {
            Name = name;
            MainViewModel = mainViewModel;
            ConnectedMembers = new ObservableCollection<MembersList>();

            JoinChannelCommand = new RelayCommand(() => JoinChannel());
            LeaveChannelCommand = new RelayCommand(() => LeaveChannel());


        }
        public bool IsCurrentUserConnected
        {
            get => _isCurrentUserConnected;
            set
            {
                if (_isCurrentUserConnected != value)
                {
                    _isCurrentUserConnected = value;
                    OnPropertyChanged();
                    // Меняем цвет канала при подключении
                    ChannelForeground = value ? "#ffffff" : "#8e9297";
                }
            }
        }

        public void JoinChannel()
        {
            // Логика подключения к каналу
            // Здесь можно добавить подключение пользователя

            if (MainViewModel == null) return;


            // Отключаемся от всех других каналов
            MainViewModel.DisconnectFromAllChannels();

            // Получаем текущего пользователя (можно настроить по-разному)
            var currentUser = MainViewModel.GetCurrentUser();
            //     if (currentUser == null) return;

            // Подключаем пользователя к этому каналу
            if (!ConnectedMembers.Contains(currentUser))
            {
                ConnectedMembers.Add(currentUser);
                IsCurrentUserConnected = true;
            }
            // currentUser.RoomName = Name;
            Console.WriteLine($"Joining voice channel: {Name}");

        }


        public void LeaveChannel()
        {
            if (MainViewModel == null) return;

            var currentUser = MainViewModel.GetCurrentUser();
            if (currentUser != null)
            {
                ConnectedMembers.Remove(currentUser);
                IsCurrentUserConnected = false;
            }
        }




        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Модель для категории каналов
    public class ChannelCategoryViewModel : INotifyPropertyChanged
    {
        private string _name;
        private bool _isExpanded = true;
        private ObservableCollection<VoiceChannelViewModel> _channels;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<VoiceChannelViewModel> Channels
        {
            get => _channels;
            set
            {
                if (_channels != value)
                {
                    _channels = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleExpandCommand { get; }

        public ChannelCategoryViewModel(string name)
        {
            Name = name;
            Channels = new ObservableCollection<VoiceChannelViewModel>();
            ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }

}