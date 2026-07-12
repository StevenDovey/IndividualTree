#09.07.26 19:57
# ============================================================
# Driver skeleton for individual_tree_model.R
#
# tree_array_setup() only needs site/species/spacing parameters and a
# starting mean height -- it initialises the grid once, at planting.
#
# update_tree_list() is the monthly step, and it is NOT self-driving: it
# takes the CABALA stand model's monthly output as input (gross
# photosynthesis, four respiration terms, actual evapotranspiration, solar
# radiation, and the current stand biomass pools). This individual-tree
# module disaggregates a stand model's monthly result across the grid; it
# does not compute stand-level photosynthesis, respiration, or water
# balance itself. Without a stand model supplying those values every
# month, it cannot advance past initialisation.
#
# Values below are split into three groups:
#   SOURCED   -- from individual_tree_model_parameters.csv (Pradiata2010
#                parameter set, category B)
#   SCENARIO  -- site/regime numbers that must be supplied for a specific
#                run (planting spacing, latitude, block layout); no
#                defaults exist in the extracted source, left unset
#   MONTHLY   -- must come from the stand model, per month; left unset
# ============================================================

pars <- list(
  # --- SOURCED (individual_tree_model_parameters.csv, Pradiata2010) ---
  crownRatio      = 2.0,
  k               = 0.5,
  theta           = 0.9,
  branchangle     = 45.0,
  branchTaper     = 1.3,
  density         = 0.425,
  beta1           = 0.41,
  beta2           = 0.76,
  beta3           = 1.12,
  wfAlloc1ForCalc = 1.5,
  Wfalloc2        = 0.9,
  Wfalloc3        = 0.4,
  alpha           = c(0.050, 0.016),  # alpha0, alpha1 (quantum efficiency, layer 1/2)
  ax              = c(40.0, NA),      # ax[1] = aoptstar (layer 1); ax[2] not in CSV -- unsourced
  rC              = 0.25,
  wSx1000         = 600.0,
  varscalar       = 0.3,
  varscalarself   = 0.5,
  percentselfs    = 30.0,
  performself     = 0.7,
  seedvar         = 0.3,
  bnormdistseed   = TRUE,             # CSV value -1.0 = true
  monthcstarve    = 80,               # CSV overrides the C# hardcoded default of 36
  Gamma           = 2000000,
  dmc             = 30,

  # West1/West2/West3 (81.9, -0.29, -0.12 in the CSV) are NOT wired in:
  # the extracted update_tree_list() hardcodes 1.3 / -0.22 / -0.136 for
  # the H/D allometry directly and never reads pars$West1/2/3. Left out
  # rather than included-but-unused, to avoid implying they do something.

  # --- SCENARIO (site/regime -- no source in the extracted fragment) ---
  initInterRow = NA,  # m, fixed spacing at planting
  initIntraRow = NA,  # m
  interRow     = NA,  # m, current spacing (== initInterRow before any thinning)
  intraRow     = NA,  # m
  interbay     = NA,  # m, 0 if bays == normal row spacing
  interblock   = NA,  # m, 0 if blocks == normal intra-row spacing
  rowsblock    = NA,
  colsblock    = NA,
  rowdirection = NA,  # degrees from north
  lat          = NA,  # degrees
  dayOfYear    = NA,  # representative day of year for the simulated month
  ht           = NA,  # m, starting stand mean height
  actualstocking = NA,
  effectivestocking = NA,

  # unsourced, needed by update_tree_list() -- no value available in the
  # extracted fragment or its parameter CSV
  rootdepth = NA
)

# st carries the per-tree grid state (built by tree_array_setup) plus the
# stand-level pools update_tree_list() reads each month. The stand-level
# pools below are placeholders: for month 1 they should be the stand
# model's initial condition; for month N+1 they are month N's stand-model
# output, not this module's own output (the individual-tree grid tracks
# volume/height/diameter per tree, but total pool masses -- wf, Wssw, etc.
# -- are stand-model state that this module reads, not writes).
st <- list(
  greenht = NA, L = NA, vol = NA, wf = NA, Wssw = NA, Wshw = NA,
  Wb = NA, Wcr = NA, Wcrsw = NA, Wcrhw = NA, Wfr = NA, Ws = NA, Wbk = NA,
  h = NA,  # unsourced -- see header note
  lostWf = 0, lostWfr = 0, lostWssw = 0, lostWb = 0, lostWcr = 0,
  LostStemMass = 0, selfThinnedWssw = 0, selfThinnedWshw = 0,
  selfThinnedWbk = 0, selfThinnedWcr = 0
)

# --- STEP 1: one-off initialisation at planting ---
# st <- tree_array_setup(st, pars)

# --- STEP 2: monthly loop, driven by the stand model's output ---
# for (each simulated month) {
#   stand_month <- <read from stand model output for this month>
#   st <- update_tree_list(
#     st, pars,
#     stemincmonth = stand_month$stemincmonth,
#     ggrossmonth  = stand_month$ggrossmonth,
#     Rcrmonthin   = stand_month$Rcrmonthin,
#     Rbmonthin    = stand_month$Rbmonthin,
#     Rfmonthin    = stand_month$Rfmonthin,
#     Rfrmonthin   = stand_month$Rfrmonthin,
#     Rsmonthin    = stand_month$Rsmonthin,
#     mmonth       = stand_month$month_index,
#     wdate        = stand_month$date,
#     wcf          = stand_month$date,
#     aetmonth     = stand_month$aetmonth,
#     qmonth       = stand_month$qmonth
#   )
#   # st$wf, st$Wssw, st$Wshw, st$Wb, st$Wcr, st$Wcrsw, st$Wcrhw, st$Wfr,
#   # st$greenht, st$L must also be refreshed from the stand model's
#   # month-end state before the next iteration.
# }
