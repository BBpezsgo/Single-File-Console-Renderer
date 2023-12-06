#if IS_LINUX
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Game
{
    namespace Win32
    {
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

            public static CharInfo Zero => new CharInfo('\0', (byte)0, (byte)0);

            public CharInfo(char @char, byte foregroundColor, byte backgroundColor)
            {
                Char = @char;

                foregroundColor &= 0xf;
                backgroundColor &= 0xf;

                Attributes = (ushort)((backgroundColor << 4) | foregroundColor);
            }

            public CharInfo(char @char, Color foregroundColor, Color backgroundColor) : this(@char, (byte)foregroundColor, (byte)backgroundColor)
            { }

            public static implicit operator char(CharInfo v) => v.Char;
            public static implicit operator CharInfo(char v) => new CharInfo(v, 0x7, 0x0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
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

    public static class Keyboard
    {
        static readonly bool[] Buffer = new bool[byte.MaxValue];

        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(char key)
        {
            key = char.ToUpperInvariant(key);
            if (key < byte.MinValue || key >= byte.MaxValue) return false;
            return Buffer[key];
        }

        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(int virtualScanCode)
        {
            if (virtualScanCode < byte.MinValue || virtualScanCode >= byte.MaxValue) return false;
            return Buffer[virtualScanCode];
        }

        /// <summary>
        /// Átkonvertálja a karaktert egy virtuális billentyű azonosítóvá
        /// </summary>
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

        public static void Reset() => Array.Clear(Buffer, 0, Buffer.Length);

        public static void Feed(ConsoleKeyInfo c)
        {
            char keyChar = char.ToUpperInvariant(c.KeyChar);
            if (keyChar < byte.MinValue || keyChar > byte.MaxValue - 1) return;

            if (keyChar != 0)
            {
                Buffer[keyChar] = true;
                return;
            }

            int key = (int)c.Key;
            if (key < byte.MinValue || key > byte.MaxValue - 1) return;

            Buffer[key] = true;
        }
    }

    // Sry de ez szerintem egyértelmű mit csinál, nem commentelem
    public struct Drawer
    {
        readonly Win32.CharInfo[] buffer;

        public readonly int Width;
        public readonly short Height;

        public Win32.CharInfo this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return Win32.CharInfo.Zero;
                if (x >= Width || y >= Height) return Win32.CharInfo.Zero;
                return buffer[x + (y * Width)];
            }
            set
            {
                if (x < 0 || y < 0) return;
                if (x >= Width || y >= Height) return;
                buffer[x + (y * Width)] = value;
#if false
                Console.Out.Write($"\x1b[{y + 1};{x + 1}H");
                Console.Out.Write($"\x1b[38;5;{value.Attributes & 0b_1111}m");
                Console.Out.Write($"\x1b[48;5;{(value.Attributes >> 4) & 0b_1111}m");
                Console.Out.Write(value.Char);
#endif
            }
        }

        public Win32.CharInfo this[double x, double y]
        {
            get => this[(int)Math.Round(x), (int)Math.Round(y)];
            set => this[(int)Math.Round(x), (int)Math.Round(y)] = value;
        }

        public Win32.CharInfo this[Vector2 point]
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
            { this[x + i, y] = new Win32.CharInfo(text[i], foregroundColor, backgroundColor); }
        }

        internal void Clear()
        {
            Console.Clear();
            Array.Clear(buffer, 0, buffer.Length);
        }
    }

    public class Mouse
    {
        public static Vector2 Position { get; private set; }
        public static bool IsLeftDown { get; private set; }

#if WIN32
        // Ez lefut ha az egérrel történik valami
        public static void HandleMouseEvent(Win32.MOUSE_EVENT_RECORD e)
        {
            // Lementjük az egér pozícióját (a konzolhoz relatívan, avagy "console space"-en)
            Position = new Vector2(e.dwMousePosition.X, e.dwMousePosition.Y);
            // A "dwButtonState" minden bit-je egy igaz-hamis érték.
            // Ezzel akár 32 igaz-hamis értéket is eltárolhatunk egy int-ben.
            // Tehát ennek a legutolsó bit-jét szedjük ki, ami a bal klikk helyzete (1 = lenyomva, 0 = nem lenyomva)
            IsLeftDown = (e.dwButtonState & 0b_1) != 0;
        }
#endif
    }

    /// <summary>
    /// A játékod interfésze
    /// </summary>
    public interface IGame
    {
        /// <summary>
        /// Ez egyszer fog lefutni az első "Update" előtt
        /// </summary>
        void Initialize();

        /// <summary>
        /// Ez automatikusan meg lesz hívva egy while loop-ban
        /// </summary>
        /// <param name="drawer">
        /// Rajzoló kezelő cucc
        /// </param>
        void Update(Drawer drawer);
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
            Console.CursorVisible = false;

            Thread keyboardInputThread = new Thread(() =>
            {
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    Keyboard.Feed(key);
                }
            })
            { IsBackground = true };
            keyboardInputThread.Start();

            float shouldClearKeyboard = 0f;

            // Meghívjuk a te function-odat, ami initializálja a játékod
            Game.Initialize();

            while (true)
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

                shouldClearKeyboard += deltaTime;
                if (shouldClearKeyboard > .1f)
                {
                    Keyboard.Reset();
                    shouldClearKeyboard = 0f;
                }

                Console.SetCursorPosition(0, 0);
                char[] bufferChars = new char[Buffer.Length];
                for (int i = 0; i < Buffer.Length; i++)
                { bufferChars[i] = (Buffer[i].Char == '\0') ? ' ' : Buffer[i].Char; }
                Console.Write(bufferChars);

                if (ShouldExit)
                { break; }
            }

            // Ide akkor jövünk ha bezártuk a konzolt

            Console.CursorVisible = true;
        }

        /// <summary>
        /// Kilép a while loop-ból ami lefuttatja a te "Update" function-odat. Vagyis kilép a játékból.
        /// Lehet hogy nem működik, nincs kedvem kijavítani a hibát.
        /// </summary>
        public static void Exit() => Instance.ShouldExit = true;
    }
}
#endif
