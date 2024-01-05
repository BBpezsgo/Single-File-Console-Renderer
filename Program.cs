using System;

namespace Game
{
    public class Program
    {
        struct Player
        {
            public const float MaxSpeed = 30f;

            public float X;
            public float Y;
        }

        class ExampleGame : IGame
        {
            Player player;

            public void Initialize()
            {

            }

            public void Update(Drawer drawer)
            {
                if (Keyboard.IsKeyPressed('W')) player.Y -= Player.MaxSpeed * Engine.DeltaTime;
                if (Keyboard.IsKeyPressed('A')) player.X -= Player.MaxSpeed * Engine.DeltaTime;
                if (Keyboard.IsKeyPressed('S')) player.Y += Player.MaxSpeed * Engine.DeltaTime;
                if (Keyboard.IsKeyPressed('D')) player.X += Player.MaxSpeed * Engine.DeltaTime;

                if (Keyboard.IsKeyPressed(0x1B)) // ESC key
                {
                    Engine.Exit();
                    return;
                }

                player.X = Math.Max(player.X, 0);
                player.Y = Math.Max(player.Y, 0);
                player.X = Math.Min(player.X, Engine.Width - 1);
                player.Y = Math.Min(player.Y, Engine.Height - 1);

                drawer[player.X, player.Y] = new Win32.ConsoleCharacter('P', Color.BrightGreen, Color.Black);
            }
        }

        static void Main(string[] args)
        {
            Engine.DoTheStuff(new ExampleGame());
        }
    }
}
