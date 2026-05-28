# Agent Instructions — Spirit Sling Tabletop

Unity social MR tabletop game that demonstrates how to build colocated multiplayer experiences with Meta XR Avatars, MRUK, the Meta Interaction SDK, and Photon Fusion.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, dependencies, and build/run instructions
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions (Meta XR Core / Interaction / Avatars, MRUK, Photon)
- `Documentation/ProjectConfiguration.md` — Meta Quest + Photon dashboard configuration steps
- `Assets/GameSettings.asset` — runtime config (Application Identifier, Meta Quest App ID, Photon App IDs, optional keystore)
- `.gitattributes` — Git LFS configuration
- `LICENSE` — license terms (note: TextMeshPro and Photon SDK are under their own licenses)

## Quest / Horizon-specific notes

- **Git LFS is required**; run `git lfs install` before cloning.
- Quest 2 is **not** in the supported-device list. Board placement and other MR features rely on Quest 3-class capabilities (MRUK, depth, passthrough fidelity).
- `Assets/GameSettings.asset` holds personal App IDs and may hold an Android keystore name/password. **Do not commit a populated `GameSettings.asset` back to the repo.**
- Multiplayer flow runs through `Assets/SpiritSling/Common/Networking/Scripts/ConnectionManager.cs` and is sequenced by `Assets/SpiritSling/TableTop/Gameplay/Scripts/Tabletop/TabletopGameStateMachine.cs`. State authority for tabletop pieces is networked — never assume the local player owns a piece.

## Meta Quest tooling

This repository is part of the Meta Quest / Horizon OS ecosystem (a sample, library, template, or related project — the bespoke intro above describes which). Use that intro and the source-of-truth files it references for project-specific decisions; don't restate or invent facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools). Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
