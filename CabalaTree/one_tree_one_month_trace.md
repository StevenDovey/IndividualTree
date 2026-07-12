09.07.26

# One tree, one month: linear trace

Follows a single grid cell (`row`, `col`) through one call to `update_tree_list()`.
No function jumps, just the order of operations and the variable names involved.

1. Find this tree's 8 neighbours (left, right, top, bottom, 4 diagonals) by searching outward until a living tree or the 100 m edge is hit.
2. From those neighbours, work out `MaxSpaceTree`, the ground area this tree has to itself.
3. Crown length: `treelcrown = height - greenht`.
4. Crown width: grows toward `0.5 x crownRatio x height`, capped by available space, never shrinks.
5. Crown area and crown volume computed from crown width and crown length (treated as an ellipsoid).
6. Hourly loop, 24 hours: sun position computed, and for each of the 8 neighbours, check if it's between this tree and the sun and taller. If so, attenuate that hour's light through the neighbour's crown (Beer-Lambert). Keep the lowest of the 8 directional values each hour. Sum over the day, divide by unshaded total, this tree's `crownfac` (0 to 1).
7. Leaf area: this tree's share of stand total leaf area, weighted by its crown volume against the whole grid's crown volume.
8. Sapwood area and mass (stem, coarse root, branch) computed from that leaf area and height, pipe model.
9. Light-limited carbon production computed for this tree, scaled by `crownfac` and this tree's individual random productivity factor.
10. Allocation ratios: this tree's share of total grid production, total leaf area, total sapwood mass.
11. Those ratios split the stand's foliage, root, stem, bark, branch, coarse-root pools down to this tree.
12. This tree's carbon balance this month: its share of gross production, minus its share of the four respiration terms.
13. That value pushed into this tree's rolling monthly history (`treecbal`); summed to get `treecbalyear`.
14. Volume increment applied, proportional to this tree's share of total carbon allocated grid-wide this month.
15. Mortality check: dead if `treecbalyear <= 0`, or volume `<= 0`, or overtopped (`0.9 x height < greenht`).
16. If alive: H/D ratio recomputed from this tree's current foliage/branch/stem/bark mass ratio (West allometry, capped at 2% change).
17. Height and diameter recomputed from volume using that H/D ratio.
18. Volume rescaled so the grid total matches the stand model's total; height and diameter recomputed again from the rescaled volume.
19. If dead: every per-tree array for this cell zeroed.
20. Grid-wide: stocking, basal area, mean height/diameter, spacing updated for next month.

Step 6 is the "ray tracing" component. Steps 16-18 are the West allometry / height-diameter recovery.
