#09.07.26 12:56
# ============================================================
# CABALA Individual Tree Model -- R transcode of individual_tree_model.cs
# Source: CabalaTree/individual_tree_model.cs (extracted from CCabala.cs)
# Original C# line numbers are preserved in the section headers below.
#
# State representation
#   The C# original operates on module-level 2-D arrays (row, col) and
#   module-level scalars shared across all methods (VB6/VBA translation
#   pattern). R has no implicit shared mutable state, so all of that state
#   is carried in a single list `st` (matrices for per-tree state, named
#   scalars for stand-level state) and `pars` (site/species parameters,
#   Section A of individual_tree_module_reference.txt). Every function
#   takes `st`/`pars` as input and returns the updated `st` -- callers
#   must reassign, e.g. st <- tree_array_setup(st, pars).
#
#   Matrices are indexed [row, col], 1-based, matching the original.
#
# Not extracted from CCabala.cs (referenced by the extracted methods but
# not present in the source fragment -- calls are kept as-is rather than
# invented; they will error until supplied):
#   resetMonthlyArray(cmonth)   -- called from tree_array_setup()
#   RoundupToValue(x, to)       -- inferred utility, implemented below as
#                                  ceiling(x / to) * to
#   ThinningTypes enum          -- represented below as character constants
# ============================================================

# ------------------------------------------------------------
# METHOD: RoundupToValue
# Inferred utility (referenced but not defined in the extracted source).
# Rounds x up to the next multiple of `to`.
# ------------------------------------------------------------
RoundupToValue <- function(x, to) {
  ceiling(x / to) * to
}

# Character constants standing in for the C# `ThinningTypes` enum
ThinningTypes <- list(
  Random               = "Random",
  GroupSelectFromBelow = "GroupSelectFromBelow",
  GroupSelectFromAbove = "GroupSelectFromAbove",
  ThinBelowDiameter    = "ThinBelowDiameter",
  ThinAboveDiameter    = "ThinAboveDiameter"
)

# ============================================================
# METHOD: radians
# Original lines in CCabala.cs: 9049-9054
# ============================================================
f_radians <- function(x) {
  pi / 180 * x
}

# ============================================================
# METHOD: fTreeVolume
# Original lines in CCabala.cs: 3897-3900
# ============================================================
f_tree_volume <- function(Ht, HtoD, SPH, beta1, beta2, beta3) {
  beta1 * (Ht / 0.9)^beta2 * ((Ht / HtoD / 200)^2 * pi * SPH)^beta3
}

# ============================================================
# METHOD: lightcalc
# Original lines in CCabala.cs: 5874-5890
# ============================================================
f_lightcalc <- function(daylen, hour, qday) {
  td <- daylen * 24
  T <- abs(12 - hour)
  if (T > td / 2) {
    0
  } else {
    2.2 * qday * 1e6 * pi / (2 * td) * cos(pi * T / td) / 3600
  }
}

# ============================================================
# METHOD: gasdev
# Original lines in CCabala.cs: 9011-9031 / 9306-9327 (Box-Muller)
# Uses runif() in place of VBMath.Rnd() to preserve the original
# Box-Muller rejection-sampling algorithm exactly (rather than
# substituting rnorm(), which would change the random stream).
# ============================================================
f_gasdev <- function() {
  repeat {
    v1 <- 2 * (0.5 - runif(1))
    v2 <- 2 * (0.5 - runif(1))
    r <- v1^2 + v2^2
    if (r < 1) break
  }
  fac <- sqrt(-2 * log(r) / r)
  v2 * fac
}

# ============================================================
# METHOD: getAzimuthAndAlt
# Original lines in CCabala.cs: 9160-9208
# Returns list(azimuth = ..., solaralt = ...)
# ============================================================
f_get_azimuth_and_alt <- function(hin, dayofyr, lat) {
  HA <- 2 * pi * (12 - hin) / 24
  cosHA <- cos(HA)

  delta <- -(23.45 * cos(2 * pi * (dayofyr + 10) / 365)) * (pi / 180)
  sinDelta <- sin(delta)
  cosDelta <- cos(delta)

  mylat <- lat * pi / 180
  sinLat <- sin(mylat)
  cosLat <- cos(mylat)

  sinAlpha <- sinDelta * sinLat + cosDelta * cosLat * cosHA
  cosAlpha <- sqrt(1 - sinAlpha^2)
  alphaS <- asin(sinAlpha) * 180 / pi

  cosPhi <- (sinAlpha * sinLat - sinDelta) / (cosAlpha * cosLat)
  cosPhi <- max(min(cosPhi, 1), -1)
  psiS <- acos(cosPhi) * 180 / pi

  if (hin > 12) {
    psiS <- 180 + psiS
  } else {
    psiS <- 180 - psiS
  }

  list(azimuth = psiS, solaralt = alphaS)
}

# ============================================================
# METHOD: meanBranchLength
# Original lines in CCabala.cs: 9272-9290
# ============================================================
f_mean_branch_length <- function(baX, baY, baZ, btheta, bphi) {
  bmmX <- cos(pi * btheta / 180) * cos(pi * bphi / 180) / baX
  bmmY <- sin(pi * btheta / 180) / baY
  bmmZ <- cos(pi * btheta / 180) * sin(pi * bphi / 180) / baZ
  bmm  <- sqrt(bmmX^2 + bmmY^2 + bmmZ^2)
  BM   <- sqrt(bmmX^2 + bmmZ^2) / bmm
  1 / 2 / bmm * (sqrt(1 - BM^2) + asin(BM) / BM)
}

# ============================================================
# METHOD: meanAllBranchLength
# Original lines in CCabala.cs: 9291-9303
# ============================================================
f_mean_all_branch_length <- function(baX, baY, baZ, btheta) {
  BL1 <- f_mean_branch_length(baX, baY, baZ, btheta, 0)
  BL2 <- f_mean_branch_length(baX, baY, baZ, btheta, 45)
  BL3 <- f_mean_branch_length(baX, baY, baZ, btheta, 90)
  (BL1 * BL2 * BL3)^(1 / 3)
}

# ============================================================
# METHOD: areascalene
# Original lines in CCabala.cs: 12795-12819
# ============================================================
f_areascalene <- function(distance1, distance2, crown1, crown2) {
  t1a <- distance1 - crown1
  t1b <- distance2 - crown2
  if (t1a < t1b) {
    tmp <- t1a; t1a <- t1b; t1b <- tmp
  }
  t1c <- sqrt((t1a - t1b * cos(f_radians(45)))^2 + (t1b * sin(f_radians(45)))^2)
  st  <- (t1a + t1b + t1c) / 2
  sqrt(st * (st - t1a) * (st - t1b) * (st - t1c))
}

# ============================================================
# METHOD: fAgeMax
# Original lines in CCabala.cs: 2089-2092
# Called by: RowThinningTreatment, withinThinningTreatment
# ============================================================
f_age_max <- function(gammaF) {
  round(floor(1 / gammaF * 365))
}

# ============================================================
# METHOD: calcActualStocking
# Original lines in CCabala.cs: 13650-13673
# ============================================================
f_calc_actual_stocking <- function(rowsinblock, interrowdist, distbetweenbays,
                                    colsinblock, intrarowdist, distbetweenblocks) {
  if (distbetweenbays == 0)   distbetweenbays   <- interrowdist
  if (distbetweenblocks == 0) distbetweenblocks <- intrarowdist

  rowunit <- (rowsinblock - 1) * interrowdist + distbetweenbays
  RowNo   <- rowsinblock

  colunit <- (colsinblock - 1) * intrarowdist + distbetweenblocks
  colNo   <- colsinblock

  RowNo * 100 / rowunit * (colNo * 100 / colunit)
}

# ============================================================
# METHOD: calcLightLimitedProductiontree
# Original lines in CCabala.cs: 12864-12929
# ============================================================
f_calc_light_limited_production_tree <- function(treefrac, axtree1, axtree2,
                                                   alpha1, alpha2, leaftree, areatree, qtree,
                                                   k, theta, Gamma, h, dmc, m = 0) {
  a1 <- 0.22; b1 <- 0.74
  a2 <- -0.18; b2 <- 0.5

  lightfractree <- 1 - exp(-k * leaftree / areatree)
  # NOTE: original also assigns module-level `Qint` here (qtree * lightfractree);
  # that side effect is dropped since this function is pure in R. Recompute
  # Qint <- qtree * lightfractree at the call site if needed.

  Acdtree <- numeric(2)
  for (z in 1:2) {
    if (z == 1) {
      axtree <- axtree1; alphatree <- alpha1
    } else {
      axtree <- axtree2; alphatree <- alpha1  # NOTE: original uses alpha1 for both z==1 and z==2 (line 1879); preserved as-is
    }
    qq <- pi * k * alphatree * qtree * Gamma / (2 * h * 86400 * (1 - m) * axtree)
    f1 <- 1 + a1 * theta * (1 - theta) + b1 * theta^2 * (1 - theta)^2
    f2 <- a2 * theta + b2 * theta^2 + (1 - a2 - b2) * theta^3

    if (qq < 1) {
      gR <- 1 - 4 / (pi * sqrt(1 - qq^2)) * atan(sqrt((1 - qq) / (1 + qq)))
    } else if (qq == 1) {
      gR <- 1 - 2 / pi
    } else {
      gR <- 1 - 2 / (pi * sqrt(qq^2 - 1)) *
        log((1 + sqrt((qq - 1) / (qq + 1))) / (1 - sqrt((qq - 1) / (qq + 1))))
    }

    if (qq <= 1) {
      gB <- 2 / pi * qq
    } else {
      gB <- 1 + 2 / pi * (qq - sqrt(qq^2 - 1) - asin(1 / qq))
    }

    gg <- gR * f1 / (1 + f2 * (gR / gB - 1))
    Acdtree[z] <- lightfractree / k * axtree * h * 86400 * gg * 1e-6
  }

  dmc * (Acdtree[1] + Acdtree[2]) / 2  # g/m2
}

# ============================================================
# METHOD: TreeArraySetUp
# Original lines in CCabala.cs: 3744-3890
#
# pars must supply: interRow, intraRow, interbay, interblock, rowsblock,
#   colsblock, bnormdistseed, ht, seedvar, effectivestocking, varscalar,
#   percentselfs, performself, varscalarself, crownRatio, L, beta1, beta2,
#   beta3, monthcstarve
# ============================================================
tree_array_setup <- function(st, pars) {
  treehtodstart <- 120

  numberRows <- round(100 / pars$interRow)
  numberCols <- round(100 / pars$intraRow)
  numberofrowsbay    <- max(0, round(pars$interbay   / pars$interRow) - 1)
  numbercolsinterblock <- max(0, round(pars$interblock / pars$intraRow) - 1)

  mats <- c("treeht", "TreeNo", "treesurvive", "treevol", "treediam",
            "MaxSpaceTree", "MaxSpaceTreeLast", "treelisthtodlast", "treelisthtod",
            "treeflag", "treehtlast", "areatreelast", "treediamlast", "treeasw",
            "thintree", "treecrownwidth", "treecrownwidthlast", "treecbalyear",
            "leafarea", "leafarealast", "treelcrown", "treelcrownlast", "treerandfac")
  for (m in mats) st[[m]] <- matrix(0, nrow = numberRows, ncol = numberCols)

  st$treecbal <- array(0, dim = c(numberRows, numberCols, 60))
  st$RowThinned <- integer(numberRows)

  mycount <- 0
  vol <- 0
  CUMHT <- 0
  treecount <- 0
  lastrow <- 0

  for (row in 1:numberRows) {
    lastcol <- 0
    for (col in 1:numberCols) {
      mycount <- mycount + 1
      st$treeht[row, col] <- 0
      st$TreeNo[row, col] <- mycount
      st$treesurvive[row, col] <- 1
      st$treevol[row, col] <- 0
      st$treediam[row, col] <- 0
      st$MaxSpaceTree[row, col] <- sqrt(10000 / 0)  # NOTE: original divides by literal 0.0d -> Inf, preserved
      st$MaxSpaceTreeLast[row, col] <- st$MaxSpaceTree[row, col]
      st$treelisthtodlast[row, col] <- 1.3
      st$treelisthtod[row, col] <- 1.3
      st$treeflag[row, col] <- 0

      if (isTRUE(pars$bnormdistseed)) {
        st$treeht[row, col] <- max(0, pars$ht + pars$ht * pars$seedvar * f_gasdev())
      } else {
        st$treeht[row, col] <- max(0, pars$ht + pars$ht * (0.5 - pars$seedvar * runif(1)))
      }

      st$treehtlast[row, col] <- st$treeht[row, col]
      st$areatreelast[row, col] <- 9 * pars$interRow * pars$intraRow
      st$treesurvive[row, col] <- 1

      st$treevol[row, col] <- f_tree_volume(st$treeht[row, col], treehtodstart,
                                             pars$effectivestocking,
                                             pars$beta1, pars$beta2, pars$beta3) / pars$effectivestocking
      st$treediam[row, col] <- st$treeht[row, col] / treehtodstart
      st$treediamlast[row, col] <- st$treediam[row, col]
      st$treeasw[row, col] <- pi * (st$treediam[row, col] / 2)^2
      st$treeasw[row, col] <- 0.01
      st$thintree[row, col] <- 0
      vol <- vol + st$treevol[row, col]
      st$treecrownwidth[row, col] <- pars$crownRatio * st$treeht[row, col] * 0.5
      st$treecrownwidthlast[row, col] <- st$treecrownwidth[row, col]
      CUMHT <- CUMHT + st$treeht[row, col]
      st$treecbalyear[row, col] <- 0
      st$leafarea[row, col] <- pars$L
      st$leafarealast[row, col] <- st$leafarea[row, col]
      st$treelcrown[row, col] <- st$treeht[row, col]
      st$treelcrownlast[row, col] <- st$treelcrown[row, col]

      if (pars$varscalar == 0) {
        st$treerandfac[row, col] <- 1
      } else if (runif(1) < pars$percentselfs / 100) {
        st$treerandfac[row, col] <- pars$performself *
          (1 + (runif(1) * pars$varscalarself - pars$varscalarself / 2))
      } else {
        st$treerandfac[row, col] <- 1 + (runif(1) * pars$varscalar - pars$varscalar / 2)
      }

      if (row > lastrow + pars$rowsblock) {
        if (row <= lastrow + pars$rowsblock + numberofrowsbay) {
          st$treesurvive[row, col] <- 0
          st$treevol[row, col] <- 0
          st$treediam[row, col] <- 0
          st$treeht[row, col] <- 0
          st$leafarea[row, col] <- 0
          st$treecrownwidth[row, col] <- 0
          st$treecbalyear[row, col] <- 0
          st$treeasw[row, col] <- 0
          st$thintree[row, col] <- 1
          st$treediamlast[row, col] <- 0
          st$treehtlast[row, col] <- 0
        }
      }

      if (col > lastcol + pars$colsblock) {
        if (col <= lastcol + pars$colsblock + numbercolsinterblock) {
          st$treesurvive[row, col] <- 0
          st$treevol[row, col] <- 0
          st$treediam[row, col] <- 0
          st$treeht[row, col] <- 0
          st$leafarea[row, col] <- 0
          st$treecrownwidth[row, col] <- 0
          st$treecbalyear[row, col] <- 0
          st$treeasw[row, col] <- 0
          st$thintree[row, col] <- 1
          st$treediamlast[row, col] <- 0
          st$treehtlast[row, col] <- 0
        }
      }

      # for (cmonth in 1:12) resetMonthlyArray(cmonth)
      # NOTE: resetMonthlyArray() is not part of the extracted source (see
      # file header). Left as an explicit unresolved dependency rather than
      # invented -- uncomment and supply an implementation before running.
      # for (cmonth in 1:12) reset_monthly_array(cmonth)

      for (cmonth in 1:60) {
        if (st$treesurvive[row, col] == 1) {
          st$treecbal[row, col, cmonth] <- 0.0001
        } else {
          st$treecbal[row, col, cmonth] <- 0
        }
      }

      if (st$treesurvive[row, col] == 1) treecount <- treecount + 1
      if (col == lastcol + pars$colsblock + numbercolsinterblock) lastcol <- col
    }

    st$RowThinned[row] <- 0
    if (row == lastrow + pars$rowsblock + numberofrowsbay) lastrow <- row
  }

  st$numberRows <- numberRows
  st$numberCols <- numberCols
  st$vol <- vol
  st$initspha <- round(pars$actualstocking)

  st
}

# ============================================================
# METHOD: deadtreereset
# Original lines in CCabala.cs: 8806-8847
#
# cum* are running per-thinning-event totals passed in and returned
# updated (they are ref parameters in the original).
# ============================================================
deadtreereset <- function(st, nowrow, nowcol, cumwsw, cumshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr) {
  st$silvicthinnedwithinWssw <- st$silvicthinnedwithinWssw + st$wswtree[nowrow, nowcol] / cumwsw * st$Wssw
  st$silvicthinnedwithinWshw <- st$silvicthinnedwithinWshw + max(0, st$wshwtree[nowrow, nowcol]) / cumshw * st$Wshw
  st$silvicthinnedwithinWbk  <- st$silvicthinnedwithinWbk  + st$wbktree[nowrow, nowcol] / cumwbk * st$Wbk
  st$silvicthinnedwithinWf   <- st$silvicthinnedwithinWf   + st$wftree[nowrow, nowcol] / cumwf * st$wf
  st$silvicthinnedwithinWfr  <- st$silvicthinnedwithinWfr  + st$WfrTree[nowrow, nowcol] / cumwfr * st$Wfr
  st$silvicthinnedwithinWb   <- st$silvicthinnedwithinWb   + st$wbswtree[nowrow, nowcol] / cumwb * st$Wb
  st$silvicthinnedwithinWcr  <- st$silvicthinnedwithinWcr  +
    (st$wcrhwtree[nowrow, nowcol] + st$wcrswtree[nowrow, nowcol]) / cumwcr * st$Wcr

  st$wswtree[nowrow, nowcol]   <- 0
  st$wshwtree[nowrow, nowcol]  <- 0
  st$wbktree[nowrow, nowcol]   <- 0
  st$wftree[nowrow, nowcol]    <- 0
  st$WfrTree[nowrow, nowcol]   <- 0
  st$wbswtree[nowrow, nowcol]  <- 0
  st$wcrswtree[nowrow, nowcol] <- 0
  st$wcrhwtree[nowrow, nowcol] <- 0
  st$treesurvive[nowrow, nowcol] <- 0
  st$treediam[nowrow, nowcol]  <- 0
  st$treeht[nowrow, nowcol]    <- 0
  st$treehtlast[nowrow, nowcol] <- 0
  st$areatreelast[nowrow, nowcol] <- 0
  st$htinclast[nowrow, nowcol] <- 0
  st$leafarea[nowrow, nowcol]  <- 0
  st$thintree[nowrow, nowcol]  <- 1
  st$treevol[nowrow, nowcol]   <- 0
  st$treediamlast[nowrow, nowcol] <- 0
  st$leafarealast[nowrow, nowcol] <- 0
  st$treecrownwidth[nowrow, nowcol] <- 0
  st$treecrownwidthlast[nowrow, nowcol] <- 0
  st$aswtree[nowrow, nowcol]   <- 0
  st$Ltree[nowrow, nowcol]     <- 0
  st$treeasw[nowrow, nowcol]   <- 0
  st$treeflag[nowrow, nowcol]  <- 0
  # DEVIATION FROM SOURCE (confirmed, not preserved): original resets
  # treecbalyear at [row, col] -- the caller's class-level loop indices --
  # instead of [nowrow, nowcol], the tree actually being killed. This is a
  # latent bug in CCabala.cs (resets the wrong tree's carbon balance when
  # called from within a thinning loop). Fixed here to use this function's
  # own nowrow/nowcol.
  st$treecbalyear[nowrow, nowcol] <- 0

  st
}

# ============================================================
# METHOD: RowThinningTreatment
# Original lines in CCabala.cs: 8209-8364
# ============================================================
row_thinning_treatment <- function(st, pars, thinproportion, thintype) {
  st$commercialT <- thintype

  loopRows <- round(100 / pars$initInterRow)
  loopCols <- round(100 / pars$initIntraRow)

  cumvolstart <- 0
  cumwssw <- 0; cumwshw <- 0; cumwbk <- 0; cumwf <- 0; cumwb <- 0; cumwfr <- 0; cumwcr <- 0

  for (nowrow in 1:loopRows) {
    for (nowcol in 1:loopCols) {
      cumvolstart <- cumvolstart + st$treevol[nowrow, nowcol]
      cumwssw <- cumwssw + st$wswtree[nowrow, nowcol]
      cumwshw <- cumwshw + max(0, st$wshwtree[nowrow, nowcol])
      cumwbk  <- cumwbk  + st$wbktree[nowrow, nowcol]
      cumwb   <- cumwb   + st$wbswtree[nowrow, nowcol]
      cumwf   <- cumwf   + st$wftree[nowrow, nowcol]
      cumwfr  <- cumwfr  + st$WfrTree[nowrow, nowcol]
      cumwcr  <- cumwcr  + st$wcrhwtree[nowrow, nowcol] + st$wcrswtree[nowrow, nowcol]
    }
  }

  voladjfac <- 1  # NOTE: original computes vol/cumvolstart then immediately overwrites with 1 (lines 246-249), preserved

  st$silvicthinnedrowWssw <- 0; st$silvicthinnedrowWshw <- 0; st$silvicthinnedrowWbk <- 0
  st$silvicthinnedrowWf   <- 0; st$silvicthinnedrowWfr  <- 0; st$silvicthinnedrowWb  <- 0
  st$silvicthinnedrowWcr  <- 0

  lostvol <- 0
  outrow <- round(1 / thinproportion)

  for (nowrow in 1:loopRows) {
    if (nowrow %% outrow == 0) {
      if (st$RowThinned[nowrow] == 1) nowrow <- nowrow + 1
      if (nowrow > loopRows) break
      st$RowThinned[nowrow] <- 1

      for (nowcol in 1:loopCols) {
        st$silvicthinnedwithinWssw <- st$silvicthinnedwithinWssw + st$wswtree[nowrow, nowcol] / cumwssw * st$Wssw
        st$silvicthinnedwithinWshw <- st$silvicthinnedwithinWshw + max(0, st$wshwtree[nowrow, nowcol]) / cumwshw * st$Wshw
        st$silvicthinnedwithinWbk  <- st$silvicthinnedwithinWbk  + st$wbktree[nowrow, nowcol] / cumwbk * st$Wbk
        st$silvicthinnedwithinWf   <- st$silvicthinnedwithinWf   + st$wftree[nowrow, nowcol] / cumwf * st$wf
        st$silvicthinnedwithinWfr  <- st$silvicthinnedwithinWfr  + st$WfrTree[nowrow, nowcol] / cumwfr * st$Wfr
        st$silvicthinnedwithinWb   <- st$silvicthinnedwithinWb   + st$wbswtree[nowrow, nowcol] / cumwb * st$Wb
        st$silvicthinnedwithinWcr  <- st$silvicthinnedwithinWcr  +
          (st$wcrhwtree[nowrow, nowcol] + st$wcrswtree[nowrow, nowcol]) / cumwcr * st$Wcr

        lostvol <- lostvol + st$treevol[nowrow, nowcol] * voladjfac

        st$wswtree[nowrow, nowcol] <- 0; st$wshwtree[nowrow, nowcol] <- 0
        st$wbktree[nowrow, nowcol] <- 0; st$wftree[nowrow, nowcol] <- 0
        st$WfrTree[nowrow, nowcol] <- 0; st$wbswtree[nowrow, nowcol] <- 0
        st$wcrswtree[nowrow, nowcol] <- 0; st$wcrhwtree[nowrow, nowcol] <- 0
        st$treesurvive[nowrow, nowcol] <- 0
        st$treediam[nowrow, nowcol] <- 0; st$treeht[nowrow, nowcol] <- 0
        st$treehtlast[nowrow, nowcol] <- 0; st$areatreelast[nowrow, nowcol] <- 0
        st$htinclast[nowrow, nowcol] <- 0; st$leafarea[nowrow, nowcol] <- 0
        st$thintree[nowrow, nowcol] <- 1; st$treevol[nowrow, nowcol] <- 0
        st$treediamlast[nowrow, nowcol] <- 0; st$leafarealast[nowrow, nowcol] <- 0
        st$treecrownwidth[nowrow, nowcol] <- 0; st$treecrownwidthlast[nowrow, nowcol] <- 0
        st$aswtree[nowrow, nowcol] <- 0; st$Ltree[nowrow, nowcol] <- 0
        st$treeasw[nowrow, nowcol] <- 0; st$treeflag[nowrow, nowcol] <- 0
        st$treecbalyear[nowrow, nowcol] <- 0
      }
    }
  }

  st$vol <- st$vol - lostvol
  st$cumthinvol <- st$cumthinvol + lostvol

  ageMax <- f_age_max(pars$gammaF)
  for (iii in 1:ageMax) {
    st$wfage[iii] <- st$wfage[iii] * st$silvicthinnedrowWf / st$wf
  }

  spha <- 0
  for (nowrow in 1:loopRows) for (nowcol in 1:loopCols) spha <- spha + st$treesurvive[nowrow, nowcol]
  st$spha <- spha

  pars$interRow <- 10000 / (pars$intraRow * pars$actualstocking * (1 - thinproportion))
  st$effectivestocking <- 10000 / (pars$intraRow * pars$interRow)
  st$effectivestockingStored <- st$effectivestocking

  st$pars_out <- pars  # updated interRow must propagate back to caller's pars
  st
}

# ============================================================
# METHOD: withinThinningTreatment
# Original lines in CCabala.cs: 8390-8803
#
# withThinType, groupSelect, specified_Diameter come from pars (site/regime
# configuration external to this extract -- see individual_tree_module_reference.txt).
# ============================================================
within_thinning_treatment <- function(st, pars, thinproportion, thinstems, thintype,
                                       minthinvol, is_coppice_reduction = FALSE) {
  loopRows <- round(100 / pars$initInterRow)
  loopCols <- round(100 / pars$initIntraRow)

  totthinvol <- 0
  st$silvicthinnedrowWssw <- 0; st$silvicthinnedrowWshw <- 0; st$silvicthinnedrowWbk <- 0
  st$silvicthinnedrowWf   <- 0; st$silvicthinnedrowWfr  <- 0; st$silvicthinnedrowWb  <- 0
  st$silvicthinnedrowWcr  <- 0
  cumvolstart <- 0
  cumwssw <- 0; cumwshw <- 0; cumwbk <- 0; cumwf <- 0; cumwb <- 0; cumwfr <- 0; cumwcr <- 0

  for (nowrow in 1:loopRows) {
    for (nowcol in 1:loopCols) {
      cumvolstart <- cumvolstart + st$treevol[nowrow, nowcol]
      cumwssw <- cumwssw + st$wswtree[nowrow, nowcol]
      cumwshw <- cumwshw + max(0, st$wshwtree[nowrow, nowcol])
      cumwbk  <- cumwbk  + st$wbktree[nowrow, nowcol]
      cumwb   <- cumwb   + st$wbswtree[nowrow, nowcol]
      cumwf   <- cumwf   + st$wftree[nowrow, nowcol]
      cumwfr  <- cumwfr  + st$WfrTree[nowrow, nowcol]
      cumwcr  <- cumwcr  + st$wcrhwtree[nowrow, nowcol] + st$wcrswtree[nowrow, nowcol]
    }
  }

  voladjfac <- 1  # NOTE: as in row_thinning_treatment, original overwrites the computed ratio with 1

  thinnedtrees <- max(0, round(st$effectivestocking * thinstems))
  removedtrees <- 0

  if (pars$withThinType == ThinningTypes$Random) {

    gap <- RoundupToValue(pars$groupSelect, 1)
    if (!is_coppice_reduction) gap <- max(RoundupToValue(1 / thinstems, 1), gap)

    while (thinnedtrees > removedtrees) {
      nowrow <- round(100 / pars$initInterRow * runif(1))
      nowcol <- round(100 / pars$initIntraRow * runif(1))
      nowrow <- min(max(nowrow, 1), loopRows)
      nowcol <- min(max(nowcol, 1), loopCols)

      if (st$treesurvive[nowrow, nowcol] == 1) {
        if (thinnedtrees > removedtrees) {
          totthinvol <- totthinvol + st$treevol[nowrow, nowcol] * voladjfac
          if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1) {
            thinnedtrees <- thinnedtrees + 1
            thinstems <- thinnedtrees / st$effectivestocking
          }
          removedtrees <- removedtrees + 1
          st <- deadtreereset(st, nowrow, nowcol, cumwssw, cumwshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr)
        }
      }
    }

  } else if (pars$withThinType == ThinningTypes$GroupSelectFromBelow) {

    gap <- RoundupToValue(pars$groupSelect, 1)
    if (!is_coppice_reduction) gap <- max(RoundupToValue(1 / thinstems, 1), gap)
    ggap <- round(gap)
    smallest <- 9999

    while (thinnedtrees > removedtrees) {
      for (nowrow in 1:loopRows) {
        if (st$RowThinned[nowrow] == 0) {
          count <- 0
          removerow <- NA; removecol <- NA
          for (nowcol in 1:loopCols) {
            if (st$treesurvive[nowrow, nowcol] == 1) {
              if (st$treediam[nowrow, nowcol] < smallest) {
                smallest <- st$treediam[nowrow, nowcol]
                removerow <- nowrow; removecol <- nowcol
              }
              count <- count + 1
            }
            if (count == ggap & thinnedtrees > removedtrees & !is.na(removerow)) {
              totthinvol <- totthinvol + st$treevol[removerow, removecol] * voladjfac
              if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1) {
                thinnedtrees <- thinnedtrees + 1
                thinstems <- thinnedtrees / st$effectivestocking
              }
              removedtrees <- removedtrees + 1
              st <- deadtreereset(st, removerow, removecol, cumwssw, cumwshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr)
              smallest <- 9999
              count <- 0
            }
          }
        }
      }
    }

    if (is_coppice_reduction) {
      for (nowrow in 1:loopRows) {
        for (nowcol in 1:loopCols) {
          if (st$treesurvive[nowrow, nowcol] == 1) {
            totthinvol <- totthinvol + st$treevol[nowrow, nowcol] * thinproportion
            st$treevol[nowrow, nowcol] <- (1 - thinproportion) * st$treevol[nowrow, nowcol] * voladjfac
            st$treediam[nowrow, nowcol] <- sqrt((st$treevol[nowrow, nowcol] * st$spha /
              (pars$beta1 * (st$treeht[nowrow, nowcol] / 0.9)^pars$beta2))^(1 / pars$beta3) / st$spha / pi) * 100
            st$leafarea[nowrow, nowcol] <- st$leafarea[nowrow, nowcol] * (1 - thinproportion)
            st$leafarealast[nowrow, nowcol] <- st$leafarea[nowrow, nowcol]
          }
        }
      }
      st$silvicthinnedwithinWf <- st$silvicthinnedwithinWf + (st$wf - st$silvicthinnedrowWf) * thinproportion
      st$silvicthinnedwithinWb <- st$silvicthinnedwithinWb + (st$Wb - st$silvicthinnedrowWb) * thinproportion
    }

  } else if (pars$withThinType == ThinningTypes$GroupSelectFromAbove) {

    gap <- RoundupToValue(pars$groupSelect, 1)
    if (!is_coppice_reduction) gap <- max(RoundupToValue(1 / thinstems, 1), gap)
    ggap <- round(gap)
    largest <- -999

    while (thinnedtrees > removedtrees) {
      for (nowrow in 1:loopRows) {
        if (st$RowThinned[nowrow] == 0) {
          count <- 0
          removerow <- NA; removecol <- NA
          for (nowcol in 1:loopCols) {
            if (st$treesurvive[nowrow, nowcol] == 1) {
              if (st$treediam[nowrow, nowcol] > largest) {
                largest <- st$treediam[nowrow, nowcol]
                removerow <- nowrow; removecol <- nowcol
              }
              count <- count + 1
            }
            if (count == ggap & thinnedtrees > removedtrees & !is.na(removerow)) {
              totthinvol <- totthinvol + st$treevol[removerow, removecol] * voladjfac
              if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1) {
                thinnedtrees <- thinnedtrees + 1
                thinstems <- thinnedtrees / st$effectivestocking
              }
              removedtrees <- removedtrees + 1
              st <- deadtreereset(st, removerow, removecol, cumwssw, cumwshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr)
              largest <- -999
              count <- 0
            }
          }
        }
      }
    }

  } else if (pars$withThinType == ThinningTypes$ThinBelowDiameter) {

    for (nowrow in 1:loopRows) {
      if (st$RowThinned[nowrow] == 0) {
        for (nowcol in 1:loopCols) {
          if (st$treediam[nowrow, nowcol] < pars$specified_Diameter) {
            if (st$treesurvive[nowrow, nowcol] == 1) {
              totthinvol <- totthinvol + st$treevol[nowrow, nowcol] * voladjfac
              removedtrees <- removedtrees + 1
              st <- deadtreereset(st, nowrow, nowcol, cumwssw, cumwshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr)
            }
          }
        }
      }
    }
    thinstems <- max(0, removedtrees / st$spha)

  } else if (pars$withThinType == ThinningTypes$ThinAboveDiameter) {

    for (nowrow in 1:loopRows) {
      if (st$RowThinned[nowrow] == 0) {
        for (nowcol in 1:loopCols) {
          if (st$treediam[nowrow, nowcol] > pars$specified_Diameter) {
            if (st$treesurvive[nowrow, nowcol] == 1) {
              totthinvol <- totthinvol + st$treevol[nowrow, nowcol] * voladjfac
              removedtrees <- removedtrees + 1
              st <- deadtreereset(st, nowrow, nowcol, cumwssw, cumwshw, cumwbk, cumwb, cumwf, cumwcr, cumwfr)
            }
          }
        }
      }
    }
    thinstems <- max(0, removedtrees / st$spha)
  }

  ba <- 0; cumvol <- 0
  for (nowrow in 1:loopRows) {
    for (nowcol in 1:loopCols) {
      ba <- ba + (st$treediam[nowrow, nowcol] / 200)^2 * pi
      cumvol <- cumvol + st$treevol[nowrow, nowcol]
    }
  }
  st$ba <- ba
  st$vol <- cumvol

  st$cumThinProp <- st$cumThinProp * (1 - thinstems)
  st$cumthinvol  <- st$cumthinvol + totthinvol
  pars$intraRow  <- 10000 / (pars$interRow * pars$actualstocking * (1 - thinstems))
  st$effectivestocking <- 10000 / (pars$intraRow * pars$interRow)
  st$effectivestockingStored <- st$effectivestocking
  pars$actualstocking <- f_calc_actual_stocking(pars$rowsblock, pars$interRow, pars$interbay,
                                                 pars$colsblock, pars$intraRow, pars$interblock)

  spha <- 0
  for (nowrow in 1:loopRows) for (nowcol in 1:loopCols) spha <- spha + st$treesurvive[nowrow, nowcol]
  st$spha <- spha

  ageMax <- f_age_max(pars$gammaF)
  for (i in 1:ageMax) {
    st$wfage[i] <- st$wfage[i] * (st$wf - st$silvicthinnedwithinWf) / st$wf
  }

  st$commercialT <- thintype
  st$pars_out <- pars
  st
}

# ============================================================
# METHOD: UpdateTreeListnewersimple21062012
# Original lines in CCabala.cs: 11850-12739
#
# This is the monthly per-tree update: neighbour search (8 directions,
# toroidal wraparound), crown geometry, hourly light/shading loop,
# sapwood/biomass allocation, West (1998) H/D allometry, height/diameter
# recovery, mortality checks, and stocking update.
#
# pars must supply (see individual_tree_module_reference.txt, Section A/B):
#   initInterRow, initIntraRow, dayOfYear, lat, rowdirection, k, greenht,
#   crownRatio, rootdepth, branchTaper, branchangle, density, Wfalloc1..3
#   (wfAlloc1ForCalc), beta1..3, wSx1000, monthcstarve, rC, ax (len 2),
#   alpha (len 2), theta, Gamma, h, dmc
# st must supply the stand-level pools: L, vol, spha, wf, Wb, Wssw, Wshw,
#   Wbk, Wcr, Wcrsw, Wcrhw, Wfr, greenht, interRow, intraRow, h
# ============================================================
update_tree_list <- function(st, pars, stemincmonth, ggrossmonth, Rcrmonthin, Rbmonthin,
                              Rfmonthin, Rfrmonthin, Rsmonthin, mmonth, wdate, wcf,
                              aetmonth, qmonth) {

  numberows <- round(100 / pars$initInterRow)
  numbercols <- round(100 / pars$initIntraRow)
  gridsize <- pars$initInterRow * pars$initIntraRow
  st$initspha <- numberows * numbercols

  wnewdate <- seq(wcf, by = "-1 month", length.out = 2)[2]

  # working matrices (re-created each call, matching original's local arrays)
  avhtaround <- matrix(0, numberows, numbercols)
  Freespace  <- matrix(0, numberows, numbercols)
  crownfac   <- matrix(0, numberows, numbercols)
  rootcompwater <- matrix(0, numberows, numbercols)
  treecrownvol  <- matrix(0, numberows, numbercols)
  areatree      <- matrix(0, numberows, numbercols)
  alloctree     <- matrix(0, numberows, numbercols)
  production    <- matrix(0, numberows, numbercols)

  baseangle <- c(180, 0, 90, 270, 135, 45, 225, 315)

  spha <- 0
  for (row in 1:numberows) for (col in 1:numbercols) spha <- spha + st$treesurvive[row, col]

  totalcrownvolume <- 0
  totaltreesurfacearea <- 0

  # --- neighbour search + crown geometry + hourly shading loop ---
  for (row in 1:numberows) {
    for (col in 1:numbercols) {

      wrap <- function(x, n) ((x - 1) %% n) + 1

      # left
      myrow <- row; Distance <- 0; treecheck <- 0
      repeat {
        myrow <- myrow - 1; Distance <- Distance + pars$initInterRow
        if (myrow == 0) myrow <- numberows
        leftneighbour <- st$TreeNo[myrow, col]; dleftneighbour <- Distance
        hleftneighbour <- st$treehtlast[myrow, col]; lleftneighbour <- st$leafarea[myrow, col]
        diamleftneighbour <- st$treediam[myrow, col]; cleftneighbour <- st$treecrownwidth[myrow, col]
        if (st$treesurvive[myrow, col] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }
      # right
      myrow <- row; Distance <- 0; treecheck <- 0
      repeat {
        myrow <- myrow + 1; Distance <- Distance + pars$initInterRow
        if (myrow > numberows) myrow <- 1
        rightneighbour <- st$TreeNo[myrow, col]; drightneighbour <- Distance
        hrightneighbour <- st$treehtlast[myrow, col]; lrightneighbour <- st$leafarea[myrow, col]
        diamrightneighbour <- st$treediam[myrow, col]; crightneighbour <- st$treecrownwidth[myrow, col]
        if (st$treesurvive[myrow, col] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }
      # top
      mycol <- col; Distance <- 0; treecheck <- 0
      repeat {
        mycol <- mycol + 1; Distance <- Distance + pars$initIntraRow
        if (mycol > numbercols) mycol <- 1
        topneighbour <- st$TreeNo[row, mycol]; dtopneighbour <- Distance
        htopneighbour <- st$treehtlast[row, mycol]; ltopneighbour <- st$leafarea[row, mycol]
        diamtopneighbour <- st$treediam[row, mycol]; ctopneighbour <- st$treecrownwidth[row, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }
      # bottom
      mycol <- col; Distance <- 0; treecheck <- 0
      repeat {
        mycol <- mycol - 1; Distance <- Distance + pars$initIntraRow
        if (mycol == 0) mycol <- numbercols
        bottomneighbour <- st$TreeNo[row, mycol]; dbottomneighbour <- Distance
        hbottomneighbour <- st$treehtlast[row, mycol]; lbottomneighbour <- st$leafarea[row, mycol]
        diambottomneighbour <- st$treediam[row, mycol]; cbottomneighbour <- st$treecrownwidth[row, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }
      # top-right
      mycol <- col; myrow <- row; Distance <- 0; treecheck <- 0
      diag <- sqrt(pars$initIntraRow^2 + pars$initInterRow^2)
      repeat {
        mycol <- mycol + 1; myrow <- myrow + 1; Distance <- Distance + diag
        if (mycol > numbercols) mycol <- numbercols  # NOTE: original sets index to 0 (invalid in C# too, guarded by mycol>numbercols check below); clamped here to stay in range
        if (myrow > numberows) myrow <- numberows
        toprightneighbour <- st$TreeNo[myrow, mycol]; dtoprightneighbour <- Distance
        htoprightneighbour <- st$treehtlast[myrow, mycol]; ltoprightneighbour <- st$leafarea[myrow, mycol]
        diamtoprightneighbour <- st$treediam[row, mycol]; ctoprightneighbour <- st$treecrownwidth[myrow, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= sqrt(2 * 100^2)) treecheck <- 1
        if (treecheck == 1) break
      }
      # bottom-right
      mycol <- col; myrow <- row; Distance <- 0; treecheck <- 0
      repeat {
        mycol <- mycol + 1; myrow <- myrow - 1; Distance <- Distance + diag
        if (mycol > numbercols) mycol <- numbercols
        if (myrow == 0) myrow <- numberows
        botrightneighbour <- st$TreeNo[myrow, mycol]; dbotrightneighbour <- Distance
        hbotrightneighbour <- st$treehtlast[myrow, mycol]; lbotrightneighbour <- st$leafarea[myrow, mycol]
        diambotrightneighbour <- st$treediam[row, mycol]; cbotrightneighbour <- st$treecrownwidth[myrow, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= sqrt(2 * 100^2)) treecheck <- 1
        if (treecheck == 1) break
      }
      # bottom-left
      mycol <- col; myrow <- row; Distance <- 0; treecheck <- 0
      repeat {
        mycol <- mycol - 1; myrow <- myrow - 1; Distance <- Distance + diag
        if (mycol == 0) mycol <- numbercols
        if (myrow == 0) myrow <- numberows
        botleftneighbour <- st$TreeNo[myrow, mycol]; dbotleftneighbour <- Distance
        hbotleftneighbour <- st$treehtlast[myrow, mycol]; lbotleftneighbour <- st$leafarea[myrow, mycol]
        diambotleftneighbour <- st$treediam[row, mycol]; cbotleftneighbour <- st$treecrownwidth[myrow, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }
      # top-left
      mycol <- col; myrow <- row; Distance <- 0; treecheck <- 0
      repeat {
        mycol <- mycol - 1; myrow <- myrow + 1; Distance <- Distance + diag
        if (mycol == 0) mycol <- numbercols
        if (myrow > numberows) myrow <- numberows
        topleftneighbour <- st$TreeNo[myrow, mycol]; dtopleftneighbour <- Distance
        htopleftneighbour <- st$treehtlast[myrow, mycol]; ltopleftneighbour <- st$leafarea[myrow, mycol]
        diamtopleftneighbour <- st$treediam[row, mycol]; ctopleftneighbour <- st$treecrownwidth[myrow, mycol]
        if (st$treesurvive[row, mycol] == 1) treecheck <- 1
        if (Distance >= 100) treecheck <- 1
        if (treecheck == 1) break
      }

      dneighbours <- c(dleftneighbour, drightneighbour, dtopneighbour, dbottomneighbour,
                        dtopleftneighbour, dtoprightneighbour, dbotleftneighbour, dbotrightneighbour)
      cneighbours <- c(cleftneighbour, crightneighbour, ctopneighbour, cbottomneighbour,
                        ctopleftneighbour, ctoprightneighbour, cbotleftneighbour, cbotrightneighbour)
      lneighbours <- c(lleftneighbour, lrightneighbour, ltopneighbour, lbottomneighbour,
                        ltopleftneighbour, ltoprightneighbour, lbotleftneighbour, lbotrightneighbour)
      hneighbours <- c(hleftneighbour, hrightneighbour, htopneighbour, hbottomneighbour,
                        htopleftneighbour, htoprightneighbour, hbotleftneighbour, hbotrightneighbour)

      area1 <- f_areascalene(dneighbours[1], dneighbours[5], cneighbours[1], cneighbours[5])
      area2 <- f_areascalene(dneighbours[5], dneighbours[3], cneighbours[5], cneighbours[3])
      area3 <- f_areascalene(dneighbours[3], dneighbours[6], cneighbours[3], cneighbours[6])
      area4 <- f_areascalene(dneighbours[6], dneighbours[2], cneighbours[6], cneighbours[2])
      area5 <- f_areascalene(dneighbours[2], dneighbours[8], cneighbours[2], cneighbours[8])
      area6 <- f_areascalene(dneighbours[8], dneighbours[4], cneighbours[8], cneighbours[4])
      area7 <- f_areascalene(dneighbours[4], dneighbours[7], cneighbours[4], cneighbours[7])
      area8 <- f_areascalene(dneighbours[7], dneighbours[1], cneighbours[7], cneighbours[1])

      st$MaxSpaceTree[row, col] <- area1 + area2 + area3 + area4 + area5 + area6 + area7 + area8

      avhtaround[row, col] <- mean(hneighbours)

      st$treelcrown[row, col] <- max(0, min(st$treeht[row, col], st$treeht[row, col] - st$greenht))

      if (st$treeht[row, col] > 0) {
        st$treecrownwidth[row, col] <- max(
          st$treecrownwidthlast[row, col],
          min(st$treecrownwidthlast[row, col] + 0.5 * st$htinclast[row, col],
              0.5 * pars$crownRatio * st$treeht[row, col],
              sqrt(st$MaxSpaceTree[row, col]) / pi)
        )
      } else {
        st$treecrownwidth[row, col] <- st$treecrownwidthlast[row, col]
      }

      areatree[row, col] <- pi * st$treecrownwidth[row, col]^2
      totaltreesurfacearea <- totaltreesurfacearea + areatree[row, col]
      treecrownvol[row, col] <- 4 / 3 * pi * st$treelcrown[row, col] / 2 * st$treecrownwidth[row, col]^2
      totalcrownvolume <- totalcrownvolume + treecrownvol[row, col]

      # hourly light / shading loop
      cumrad <- 0; cumtreerad <- 0
      for (hhour in 1:24) {
        az <- f_get_azimuth_and_alt(hhour, pars$dayOfYear, pars$lat)
        azimuth  <- round(az$azimuth)
        solaralt <- round(az$solaralt)
        azimuth  <- round(90 - azimuth + pars$rowdirection)

        radnow <- if (solaralt > 0) f_lightcalc(st$daylen, hhour, qmonth) else 0
        cumrad <- cumrad + radnow

        hourtreerad <- numeric(8)
        minhourrad <- 99999
        for (ii in 1:8) {
          angleoffset <- 180 / pi * atan(cneighbours[ii] / dneighbours[ii])
          anglelower <- baseangle[ii] - angleoffset
          angleupper <- baseangle[ii] + angleoffset

          if (anglelower <= azimuth && azimuth <= angleupper &&
              st$treeht[row, col] < hneighbours[ii] &&
              dneighbours[ii] < (hneighbours[ii] - st$treeht[row, col]) / tan(solaralt * pi / 180)) {
            nLad <- lneighbours[ii] / (4 / 3 * pi * cneighbours[ii]^2 * max(0, hneighbours[ii] - st$greenht) / 2)
            pathLength <- cneighbours[ii] / cos(f_radians(solaralt))
            hourtreerad[ii] <- radnow * exp(-pars$k * nLad * pathLength)
          } else {
            hourtreerad[ii] <- radnow
          }
          if (hourtreerad[ii] < minhourrad) minhourrad <- hourtreerad[ii]
        }
        cumtreerad <- cumtreerad + minhourrad
      }

      crownfac[row, col] <- cumtreerad / cumrad
    }
  }

  # --- leaf area distributed proportional to crown volume ---
  sumleafarea <- 0; sumcrownl <- 0
  cumwswtree <- 0; cumaswtree <- 0; cumwbswtree <- 0; cumwcrswtree <- 0

  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      if (areatree[row, col] > 0) {
        st$leafarea[row, col] <- st$treesurvive[row, col] * st$L * 10000 * treecrownvol[row, col] / totalcrownvolume
      } else {
        st$leafarea[row, col] <- 0
      }

      if (areatree[row, col] > 0) {
        st$Ltree[row, col] <- st$leafarea[row, col] / areatree[row, col]
        st$aswtree[row, col] <- max(0, st$Ltree[row, col]^pars$Wfalloc2 * pars$wfAlloc1ForCalc *
                                       st$treeht[row, col]^pars$Wfalloc3) / spha
        st$wswtree[row, col] <- max(0, st$aswtree[row, col] *
          (max(0.01, st$treeht[row, col] - st$treelcrown[row, col]) + 0.33 * st$treelcrown[row, col]) *
          pars$density) / 1000
        st$wcrswtree[row, col] <- st$aswtree[row, col] * (st$treecrownwidth[row, col] + pars$rootdepth) / 2 *
          pars$branchTaper * pars$density / 1000
        argbtheta <- pi / 2 - pars$branchangle * pi / 180
        TREEBL <- f_mean_all_branch_length(st$treecrownwidth[row, col], st$treelcrown[row, col],
                                            st$treecrownwidth[row, col], argbtheta)
        if (st$wcrswtree[row, col] == 0) TREEBL <- 0
        st$wbswtree[row, col] <- st$aswtree[row, col] * TREEBL * pars$branchTaper * pars$density / 1000
      } else {
        st$wswtree[row, col] <- 0; st$wcrswtree[row, col] <- 0; st$wbswtree[row, col] <- 0
      }

      sumleafarea <- sumleafarea + st$leafarea[row, col]
      cumwswtree  <- cumwswtree  + st$wswtree[row, col]
      cumaswtree  <- cumaswtree  + st$aswtree[row, col]
      cumwbswtree <- cumwbswtree + st$wbswtree[row, col]
      cumwcrswtree <- cumwcrswtree + st$wcrswtree[row, col]
    }
  }

  # --- light-limited production per tree ---
  sumproduction <- 0
  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      Pnscalar <- crownfac[row, col]
      if (avhtaround[row, col] > 0) {
        axtree1 <- Pnscalar * pars$ax[1]
        axtree2 <- Pnscalar * pars$ax[2]
      } else {
        axtree1 <- pars$ax[1]
        axtree2 <- pars$ax[2]
      }

      if (st$treecrownwidth[row, col] > 0 && st$treesurvive[row, col] > 0 && crownfac[row, col] > 0) {
        qtree <- qmonth * crownfac[row, col]
        treeprod <- f_calc_light_limited_production_tree(1, axtree1, axtree2, pars$alpha[1], pars$alpha[2],
                                                           st$leafarea[row, col], areatree[row, col], qtree,
                                                           pars$k, pars$theta, pars$Gamma, st$h, pars$dmc)
        production[row, col] <- st$treerandfac[row, col] * areatree[row, col] * treeprod
      } else {
        production[row, col] <- 0
      }
      sumproduction <- sumproduction + production[row, col]
    }
  }

  # --- allocation ratios and per-tree carbon balance ---
  totalalloc <- 0
  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      aratio <- if (sumproduction > 0) production[row, col] / sumproduction else 0
      fratio <- if (sumleafarea > 0) st$leafarea[row, col] / sumleafarea else 0
      sratio <- if (cumwswtree > 0) st$wswtree[row, col] / cumwswtree else 0

      st$wftree[row, col]   <- fratio * st$wf
      st$WfrTree[row, col]  <- fratio * st$Wfr
      st$wshwtree[row, col] <- max(0, st$Ws * sratio - st$wswtree[row, col])
      st$wbktree[row, col]  <- st$Wbk * sratio
      st$wcrhwtree[row, col] <- max(0, st$Wcrhw * sratio)

      crswratio <- if (cumwcrswtree > 0) st$wcrswtree[row, col] / cumwcrswtree else 0
      bswratio  <- if (cumwbswtree > 0) st$wbswtree[row, col] / cumwbswtree else 0

      st$wbswtree[row, col]  <- bswratio * st$Wb
      st$wcrswtree[row, col] <- crswratio * st$Wcrsw
      st$transpiration[row, col] <- aetmonth * aratio * 10000

      alloctree[row, col] <- (aratio * ggrossmonth -
        (fratio * (Rfmonthin + st$lostWf) - fratio * (st$lostWfr + Rfrmonthin)) -
        sratio * (Rsmonthin + st$lostWssw) -
        bswratio * (Rbmonthin + st$lostWb) -
        crswratio * (Rcrmonthin + st$lostWcr)) / (1 + pars$rC)

      for (cmonth in 1:(pars$monthcstarve - 1)) {
        st$treecbal[row, col, cmonth] <- st$treecbal[row, col, cmonth + 1]
      }
      st$treecbal[row, col, pars$monthcstarve] <- alloctree[row, col]

      st$treecbalyear[row, col] <- sum(st$treecbal[row, col, 1:pars$monthcstarve])
      totalalloc <- totalalloc + alloctree[row, col]
    }
  }

  # --- apply volume increment ---
  cumvol <- 0
  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      if (totalalloc > 0) {
        st$treevol[row, col] <- max(0, st$treevol[row, col] + alloctree[row, col] / totalalloc * stemincmonth)
      }
      cumvol <- cumvol + st$treevol[row, col]
    }
  }

  # --- mortality, West (1998) H/D allometry, height/diameter recovery ---
  LostTrees <- 0
  newcumvol <- 0; newcumdiam <- 0; newcumht <- 0

  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      st$treecbalyear[row, col] <- max(0, st$treecbalyear[row, col])
      if (st$treecbalyear[row, col] <= 0) {
        st$treesurvive[row, col] <- 0; st$treeflag[row, col] <- 1
      }
      if (st$treevol[row, col] <= 0 & st$treeflag[row, col] == 0) {
        st$treesurvive[row, col] <- 0; st$treeflag[row, col] <- 1
      }
      if (st$treesurvive[row, col] == 0) LostTrees <- LostTrees + st$treevol[row, col]
      if (0.9 * st$treeht[row, col] < st$greenht) {
        st$treesurvive[row, col] <- 0; st$treeflag[row, col] <- 1
      }

      fratio <- st$leafarea[row, col] / sumleafarea
      sratio <- st$treevol[row, col] / cumvol

      if (st$treesurvive[row, col] > 0) {
        if (st$treevol[row, col] > 0) {
          treeWestR <- (st$wf * fratio * 1000 + st$Wb * fratio * 1000) /
            (st$treevol[row, col] * pars$density * 1000 + st$Wbk * sratio * 1000)
          treeWestW <- st$wf * fratio * 1000 + st$Wb * fratio * 1000 +
            st$treevol[row, col] * pars$density * 1000 + st$Wbk * sratio * 1000
          st$treelisthtod[row, col] <- 1.3 * treeWestR^(-0.22) * treeWestW^(-0.136)

          ratechangehtod <- 0.02
          if (st$treelisthtod[row, col] < st$treelisthtodlast[row, col] * (1 - ratechangehtod)) {
            st$treelisthtod[row, col] <- st$treelisthtodlast[row, col] * (1 - ratechangehtod)
          }
          if (st$treelisthtod[row, col] > st$treelisthtodlast[row, col] * (1 + ratechangehtod)) {
            st$treelisthtod[row, col] <- st$treelisthtodlast[row, col] * (1 + ratechangehtod)
          }
        } else {
          st$treelisthtod[row, col] <- 1
        }
      }
      st$treelisthtodlast[row, col] <- st$treelisthtod[row, col]

      if (totalalloc != 0 && alloctree[row, col] / totalalloc * stemincmonth > 0) {
        treehtnow <- st$treesurvive[row, col] * (0.9 * st$treevol[row, col] * spha *
          (st$treelisthtod[row, col] * 200)^(2 * pars$beta3) /
          (pars$beta1 * (pi * spha)^pars$beta3))^(1 / (pars$beta2 + 2 * pars$beta3))
        st$treeht[row, col] <- treehtnow
        st$treediam[row, col] <- st$treesurvive[row, col] * st$treeht[row, col] / st$treelisthtod[row, col] / 0.9
      } else {
        st$treeht[row, col] <- st$treehtlast[row, col] * st$treesurvive[row, col]
        st$treediam[row, col] <- st$treediamlast[row, col] * st$treesurvive[row, col]
      }

      st$treevol[row, col] <- st$treesurvive[row, col] * st$treevol[row, col] * st$vol / cumvol
      st$treeht[row, col] <- st$treesurvive[row, col] * (0.9 * st$treevol[row, col] * spha *
        (st$treelisthtod[row, col] * 200)^(2 * pars$beta3) /
        (pars$beta1 * (pi * spha)^pars$beta3))^(1 / (pars$beta2 + 2 * pars$beta3))
      st$treediam[row, col] <- st$treesurvive[row, col] * st$treeht[row, col] / st$treelisthtod[row, col] / 0.9

      st$treediamlast[row, col] <- st$treediam[row, col]
      st$treehtlast[row, col] <- st$treeht[row, col]

      newcumvol <- newcumvol + st$treevol[row, col]
      newcumht  <- newcumht  + st$treeht[row, col]
      newcumdiam <- newcumdiam + st$treediam[row, col]
    }
  }

  cumdiam <- newcumdiam
  cumba <- 0
  cumvol <- newcumvol

  if (cumvol > 0) {
    st$LostStemMass  <- st$LostStemMass + (st$Wssw + st$Wshw) * LostTrees / cumvol
    st$selfThinnedWssw <- st$selfThinnedWssw + st$Wssw * LostTrees / cumvol
    st$selfThinnedWshw <- st$selfThinnedWshw + st$Wshw * LostTrees / cumvol
    st$selfThinnedWbk  <- st$selfThinnedWbk  + st$Wbk  * LostTrees / cumvol
    st$selfThinnedWcr  <- st$selfThinnedWcr  + st$Wcr  * LostTrees / cumvol
    st$selfThinnedWcr  <- st$selfThinnedWcr  + st$Wcrhw * LostTrees / cumvol
  }

  spha <- 0
  for (row in 1:numberows) {
    for (col in 1:numbercols) {
      if (st$treesurvive[row, col] == 0) {
        st$treevol[row, col] <- 0; st$treeht[row, col] <- 0; st$treediam[row, col] <- 0
        st$treehtlast[row, col] <- 0; st$treediamlast[row, col] <- 0
        st$leafarea[row, col] <- 0; st$treecrownwidth[row, col] <- 0; st$treelcrown[row, col] <- 0
        areatree[row, col] <- 0; st$treesurvivelast[row, col] <- 0
        st$wswtree[row, col] <- 0; st$wshwtree[row, col] <- 0; st$wbktree[row, col] <- 0
        st$wftree[row, col] <- 0; st$WfrTree[row, col] <- 0; st$wbswtree[row, col] <- 0
        st$wcrswtree[row, col] <- 0; st$wcrhwtree[row, col] <- 0; st$treecbalyear[row, col] <- 0
      }
      spha <- spha + st$treesurvive[row, col]
      st$htinclast[row, col] <- max(0, st$treeht[row, col] - st$treehtlast[row, col])
      cumba <- cumba + (st$treediam[row, col] / 100 / 2)^2 * pi
      sumcrownl <- sumcrownl + st$treelcrown[row, col]
      st$treediamlast[row, col] <- st$treediam[row, col]
      st$treehtlast[row, col] <- st$treeht[row, col]
      st$areatreelast[row, col] <- areatree[row, col]
      st$leafarealast[row, col] <- st$leafarea[row, col]
      st$treelcrownlast[row, col] <- st$treelcrown[row, col]
      st$treecrownwidthlast[row, col] <- st$treecrownwidth[row, col]
    }
  }

  if (spha == 0) {
    spha <- 1
    st$effectivestocking <- 1
    pars$actualstocking <- 1
  } else {
    pars$actualstocking <- spha
    pars$intraRow <- 10000 / (pars$interRow * pars$actualstocking)
    st$effectivestocking <- 10000 / (pars$intraRow * pars$interRow)
    pars$actualstocking <- f_calc_actual_stocking(pars$rowsblock, pars$interRow, pars$interbay,
                                                   pars$colsblock, pars$intraRow, pars$interblock)
  }

  st$Meanht <- newcumht / spha
  st$Meandiam <- cumdiam / spha
  st$vol <- cumvol
  st$spha <- spha
  st$ba <- cumba
  st$totalcrownvolume <- totalcrownvolume
  st$totaltreesurfacearea <- totaltreesurfacearea

  st$pars_out <- pars
  st
}
