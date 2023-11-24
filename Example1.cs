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

        static void Main(string[] args)
        {
            // Csinálok egy új player-t
            Player player = new();

            List<Projectile> projectiles = new();

            // Eltárolom az időt, ez később kelleni fog a
            // "delta time" kiszámolásához
            double lastTime = DateTime.UtcNow.TimeOfDay.TotalSeconds;

            void Update(Drawer drawer)
            {
                // Ezzel a 3 sorral számolom ki a "delta time"-t, vagyis

                // Lementem a mostani időt
                double now = DateTime.UtcNow.TimeOfDay.TotalSeconds;
                // Kivonom a mostani időből az előző időt, megkapom a "delta time"-t
                float delta = (float)(now - lastTime);
                // Elmentem a mostani időt, hogy legközelebb azzal számolhassunk
                // (a következő frissítésben a mostani idő már úgymond az előző
                // idő, azért kell lementeni. Meg tudnunk kell az előző időt, hogy
                // a különbséget kiszámolhassuk)
                lastTime = now;

                /*
                 * A "delta time" az az idő (másodpercekben) amennyi eltelt az előző
                 * frissítés óta.
                 * Vagyis az az idő intervallum ami az `Update` két meghívása között van.
                 */

                // Rajzolok egy "P"-t, ahol a játékos van
                drawer[player.Position] = 'P';

                // Végigmegyek a összes lövedéken ami létezik
                // (visszafelé kell, mert lehet hogy ki kell majd szednünk pár
                // elemet a listából)
                for (int i = projectiles.Count - 1; i >= 0; i--)
                {
                    Projectile projectile = projectiles[i];

                    // Rajzolok egy "o"-t ahol a lövedék van
                    drawer[projectile.Position] = 'o';
                    // Elmozgatom a lövedéket. Emlékszel a távolság képletre?
                    // távolság = sebesség * idő
                    projectile.Position += projectile.Velocity * delta;

                    // Ha több ideig létezik mint 5 másodperc, kiveszem a listából
                    // (és ezzel megszűnik létezni)
                    if (now - projectile.CreatedAt > 5f)
                    { projectiles.RemoveAt(i); }
                }

                // Rajzolok egy szöveget.
                // A "delta time" reciproka az FPS,
                // tehát ha 1-et elosztod "delta time"-al, megkapod az 
                // FPS-t. És azt írom ki bal fentre.
                drawer.DrawText(0, 0, ((int)(1.0 / delta)).ToString());

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
                    (now - player.ShotAt) >= 1f)
                {
                    // Frissítem, hogy mikor lőtt utoljára
                    player.ShotAt = now;

                    // Kiszámolom az irányt
                    Vector2 direction = Mouse.Position - player.Position;
                    // Normalizálom -1 és 1 közé
                    direction = Vector2.Normalize(direction);

                    // Csinálok egy új lövedéket
                    Projectile newProjectile = new()
                    {
                        CreatedAt = now, // Beállítom hogy mikor lett megcsinálva
                        Position = player.Position, // A helye az a játékos helye (mert onnan lövődik ki)
                        Velocity = direction * 20f, // A sebessége pedig az irány szorozva 20-al
                                                    // (20 egységet fog megtenni 1 másodperc alatt)
                    };
                    // Hozzáadom a listához
                    projectiles.Add(newProjectile);
                }
            }

            // Elindítom a játékot
            Engine.DoTheStuff(Update);

            // Itt már semmi se fut le
        }
    }
}
