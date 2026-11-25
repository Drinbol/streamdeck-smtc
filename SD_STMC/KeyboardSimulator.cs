using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace dbsmtc
{
    public class KeyboardSimulator
    {
        public struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint cInputs, INPUT[] inputs, int cbSize);

        public const int INPUT_KEYBOARD = 1;
        public const uint KEYEVENTF_KEYDOWN = 0;
        public const uint KEYEVENTF_KEYUP = 2;

        public static void SendKey(ushort key, uint flag)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = key;
            inputs[0].u.ki.dwFlags = flag;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendCtrlAltNum2()
        {
            // Key down events
            SendKey((ushort)Keys.ControlKey, KEYEVENTF_KEYDOWN);
            SendKey((ushort)Keys.Menu, KEYEVENTF_KEYDOWN); // Alt key
            SendKey((ushort)Keys.NumPad2, KEYEVENTF_KEYDOWN);

            // Key up events
            SendKey((ushort)Keys.NumPad2, KEYEVENTF_KEYUP);
            SendKey((ushort)Keys.Menu, KEYEVENTF_KEYUP);
            SendKey((ushort)Keys.ControlKey, KEYEVENTF_KEYUP);
        }

    }
}
