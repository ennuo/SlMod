using System.Runtime.InteropServices;

namespace SeEditor.Dialogs;

public class TinyFileDialog
{
    [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr tinyfd_openFileDialog(string aTitle,
        string aDefaultPathAndFile,
        int aNumOfFilterPatterns,
        string[] aFilterPatterns,
        string aSingleFilterDescription,
        int aAllowMultipleSelects);

    [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr tinyfd_saveFileDialog(string aTitle,
        string aDefaultPathAndFile,
        int aNumOfFilterPatterns,
        string[] aFilterPatterns,
        string aSingleFilterDescription);

    [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);

    [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tinyfd_notifyPopup(string aTitle, string aMessage, string aIconType);
    [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int tinyfd_messageBox(string aTitle, string aMessage, string aDialogTyle, string aIconType, int aDefaultButton);
}