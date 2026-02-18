using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using hra2048.Models;
using hra2048.ViewModels;

namespace hra2048.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += (_, _) =>
            {
                if (DataContext is MainWindowViewModel vm)
                    vm.Window = this;
            };
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                Direction? direction = e.Key switch
                {
                    Key.Up => Direction.Up,
                    Key.Down => Direction.Down,
                    Key.Left => Direction.Left,
                    Key.Right => Direction.Right,
                    _ => null
                };

                if (direction.HasValue)
                {
                    vm.ManualMove(direction.Value);
                    e.Handled = true;
                }
            }
        }
    }
}