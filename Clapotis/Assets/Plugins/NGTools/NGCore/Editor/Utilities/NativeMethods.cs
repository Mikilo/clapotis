using System;
using System.Runtime.InteropServices;

namespace NGToolsEditor
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct INPUT
	{
		public UInt32					type;
		public MOUSEKEYBDHARDWAREINPUT	data;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct MOUSEKEYBDHARDWAREINPUT
	{
		[FieldOffset(0)]
		public HARDWAREINPUT	hardware;
		[FieldOffset(0)]
		public KEYBDINPUT		keyboard;
		[FieldOffset(0)]
		public MOUSEINPUT		mouse;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct HARDWAREINPUT
	{
		public UInt32	msg;
		public UInt16	paramL;
		public UInt16	paramH;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct KEYBDINPUT
	{
		public UInt16	vk;
		public UInt16	scan;
		public UInt32	flags;
		public UInt32	time;
		public IntPtr	extraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MOUSEINPUT
	{
		public Int32	x;
		public Int32	y;
		public UInt32	mouseData;
		public UInt32	flags;
		public UInt32	time;
		public IntPtr	extraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MARGINS
	{
		public Int32	left;
		public Int32	right;
		public Int32	top;
		public Int32	bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT
	{
		public Int32	x;
		public Int32	y;
	}

	internal class NativeMethods
	{
		[DllImport("user32.dll")]
		public static extern Boolean	GetCursorPos(out POINT point);

		[DllImport("user32.dll")]
		public static extern Boolean	SetCursorPos(Int32 x, Int32 y);

		[DllImport("user32.dll")]
		public static extern Int16	GetAsyncKeyState(Int32 virtualKeyCode);

		[DllImport("user32.dll")]
		public static extern IntPtr	GetActiveWindow();

		[DllImport("user32.dll")]
		public static extern Int16	GetKeyState(Int32 nVirtKey);

		[DllImport("user32.dll")]
		public static extern void	mouse_event(UInt32 dwFlags, Int32 dx, Int32 dy, UInt32 cButtons, IntPtr dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern Int32	SetWindowLong(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);

		[DllImport("user32.dll")]
		public static extern Boolean	SetLayeredWindowAttributes(IntPtr hwnd, Int32 crKey, Int32 bAlpha, UInt32 dwFlags);

		[DllImport("user32.dll")]
		public static extern Boolean	SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, Int32 x, Int32 y, Int32 cx, Int32 cy, UInt32 uFlags);

		[DllImport("user32.dll")]
		public static extern Boolean	ShowWindowAsync(IntPtr hWnd, Int32 nCmdShow);

		[DllImport("Dwmapi.dll")]
		public static extern Int32	DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

		[DllImport("user32.dll")]
		public static extern UInt32	SendInput(UInt32 nInputs, INPUT[] pInputs, Int32 cbSize);

		public static void	SendKeyPress(UInt16 keyCode)
		{
			INPUT	input = new INPUT {
				type = 1
			};
			input.data.keyboard = new KEYBDINPUT() {
				vk = keyCode,
				scan = 0,
				flags = 0,
				time = 0,
				extraInfo = IntPtr.Zero,
			};

			INPUT	input2 = new INPUT {
				type = 1
			};
			input2.data.keyboard = new KEYBDINPUT() {
				vk = keyCode,
				scan = 0,
				flags = 2,
				time = 0,
				extraInfo = IntPtr.Zero
			};

			INPUT[]	inputs = new INPUT[] { input, input2 };

			if (NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
				throw new Exception();
		}

		public static void	SendKeyDown(UInt16 keyCode)
		{
			INPUT	input = new INPUT{
				type = 1
			};
			input.data.keyboard = new KEYBDINPUT()
			{
				vk = keyCode,
				scan = 0,
				flags = 0,
				time = 0,
				extraInfo = IntPtr.Zero
			};

			INPUT[]	inputs = new INPUT[] { input };

			if (NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
				throw new Exception();
		}

		public static void	SendKeyUp(UInt16 keyCode)
		{
			INPUT	input = new INPUT {
				type = 1
			};
			input.data.keyboard = new KEYBDINPUT()
			{
				vk = keyCode,
				scan = 0,
				flags = 2,
				time = 0,
				extraInfo = IntPtr.Zero
			};

			INPUT[]	inputs = new INPUT[] { input };

			if (NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
				throw new Exception();
		}
	}
}