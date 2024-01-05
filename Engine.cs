#if !IS_LINUX
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Game.Win32;
using Microsoft.Win32.SafeHandles;

namespace Game
{
    namespace Win32
    {
        public delegate void ConsoleMouseEvent(MouseEvent r);
        public delegate void ConsoleKeyEvent(KeyEvent r);

        public static class ConsoleListener
        {
            public static event ConsoleMouseEvent OnMouseEvent;
            public static event ConsoleKeyEvent OnKeyEvent;

            static bool IsRunning = false;

            public static void Start()
            {
                if (!IsRunning)
                {
                    IsRunning = true;

                    ConsoleHandle inputHandle = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);

                    new Thread(() =>
                    {
                        while (IsRunning)
                        {
                            uint readRecords = 0;
                            InputEvent record = new InputEvent();
                            Kernel32.ReadConsoleInput(inputHandle, ref record, 1, ref readRecords);

                            if (IsRunning)
                            {
                                switch (record.EventType)
                                {
                                    case Kernel32.MOUSE_EVENT:
                                        OnMouseEvent?.Invoke(record.MouseEvent);
                                        break;
                                    case Kernel32.KEY_EVENT:
                                        OnKeyEvent?.Invoke(record.KeyEvent);
                                        break;
                                }
                            }
                        }
                    }).Start();
                }
            }

            public static void Stop() => IsRunning = false;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputEvent
        {
            [FieldOffset(0)] public short EventType;
            [FieldOffset(4)] public KeyEvent KeyEvent;
            [FieldOffset(4)] public MouseEvent MouseEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseEvent
        {
            public ConsolePoint MousePosition;
            public int ButtonState;
            public int ControlKeyState;
            public int EventFlags;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct KeyEvent
        {
            [MarshalAs(UnmanagedType.Bool)]
            [FieldOffset(0)] public bool IsKeyDown;
            [FieldOffset(4)] public ushort RepeatCount;
            [FieldOffset(6)] public ushort VirtualKeyCode;
            [FieldOffset(8)] public ushort VirtualScanCode;
            [FieldOffset(10)] public char UnicodeChar;
            [FieldOffset(10)] public byte AsciiChar;
            [FieldOffset(12)] public int ControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ConsolePoint
        {
            public short X;
            public short Y;

            public ConsolePoint(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct ConsoleCharacter
        {
            [FieldOffset(0)] public char Char;
            [FieldOffset(2)] public ushort Attributes;

            public static ConsoleCharacter Zero => new ConsoleCharacter('\0', (byte)0, (byte)0);

            public ConsoleCharacter(char @char, byte foregroundColor, byte backgroundColor)
            {
                Char = @char;

                foregroundColor &= 0xf;
                backgroundColor &= 0xf;

                Attributes = (ushort)((backgroundColor << 4) | foregroundColor);
            }

            public ConsoleCharacter(char @char, Color foregroundColor, Color backgroundColor) : this(@char, (byte)foregroundColor, (byte)backgroundColor)
            { }

            public static implicit operator char(ConsoleCharacter v) => v.Char;
            public static implicit operator ConsoleCharacter(char v) => new ConsoleCharacter(v, Color.Silver, Color.Black);
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
            public const int STD_INPUT_HANDLE = -10;

            public const uint ENABLE_MOUSE_INPUT = 0x0010;
            public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
            public const uint ENABLE_EXTENDED_FLAGS = 0x0080;

            public const short KEY_EVENT = 1;
            public const short MOUSE_EVENT = 2;

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetConsoleMode(
                ConsoleHandle consoleHandle,
                ref uint mode);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern ConsoleHandle GetStdHandle(int stdHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ReadConsoleInput(
                ConsoleHandle consoleInputHandle,
                ref InputEvent buffer,
                uint length,
                ref uint numberOfEventsRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetConsoleMode(
                ConsoleHandle consoleHandle,
                uint mode);

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
              SafeFileHandle consoleOutputHandle,
              ConsoleCharacter[] buffer,
              ConsolePoint bufferSize,
              ConsolePoint bufferCoord,
              ref SmallRect writeRegion);

            [DllImport("user32.dll")]
            public static extern short GetKeyState(int virtualKey);
        }
    }

    /// <summary>
    /// Console color
    /// </summary>
    /// <remarks>
    /// Represents console color in 4-bit format as follows: <c>IRGB</c>
    /// <br/>
    /// <c>I</c> - Intensity
    /// <br/>
    /// <c>R</c> - Red
    /// <br/>
    /// <c>G</c> - Green
    /// <br/>
    /// <c>B</c> - Blue
    /// </remarks>
    public enum Color : byte
    {
        Red = 0x4,
        Green = 0x2,
        Blue = 0x1,
        Yellow = Color.Red | Color.Green,
        Cyan = Color.Green | Color.Blue,
        Magenta = Color.Red | Color.Blue,

        BrightRed = 0x8 | Color.Red,
        BrightGreen = 0x8 | Color.Green,
        BrightBlue = 0x8 | Color.Blue,
        BrightYellow = 0x8 | Color.Yellow,
        BrightCyan = 0x8 | Color.Cyan,
        BrightMagenta = 0x8 | Color.Magenta,

        Black = 0x0,
        Silver = 0x7,
        Gray = 0x8,
        White = 0xf,
    }

    /// <summary>
    /// Helper class for handling keyboard input
    /// </summary>
    public static class Keyboard
    {
        /// <summary>
        /// Checks whether the key associated with <paramref name="character"/> is down or not.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the key is pressed, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(char character)
            => IsKeyPressed(KeyToVirtual(character));

        /// <summary>
        /// Checks whether the specified key is down or not.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the key is pressed, <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// For the list of key codes, see <see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes"/>
        /// </remarks>
        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(int virtualScanCode)
        {
            short state = Win32.Kernel32.GetKeyState(virtualScanCode);
            return state != 0 && state != 1;
        }

        /// <summary>
        /// Converts the character to a virtual key identifier
        /// </summary>
        /// <remarks>
        /// For the list of key codes, see <see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes"/>
        /// </remarks>
        /// <exception cref="NotImplementedException"/>
        static int KeyToVirtual(char key)
        {
            key = char.ToUpperInvariant(key);
            if (key >= '0' && key <= '9') return key;
            if (key >= 'A' && key <= 'Z') return key;
            switch (key)
            {
                case '\r': return 0x0D;
                case ' ': return 0x20;
                default: throw new NotImplementedException($":(");
            }
        }
    }

    /// <summary>
    /// Simple handler for manipulating the console buffer
    /// </summary>
    public readonly struct Drawer
    {
        readonly ConsoleCharacter[] Buffer;

        public readonly int Width;
        public readonly short Height;

        public ConsoleCharacter this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return ConsoleCharacter.Zero;
                if (x >= Width || y >= Height) return ConsoleCharacter.Zero;
                return Buffer[x + (y * Width)];
            }
            set
            {
                if (x < 0 || y < 0) return;
                if (x >= Width || y >= Height) return;
                Buffer[x + (y * Width)] = value;
            }
        }

        public ConsoleCharacter this[double x, double y]
        {
            get => this[(int)Math.Round(x), (int)Math.Round(y)];
            set => this[(int)Math.Round(x), (int)Math.Round(y)] = value;
        }

        public ConsoleCharacter this[ConsolePoint point]
        {
            get => this[point.X, point.Y];
            set => this[point.X, point.Y] = value;
        }

        public Drawer(ConsoleCharacter[] buffer, int width, short height)
        {
            Buffer = buffer;
            Width = width;
            Height = height;
        }

        public void DrawText(int x, int y, string text, Color foregroundColor = Color.Silver, Color backgroundColor = Color.Black)
        {
            for (int i = 0; i < text.Length; i++)
            { this[x + i, y] = new ConsoleCharacter(text[i], foregroundColor, backgroundColor); }
        }

        public void Clear() => Array.Clear(Buffer, 0, Buffer.Length);
    }

    /// <summary>
    /// Helper class for handling mouse input
    /// </summary>
    public class Mouse
    {
        /// <summary>
        /// The mouse position in console rows and columns
        /// </summary>
        public static ConsolePoint Position { get; private set; }

        /// <summary>
        /// Is the left mouse button down?
        /// </summary>
        public static bool IsLeftDown { get; private set; }
        /// <summary>
        /// Is the right mouse button down?
        /// </summary>
        public static bool IsRightDown { get; private set; }

        public static void HandleMouseEvent(Win32.MouseEvent e)
        {
            Position = new ConsolePoint(e.MousePosition.X, e.MousePosition.Y);
            IsLeftDown = (e.ButtonState & 1) != 0;  // 0b_0001
            IsRightDown = (e.ButtonState & 2) != 0; // 0b_0010
        }
    }

    /// <summary>
    /// The interface of your game
    /// </summary>
    public interface IGame
    {
        /// <summary>
        /// This will run once before the first <see cref="Update(Drawer)"/>.
        /// </summary>
        void Initialize();

        /// <summary>
        /// This will automatically called within a while loop
        /// </summary>
        /// <param name="drawer">
        /// An instance of a console buffer manipulating handler
        /// </param>
        void Update(Drawer drawer);
    }

    public class Engine
    {
        static Engine Instance;

        bool ShouldExit;
        readonly SafeFileHandle Handle;

        /// <summary>
        /// The instance of your game
        /// </summary>
        readonly IGame Game;

        /// <summary>
        /// <para>
        /// The console buffer
        /// </para>
        /// <para>
        /// Think of it as a 2d array, but the windows API function can't accept a 2d array,
        /// but this is it, believe me.
        /// </para>
        /// </summary>
        ConsoleCharacter[] Buffer;

        /// <summary>
        /// The size of <see cref="Buffer"/>
        /// </summary>
        Win32.SmallRect Rect;

        double lastTime;
        float deltaTime;

        /// <summary>
        /// The elapsed time since the last update measured in seconds
        /// </summary>
        public static float DeltaTime => Instance.deltaTime;

        /// <summary>
        /// The current time measured in seconds
        /// </summary>
        /// <remarks>
        /// More accurately, this is how many seconds passed since midnight.
        /// </remarks>
        public static float Now => (float)Instance.lastTime;

        /// <summary>
        /// Frame / seconds
        /// </summary>
        public static float FPS => 1f / Instance.deltaTime;

        /// <summary>
        /// Console width in characters (number of columns)
        /// </summary>
        public static short Width => (short)Console.WindowWidth;
        /// <summary>
        /// Console height measured in characters (number of lines)
        /// </summary>
        public static short Height => (short)Console.WindowHeight;

        Engine(IGame game)
        {
            Instance = this;

            Game = game;

            Buffer = new ConsoleCharacter[Width * Height];

            // Lekérjük a standard output handle-jét.
            // Úgy néz ki mint ha egy fájlal csinálnánk valamit. Ez igazából igaz, de a fájl
            // az a standard output.
            Handle = Win32.Kernel32.CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            Rect = new Win32.SmallRect()
            {
                Left = 0,
                Top = 0,
                Right = Width,
                Bottom = Height,
            };

            lastTime = DateTime.UtcNow.TimeOfDay.TotalSeconds;
        }

        /// <summary>
        /// Call this to start your game
        /// </summary>
        /// <param name="game">
        /// The instance of your game
        /// </param>
        /// <remarks>
        /// To get started, create a class that implements the
        /// interface <see cref="IGame"/>. Then create an instance of it, and
        /// pass that into this function.
        /// </remarks>
        public static void DoTheStuff(IGame game) => new Engine(game).OnStart();

        void OnStart()
        {
            // Lekérjük a standard input handle-jét, mert
            // be kell állítani pár dolgot rajta
            Win32.ConsoleHandle inHandle = Win32.Kernel32.GetStdHandle(Win32.Kernel32.STD_INPUT_HANDLE);
            uint mode = 0;
            // először lekérjük a konzol módot
            Win32.Kernel32.GetConsoleMode(inHandle, ref mode);
            // letiltjuk a kijelölést
            mode &= ~Win32.Kernel32.ENABLE_QUICK_EDIT_MODE;
            // engedélyezzük az egér bemenetet
            mode |= Win32.Kernel32.ENABLE_MOUSE_INPUT;
            // beállítjuk a konzol módot
            Win32.Kernel32.SetConsoleMode(inHandle, mode);

            // Hallgatunk egy egér event-re
            // Ha valami történik az egérrel, a "Mouse.HandleMouseEvent" fog lefutni
            Win32.ConsoleListener.OnMouseEvent += Mouse.HandleMouseEvent;
            Win32.ConsoleListener.Start();

            // Meghívjuk a te function-odat, ami initializálja a játékod
            Game.Initialize();

            // Addig amíg a stdin handle érvényes (ha a konzolt bezárjuk akkor érvénytelen lesz) ...
            while (!Handle.IsInvalid)
            {
                // Kiszámítjuk a delta time-t (ez a 3 sor)
                double now = DateTime.UtcNow.TimeOfDay.TotalSeconds;
                deltaTime = (float)(now - lastTime);
                lastTime = now;

                // Ha az elmentett konzol mérete nem egyezik meg a ténylegessel, ...
                if (Rect.Right != Width || Rect.Bottom != Height)
                {
                    // Frissítjük a buffer-t (ezzel kitörlünk mindent amit eddig "rajzoltunk" a buffer-be)
                    Buffer = new ConsoleCharacter[Width * Height];
                    // és elmentjük az új méretet (2 sor)
                    Rect.Right = Width;
                    Rect.Bottom = Height;
                }
                else
                {
                    // Kitöröljük a buffer-t
                    Array.Clear(Buffer, 0, Buffer.Length);
                }

                // Most itt a buffer mérete rendben van, és üres karakterekkel van feltöltve

                // Meghívjuk a te function-odat, ami kezeli majd a játék dolgokat
                Game.Update(new Drawer(Buffer, Width, Height));

                // Meghívunk egy Win32 API-t, ami a buffert tényleg átmásolja a konzolba
                _ = Win32.Kernel32.WriteConsoleOutputW(Handle, Buffer,
                        new Win32.ConsolePoint() { X = Width, Y = Height },
                        new Win32.ConsolePoint() { X = 0, Y = 0 },
                        ref Rect);

                if (ShouldExit)
                { break; }
            }

            // Ide akkor jövünk ha bezártuk a konzolt

            // Befejezzük a hallgatást az egérre
            Win32.ConsoleListener.OnMouseEvent -= Mouse.HandleMouseEvent;
            // Leállítjuk a "ConsoleListener"-t
            Win32.ConsoleListener.Stop();
        }

        /// <summary>
        /// It exits the while loop, which calls your <see cref="IGame.Update(Drawer)"/> function.
        /// That is, it exits the game.
        /// Maybe it doesn't work, I don't feel like fixing it.
        /// </summary>
        public static void Exit() => Instance.ShouldExit = true;
    }
}
#endif
