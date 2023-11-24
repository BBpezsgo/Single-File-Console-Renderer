using System;
using System.Collections.Generic;
using System.Numerics;
using Game;

namespace Feladatozas
{
    class Program
    {
        // Játékos
        class Player
        {
            public Vector2 Position; // Pozíció
            public double ShotAt; // Mikor lőtt utoljára
        }

        // Lövedék
        class Projectile
        {
            public Vector2 Position; // Pozíció
            public Vector2 Velocity; // Sebesség
            public double CreatedAt; // Mikor lett kilőve
        }

        // Maga a játék class
        class TheBruhGame : Game.IGame
        {
            // A játékos
            Player player = new();
            
            // A lövedékek
            List<Projectile> projectiles = new();

            // Eltárolom az időt, ez később kelleni fog a
            // "delta time" kiszámolásához
            double lastTime = DateTime.UtcNow.TimeOfDay.TotalSeconds;

            public void Update(Drawer drawer)
            {
                // Rajzolok egy "P"-t, ahol a játékos van
                drawer[player.Position] = 'P';

                // Végig megyek a összes lövedéken ami létezik
                // (visszafelé kell, mert lehet hogy ki kell majd szednünk pár
                // elemet a listából)
                for (int i = projectiles.Count - 1; i >= 0; i--)
                {
                    Projectile projectile = projectiles[i];

                    // Rajzolok egy "o"-t ahol a lövedék van
                    drawer[projectile.Position] = 'o';
                    // Elmozgatom a lövedéket. Emlékszel a távolság képletre?
                    // távolság = sebesség * idő
                    projectile.Position += projectile.Velocity * Engine.DeltaTime;

                    // Ha több ideig létezik mint 5 másodperc, kiveszem a listából
                    // (és ezzel megszűnik létezni)
                    if (Engine.Now - projectile.CreatedAt > 5f)
                    { projectiles.RemoveAt(i); }
                }

                // Kirajzolom az FPS-t bal fentre (átalakítom int-re, hogy ne legyen tört szám az FPS)
                drawer.DrawText(0, 0, ((int)Engine.FPS).ToString());

                // Ha a W billentyű le van nyomva ...
                if (Keyboard.IsKeyPressed('w'))
                { player.Position.Y -= 0.01f; }

                // Ha a S billentyű le van nyomva ...
                if (Keyboard.IsKeyPressed('s'))
                { player.Position.Y += 0.01f; }

                // Ha a A billentyű le van nyomva ...
                if (Keyboard.IsKeyPressed('a'))
                { player.Position.X -= 0.01f; }

                // Ha a D billentyű le van nyomva ...
                if (Keyboard.IsKeyPressed('d'))
                { player.Position.X += 0.01f; }

                // Ha a bal egérgomb le van nyomva, és
                // a játékos még nem lőtt az előző 1 másodpercben
                if (Mouse.IsLeftDown &&
                    (Engine.Now - player.ShotAt) >= 1f)
                {
                    // Frissítem, hogy mikor lőtt utoljára
                    player.ShotAt = Engine.Now;

                    // Kiszámolom az irányt
                    Vector2 direction = Mouse.Position - player.Position;
                    // Normalizálom -1 és 1 közé
                    direction = Vector2.Normalize(direction);

                    // Csinálok egy új lövedéket
                    Projectile newProjectile = new()
                    {
                        CreatedAt = Engine.Now, // Beállítom hogy mikor lett megcsinálva
                        Position = player.Position, // A helye az a játékos helye (mert onnan lövődik ki)
                        Velocity = direction * 20f, // A sebessége pedig az irány szorozva 20-al
                                                    // (20 egységet fog megtenni 1 másodperc alatt)
                    };
                    // Hozzáadom a listához
                    projectiles.Add(newProjectile);
                }
            }
        }

        static void Main(string[] args)
        {
            // Egy új class a játéknak
            TheBruhGame game = new();

            // Elindítom a játékot
            Engine.DoTheStuff(game.Update); // Figyeld meg hogy nem hívom meg a `Update` függvényt,
                                            // csak egy úgynevezett "function pointer"-t használok.
                                            // Majd az "Engine" cucc meghívja ezt a függvényt.

            // Itt már semmi se fut le
        }
    }
}
