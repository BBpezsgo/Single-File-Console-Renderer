# Single-File Console Renderer

[![.NET Framework 2.0](https://img.shields.io/badge/.NET_Framework-2.0-5C2D91)](#)

## Installation

Copy the `Engine.cs` file into your project and you're ready to go.

> [!NOTE]
> It uses the Win32 API. If you want it to work on Linux, use `Engine.NoWin32.cs` instead.

## Usage

> Look at `Program.cs` for a small example.

### Setup

First, make a class that implements the `IGame` interface.

```cs
class MyGame : IGame
{
	public void Initialize()
	{
		
	}

	public void Update(Drawer drawer)
	{
		
	}
}
```

Then call the `Engine.DoTheStuff` with an instance of your game to start the engine (and your game).

```cs
// This is your entry point, it should be already there
static void Main(string[] args)
{
    Engine.DoTheStuff(new MyGame());
}
```

That's it.

### Drawing

To draw something on the console, call the `drawer`'s indexer in the `Update` method.

```cs
// This will draws an X at column 3 and row 7
drawer[3, 7] = 'X';
```

To draw something in color, use the `drawer`'s indexer with an `Win32.ConsoleCharacter` instance.

```cs
// This will draws a red X at column 3 and row 7
drawer[3, 7] = new Game.Win32.ConsoleCharacter('X', Game.Color.Red);
```

### Input

#### Keyboard

Use the `Game.Keyboard` static class to handle keyboard input.

Methods:
- `IsKeyPressed` - Checks whether the specified button is pressed or not.

#### Mouse

Use the `Game.Mouse` static class to handle mouse input.

Properties:
- `Position` - Returns the current mouse position relative to the console.
- `IsLeftDown` - Checks whether the left mouse button is pressed or not.
- `IsRightDown` - Checks whether the right mouse button is pressed or not.

### Time

To do math with time, use the `Game.Engine` class.

Properties:
- `DeltaTime` - Returns the elapsed time since the last update measured in seconds.
- `Now` - Returns the current time measured in seconds.
- `FPS` - Returns the frames / second.

### Lifecycle

To exit the game, call the static method `Game.Engine.Exit`.
Note that this does not terminate the program immediately, but rather after the current update is complete.

## Note

If you find any bugs, have any problems or need some help, [open a new Issue](https://github.com/BBpezsgo/Single-File-Console-Renderer/issues/new) or text me on [Discord](https://discord.com/app) (my id is `blasius0057`).
