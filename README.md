# Assignment 3: Antymology

As we\'ve seen in class, ants exhibit very interesting behaviour. From finding the shortest path to building bridges out of bodies ants have evolved to produce complex emergents from very simple rules. For your assignment you will need to create a species of ant which is capable of generating the biggest nest possible.

I have already created the base code you will use for the assignment. Currently the simulation environment is devoid of any dynamic behaviour and exists only as a landscape. You will need to extend the functionality of what I have written in order to produce \"intelligent\" behaviour. Absolutely no behaviour has been added to this project so you are free to implement whatever you want however you want, with only a few stipulations.

![Ants](Images/Ants.gif)

## Goal

The only goal you have is to implement some sort of evolutionary algorithm which maximises nest production. You are in complete control over how your ants breed, make choices, and interact with the environment. Because of this, your mark is primarily going to be reflective of how much effort it looks like you put into this vs. how well your agents maximise their fitness (I.e. don\'t worry about having your ants perform exceptionally well).

## Current Code
My code is currently broken into 4 components (found within the components folder)
1. Agents
2. Configuration
3. Terrain
4. UI

You are able to experience it generating an environment by simply running the project once you have loaded it into unity.

### Agents
The agents component is currently empty. This is where you will place most of your code. The component will be responsible for moving ants, digging, making nests, etc. You will need to come up with a system for how ants interact within the world, as well as how you will be maximising their fitness (see ant behaviour).

### Configuration
This is the component responsible for configuring the system. For example, currently there exists a file called ConfigurationManager which holds the values responsible for world generation such as the dimensions of the world, and the seed used in the RNG. As you build parameters into your system, you will need to place your necesarry configuration components in here.

### Terrain
The terrain memory, generation, and display all take place in the terrain component. The main WorldManager is responsible for generating everything.

### UI
This is where all UI components will go. Currently only a fly camera, and a camera-controlled map editor are present here.

## Requirements

### Admin
 - This assignment must be implemented using Unity 2019or above (see appendix)
 - Your code must be maintained in a github (or other similar git environment) repository.
 - You must fork from this repo to start your project.
 - You will be marked for your commit messages as well as the frequency with which you commit. Committing everything at once will receive a letter grade reduction (A →A-).
 - All project documentation should be provided via a Readme.md file found in your repo. Write it as if I was an employer who wanted to see a portfolio of your work. By that I mean write it as if I have no idea what the project is. Describe it in detail. Include images/gifs.

### Interface
- The camera must be usable in play-mode so as to allow the grader the ability to look at what is happening in the scene.
- You must create a basic UI which shows the current number of nest blocks in the world

### Ant Behaviour
- Ants must have some measure of health. When an ants health hits 0, it dies and needs to be removed from the simulation
- Every timestep, you must reduce each ants health by some fixed amount
- Ants can refill their health by consuming Mulch blocks. To consume a mulch block, and ant must be directly ontop of a mulch block. After consuming, the mulch block must be removed from the world.
- Ants cannot consume mulch if another ant is also on the same mulch block
- When moving from one black to another, ants are not allowed to move to a block that is greater than 2 units in height difference
- Ants are able to dig up parts of the world. To dig up some of the world, an ant must be directly ontop of the block. After digging, the block is removed from the map
- Ants cannot dig up a block of type ContainerBlock
- Ants standing on an AcidicBlock will have the rate at which their health decreases multiplied by 2.
- Ants may give some of their health to other ants occupying the same space (must be a zero-sum exchange)
- Among your ants must exists a singular queen ant who is responsible for producing nest blocks
- Producing a single nest block must cost the queen 1/3rd of her maximum health.
- No new ants can be created during each evaluation phase (you are allowed to create as many ants as you need for each new generation though).

## Tips
Initially you should first come up with some mechanism which each ant uses to interact with the environment. For the beginning phases your ants should behave completely randomly, at least until you have gotten it so that your ants don't break the pre-defined behaviour above.

Once you have the interaction mechanism nailed down, begin thinking about how you will get your ants to change over time. One approach might be to use a neural network to dictate ant behaviour

https://youtu.be/zIkBYwdkuTk

another approach might be to use phermone deposits (I\'ve commented how you could achieve this in the code for the AirBlock) and have your genes be what action should be taken for different phermone concentrations, etc.

## Submission
Export your project as a Unity package file. Submit your Unity package file and additional document using the D2L system under the corresponding entry in Assessments/Dropbox. Inlude in the message a link to your git repo where you did your work.

---

## Current Implementation (Feb 4, 2026)

- A queen plus a small worker cohort spawn near world centre on play. A runtime fallback prefab is created if none is wired in the scene, so the simulation always runs.
- Ant agents track health, lose it each tick (double rate on `AcidicBlock`), die at zero, and can share health with co-located ants (zero-sum transfer).
- Mulch can be consumed only when a single ant occupies the mulch block; doing so restores that ant to full health and removes the block.
- Ants dig non-container, non-nest blocks beneath them to open tunnels, settling onto the new surface afterwards.
- Movement respects the max climb/drop difference of 2 vertical units; ants pick random reachable neighbours to keep things simple.
- One queen produces `NestBlock`s; each block costs one-third of her max health and is placed at her current tile after ensuring it is cleared.
- Nest block count is tracked live inside `WorldManager` to support UI display and scoring.

### Tuning & Visuals
- Use the `AntConfig` ScriptableObject (Assets/Components/Agents/AntConfig.cs, create via Assets → Create → Antymology → Ant Config) to set shared health/decay/movement/dig/share/nest values once; assign it on the `WorldManager` component (Ant Config field) to apply to all spawned ants.
- Assign custom prefabs/materials in `WorldManager` (`Ant Prefab` for workers, optional `Queen Prefab`, `Worker Material`, `Queen Material`) to improve visuals. Fallback uses a capsule with tinting and separate worker/queen scales.
- Quick sizing: adjust `workerScale` and `queenScale` on `WorldManager` to change the rendered size of ants without touching prefabs.
- Early evolutionary loop: `EvolutionManager` (auto-created by WorldManager if missing) runs timed generations when `autoRun` is true. Each generation uses a mutated `AntGenome` (behaviour knobs); fitness = nest blocks built during the window. Set `evaluationDurationSeconds`, `populationSize`, `eliteCount`, and `mutationStrength`. If `regenerateTerrainEachGeneration` is on, the world rebuilds between generations.
- Visuals: default worker/queen prefabs auto-load from `Assets/Resources/AntPrefabs/AntWorker.prefab` and `AntQueen.prefab` (stylized ant model). Set your own via `WorldManager` fields to override; queen and worker scales remain separate.
- Pheromones: ants deposit pheromones on blocks as they move; pheromones decay/diffuse to neighbours. Movement is biased toward higher pheromone tiles, and queens avoid placing nests deep underground or without air above, so nests form near the surface where pheromone trails lead.

## Controls & UI

- Camera: Fly-style controls auto-attach to the main camera at runtime. Use `W/A/S/D` to move horizontally, `Q/E` for vertical movement, hold `Shift` to accelerate, and hold middle mouse to look.
- HUD: A small overlay shows `Nest Blocks` and `Ants` counts. It is spawned automatically if the scene does not already include a `NestCounterUI` component.
- Terrain editor (unchanged from starter): Number keys `1-5` choose block types; left click adds, right click removes where the cursor points.

## A+ Checklist
- Unity 6000.3.* (Hub target set) and README documents how to run the scene, with images/gifs of ants digging/building and the HUD visible.
- Queen visually distinct (material/scale/tint) and only one queen per generation; no new ants spawned mid-evaluation.
- Health rules: decay each tick, death at 0, double decay on acid, mulch restores health only when uncontested, digging blocked on ContainerBlock, move height delta ≤2, health sharing zero-sum.
- UI shows nest block count; camera usable in play mode.
- Evolutionary behaviour visible: EvolutionManager in scene (auto-created), `autoRun` true, reasonable `evaluationDurationSeconds` (e.g., 60s) to demonstrate generations.

## Capturing Screenshots/GIFs
- Screenshots: In Play mode, frame the action with the fly camera; press `Print Screen` or use `Game` view’s resolution dropdown → `Maximize On Play` for clarity. Save as PNGs in `Images/` and link in README with relative paths.
- GIFs/video: Use Unity Recorder (`Package Manager` → add `Unity Recorder`; menu `Window` → `General` → `Recorder`). Create a GameView recording, set duration (e.g., 10–15s) and output path under `Images/`. Alternatively use OBS with a cropped Game view. Convert MP4 to GIF with ffmpeg if needed (`ffmpeg -i input.mp4 -vf fps=12,scale=640:-1 -loop 0 output.gif`).

## How to Run

1. Open `Assets/Scenes/SampleScene.unity` in Unity 2019+.
2. Press Play. The world is generated, camera controls and HUD are injected automatically, and ants begin roaming/digging/building.
3. To tweak behaviour without code changes, adjust parameters on `WorldManager` (world size/seed) and on the `AntAgent` component (health decay, dig chance, climb height, nest cooldown) on the ant prefab or runtime prefab.
