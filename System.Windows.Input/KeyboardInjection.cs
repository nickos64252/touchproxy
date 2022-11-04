using System;
using System.Runtime.InteropServices;

namespace System.Windows.Input
{
	public static class KeyboardInjection
	{
		public static void Send(KeyboardInputSequence keyboardInputSequence)
		{
			switch (keyboardInputSequence)
			{
				case KeyboardInputSequence.WindowsKeyPress:
					NativeMethods.SendInput
					(
						2,
						new InputInfo[] 
						{ 
							new InputInfo { Type = InputInfoType.KEYBOARD, Union = new InputUnion { KeyboardInput = new KeyboardInput { VirtualKeyCode = VirtualKeyCode.LWIN, Flags = KeyboardFlag.KEYDOWN } } },
							new InputInfo { Type = InputInfoType.KEYBOARD, Union = new InputUnion { KeyboardInput = new KeyboardInput { VirtualKeyCode = VirtualKeyCode.LWIN, Flags = KeyboardFlag.KEYUP } } }
						},
						Marshal.SizeOf(typeof(InputInfo))
					);
					break;
				default:
					break;
			}
		}


        public static void SendMouse(int x, int y, bool leftButton)
        {
            NativeMethods.SendInput
            (
                1,
                new InputInfo[]
                {
                        new InputInfo { Type = InputInfoType.MOUSE, Union = new InputUnion { MouseInput = new MouseInput { X = 0, Y = 0, MouseData = 0, Flags = MouseEventFlags.MOUSEEVENTF_MOVE |MouseEventFlags.MOUSEEVENTF_ABSOLUTE } } }
                },
                Marshal.SizeOf(typeof(InputInfo))
            );
        }

		internal static class NativeMethods
		{
			[DllImport("User32.dll")]
			internal static extern uint SendInput(uint count, [MarshalAs(UnmanagedType.LPArray), In] InputInfo[] inputs, int size);
		}
	}

	public enum KeyboardInputSequence
	{
		WindowsKeyPress
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct InputInfo
	{
		internal InputInfoType Type;
		internal InputUnion Union;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct InputUnion
	{
		[FieldOffset(0)]
		internal MouseInput MouseInput;

		[FieldOffset(0)]
		internal KeyboardInput KeyboardInput;

		[FieldOffset(0)]
		internal HardwareInput HardwareInput;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct KeyboardInput
	{
		internal VirtualKeyCode VirtualKeyCode;
		internal ushort ScanCode;
		internal KeyboardFlag Flags;
		internal uint Time;
		internal IntPtr ExtraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MouseInput
	{
		internal int X;
		internal int Y;
		internal uint MouseData;
		internal MouseEventFlags Flags;
		internal uint Time;
		internal IntPtr ExtraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct HardwareInput
	{
		internal uint Msg;
		internal ushort ParamL;
		internal ushort ParamH;
	}

	internal enum InputInfoType : uint
	{
        MOUSE = 0,
		KEYBOARD = 1
	}

	internal enum VirtualKeyCode : ushort
	{
		LWIN = 0x5B
	}

	internal enum KeyboardFlag : uint
	{
		KEYDOWN = 0x0000,
		KEYUP = 0x0002,
	}

    enum MouseEventFlags : uint
    {
        MOUSEEVENTF_MOVE = 0x0001,
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        MOUSEEVENTF_LEFTUP = 0x0004,
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        MOUSEEVENTF_RIGHTUP = 0x0010,
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080,
        MOUSEEVENTF_XUP = 0x0100,
        MOUSEEVENTF_WHEEL = 0x0800,
        MOUSEEVENTF_VIRTUALDESK = 0x4000,
        MOUSEEVENTF_ABSOLUTE = 0x8000
    }
}
