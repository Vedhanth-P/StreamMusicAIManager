using System.Windows;
using System.Windows.Input;

namespace StreamMusicAIManager;

public partial class FloatingOverlayBubble : Window
{
    public FloatingOverlayBubble()
    {
        InitializeComponent();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
