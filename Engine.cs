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
                // Ha még nincs elindítva (azért ellenőrizzük, hogy ha pl 2x hívjuk meg ezt
                // a function-t akkor ne rontsunk el semmit)
                if (!Run)
                {
                    Run = true;

                    // Win32 API, lekérjük az stdin handle-jét
                    var handleIn = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);

                    // Egy új thread-ot csinálunk
                    // Egy thread külön fut a kód többi részeitől. Saját stack-ja van neki, de a HEAP az közös.
                    new System.Threading.Thread(() =>
                    {
                        while (true)
                        {
                            // Win32 API, olvasunk egy event-et az stdin-ről (3 sor)
                            uint numRead = 0;
                            INPUT_RECORD record = default;
                            Kernel32.ReadConsoleInput(handleIn, ref record, 1, ref numRead);

                            // Ha nem kéne még leállni, akkor yah
                            if (Run)
                            {
                                switch (record.EventType)
                                {
                                    case Kernel32.MOUSE_EVENT: // Ha az egérrel történt valami
                                        // Meghívjuk az összes function-t ami ezt hallgatja
                                        MouseEvent?.Invoke(record.MouseEvent);
                                        break;
                                    case Kernel32.KEY_EVENT: // Ha a billentyűzettel történt valami
                                        // Meghívjuk az összes function-t ami ezt hallgatja
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
            public const int STD_INPUT_HANDLE = -10;

            public const uint ENABLE_MOUSE_INPUT = 0x0010;
            public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
            public const uint ENABLE_EXTENDED_FLAGS = 0x0080;

            public const short KEY_EVENT = 1;
            public const short MOUSE_EVENT = 2;

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetConsoleMode(ConsoleHandle hConsoleHandle, ref uint lpMode);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern ConsoleHandle GetStdHandle(int nStdHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ReadConsoleInput(ConsoleHandle hConsoleInput, ref INPUT_RECORD lpBuffer, uint nLength, ref uint lpNumberOfEventsRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetConsoleMode(ConsoleHandle hConsoleHandle, uint dwMode);

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
            public static extern short GetKeyState(int nVirtualKey);
        }
    }

    /// <summary>
    /// Egy konzol szín. A konzolban nem használhatunk (vagyis de de mind1)
    /// RGB színt, mert a Win32 API nem support-álja.
    /// Ez is egy bitmask cucc, amit így kell értelmezni:<br/>
    /// <c>IRGB</c>
    /// <br/>
    /// <c>I</c> - Intensity
    /// <br/>
    /// <c>R</c> - Red
    /// <br/>
    /// <c>G</c> - Green
    /// <br/>
    /// <c>B</c> - Blue
    /// </summary>
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
        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(char key)
            => IsKeyPressed(KeyToVirtual(key));

        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(int virtualScanCode)
        {
            // Ez is egy Win32 API, lekéri a billentyű helyzetét
            short state = Win32.Kernel32.GetKeyState(virtualScanCode);
            return state switch
            {
                0 => false,
                1 => false,
                _ => true,
            };
        }

        /// <summary>
        /// Átkonvertálja a karaktert egy virtális billentyű azonosítóvá
        /// <br/>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes"/>
        /// </summary>
        /// <exception cref="NotImplementedException"/>
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

    // Sry de ez szerintem egyértelmű mit csinál, nem commentelem
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

        internal void Clear() => Array.Clear(buffer, 0, buffer.Length);
    }

    public class Mouse
    {
        public static System.Numerics.Vector2 Position { get; private set; }
        public static bool IsLeftDown { get; private set; }

        // Ez lefut ha az egérrel történik valami
        public static void HandleMouseEvent(Win32.MOUSE_EVENT_RECORD e)
        {
            // Lementjük az egér pozícióját (a konzolhoz relatívan, avagy "console space"-en)
            Position = new System.Numerics.Vector2(e.dwMousePosition.X, e.dwMousePosition.Y);
            // A "dwButtonState" minden bit-je egy igaz-hamis érték.
            // Ezzel akár 32 igaz-hamis értéket is eltárolhatunk egy int-ben.
            // Tehát ennek a legutolsó bit-jét szedjük ki, ami a bal klikk helyzete (1 = lenyomva, 0 = nem lenyomva)
            IsLeftDown = (e.dwButtonState & 0b_1) != 0;
        }
    }

    /// <summary>
    /// A játékod interfésze
    /// </summary>
    public interface IGame
    {
        /// <summary>
        /// Ez egyszer fog lefutni az első "Update" előtt
        /// </summary>
        public void Initialize();

        /// <summary>
        /// Ez automatikusan meg lesz hívva egy while loop-ban
        /// </summary>
        /// <param name="drawer">
        /// Rajzoló kezelő cucc
        /// </param>
        public void Update(Drawer drawer);
    }

    /// <summary>
    /// A fő game engine cucc, ez kezeli a mindent is amit neked nem kell
    /// </summary>
    public class Engine
    {
        static Engine Instance;

        /// <summary>
        /// Ki kéne lépni a játékból?
        /// </summary>
        bool ShouldExit;

        /// <summary>
        /// A játékod példánya
        /// <br/>
        /// Az <see cref="Engine"/>-t nem érdekli hogy működik, a lényeg, hogy legyen benne
        /// egy Update és egy Initialize function
        /// </summary>
        readonly IGame Game;

        /// <summary>
        /// Standard output handle
        /// <br/>
        /// A standard output az egy úgymond adat stream,
        /// amit szöveges kimenetként szoktunk használni.
        /// Pl a Console.WriteLine ebbe a stream-ba ír.
        /// <br/>
        /// A handle egy windows cucc, kissé hosszú lenne elmagyarázni.
        /// </summary>
        readonly SafeFileHandle Handle;

        /// <summary>
        /// A konzol buffer
        /// <br/>
        /// Ezt egy 2d-s tömbnek képzeld el, de a windows API function nem
        /// tud 2d-s tömböt elfogadni, de ez az hidd el nekem.
        /// <br/>
        /// Minden elem a tömbben egy karakter a konzolon.
        /// Ezért ha a konzolt átméretezed, kezelni kell ennek a tömbnek a méretét is.
        /// </summary>
        Win32.CharInfo[] Buffer;
        /// <summary>
        /// A buffer mérete
        /// </summary>
        Win32.SmallRect Rect;

        /// <summary>
        /// Az előző game frame ideje (kell a delta time számításához)
        /// </summary>
        double lastTime;
        /// <summary>
        /// Az eltárolt delta time, hogy ne kelljen folyton kiszámítani amikor kell nekünk.
        /// </summary>
        float deltaTime;

        /// <summary>
        /// Az eltelt idő az előző frissítés óta másodpercekben mérve
        /// </summary>
        public static float DeltaTime => Instance.deltaTime;

        /// <summary>
        /// A mostani idő másodpercekben mérve
        /// </summary>
        public static float Now => (float)Instance.lastTime;

        /// <summary>
        /// Frame per seconds
        /// </summary>
        public static float FPS => 1f / Instance.deltaTime;

        /// <summary>
        /// A konzol szélessége karakterekben mérve (oszlopok száma)
        /// </summary>
        public static short Width => (short)Console.WindowWidth;
        /// <summary>
        /// A konzol magassága karakterekben mérve (sorok száma)
        /// </summary>
        public static short Height => (short)Console.WindowHeight;

        Engine(IGame game)
        {
            Instance = this;

            Game = game;

            Buffer = new Win32.CharInfo[Width * Height];

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
        /// Ezt hívd meg hogy elindítsd a játékod
        /// </summary>
        /// <param name="game">
        /// Ide a játékod példányát adj meg, amit majd ez automatikusan lefuttat
        /// </param>
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

            // Elindítjuk a ConsoleListener-t
            Win32.ConsoleListener.Start();
            // Hallgatunk egy egér event-re
            // Ha valami történik az egérrel, a "Mouse.HandleMouseEvent" fog lefutni
            Win32.ConsoleListener.MouseEvent += Mouse.HandleMouseEvent;

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
                    Buffer = new Win32.CharInfo[Width * Height];
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

                // Meghívunk egy Win32 API-t, ami a buffert tényleg átmásolja a konzole-ba
                _ = Win32.Kernel32.WriteConsoleOutputW(Handle, Buffer,
                        new Win32.Coord() { X = Width, Y = Height },
                        new Win32.Coord() { X = 0, Y = 0 },
                        ref Rect);

                if (ShouldExit)
                { break; }
            }

            // Ide akkor jövünk ha bezártuk a konzolt

            // Befejezzük a hallgatást az egérre
            Win32.ConsoleListener.MouseEvent -= Mouse.HandleMouseEvent;
            // Leállítjuk a "ConsoleListener"-t
            Win32.ConsoleListener.Stop();
        }

        /// <summary>
        /// Kilép a while loop-ból ami lefuttatja a te "Update" function-odat. Vagyis kilép a játékból.
        /// Lehet hogy nem működik, nincs kedvem kijavítani a hibát.
        /// </summary>
        public static void Exit() => Instance.ShouldExit = true;
    }
}
