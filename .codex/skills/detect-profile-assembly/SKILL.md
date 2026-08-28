---
name: detect-profile-assembly
description: Reconstruct and verify aluminium T-slot profile assemblies, their step-by-step assembly instructions, and functional adapter plates from renders, photos, drawings, or assembly-coordinate data. Use when profile type, orientation, endpoints, joints, movement, coplanar faces, plate interfaces, holes, slots, or simplified manufacturable plate contours must be established.
---

# Detect profile assembly

Determine the construction per individual profile. A visually plausible overall shape is not sufficient.

## Choose the evidence mode

- Prefer native CAD, BOM, or assembly-placement coordinates when available. Treat these as the dimensional source and use the image as an independent render check.
- For an orthographic image with dimensions, calibrate pixels separately for each principal plane.
- For a perspective render or photo, reconstruct the three vanishing directions before measuring. Do not use one global pixel/mm ratio.
- Treat a single perspective view as provisional for hidden endpoints and depth orientation. Ask for or generate another view when those facts affect the model.

Read [references/measurement-method.md](references/measurement-method.md) for image reconstruction. For SWwerkplaats data or portal renders, also read [references/project-integration.md](references/project-integration.md).
When custom connection, mounting, hinge, or adjustment plates must be inferred or simplified, also read [references/adapter-plate-inference.md](references/adapter-plate-inference.md).
When generating or reviewing step-by-step profile assembly instructions, also read [references/assembly-instruction-validation.md](references/assembly-instruction-validation.md).

## Reference-to-candidate comparisons

Never audit only the candidate when a reference is supplied. Build two separate inventories first, then produce a delta table with `missing`, `extra`, `wrong section`, `wrong axis`, `wrong cross-section orientation`, `wrong extent`, `wrong plane`, and `wrong joint face`.

Treat an image-derived reference manifest as a versioned hypothesis, not ground truth. Store the evidence and confidence for each inferred orientation or offset. If a later view disproves it, revise the reference manifest first, record why, and then compare the candidate again.

Freeze a blind reference inventory before reading candidate coordinates. Every claimed 40/80/160-mm face must cite independent image evidence such as module count, slot-centre count, an end face, or a calibrated neighbouring width. A zero programmatic delta is invalid when the rendered candidate still contradicts this evidence.

Keep reference context outside the requested assembly scope separate. For example, a robot shown on a frame is not automatically a BOM item, but its mounting profile and interface are part of the frame when visible and requested.

## Required profile record

Assign every load-bearing profile a stable ID and report:

- proposed family and series;
- longitudinal axis (`X`, `Y`, or `Z`);
- cross-section dimensions mapped to the other two axes;
- start and end coordinates or calibrated image points;
- six outside face planes;
- visible versus inferred evidence;
- contacts, coplanar faces, gaps, and overlaps;
- confidence (`confirmed`, `probable`, or `unresolved`).

Record an explicit role such as `top-front-rail`, `lower-left-rail`, or `lower-crossmember-01`. A profile family alone is insufficient: `40x80 axis X, Y=80, Z=40` is standing, while `40x80 axis X, Y=40, Z=80` is flat.

Assign every member to a construction layer and freeze an expected count manifest before building the candidate. The manifest must distinguish roles, physical quantity, shared specification, and numerical orientation—for example `lower-perimeter: 4 x 40x80 standing, Y=80` plus `lower-crossmember: 1 x 40x80 standing, Y=80`. The build must consume this manifest and the final audit must compare actual versus expected counts per role and per layer. Do not replace an uncertain count with zero.

Classify 40 mm modular profiles using combined evidence: slot-centre pitch, visible face-width ratio, end face, neighbouring joints, BOM names, and known dimensions. Groove count alone is insufficient because slots can be hidden, doubled by edge highlights, or distorted by perspective.

Do not invent a new member or profile family for every visible band. First test whether the band is a T-slot, edge highlight, shadow, sheet edge, or another face of an already inventoried profile. When a lower perimeter and its crossmembers share the same section and orientation, record them as separate members with one shared specification—for example `40x80 standing`—not as an additional profile layer or type.

## Geometric checks

For coordinate data, calculate each box as centre ± half-size. Compare face planes rather than centre points.

- A load-bearing face contact requires opposing faces within tolerance and positive overlap on both in-plane axes. Report line and point contacts separately; do not accept them as structural joints when that pair is an intended load path. Incidental edge contact between members that both connect elsewhere is informational.
- Coplanarity requires equal plane coordinates; identify whether it is outer-face flushness, an opposing joint plane, or coincidental alignment.
- Report positive separation as a gap and intersecting volumes as overlap.
- Before rejecting a profile-to-profile gap, check whether a sheet, adapter, or other intended interposer fills it.
- Verify intended topology: which profile terminates against which, which profile continues through, and which members merely appear connected because of occlusion.
- Verify the expected contact faces. A valid contact on the wrong face is still a topology mismatch; for example, `beam bottom on upright top` is not equivalent to `beam end against upright side`.
- Never equate equal centre coordinates with `flush_with`. Treat an 80-mm profile face as two discrete 40-mm module lanes. A 40-mm beam connected to that face must occupy one lane completely: the outer/front lane or the adjacent lane one full module (40 mm) farther in. The two valid beam-centre positions are therefore `upright centre - 20 mm` and `upright centre + 20 mm` along that cross-axis; `0 mm` relative offset is invalid because it lies between the T-slot centre lines. Describe the chosen lane by named faces, not as a generic required 20-mm offset.
- Validate T-slot compatibility at every unequal-section joint: series/type, 40-mm module pitch, slot centre-line coincidence, chosen lane, and connector access. A silhouette that looks flush is not enough.
- For each perimeter rail, check all four relevant outside planes against the adjoining uprights. Report `outer face flush`, `inner face flush`, `centred`, or the signed step in millimetres; the word `aligned` without a named face is insufficient.

## Construction manifest and end treatment

Before generating geometry, store a versioned manifest with at least `layer`, `role`, `section`, `orientation`, `axis`, `expected_count`, and evidence/confidence. Preserve that count through the build, BOM, and final audit. Repeated members get stable indexed roles such as `lower-crossmember-01`.

Treat orientation as an executable geometry contract, not a prose label. For every `40x80 standing` member, assert that the model vertical extent is 80 mm and the transverse horizontal extent is 40 mm; for `40x80 flat`, assert the inverse. A candidate that matches the count but violates this axis mapping fails before visual approval. When evidence changes an orientation, revise the manifest first and then rebuild—never keep an obsolete manifest merely because the generated model matches it.

Inventory every profile end as `joined`, `covered`, `machined/fastened`, or `exposed`. Every exposed end must either have a documented reason to remain open or receive a compatible end cap. Match caps by profile series/type and exact cross-section, not dimensions alone. Record cap article, quantity, and the exact profile end where it is placed. Count one cap per exposed end; an end hidden in the chosen view is still an end.

Treat every custom plate as its own manifest member. Record the connected faces, constrained and adjustable degrees of freedom, required fastener or equipment interfaces, thickness status, contour confidence, physical quantity, mirror relationship, and every hole/slot with its functional owner. Do not copy decorative lightening cutouts unless they serve a demonstrated clearance, ventilation, access, weight, or load-path function.

Use `scripts/profile_geometry_audit.py` for exact assembly JSON, `scripts/profile_geometry_compare.py` to regression-compare expected and actual audit JSON, and `scripts/profile_layer_manifest_check.py` to verify expected counts per layer/role including accessories. Use `scripts/profile_pixel_probe.py` only to produce line and intensity evidence from an image crop; interpret its candidates rather than accepting them as profile classifications.

## Visual verification loop

1. Produce an ID-coloured overlay with axis arrows, start/end markers, and profile labels.
2. Inspect at least two useful viewing angles, including one that exposes depth members and the underside where relevant.
3. Mark contacts green, unexplained gaps red, overlaps magenta, and unresolved hidden extents amber.
4. Correct source geometry, rebuild the render, and repeat the same views.
5. Re-check the reference manifest against the new view; revise inferred facts before using it as the regression oracle.
6. Repeat the same reference-to-candidate delta after every rebuild.
7. Overlay or inspect the candidate at the same corner/angle as the reference and explicitly verify silhouette steps at every unequal-section joint.
8. Reconcile the built profile and accessory counts with the frozen layer manifest; any non-zero delta blocks approval.
9. Stop only when there are no unexplained missing/extra profiles or caps, section/orientation/extent mismatches, wrong-face joints, or reference-visible coplanarity steps; the contact graph matches the intended construction; and remaining uncertainty is explicitly recorded.

Do not change exact geometry solely to make one perspective image look better.
