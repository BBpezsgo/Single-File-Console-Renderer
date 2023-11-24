using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CA1401 // P/Invokes should not be visible

namespace Game
{
    namespace Win32
    {
        public static class ConsoleListener
        {
            public static event ConsoleMouseEvent MouseEvent;
            public static event ConsoleKeyEvent KeyEvent;

            static bool Run = false;

            public static void Start()
            {
                if (!Run)
                {
                    Run = true;
                    var handleIn = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);
                    new System.Threading.Thread(() =>
                    {
                        while (true)
                        {
                            uint numRead = 0;
                            INPUT_RECORD record = default;
                            Kernel32.ReadConsoleInput(handleIn, ref record, 1, ref numRead);
                            if (Run)
                            {
                                switch (record.EventType)
                                {
                                    case Kernel32.MOUSE_EVENT:
                                        MouseEvent?.Invoke(record.MouseEvent);
                                        break;
                                    case Kernel32.KEY_EVENT:
                                        KeyEvent?.Invoke(record.KeyEvent);
                                        break;
                                }
                            }
                        }
                    }).Start();
                }
            }

            public static void Stop() => Run = false;


            public delegate void ConsoleMouseEvent(MOUSE_EVENT_RECORD r);
            public delegate void ConsoleKeyEvent(KEY_EVENT_RECORD r);
        }

        [DebuggerDisplay("EventType: {EventType}")]
        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public Int16 EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;
        }

        [DebuggerDisplay("{dwMousePosition.X}, {dwMousePosition.Y}")]
        public struct MOUSE_EVENT_RECORD
        {
            public Coord dwMousePosition;
            public Int32 dwButtonState;
            public Int32 dwControlKeyState;
            public Int32 dwEventFlags;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct KEY_EVENT_RECORD
        {
            [FieldOffset(0)]
            [MarshalAsAttribute(UnmanagedType.Bool)]
            public Boolean bKeyDown;
            [FieldOffset(4)]
            public UInt16 wRepeatCount;
            [FieldOffset(6)]
            public UInt16 wVirtualKeyCode;
            [FieldOffset(8)]
            public UInt16 wVirtualScanCode;
            [FieldOffset(10)]
            public Char UnicodeChar;
            [FieldOffset(10)]
            public Byte AsciiChar;
            [FieldOffset(12)]
            public Int32 dwControlKeyState;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct CharInfo
        {
            [FieldOffset(0)] public char Char;
            [FieldOffset(2)] public ushort Attributes;

            public CharInfo(char @char, byte foregroundColor, byte backgroundColor)
            {
                Char = @char;

                foregroundColor &= 0b_1111;
                backgroundColor &= 0b_1111;

                Attributes = (ushort)((backgroundColor << 4) | foregroundColor);
            }

            public CharInfo(char @char, Color foregroundColor, Color backgroundColor) : this(@char, (byte)foregroundColor, (byte)backgroundColor)
            { }

            public static implicit operator char(CharInfo v) => v.Char;
            public static implicit operator CharInfo(char v) => new(v, 0b_0111, 0b_0000);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        public class ConsoleHandle : SafeHandleMinusOneIsInvalid
        {
            public ConsoleHandle() : base(false) { }
            protected override bool ReleaseHandle() => true;
        }

        public static class Kernel32
        {
            public const Int32 STD_INPUT_HANDLE = -10;

            public const UInt32 ENABLE_MOUSE_INPUT = 0x0010;
            public const UInt32 ENABLE_QUICK_EDIT_MODE = 0x0040;
            public const UInt32 ENABLE_EXTENDED_FLAGS = 0x0080;

            public const short KEY_EVENT = 1;
            public const short MOUSE_EVENT = 2;

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean GetConsoleMode(ConsoleHandle hConsoleHandle, ref UInt32 lpMode);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern ConsoleHandle GetStdHandle(Int32 nStdHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean ReadConsoleInput(ConsoleHandle hConsoleInput, ref INPUT_RECORD lpBuffer, UInt32 nLength, ref UInt32 lpNumberOfEventsRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean SetConsoleMode(ConsoleHandle hConsoleHandle, UInt32 dwMode);

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(
                string fileName,
                [MarshalAs(UnmanagedType.U4)] uint fileAccess,
                [MarshalAs(UnmanagedType.U4)] uint fileShare,
                IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] int flags,
                IntPtr template);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteConsoleOutputW(
              SafeFileHandle hConsoleOutput,
              CharInfo[] lpBuffer,
              Coord dwBufferSize,
              Coord dwBufferCoord,
              ref SmallRect lpWriteRegion);

            [DllImport("user32.dll")]
            public static extern short GetKeyState(int nVirtKey);
        }
    }

    public enum Color : byte
    {
        Red = 0b_0100,
        Green = 0b_0010,
        Blue = 0b_0001,
        Yellow = 0b_0110,
        Cyan = 0b_0011,
        Magenta = 0b_0101,

        BrightRed = 0b_1100,
        BrightGreen = 0b_1010,
        BrightBlue = 0b_1001,
        BrightYellow = 0b_1110,
        BrightCyan = 0b_1011,
        BrightMagenta = 0b_1101,

        Black = 0b_0000,
        Silver = 0b_0111,
        Gray = 0b_1000,
        White = 0b_1111,
    }

    public static class Keyboard
    {
        public static bool IsKeyPressed(char key)
            => IsKeyPressed(KeyToVirtual(key));

        public static bool IsKeyPressed(int virtualScanCode)
        {
            short state = Win32.Kernel32.GetKeyState(virtualScanCode);
            return state switch
            {
                0 => false,
                1 => false,
                _ => true,
            };
        }

        static int KeyToVirtual(char key)
        {
            key = char.ToUpperInvariant(key);
            if (key >= '0' && key <= '9') return key;
            if (key >= 'A' && key <= 'Z') return key;
            return key switch
            {
                '\r' => 0x0D,
                ' ' => 0x20,
                _ => throw new NotImplementedException($":("),
            };
        }
    }

    public readonly struct Drawer
    {
        readonly Win32.CharInfo[] buffer;

        public readonly int Width;
        public readonly short Height;

        public Win32.CharInfo this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return default;
                if (x >= Width || y >= Height) return default;
                return buffer[x + (y * Width)];
            }
            set
            {
                if (x < 0 || y < 0) return;
                if (x >= Width || y >= Height) return;
                buffer[x + (y * Width)] = value;
            }
        }

        public Win32.CharInfo this[double x, double y]
        {
            get => this[(int)Math.Round(x), (int)Math.Round(y)];
            set => this[(int)Math.Round(x), (int)Math.Round(y)] = value;
        }

        public Win32.CharInfo this[System.Numerics.Vector2 point]
        {
            get => this[point.X, point.Y];
            set => this[point.X, point.Y] = value;
        }

        public Drawer(Win32.CharInfo[] buffer, int width, short height)
        {
            this.buffer = buffer;
            Width = width;
            Height = height;
        }

        public void DrawText(int x, int y, string text, Color foregroundColor = Color.Silver, Color backgroundColor = Color.Black)
        {
            for (int i = 0; i < text.Length; i++)
            {
                this[x + i, y] = new Win32.CharInfo(text[i], foregroundColor, backgroundColor);
            }
        }
    }

    public class Mouse
    {
        public static System.Numerics.Vector2 Position { get; private set; }
        public static bool IsLeftDown { get; private set; }

        public static void HandleMouseEvent(Win32.MOUSE_EVENT_RECORD e)
        {
            Position = new System.Numerics.Vector2(e.dwMousePosition.X, e.dwMousePosition.Y);
            IsLeftDown = (e.dwButtonState & 0b_1) != 0;
        }
    }

    public class Engine
    {
        readonly Action<Drawer> UpdateCallback;
        readonly SafeFileHandle Handle;

        Win32.CharInfo[] Buffer;
        Win32.SmallRect Rect;

        static short Width => (short)Console.WindowWidth;
        static short Height => (short)Console.WindowHeight;

        Engine(Action<Drawer> updateCallback)
        {
            UpdateCallback = updateCallback;
            Buffer = new Win32.CharInfo[Width * Height];
            Handle = Win32.Kernel32.CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            Rect = new Win32.SmallRect()
            {
                Left = 0,
                Top = 0,
                Right = Width,
                Bottom = Height,
            };
        }

        public static void DoTheStuff(Action<Drawer> callback) => new Engine(callback).OnStart();

        void OnStart()
        {
            Win32.ConsoleHandle inHandle = Win32.Kernel32.GetStdHandle(Win32.Kernel32.STD_INPUT_HANDLE);
            uint mode = 0;
            Win32.Kernel32.GetConsoleMode(inHandle, ref mode);
            mode &= ~Win32.Kernel32.ENABLE_QUICK_EDIT_MODE;
            mode |= Win32.Kernel32.ENABLE_MOUSE_INPUT;
            Win32.Kernel32.SetConsoleMode(inHandle, mode);

            Win32.ConsoleListener.Start();
            Win32.ConsoleListener.MouseEvent += Mouse.HandleMouseEvent;

            while (!Handle.IsInvalid)
            {
                if (Rect.Right != Width || Rect.Bottom != Height)
                {
                    Buffer = new Win32.CharInfo[Width * Height];
                    Rect.Right = Width;
                    Rect.Bottom = Height;
                }
                else
                {
                    Array.Clear(Buffer, 0, Buffer.Length);
                }

                UpdateCallback.Invoke(new Drawer(Buffer, Width, Height));

                _ = Win32.Kernel32.WriteConsoleOutputW(Handle, Buffer,
                        new Win32.Coord() { X = Width, Y = Height },
                        new Win32.Coord() { X = 0, Y = 0 },
                        ref Rect);
            }

            Win32.ConsoleListener.MouseEvent -= Mouse.HandleMouseEvent;
            Win32.ConsoleListener.Stop();
        }
    }
}
