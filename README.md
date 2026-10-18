# Cubelands Classic – modded source

This folder is the C# source of the game's `Assembly-CSharp.dll`, the file that holds all of the game logic (menus, login, world generation, multiplayer, building, options...).

It was decompiled from the **community-modded** DLL that ships in `../playable_cubelands`, so it includes the offline username login (no Cubelands account or server needed), the skin changer and the Host/Join game buttons.

> The `.cs` files in the repository root are the **original 2011 code** and still use the dead online login. Keep them for reference only: building from them breaks the login. Always edit the files in this folder.

## Minimum requirements

| What | Version | Why |
|---|---|---|
| Windows | 10 or 11 | The game is a Windows build |
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 or newer | Compiles the source (`winget install Microsoft.DotNet.SDK.8`) |
| Git | any | Getting and versioning the code |
| A code editor | any | VS Code, Visual Studio or Rider all work |
| DirectX 9 GPU | any | Needed to run the game (Unity 3.4.0, 2011) |

You do **not** need Unity: the game is already built, and only the DLL is replaced.

## Project layout

```
cubelands_source_code/
├── source_modded/                 ← edit code here
│   ├── Assembly-CSharp.csproj     builds Assembly-CSharp.dll (targets net20, like Unity 3.4)
│   └── *.cs                       one file per game script
└── playable_cubelands/
    ├── Cubelands_classic.exe      the game
    └── Cubelands_classic_Data/
        ├── Managed/               game DLLs (the project references UnityEngine, LitJson, firstpass from here)
        └── sharedassets*.assets   sounds, textures, meshes (not code)
```

## Making a change

The example below is the one used to test this setup: a label in the top-left corner of the menu.

### 1. Back up the game DLL (first time only)

```powershell
copy playable_cubelands\Cubelands_classic_Data\Managed\Assembly-CSharp.dll playable_cubelands\Cubelands_classic_Data\Managed\Assembly-CSharp.dll.bak
```

### 2. Edit the code

In `MainMenu.cs`, inside `OnGUI()` and right after `GUI.skin = skin;`:

```csharp
GUI.skin = skin;
GUI.Label(new Rect(10f, 10f, 400f, 25f), "Cubelands Classic - modded build 2026");
```

### 3. Build

From the repository root:

```powershell
dotnet build Assembly-CSharp.csproj -c Release
```

It should end with `Build succeeded` and `0 Error(s)`. A few `CS0169`/`CS0649` warnings about unused fields are normal.

### 4. Install the new DLL

```powershell
copy bin\Release\net20\Assembly-CSharp.dll playable_cubelands\Cubelands_classic_Data\Managed\
```

### 5. Test

Run `playable_cubelands/Cubelands_classic.exe`. The label should be in the top-left corner and the username login should still work.

If the game crashes or something does not show up, check `playable_cubelands/Cubelands_classic_Data/output_log.txt` for errors. To undo, copy `Assembly-CSharp.dll.bak` back over `Assembly-CSharp.dll`.

## Rules to avoid breaking the game

The scenes and prefabs inside the `.assets`/`level` files point to the scripts **by class name** and store the values of their **public fields by field name**. So:

- **Do not rename** classes or existing public/serialized fields (for example `Music.normalAudio`, `MainMenu.clickSound`). The game would lose the reference and the value.
- **Do not delete** classes or public fields that the game objects use.
- **Adding is safe:** new methods, new private fields, new classes, new logic inside existing methods.
- Use the **Unity 3.4 API** only (`UnityEngine.dll` in `Managed/`). Newer Unity functions do not exist in this version. For example, levels load with `Application.LoadLevel` (there is no `SceneManager`) and menus are drawn in `OnGUI()` (there is no UI Canvas).
- The code targets **.NET 2.0**: no `async`/`await`, no LINQ, no `Task`. Coroutines (`IEnumerator` + `yield return`) are how the game waits.

Assets (music, sounds, textures) live in the `.assets` files, not in the code, and need a separate patching step.

## Regenerating this source (optional)

Only needed if you ever get a different `Assembly-CSharp.dll` and want its code. The newest ILSpy needs a newer .NET, so install version 8.2:

```powershell
dotnet tool install -g ilspycmd --version 8.2.0.7535
$env:DOTNET_ROLL_FORWARD = "Major"   # lets it run on the .NET 8 runtime
ilspycmd -p -o source_new -r playable_cubelands\Cubelands_classic_Data\Managed playable_cubelands\Cubelands_classic_Data\Managed\Assembly-CSharp.dll
```

Then fix the three `<HintPath>` entries in the generated `.csproj` to start with `../playable_cubelands/`.
