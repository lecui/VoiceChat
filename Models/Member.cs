using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;


namespace WpfApp1_client.Models
{
    public class Member : INotifyPropertyChanged
    {
        private string _username;
        private string _status;
        private bool _isOnline;

        public int Id { get; set; }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Discriminator { get; set; }

        public string DisplayName => $"{Username}#{Discriminator}";

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
            }
        }

        public MemberStatus MemberStatus { get; set; }
        public string AvatarColor { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum MemberStatus
    {
        Online,
        Idle,
        DoNotDisturb,
        Offline
    }
}
