"# wism" 

For discussion and more details, see: www.facebook.com/groups/unofficalwarlords/.

[Overview]

War! Welcome to War in a Senseless Mind (WISM). WISM is an original Warlords 1990 clone, but is designed for modification and reuse/fork into similar projects as a game engine. Additionally, WISM was designed to have all core game logic outside of proprietary platforms like Unity or other game engines. This will allow for easier extension in the future and longevity as platforms evolve. A good example of this is the ASCII WISM app which provides a Console-based UI to the same game engine. 

Why another Warlords clone? Can't we just play the original on DosBox or Warlords Classic Strategy on iOS? Sure you can and you should! However, the original has limited re-playability on DosBox and old resolutions, it does not allow mods, new maps, armies, or features (though Warlords III introduced some of this). Warlords Classic is an iOS exclusive latter and closed source. We deserve more Warlords and we can with inspiration from SSG and all the subsequent great turn-based strategy games.

Here is a brief orientation to the projects.

[WISM Client]

This project is for core WISM game logic and is separated roughly into the API (Controller), Core (Model), and Agent (ASCII View). 

API:
The Client API is the local contract for a UI, AI, or remote player interface. Since all changes must be driven through Commands, the interface for a UI like Unity or the Console, an AI or set of AI controllers, or remote players is the same. This drives simplicity and is a key design principle.

Further, this layer is designed such that all game state changes driven by a controller are replayable deterministically. This allows for any controller set to the same random seed to stay in sync without copying or replicating game state other than the Command objects. This is also a key design principle and must not be violated. There may be additional non-game state, such as user information, chat, or similar state that may be managed outside of the command channel.

Core:
The Core model contains the entities like Army, Terrain, and Items; though it also contains business logic for the core game states and operations such as private IWarStrategy and IPathingStrategy implementations to be consumed or extended by the API. It may be extended either directly or via the Module interfaces. This layer is designed for mods to be created as new army types, cities, or other items are added. Examples are found under "/mod" in the form of JSON files.

Agent:
The agent contains basic primitives for constructing a game loop and interfacing with the WISM API controller and Core model (read-only). This contains a reference implementation called ASCII WISM to demonstrate extension. It also provides the structure for an agent to push or consume remote Commands. The agent will be the sync mechanism with the cloud or other remote play options to apply or send commands between systems. 

[WISM Unity]

This is the primary UI for the Warlords clone implementation. It is designed to accurately reflect Fawkner's look and feel. It borrows concepts, art, and sound to skin the game with an authentic experience. The desire is to add similar Module capability to the Unity UI to allow for easy extension, similar to WISM Core. At present updates and mods require changes to the Unity environment prefabs and GameObjects, which is not the long-term goal. As mentioned above, game logic is pushed as low as possible in the stack--down towards Core. However, there may be some user experience elements that are best left to the View (ASCII WISM or Unity WISM). Examples include display of combat sequences, army selection, or cut-scenes. 

The choice of using Unity and all .NET Standard 2.0 binaries means that this game is fully portable to all modern gaming environments, including Windows PC, iOS, Android, Xbox, or any of the platforms supported by Unity. Currently, the primary device target is Windows as it most closely honors the original Warlords spirit. 

Thank you for visiting. I hope you enjoy the game!

Legal Disclaimer: Warlords and all related imagery and references are copyrights of SSG registered trademarks and attributed to Steven Fawkner and Roger Keating. Use of this code is for educational purposes only and may not be distributed for commercial use.

[References]

https://en.wikipedia.org/wiki/Warlords_(1990_video_game)
http://www.ssg.com.au/

No affiliation with Warlords Classic. For official Warlords content, visit and support: https://www.facebook.com/WarlordsGame.
