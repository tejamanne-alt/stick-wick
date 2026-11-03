# Motion capture

The stickman's martial-arts moves come from the **CMU Graphics Lab Motion Capture Database**
(http://mocap.cs.cmu.edu), in the BVH conversion by Bruce Hahne
(mirror: https://github.com/una-dinosauria/cmu-mocap).

> "This data is free for use in research and commercial projects worldwide."
> — CMU, in the conversion's READMEFIRST.txt

Credit line for the game's credits screen:
"The data used in this project was obtained from mocap.cs.cmu.edu.
The database was created with funding from NSF EIA-0196217."

## Files

`cache/*.npz` holds joint positions (30 fps, metres, Z up) extracted from the recordings in use.
That is all the build needs:

| Recording | Used for |
| --- | --- |
| 13_17 | idle (boxing guard, looped) |
| 14_01 | jab, cross |
| 86_06 | front kick, knee |
| 86_08 | roundhouse, back kick |
| 90_05 | flying kick |
| 88_06 | spin kick |
| 88_01 | dodge (backflip) |
| 90_18 | knockdown |
| 09_02 | run |

To add or re-cut moves:
1. Put the source BVH files in `tools/mocap/bvh/`. They are not committed, and are fetched from the mirror above as `data/0XX/XX_YY.bvh`.
2. Edit `tools/blender/mocap_clips.py`.
3. Check the result with `tools/blender/preview_mocap.py`.
4. Rebuild with `tools/blender/build_stickman.py`.
