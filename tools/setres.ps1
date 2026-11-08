param([int]$W = 0, [int]$H = 0)
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices; using System.Text;
public class Res {
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
  public struct DEVMODE {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
    public ushort dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra; public uint dmFields;
    public int dmPositionX, dmPositionY; public uint dmDisplayOrientation, dmDisplayFixedOutput;
    public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
    public ushort dmLogPixels; public uint dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency, dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight; }
  [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern bool EnumDisplaySettingsA(string dev, int mode, ref DEVMODE dm);
  [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern int ChangeDisplaySettingsExA(string dev, ref DEVMODE dm, IntPtr hwnd, uint flags, IntPtr param);
  public static string Current() { var d = new DEVMODE(); d.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)); return EnumDisplaySettingsA(null, -1, ref d) ? d.dmPelsWidth + "x" + d.dmPelsHeight + "@" + d.dmDisplayFrequency : "enum failed, size " + d.dmSize; }
  public static string Modes() { var sb = new StringBuilder(); var d = new DEVMODE(); d.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)); int i = 0; while (EnumDisplaySettingsA(null, i++, ref d)) { if (d.dmBitsPerPel == 32) sb.Append(d.dmPelsWidth + "x" + d.dmPelsHeight + " "); } return sb.ToString(); }
  public static int Set(int w, int h) { var d = new DEVMODE(); d.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE)); EnumDisplaySettingsA(null, -1, ref d); d.dmPelsWidth = (uint)w; d.dmPelsHeight = (uint)h; d.dmFields = 0x80000 | 0x100000; return ChangeDisplaySettingsExA(null, ref d, IntPtr.Zero, 1, IntPtr.Zero); }
}
"@
'before: ' + [Res]::Current()
'modes: ' + [Res]::Modes()
if ($W -gt 0) { 'set result: ' + [Res]::Set($W, $H) + ' (0 = ok)'; Start-Sleep -Seconds 2; 'after: ' + [Res]::Current() }
