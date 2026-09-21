#War! ...in a senseless Mind Manual

Commands:

General:
Left-click				Select | Move | Attack
Right-click				Deselect
Right-click + Drag		Move map

Army Actions:
M	Open Army Picker (select armies)
N	Select next army with moves
D	Defect army (skipped by 'N')
Q	Quit army (skipped by 'N' until next turn)
Z	Search location (some locations require a Hero)

Hero Actions:
T	Take item
O	Drop item

City Actions:
P	Enter production mode (then select city)
R	Raze city
B	Build city defenses
C	Go to current player's owned capital (also View > Capital)

Game Actions:
E	End turn
S	Save game
L	Owned-cities production management
Shift+L	Load game
?	Toggle help
X / Ctrl+Q	Request application exit with confirmation (not Army Quit)

Game menu:
Observe AI Moves	Follow nonhuman army movement; off leaves the camera where you put it.
Show AI Combat	Show AI-versus-AI battles independently of movement observation.
Save Game / Load Game	Open the existing save/load picker when safe.
Exit Game	Confirm application exit; Cancel preserves the game.
Observe is saved with the game and defaults on for older saves. It does not skip
turns, alter AI decisions, hide human battles or enable bulk simulation. Current
command cadence is unchanged; there is no extra movement-animation wait to bypass.

View menu:
Capital	Center on the current player's owned, unrazed capital without changing
selection. Disabled if there is no owned capital. Available during final inspection.

Minimap Actions:
Left-click		Go-to on map
.				Toggle mini-map (on/off)

Production destinations:
Select an army, then Loc, then a city on the minimap or main map.
Yellow is the source (local production), white an eligible owned destination,
red an owned destination with no free incoming route, and gray an unowned city.
Back, Escape or right-click cancels destination selection without issuing orders.
Navies produce locally and cannot be vectored. A city receives from at most four
distinct sources; paid armies in transit retain their source slot. Production
finishes before the two additional transit turns. Existing routes can be renewed
without taking another slot. Ownership and capacity are checked again on delivery
of the production order, not only when Loc opens.

Production management:
Route rows distinguish current training from paid armies at two-or-more turns,
one turn, or waiting for a legal arrival tile. From/To city buttons navigate
without issuing production orders. Long route lists scroll; Stop ends training,
not paid transit. A changed route can still have deliveries bound for its previous
destination. Returning armies whose destination was razed retain their transit ETA.

Production report:
Reports > Production shows the current player's producing-city percentage and
owned-city list. Transit counts are armies, not source-city counts. White outlines
mark incoming routes and yellow outlines outgoing routes, including paid transit
after training stops. A city with both directions has both outlines. City fills
retain WISM ownership colors; no unverified classic production-fill meaning is implied.

Debug Actions:
,		Go to location
{Tab}	Toggle debug output
