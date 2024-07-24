using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

using com.atid.lib.transport.types;

namespace testApp
{
    public class ConverterIsConnecting : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string connState = (string)value;

            return connState.Equals(ConnectState.Connecting.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
