using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1_client.xaml
{
    public static class UserContext
    {
        public static string UserName { get; set; }
        public static bool IsAuthenticated => !string.IsNullOrEmpty(UserName);

        public static void Initialize(string userName)
        {
            UserName = userName;
        }
    }
}
