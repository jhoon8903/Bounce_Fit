# Stage 1 enemy design review v001
Status: concept review, not production sprites or Unity import. Generated using built-in imagegen from the established enemy vehicle style reference.
15 human / empty vehicle pairs. Top row is standalone character; bottom row is corresponding empty vehicle. Mix-and-match intended. Raster boards are not separate alpha sprite files.
Order left to right:
01 Lumberjack / logging cart / square 1x1
02 Hunter / cannon cart / square 1x1
03 Construction worker / bulldozer / horizontal 2x1
04 Log hauler / logging truck / vertical 1x2
05 Biker / passenger sidecar / L
06 Surveyor / surveying rover / diamond
07 Welder / armored wedge rover / diamond
08 Demolition worker / twin launcher carrier / horizontal 2x1
09 Waterworks worker / tanker / vertical 1x2
10 Mechanic / tool sidecar / L
11 Foreman / command cart / square 1x1
12 Miner / drill truck / vertical 1x2
13 Road worker / roller / horizontal 2x1
14 Electrician / coil rover / diamond
15 Signal scout / radio sidecar / L

Boss: mobile sawmill tower. Independent left saw launcher, central boiler tower and right twin launcher. Proposed fight: destroy side modules, then defeat central core. Attack/balance values not implemented.

Visual QA: boards contain five standalone humans plus five empty vehicles each; boss assembled plus three separated module concepts. Shared smooth SD style and broad color palette. No cropping. Some clothing details and wheel geometry will need simplification at production size. Footprints are DESIGN TARGETS: review perspective art is not precise collision geometry. In particular broad vehicles need true 2:1 calibration and sidecars need grid-aligned L silhouettes, with empty corner retained and internal seams excluded from collision.
No existing gameplay SO, scene, code or build changed.

