# GravityCa

## Controls:
### Simulation Controls:
Space - toggles simulation running or paused
TODO:
Left click to add a mass with the current divisor
Right click to remove mass
Mose wheel adjusts the divisor

0-9 - Change colors of gravity
Alt+0-9 - Change colors of mass

F1 - 2d render mode
F2-F6 3D render modes

M - Toggle mass renderer
G - Toggle gravity renderer
B - Toggle "Gen mass" mode for generating mass each step

Z - Zero-out mass field
X - Zero-out gravity field
C - Clear both fields
R - Randomly fill .01% of the remaining open mass field with the current divisor
H - Fill the mass field with the current divisor

### 3D Controls
Use a gamepad for the easiest time. For keyboard:
WASD for movement, R and F for up and down. Keypad for rotations etc.

## Simulation Details
Mass and gravity are modeled as two 2D fields. The user inputs mass particles with a given mass, determined by the Divisor input (MaxMass / Divisor). As the simulation runs, it updates the gravity and mass fields based on each other. The mass field is conserved, meaning no particles will ever "disappear". The gravity field is calculated from the mass field and itself. If in "gen mass" mode (there's an asterisk next to "Running") it will randomly generate mass each step.

For each gravity cell, it looks at the mass field, and sets the gravity to MassValue diminshed by some amount. It then takes the average of the gravity from all adjacent cells, and adds that to the current gravity from the mass field.

For each mass cell, it looks at the adjacent gravity field cells as well as its own gravitation field cell. It orders them by strength, and randomly picks the strongest gravitation to "fall" towards. This could mean "falling" toward its own cell, thus not moving. If a cell (other than itself) occupies the cell it tries to fall toward, it falls toward another random cell with the same gravitation strength. If all cells with the highest gravitational strength are occupied, it goes to the next strongest gravitational strength. It does this until there are no other cells, in which case it stays put.

The amount of mass that can occupy a since cell is proportional to the gravity at that cell.
