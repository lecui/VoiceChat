using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1_client.Converters
{
    public enum ClientStatus
    {
        Online,
        InCall,
        InRoom,
        Away,
        Streaming,
        Offline
    }
    public class StatusToColorConverter : IValueConverter
    {
       
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
         
                if (value.ToString() == "Online") return new SolidColorBrush(Color.FromRgb(220, 221, 222)); ;
                if (value.ToString() == "Offline") return new SolidColorBrush(Color.FromRgb(114, 118, 125));

            
            return new SolidColorBrush(Color.FromRgb(114, 118, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
