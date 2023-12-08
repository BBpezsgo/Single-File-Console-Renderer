#if IS_LINUX
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Game
{
    namespace Win32
    {
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

    public static class Keyboard
    {
        static readonly bool[] Buffer = new bool[byte.MaxValue];

        /// <summary>
        /// Checks whether the key associated with <paramref name="character"/> is down or not.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the key is pressed, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(char key)
        {
            key = char.ToUpperInvariant(key);
            if (key < byte.MinValue || key >= byte.MaxValue) return false;
            return Buffer[key];
        }

        /// <summary>
        /// Checks whether the specified key is down or not.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the key is pressed, <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="NotImplementedException"/>
        public static bool IsKeyPressed(int key)
        {
            if (key < byte.MinValue || key >= byte.MaxValue) return false;
            return Buffer[key];
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

    /// <summary>
    /// Simple handler for manipulating the console buffer
    /// </summary>
    public struct Drawer
    {
        readonly Win32.ConsoleCharacter[] buffer;

        public readonly int Width;
        public readonly short Height;

        public Win32.ConsoleCharacter this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return Win32.ConsoleCharacter.Zero;
                if (x >= Width || y >= Height) return Win32.ConsoleCharacter.Zero;
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

        public Win32.ConsoleCharacter this[double x, double y]
        {
            get => this[(int)Math.Round(x), (int)Math.Round(y)];
            set => this[(int)Math.Round(x), (int)Math.Round(y)] = value;
        }

        public Win32.ConsoleCharacter this[Vector2 point]
        {
            get => this[point.X, point.Y];
            set => this[point.X, point.Y] = value;
        }

        public Drawer(Win32.ConsoleCharacter[] buffer, int width, short height)
        {
            this.buffer = buffer;
            Width = width;
            Height = height;
        }

        public void DrawText(int x, int y, string text, Color foregroundColor = Color.Silver, Color backgroundColor = Color.Black)
        {
            for (int i = 0; i < text.Length; i++)
            { this[x + i, y] = new Win32.ConsoleCharacter(text[i], foregroundColor, backgroundColor); }
        }

        internal void Clear()
        {
            Console.Clear();
            Array.Clear(buffer, 0, buffer.Length);
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
        Win32.ConsoleCharacter[] Buffer;

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

            Buffer = new Win32.ConsoleCharacter[Width * Height];

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

            Game.Initialize();

            while (true)
            {
                double now = DateTime.UtcNow.TimeOfDay.TotalSeconds;
                deltaTime = (float)(now - lastTime);
                lastTime = now;

                if (Rect.Right != Width || Rect.Bottom != Height)
                {
                    Buffer = new Win32.ConsoleCharacter[Width * Height];
                    Rect.Right = Width;
                    Rect.Bottom = Height;
                }
                else
                {
                    Array.Clear(Buffer, 0, Buffer.Length);
                }

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

            Console.CursorVisible = true;
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
