# PenCliBook

Windows screen annotation overlay built with Java 21, JavaFX, JNA, and JNativeHook.

## Requirements

- Windows 10/11
- JDK 21
- Maven 3.9+

## Run

```powershell
mvn javafx:run
```

## Test

```powershell
mvn test
```

## MVP Shortcuts

| Shortcut         | Action                           |
| ---------------- | -------------------------------- |
| `Ctrl + Alt + P` | Enter pen mode                   |
| `Ctrl + Alt + M` | Enter mouse passthrough mode     |
| `Ctrl + Alt + C` | Clear all strokes                |
| `Ctrl + Z`       | Undo last stroke or erase action |
| `Esc`            | Hide overlay                     |

## Architecture

The implementation follows a lightweight DDD/ports-and-adapters structure:

- `domain`: pure Java stroke model, document aggregate, undo history, erase algorithm.
- `application`: session use cases and mode transitions.
- `adapter`: JavaFX input/rendering and JNativeHook global hotkeys.
- `infrastructure`: Windows/JNA overlay style and mouse passthrough control.

Pressure sensitivity and full multi-monitor DPI handling are intentionally left for a later version.
