using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ClipboardMonitor.Converters
{
    class BooleanToBlurRadiusConverter : IValueConverter
    {
        enum BlurRadiuses : int
        {
            SmallRadius = 7,
            LargeRadius = 17
        }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            boolValue = (parameter != null) ? !boolValue : boolValue;
            return boolValue ? BlurRadiuses.LargeRadius : BlurRadiuses.SmallRadius;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (int)value == (int)BlurRadiuses.LargeRadius;
            boolValue = (parameter != null) ? !boolValue : boolValue;
            return boolValue;
        }
    }
}
