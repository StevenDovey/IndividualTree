The Forecaster is a large C# desktop application built on Windows Presentation Foundation (~40 projects, 743 source files), but the scientific model logic is separable from the object-oriented architecture, graphical interface, and database infrastructure that surrounds it. The mathematics lives in the Modeller classes and growth model implementations — those are extractable and transcodable to R.

Critically, there is significant overlap with what is already in R. The Forecaster's carbon modeller calls C_Change, and its stand grower includes the 300-Index model — both already transcoded. The new content relative to the existing R code is:

- Additional species growth models not yet in R: cypress, Fastigata, the Forests New South Wales plantation models, the Individual Tree Growth Model (a radiata pine model for Sands or Clays soil regions), and the Carter Holt Harvey regional models (a family of radiata pine growth curves fitted to specific Carter Holt Harvey estate regions, later acquired by Hancocks Natural Resources)
- Wood property modellers not yet in R: stiffness, heartwood, sweep, spiral grain, microfibril angle
- Coast redwood carbon (the Kizha_Han empirical biomass routine) — already flagged as a known gap

---

## Audit: R vs Forecaster — Component Comparison

### Radiata Pine 300-Index Growth Model

MATCH. Height (Chapman-Richards), diameter model and all coefficients, basal area calculations, mortality function and all coefficients, volume equations, thinning logic, pruning logic, bisection solver, wood density model, and genetic gain bias adjustments are numerically identical across R and Forecaster.

---

### C_Change / DRYMAT Carbon Model

MATCH. All 16 ecosystem pool dynamics, biomass partitioning coefficients (needles, stem wood, stem bark, branches, roots), decay and decomposition constants, Euler integration scheme, thinning carbon transfers (except branch extraction — see below), pruning carbon transfers, needle retention, and shrub understorey carbon are identical in R and Forecaster.

MISMATCH 1 — Branch extraction during thinning: R includes both live branches (pool 6) and dead branches (pool 7) in the extracted biomass calculation. Forecaster includes live branches only. Forecaster appears to have a bug introduced during its translation from VBA.

MISMATCH 2 — Forest floor initialisation: R reads the forest floor pool values (pools 10, 11, 12, 13, 15) from input for the first rotation. Forecaster sets all these pools to zero regardless of input.

MISMATCH 3 — Nutrient concentrations: R uses continuous soil carbon/nitrogen ratios to calculate site-specific nitrogen and phosphorus concentrations, matching the original VBA approach. Forecaster replaced this with three fixed categories (Low, Medium, High fertility) using hardcoded reduction constants. The two implementations cannot reproduce each other's results when soil carbon/nitrogen data is available.

---

### Log Grading, Branch Index, Pruned Log Index

MATCH. Branch Index second log (BIX2), first log (BIX1), and upper log (BIXn) are all identical between R and Forecaster. The BIX2 log-argument intercept is written differently in the two sources but is algebraically the same: R carries two constant terms (0.985 and -0.212 x 14) that sum to -1.983, which is the single intercept the Forecaster uses. All predictor coefficients (0.356 mean DBH, -0.321 thinning height, -0.354 site index) are identical.

IN R ONLY (no Forecaster equivalent): log cutting algorithm, grade specification checks, Pruned Log Index, Degree of Separation, residue allocation.

---

### Wood Quality

MISMATCH 4 — Outerwood density model version. R and the Forecaster use the same ring-density functional form but a different calibration. This is a model-version gap, not an isolated equation slip.

The R ring-density function (growth_300index.R, outdens) was transcoded faithfully from the VBA (the 300-Index comparison confirmed R matches VBA exactly). The Forecaster uses the newer FFRDensity2011 calibration (Kimberley et al. 2011), which never existed in the VBA. Nearly every coefficient in the shared ring-density equation differs between the two:

| Coefficient | R outdens (older, from VBA) | Forecaster FFRDensity2011 |
| --- | --- | --- |
| Intercept | 477.8 | 579.8 |
| Ring-width term | 46.2 | 58.61 |
| Interaction term | 0.0042 | 0.002763 |
| Ring-number term | 84.8 | 80.17 |
| Decay term | 0.258 | 0.1845 |

Within the two reference-point equations that feed this model:
- Equation 12 (outerwood ring-width regression: 10.19 + 0.0893 x 300 Index - 0.255 x site index + 0.00373 x site index squared - 0.00339 x 300 Index x site index) is character-identical between R (growth_300index.R line 1226) and the Forecaster (line 287). It is the single sub-equation unchanged across the two model versions.
- Equation 10 (reference outerwood ring number) differs: R uses 18.95 - 0.024 x site index (line 1225); the Forecaster uses 18.0 - (7.8 - 0.329 x site index + 0.00388 x site index squared) (line 286).

Separately, wood_quality.R carries a second, whole-tree average density projection (density_calc: 202.3 + 0.415 x A - 3.12 x rotation age + 0.0081 x A x rotation age) used for reporting. This has no Forecaster equivalent. The density modelling therefore diverges between R and Forecaster and the two will not produce the same outputs.

MISMATCH 5 — Genetic gain adjustment: Forecaster applies an adjustment of `unimprovedDensityIndex + ((GF+ rating - 22) x 2.16)` to the density index. R does not include this adjustment. This adjustment is not present in VBA either — it is a Forecaster addition.

IN FORECASTER ONLY (no R equivalent): microfibril angle model.

IN R ONLY (no Forecaster equivalent): juvenile wood fraction calculation.

---

### Crown Closure, Pasture Production, Slash Decay

IN R ONLY. Crown closure percentage, pasture production, slash coverage decay, and livestock carrying capacity calculations have no equivalent in the Forecaster. These were added during R development and are not derived from any Forecaster component.
