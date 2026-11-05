using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Linq;
using StreamMusicAIManager.ViewModels;

namespace StreamMusicAIManager;

public partial class MainWindow : Window
{
    private FloatingOverlayBubble? _overlay;
    private HwndSource? _source;
    private const int HotkeyId = 9001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkM = 0x4D;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _source = (HwndSource)PresentationSource.FromVisual(this)!;
        _source.AddHook(WndProc);
        RegisterHotKey(_source.Handle, HotkeyId, ModControl | ModShift, VkM);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_source is not null)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _source.RemoveHook(WndProc);
        }
        _overlay?.Close();
        base.OnClosed(e);
    }

    private void ShowOverlay_Click(object sender, RoutedEventArgs e) => ToggleOverlay();

    private void ToggleOverlay()
    {
        if (_overlay is null)
        {
            _overlay = new FloatingOverlayBubble { DataContext = DataContext, Owner = this };
            _overlay.Closed += (_, _) => _overlay = null;
        }

        if (_overlay.IsVisible) _overlay.Hide();
        else _overlay.Show();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (DataContext is MainViewModel vm) vm.ImportPaths(files);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyId)
        {
            ToggleOverlay();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
