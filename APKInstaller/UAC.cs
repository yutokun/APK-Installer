using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace APKInstaller
{
    public class UAC
    {
        const int MAX_PATH = 260;
        const uint SIID_SHIELD = 0x00000004D;
        const uint SHGSI_ICON = 0x000000100;
        const uint SHGSI_LARGEICON = 0x000000000;
        const uint SHGSI_SMALLICON = 0x000000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysIconIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szPath;
        }

        [DllImport("shell32.dll", SetLastError = false)]
        static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref SHSTOCKICONINFO sii);

        public static Image GetShieldImage(bool small)
        {
            var sii = new SHSTOCKICONINFO();
            sii.cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>();
            var sizeFlag = small ? SHGSI_SMALLICON : SHGSI_LARGEICON;
            var res = SHGetStockIconInfo(SIID_SHIELD, SHGSI_ICON | sizeFlag, ref sii);
            if (res != 0) Marshal.ThrowExceptionForHR(res);
            var bitmap = System.Drawing.Icon.FromHandle(sii.hIcon).ToBitmap();

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return new Image { Source = bitmapImage };
            }
        }
    }
}
