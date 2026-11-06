#War! ...in a senseless Mind Manual

Commands:

General:
Left-click				Select | Move | Attack
Right-click				Deselect
Right-click + Drag		Move map

Army Actions:
M	Open Army Picker (select armies)
N	Select next army with moves
D	Defend selected armies and select the next army
Delete	Disband selected ordinary armies, with confirmation; heroes are preserved
Q	Quit army (skipped by 'N' until next turn)
Z	Search location (some locations require a Hero)

Hero Actions:
T	Take item
O	Drop item

City Actions:
P	Enter production mode (then select city)
R	Raze city or tower, with confirmation
B	Build city defenses or a new tower
C	Go to current player's owned capital (also View > Capital)

Game Actions:
E	End turn
S	Save game
L	Owned-cities production management
Shift+L	Load game
?	Toggle help
X / Ctrl+Q	Request application exit with confirmation (not Army Quit)

Game menu:
Menu / F10	Open the menu; the other menu tabs stay hidden during ordinary play.
Escape	Close the menu or cancel a pending Disband confirmation without an order.
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

Orders menu:
Build / Raze	Use the selected army to build or destroy a structure.
Disband	Preview the ordinary armies to remove and the heroes to preserve.
Cancel or Escape leaves the stack unchanged. Confirm permanently removes only
the previewed ordinary armies and immediately reduces upkeep. Heroes and their
artifacts remain. A changed selection or unavailable turn cannot reuse an old
confirmation. Army Quit is still a temporary selection skip, not Disband.
A new tower costs 100 gp and can only be built on clear plains. Confirm the
quoted price to build; Cancel or Escape leaves your gold and the site unchanged.
Razing a tower turns it into permanent ruins and removes its defense bonus.
Neither a razed tower nor a razed city can be rebuilt on the same site.

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

Hero views:
View > Inventory, or I, shows the selected owned heroes' artifacts and their
combat and command bonuses. Choose a hero row when a stack has multiple heroes.
View > Find Heroes, or K, lists your living heroes. Go to Hero moves only the
camera; selection, movement and ownership are unchanged. Long lists scroll.
Close or Escape returns to the map. These views are read-only and unavailable
during computer turns, pending orders or other dialogs. Inventory is empty when
no hero is selected. Read-only inspection remains available after a human game ends.

Debug Actions:
,		Go to location
{Tab}	Toggle debug output
