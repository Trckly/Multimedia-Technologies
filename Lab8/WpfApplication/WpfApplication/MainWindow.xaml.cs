using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WpfApplication
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, Storyboard> storyboards = new Dictionary<string, Storyboard>();
        private readonly Dictionary<string, SolidColorBrush> originalColors = new Dictionary<string, SolidColorBrush>();
        private readonly Dictionary<string, double> originalSizes = new Dictionary<string, double>();
        private readonly Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();

            // Initialize storyboards by accessing BeginStoryboard.Storyboard
            storyboards.Add("Ball1", ((BeginStoryboard)FindName("Ball1Storyboard")).Storyboard);
            storyboards.Add("Ball2", ((BeginStoryboard)FindName("Ball2Storyboard")).Storyboard);
            storyboards.Add("Ball3", ((BeginStoryboard)FindName("Ball3Storyboard")).Storyboard);
            storyboards.Add("Ball4", ((BeginStoryboard)FindName("Ball4Storyboard")).Storyboard);
            storyboards.Add("Ball5", ((BeginStoryboard)FindName("Ball5Storyboard")).Storyboard);
            storyboards.Add("Ball6", ((BeginStoryboard)FindName("Ball6Storyboard")).Storyboard);

            // Store original colors and sizes
            originalColors.Add("Ball1", new SolidColorBrush(Colors.Red));
            originalColors.Add("Ball2", new SolidColorBrush(Colors.Blue));
            originalColors.Add("Ball3", new SolidColorBrush(Colors.Green));
            originalColors.Add("Ball4", new SolidColorBrush(Colors.Yellow));
            originalColors.Add("Ball5", new SolidColorBrush(Colors.Magenta));
            originalColors.Add("Ball6", new SolidColorBrush(Colors.Cyan));

            originalSizes.Add("Ball1", 100);
            originalSizes.Add("Ball2", 120);
            originalSizes.Add("Ball3", 140);
            originalSizes.Add("Ball4", 110);
            originalSizes.Add("Ball5", 130);
            originalSizes.Add("Ball6", 116);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                string name = ellipse.Name;
                if (storyboards.ContainsKey(name))
                {
                    storyboards[name].Pause();
                }
            }
        }

        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                string name = ellipse.Name;
                if (storyboards.ContainsKey(name))
                {
                    // Restore original color and size
                    ellipse.Fill = originalColors[name];
                    ellipse.Width = originalSizes[name];
                    ellipse.Height = originalSizes[name];
                    storyboards[name].Resume();
                }
            }
        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                string name = ellipse.Name;
                // Change to a random color
                Color randomColor = Color.FromRgb(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256));
                ellipse.Fill = new SolidColorBrush(randomColor);

                // Increase size by 20%
                double newSize = originalSizes[name] * 1.2;
                ellipse.Width = newSize;
                ellipse.Height = newSize;

                // Center the ellipse to avoid shifting due to size change
                double oldSize = originalSizes[name];
                double deltaSize = (newSize - oldSize) / 2;
                Canvas.SetLeft(ellipse, Canvas.GetLeft(ellipse) - deltaSize);
                Canvas.SetTop(ellipse, Canvas.GetTop(ellipse) - deltaSize);
            }
        }
    }
}