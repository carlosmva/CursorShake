namespace CursorShake;

internal static class ClipboardToast
{
    public static void Show(System.Windows.Forms.Screen screen, string message = "SCREENSHOT IN CLIPBOARD")
    {
        var toast = new ClipboardToastWindow(screen, message);
        toast.Show();
    }
}
