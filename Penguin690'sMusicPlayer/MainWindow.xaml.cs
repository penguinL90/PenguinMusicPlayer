using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel;
using Penguin690_sMusicPlayer.ViewModels;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Penguin690_sMusicPlayer;

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

        viewModel = new(hwnd);
        ExtendsContentIntoTitleBar = true;
        AppBarTitle.Loaded += AppBarTitle_Loaded;
        AppBarTitle.SizeChanged += AppBarTitle_SizeChanged;
        PackageVersion version = Package.Current.Id.Version;
        VersionTxt.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
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
}
