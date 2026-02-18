using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace hra2048.ViewModels
{
    public partial class TileViewModel : ObservableObject
    {
        [ObservableProperty] private string _text = "";
        [ObservableProperty] private IBrush _color = Brushes.LightGray;

        public void Update(int value)
        {
            Text = value == 0 ? "" : value.ToString();
            Color = GetColor(value);
        }

        private IBrush GetColor(int value)
        {
            return value switch
            {
                0 => Brushes.LightGray,
                2 => Brushes.LightGoldenrodYellow,
                4 => Brushes.Gold,
                8 => Brushes.Orange,
                16 => Brushes.OrangeRed,
                32 => Brushes.IndianRed,
                64 => Brushes.Red,
                128 => Brushes.Yellow,
                2048 => Brushes.YellowGreen,
                _ => Brushes.Black
            };
        }
    }
}