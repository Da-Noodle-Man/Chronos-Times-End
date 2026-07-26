# Chronos: Time's End

## 📖 The Lore
Centuries ago, you struck a dark pact with a demon, bartering away a fragment of your soul in exchange for unmatched martial prowess. Your skill with the blade—capable of felling greater beings—eventually caught the eye of the Greek Pantheon. The gods purchased your contract from the demonic realms, binding you to their will. 

Now, Chronos, the Titan of Time, attempts to shatter his chains and escape Tartarus. Because the gods are forbidden from stepping foot in the underworld, they have sent you to act as their executioner. Your task is simple: Slay the Titan of Time, and your soul will be returned. To survive this descent, the gods have blessed your journey with the speed of Hermes.

---

## ⚙️ Setup Instructions

**To Play the Game:**
1. Navigate to the **Releases** tab on the right side of this repository.
2. Download the `Final Playable Build` ZIP file.
3. Extract the folder to your computer.
4. Run the executable (`.exe`) file to launch directly into the Hub.

**To Review the Source Code:**
1. Clone or download this repository.
2. Open the folder using **Godot Engine 4.x (.NET Edition)**.
3. Ensure you have the .NET SDK installed to compile the C# solutions.
4. The main entry point scene is `hub.tscn`.

---

## 🎮 Basic Mechanics
* **Movement:** Keyboard [W, A, S, D] or Arrow Keys.
* **Combat Aiming:** Custom Hardware Motion-Controller (calculating both physical angles and raw acceleration for precise cursor tracking).
* **Attack:** [Left Click] / [Spacebar].
* **Dash:** [Shift].

---

## ⚔️ The Mission & Mechanics (Brief Overview)
* **The Task:** Navigate the Tartarus Arena and survive a grueling, high-difficulty boss rush against Chronos to reclaim your soul.
* **The Curse:** The demonic pact binds your mortality. Taking hits from Chronos's temporal attacks is devastating, forcing a heavy reliance on precise evasion over brute force.
* **The Boon (Hermes' Blessing):** Grants the *Hermes Sandals*, which heavily modifies the player's physical dash logic—reducing stamina costs, altering dash speed, and providing crucial invincibility frames (i-frames) to dodge through attacks.
* **The Three Phases:** 
  1. **Immortality:** Chronos is shielded and invulnerable.
  2. **Execution:** The shield breaks, forcing Chronos into aggressive melee and ranged patterns.
  3. **True Form:** Chronos rewinds time, reclaiming his health and unleashing enhanced, screen-wide ultimate attacks.

---

## 🔍 Deep Dive: Boss AI & Phase Logic 

For evaluators and developers, the Chronos boss fight is governed by a custom C# state-machine (`ChronosBrain.cs` and `ChronosBoss.cs`), utilizing distance-checking algorithms to dynamically alter attack combos.

### Phase 1: Immortality Shield
Chronos begins the fight completely immune to standard damage. The player must navigate the arena and devour the Prometheus Flames scattered across the map. The state machine listens for the `OnFlameDevoured` signal; once all flames are extinguished, the shield shatters, triggering Phase 2.

### Phase 2: Execution
With immortality broken, Chronos utilizes a positioning algorithm to maintain specific ranges from the player, randomly selecting from three core attacks based on proximity:
* **The Sweep:** A targeted, tracking melee strike where Chronos teleports to the player's flank and unleashes a massive 270-degree temporal slash.
* **The Jump:** A devastating AoE (Area of Effect) attack where Chronos leaps out of bounds and slams down on the player's last known location, spawning cascading earth pillars.
* **Orbs of Time:** A bullet-hell mechanic where Chronos summons homing projectiles that calculate safe spawn distances and track the player across the arena.

### Phase 3: True Form (Rewind)
Upon reaching 0 HP in Phase 2, the AI triggers a `CallDeferred` rewind sequence, restoring his health pool and overriding his previous cooldown timers. 
* **Enhanced AI:** Phase 3 introduces advanced combo chains (`interceptorCombo`, `enragerCombo`). If the player attempts to "kite" the boss by staying too far away, Chronos detects the distance and punishes the player with an immediate teleport and a 6-orb barrage.
* **Clockwork Cleave (Ultimate):** At 66% and 33% health thresholds, Chronos enters an `UltimateOverride` state, halting all standard logic to execute a massive, multi-stage rotating laser sweep that blankets the entire Tartarus arena.
