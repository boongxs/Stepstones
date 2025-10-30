using System;
using System.Globalization;
using System.Windows.Data;

namespace stepstones.Converters
{
    public class VolumeToIconStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double volume)
            {
                // if below 0.5, return low, otherwise return high
                return (volume < 0.5) ? "Low" : "High";
            }

            return "High"; // default to high if something goes wrong
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
