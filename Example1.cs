using System;
using System.Collections.Generic;
using System.Numerics;
using Game;

// Ez egy nagyon cool feature, használd pls
// https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references
#nullable enable

namespace Feladatozas
{
    class Program
    {
        // Entitás cucc
        class Entity
        {
            public Vector2 Position; // Pozíció
        }

        // Sebezhető cucc
        interface IDamageable
        {
            void Damage(float amount);
        }

        // Játékos
        class Player : Entity, IDamageable
        {
            public double ShotAt; // Mikor lőtt utoljára
            public float Health = 10f; // Életerő

            public void Damage(float amount)
            {
                Health -= amount;
            }
        }

        // Ellenség
        class Enemy : Entity, IDamageable
        {
            public double AttackedAt; // Mikor sebzett utoljára (valakit)
            public float Health = 5f; // Életerő

            public void Damage(float amount)
            {
                Health -= amount;
            }
        }

        // Lövedék
        class Projectile : Entity
        {
            public Vector2 Velocity; // Sebesség
            public double CreatedAt; // Mikor lett kilőve
        }

        // Maga a játék class
        class TheBruhGame : IGame
        {
            // A játékos
            readonly Player player = new();

            // Az ellenségek
            readonly List<Enemy> enemies = new();

            // A lövedékek
            readonly List<Projectile> projectiles = new();

            /// <summary>
            /// Lekéri az első olyan dolgot, ami a <paramref name="position"/> ponttól kevesebb
            /// (vagy egyenlő) távolságra van mint <paramref name="maxDistance"/>
            /// </summary>
            /// <param name="position">A pont ahonnan mérjük a távolságot</param>
            /// <param name="maxDistance">A maximum elfogadható távolság</param>
            /// <returns>A megtalált dolgot, vagy <see langword="null"/> ha semmit se talált</returns>
            Entity? GetStuffAt(Vector2 position, float maxDistance)
            {
                // Végig megyek a enemy-ken
                for (int i = 0; i < enemies.Count; i++)
                {
                    // Ha a távolság kisebb mint "maxDistance"
                    if (Vector2.Distance(position, enemies[i].Position) <= maxDistance)
                    {
                        // akkor return-olom ezt
                        return enemies[i];
                    }
                }

                // Ha a távolság a játékoshoz kisebb mint "maxDistance"
                if (Vector2.Distance(position, player.Position) <= maxDistance)
                {
                    // akkor return-olom
                    return player;

                    // BTW nemtom miért nézem meg a játékost.
                    // Az egyetlen helyen ahol használom ezt a function-t,
                    // ellenőrzöm hogy ez nem egy játékos.
                    // Tehát ez a check lényegtelen, de mind1.
                }

                // Ha semmit se találtunk, "null"-t return-olok (vagyis semmit)
                return null;
            }

            public void Initialize()
            {
                // Random szám generátor
                // A "seed"-et beállítom a mostani időre, szóval
                // nem fogja minden újraindítás után ugyan azt generálni
                Random random = new((int)Engine.Now);

                // Ez spawnol egy enemy-t egy random pozíción.
                // Ez nem fog a játékos mellé közvetlen spawnolni, ezért van
                // itt "iterations" meg while cucc meg ilyenek
                void SpawnEnemy()
                {
                    // Maximum próbálkozások
                    // (ha tizeggyére se tud spawnolni enemy-t, feladja)
                    int iterations = 10;
                    while (iterations-- > 0)
                    {
                        // Random pozíció generálása
                        Vector2 randomPosition = new((float)random.NextDouble() * Engine.Width, (float)random.NextDouble() * Engine.Height);

                        // Megnézi hogy a játékoshoz való távolság kevesebb e mint 10
                        if (Vector2.Distance(randomPosition, player.Position) < 10f)
                        {
                            // Ha igen, nem fog spawnolni enemy-t és újra próbálja
                            // (vagy feladja ha "iterations" az már 10)
                            continue;
                        }

                        // Hozzáad egy új enemy-t
                        // Figyeld meg, hogy az Enemy class-ban a
                        // "Health" alapértelmezett értéke 5, tehát itt nem kell beállítani
                        enemies.Add(new Enemy()
                        {
                            Position = randomPosition,
                        });
                        // Kilép, hogy ne próbáljon újból egy enemy-t spawnolni
                        break;
                    }
                }

                // Az előző function-t lefuttatja 5x
                // Ha minden jól megy akkor 5db enemy-t fog lespawnolni
                for (int i = 0; i < 5; i++)
                { SpawnEnemy(); }
            }

            public void Update(Drawer drawer)
            {
                // Rajzolok egy "P"-t, ahol a játékos van
                drawer[player.Position] = 'P';

                // Végig megyek a összes enemy-n
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];

                    // Ha ez a enemy már megdöglött, kiveszem a
                    // listából és megyek a következő enemy-re
                    if (enemy.Health <= 0f)
                    {
                        enemies.RemoveAt(i);
                        continue;
                    }

                    // Rajzolok egy "E"-t ahol ez a enemy van
                    drawer[enemy.Position] = 'E';

                    // Ha a játékoshoz a távolság kevesebb mint 2, ...
                    if (Vector2.Distance(enemy.Position, player.Position) < 2f)
                    {
                        // ... és ez még nem sebzett az elmúlt 1 másodpercben, ...
                        if (Engine.Now - enemy.AttackedAt > 1f)
                        {
                            // ... akkor sebzi a játékost
                            player.Damage(1f);
                            // és elmenti hogy mikor sebzett utoljára
                            enemy.AttackedAt = Engine.Now;
                        }
                    }
                    // ha nem, akkor megnézi hogy 10-nél kisebb e a távolság
                    else if (Vector2.Distance(enemy.Position, player.Position) < 10f)
                    {
                        // ha igen, akkor közeledik a játékoshoz
                        // Formula:
                        // Irány (normalizált) * sebesség * idő = eltolódás
                        enemy.Position += (player.Position - enemy.Position) * 1f * Engine.DeltaTime;
                    }
                }

                // Végig megyek a összes lövedéken ami létezik
                // (visszafelé kell, mert lehet hogy ki kell majd szednünk pár
                // elemet a listából)
                for (int i = projectiles.Count - 1; i >= 0; i--)
                {
                    Projectile projectile = projectiles[i];

                    // Ha több ideig létezik mint 5 másodperc, kiveszem a listából
                    // (és ezzel megszűnik létezni)
                    if (Engine.Now - projectile.CreatedAt > 5f)
                    {
                        projectiles.RemoveAt(i);
                        continue;
                    }

                    // Rajzolok egy "o"-t ahol a lövedék van
                    drawer[projectile.Position] = 'o';

                    // Elmentem a pozíciót, ez később kelleni fog
                    Vector2 prevPosition = projectile.Position;

                    // Elmozgatom a lövedéket. Emlékszel a távolság képletre?
                    // távolság = sebesség * idő
                    projectile.Position += projectile.Velocity * Engine.DeltaTime;

                    // Kiszámolom a megtett távolságot
                    Vector2 positionOffset = projectile.Position - prevPosition;
                    float traveled = positionOffset.Length();

                    // És az irányt
                    positionOffset = Vector2.Normalize(positionOffset);

                    // Szegmensenként végig megyek a megtett úton
                    for (int offset = 0; offset < traveled; offset++)
                    {
                        // A megvizsgálandó pont
                        Vector2 checkPosition = prevPosition + (positionOffset * offset);
                        // Lekérem, hogy itt mi van (ha van itt valami)
                        Entity? stuffAt = GetStuffAt(checkPosition, 1f);


                        if (
                            stuffAt is not null and // Ha van itt valami ("stuffAt" nem "null")
                            not Player and // és ez nem játékos (saját magunkat nem akarjuk sebezni bruh)
                            IDamageable damageable) // és ez sebezhető
                        {
                            // akkor sebezzük 1-el
                            damageable.Damage(1f);
                            // és kivesszük ezt a lövedéket a listából (megsemmisítjük)
                            projectiles.RemoveAt(i);
                            // és kilépünk ebből a "sub-loop"-ból
                            break;
                        }
                    }
                }

                // Kirajzolom az FPS-t bal fentre (átalakítom int-re, hogy ne legyen tört szám az FPS)
                drawer.DrawText(0, 0, ((int)Engine.FPS).ToString());

                // Kirajzolom a játékos HP-ját bal fentre
                drawer.DrawText(0, 1, player.Health.ToString());

                // Ha a játékos megdöglött
                if (player.Health <= 0f)
                {
                    // Kitörlök mindent amit eddig rajzoltam
                    drawer.Clear();
                    // Ez egy const, hogy ne kelljen 2x leírni hogy "GAME OVER"
                    const string GameOverText = "GAME OVER";
                    // Középre kirajzolom a szöveget
                    drawer.DrawText(Engine.Width / 2 - GameOverText.Length / 2, Engine.Height / 2, GameOverText);
                    // Abbahagyom a játékot
                    Engine.Exit();
                    // Return-olok, nem kell mást gondolkodni/rajzolni
                    return;
                }

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
                // a játékos még nem lőtt az előző 0.3 másodpercben
                if (Mouse.IsLeftDown &&
                    (Engine.Now - player.ShotAt) >= .3f)
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
            Engine.DoTheStuff(game);
        }
    }
}
