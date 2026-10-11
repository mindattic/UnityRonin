# Unity Ronin

A Unity 6 portrait mobile game: explore one-thumb with a transparent analog stick covering the bottom
half of the screen, then duel one-thumb when a Samurai meets a sword — cardinal swipes parry or strike
a telegraphed katana duel.

See [docs/BIBLE.md](docs/BIBLE.md) for what this is, what it isn't, and its laws, and
[docs/USER_STORIES.md](docs/USER_STORIES.md) for verified status.

## Quick start

```powershell
pwsh tools/test.ps1            # Core movement/combat rules, seconds, no Unity needed
pwsh tools/test.ps1 -Compile   # + a Unity 6000.4.3f1 batch-mode compile check
```

Open the project in Unity 6000.4.3f1, then **UnityRonin > Build Demo Scene** to (re)generate
`Assets/UnityRonin/Scenes/Demo.unity` from primitives.
