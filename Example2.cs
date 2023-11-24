using System;

namespace Feladatozas
{
    class Example2
    {
        // Egy interfész amit majd implementálni kell
        interface IShape
        {
            public float GetArea();
            public float GetPerimeter();
        }

        // Téglalap
        class Rectangle : IShape
        {
            public float SideA; // A oldal
            public float SideB; // B oldal

            public float GetArea()
            {
                return SideA * SideB;
            }

            public float GetPerimeter()
            {
                return SideA * 2 + SideB * 2;
            }

            // Szöveggé alakító függvény.
            public override string ToString()
            {
                return $"Rectangle( A: {SideA}, B: {SideB} )";
            }
        }

        // Kör
        class Circle : IShape
        {
            public float Radius; // Sugár

            public float GetArea()
            {
                return Radius * Radius * MathF.PI;
            }

            public float GetPerimeter()
            {
                return 2 * MathF.PI * Radius;
            }

            // Szöveggé alakító függvény.
            public override string ToString()
            {
                return $"Circle( Radius: {Radius} )";
            }
        }

        static void Main(string[] args)
        {
            Rectangle rectangle = new()
            {
                SideA = 4f,
                SideB = 7f,
            };

            Circle circle = new()
            {
                Radius = 5f,
            };


            Console.WriteLine(rectangle);
            Console.WriteLine(circle);

            Console.WriteLine($"Téglalap kerülete: {rectangle.GetPerimeter()}");
            Console.WriteLine($"Téglalap területe: {rectangle.GetArea()}");

            Console.WriteLine($"Kör kerülete: {circle.GetPerimeter()}");
            Console.WriteLine($"Kör területe: {circle.GetArea()}");
        }
    }
}
