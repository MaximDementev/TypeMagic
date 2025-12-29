using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;


public class ImagePreviewWindow : Window
{
    private readonly double _aspectRatio;
    private bool _isResizing;

    public ImagePreviewWindow(BitmapImage image)
    {
        Title = "Просмотр изображения";
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Black;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = false;

        MinWidth = 300;
        MinHeight = 200;

        // DPI-aware пропорции картинки
        double dpiX = image.DpiX > 0 ? image.DpiX : 96;
        double dpiY = image.DpiY > 0 ? image.DpiY : 96;

        double imgWidth = image.PixelWidth * 96 / dpiX;
        double imgHeight = image.PixelHeight * 96 / dpiY;

        _aspectRatio = imgWidth / imgHeight;

        var imageControl = new Image
        {
            Source = image,
            Stretch = Stretch.Uniform
        };

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = imageControl
        };

        Content = viewbox;

        Loaded += (_, __) => FitToImage(imgWidth, imgHeight);

        SizeChanged += OnSizeChanged;

        // Закрытие по клику
        MouseLeftButtonUp += (_, __) => Close();

    }

    private void FitToImage(double imgWidth, double imgHeight)
    {
        var workArea = SystemParameters.WorkArea;

        Width = Math.Min(imgWidth + 40, workArea.Width);
        Height = Math.Min(imgHeight + 40, workArea.Height);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isResizing)
            return;

        _isResizing = true;

        // Определяем, что пользователь менял
        bool widthChanged = Math.Abs(e.NewSize.Width - e.PreviousSize.Width) >
                            Math.Abs(e.NewSize.Height - e.PreviousSize.Height);

        if (widthChanged)
        {
            Height = Width / _aspectRatio;
        }
        else
        {
            Width = Height * _aspectRatio;
        }

        _isResizing = false;
    }
}
