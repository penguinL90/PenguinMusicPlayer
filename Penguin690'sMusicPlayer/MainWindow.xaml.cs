using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Penguin690_sMusicPlayer.Models;
using Penguin690_sMusicPlayer.ViewModels;
using WinRT.Interop;
using Microsoft.UI;
using Microsoft.VisualBasic.Devices;
using Windows.Devices.Input;
using Microsoft.Graphics.Canvas;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Penguin690_sMusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel;

        public MainWindow()
        {
            nint hwnd = WindowNative.GetWindowHandle(this);

            this.InitializeComponent();

            viewModel = new(hwnd, canvasCtrl);
            ExtendsContentIntoTitleBar = true;
            AppBarTitle.Loaded += AppBarTitle_Loaded;
            AppBarTitle.SizeChanged += AppBarTitle_SizeChanged;
            canvasCtrl.Invalidate();
        }

        private void AppBarTitle_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTitleBar();
        }

        private void AppBarTitle_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleBar();
        }

        private void SetTitleBar()
        {
            double scale = AppBarTitle.XamlRoot.RasterizationScale;

            cLeftPadding.Width = new(AppWindow.TitleBar.LeftInset / scale);
            cRightPadding.Width = new(AppWindow.TitleBar.RightInset / scale);
        }

        private void ListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            viewModel.SetMusic(playlist.SelectedItem as MusicFile);
        }

        private void Slider_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as Slider).ValueChanged += Slider_ValueChanged;
            viewModel.SliderPointerIn();
        }

        private void Slider_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as Slider).ValueChanged -= Slider_ValueChanged;
            viewModel.SliderPointerOut();
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            long pos = (long)(sender as Slider).Value;
            viewModel.SliderPointPress(pos);
        }

        private void canvasCtrl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            CanvasDrawingSession drawer = args.DrawingSession;
            drawer.Clear(Colors.Transparent);

            if (viewModel._FFTArray == null) return;
            for (int i = 0; i < viewModel._FFTArray.Length; ++i)
            {
                double x = i * 15;
                double height = viewModel._FFTArray[i] * 150;
                drawer.FillRectangle(new Rect(x, 100 - height, 10, height), Colors.AliceBlue);
            }
        }
    }
}
