# Changelog

## 1.1.0

A stability update. Most of these fixes are for save/load problems and state carrying over between games. Existing saves keep working.

**Saves and loading**
- Loading a different save, or starting a new colony, no longer carries religion roles, abilities, the religion leader, believer counts or dissonance state over from the previous game.
- Religion role holders (including ones assigned from the social tab) keep their roles after reloading.
- Religion roles are no longer dropped whenever the game recounts believers.
- Role ability cooldowns (Convert, Reassure, speeches, etc.) are saved instead of resetting on load.
- Experimental cognitive dissonance: in-progress certainty erosion is now saved with the game.

**Gameplay**
- Religion believer counts only count your colonists of that religion, not NPCs.
- Losing a religion correctly removes the religion role it granted.
- Convert/Reassure Religion can no longer target animals or mechanoids, and Reassure only targets pawns of your own religion (so it no longer wastes its cooldown).
- Ideoligion conversion attempts check the pawn being converted instead of the one doing the converting.
- Cognitive dissonance no longer runs at the wrong speed after loading a save, and religion precepts no longer tick once per pawn.
- Experimental cognitive dissonance now reacts to enslaving, executions (including guests) and organ installs. Several of these events were previously never detected.

**NPC religions**
- NPC faction religions now have precepts that match their memes.
- Faction-specific memes (e.g. pirate Supremacist/Raider) no longer leak into shared religions.
- Generating a religion no longer permanently changes the deity-name settings of the base meme.

**UI**
- Going Back then Next in the religion wizard keeps your customizations unless you changed the memes.
- Religions no longer appear in vanilla ideoligion lists (e.g. choosing an ideoligion for a pawn or fluid ideo creation), and those screens work normally again.
- Removed a performance drain: mod settings were being saved to disk every frame while the settings window was open. Colonist bar role lookups are now cached.

**Other**
- About.xml now lists Ideology as a required DLC.

## 1.0.3
- Experimental cognitive dissonance mode, prisoner conversion, social tab rework.
