# funrika-demo

## Known issue

- Issue 1 | Hint will place unplaced piece on top of wrongly placed pieces
- Issue 2 | Some level generations produces pieces that won't fit into the screen area

## Potential solutions

### Issue 1

#### Alternative 1
- Find a piece that can be placed without overlaps. Not perfect as no such piece might exist.

#### Alternative 2
- Remove overlapping pieces back to their initial positions. Can be combined with more detailed algorithms to decide on the piece that will cause minimal removals

### Issue 2

#### Alternative 1
- Group some pieces together to for more compact pieces.

#### Alternative 2
- Add a slider to bottom area

#### Alternative 3
- Use tigther colliders instead of bounding boxes for spacing

#### Alternative 4
- Precompute viable seeds that avoid this issue and feed the seed through level definition file
