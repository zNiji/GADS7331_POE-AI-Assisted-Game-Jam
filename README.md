# Frontier Extraction

## Overview

Frontier Extraction is a 2D pixel-art roguelite where players explore a hostile alien planet to gather resources and upgrade their base.

Players must balance risk and reward: dying causes all collected resources from that run to be lost.

## Features

- 2D platforming and combat
- Resource mining system
- Roguelite progression (lose resources on death)
- Base upgrade system
- Limited ammo + ammo pickups (enemy drops)
- Ultra-rare ore gating for final upgrade tiers (Zenithite)
- 3-slot save/load system (with rename/delete from main menu)

## Play a Built Version (Windows)

1. In Unity, open `Assets/Scenes/MainMenu.unity`
2. Go to **File → Build Settings**
3. Ensure these scenes are added (and in this order):
   - `MainMenu`
   - `Level_01`
4. Select **PC, Mac & Linux Standalone**
5. Set **Target Platform: Windows**
6. Click **Build** (or **Build And Run**) and choose an output folder (e.g. `Builds/Windows/`)
7. Run the generated `.exe` from that folder

## Run in Unity (for development)

1. Open the project in Unity (Unity 2022+ recommended)
2. Open `Assets/Scenes/MainMenu.unity`
3. Press Play, then choose **Start Game** or **Load Game**

## Controls

- A/D or Arrow Keys: Move
- Space: Jump
- Left Click: Shoot
- E: Mine / Interact
- X: Extract
- Esc: Pause

## AI Tools Used

This project was developed using AI-assisted tools:

- Cursor: Used to generate and refine Unity C# scripts and editor tooling across the project (gameplay systems, UI, save/load, procedural art helpers, balancing, and bug fixes) with me acting as an AI coding assistant inside the editor.
- ChatGPT: Used for system design, planning, and prompt creation

## Credits

- Developer: Liam Fullard

