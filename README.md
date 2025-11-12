# Wang Slime

Slime Mold simulator using Wang Tiles to generate an obstacle map.

The original Wang Tiles algorithm has been modified slightly to weight tile
selection to favour different levels of porousness.

Once a map is generated, it is baked into a texture and passed to the Slime
simulation, where it is treated as negative pheromones by the agents. Agents can
ignore this if they are unable to turn before reaching a wall, or their sensors
go beyond the bounds of the walls. This is mostly an issue for highly porous
maps.

## Acknowledgement

The Slime Mold simulation portion of this project is heavily based on the work
of Sebastian Lague, who's video on the subject introduced me to Slime Molds, and
got me interested in shaders in general years ago. I learned to write compute
shaders for this project, so I often cross-referenced
[his code](github.com/SebLague/Slime-Simulation) during trouble shooting. Please
direct all your admiration towards his work.

## LLM Usage

I used LLMs to generate some functions I would rewrite--most notably
WangTextureBaker, which was generated using the prompt "Given two inputs, a nxm
bytemap, and a 16 tile long sprite sheet corresponding to the byte map values,
write a function that generates a R8 texture I can use for agent pathing on the
GPU." I did this as I was unfamiliar with generating textures from C# prior to
this.
