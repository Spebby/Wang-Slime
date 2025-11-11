# Wang Slime

Slime Mold simulator using Wang Tiles to generate an obstacle map.

## LLM Usage

I used LLMs to generate some functions I would rewrite--most notably
WangTextureBaker, which was generated using the prompt "Given two inputs, a nxm
bytemap, and a 16 tile long sprite sheet corresponding to the byte map values,
write a function that generates a R8 texture I can use for agent pathing on the
GPU." I did this as I was unfamiliar with generating textures from C# prior to
this.
