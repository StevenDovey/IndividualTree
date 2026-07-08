// ============================================================
// CABALA Individual Tree Model — extracted from CCabala.cs
// For R transcoding reference only. Do not compile standalone.
// Source file: engine/Engine/CCabala.cs
//
// Extracted methods and original line numbers:
//   TreeArraySetUp                      lines 3744-3890
//   fTreeVolume                         lines 3897-3900
//   lightcalc                           lines 5874-5890
//   RowThinningTreatment                lines 8209-8364
//   withinThinningTreatment             lines 8390-8803
//   deadtreereset                       lines 8806-8847
//   radians                             lines 9049-9054
//   getAzimuthAndAlt                    lines 9160-9208
//   meanAllBranchLength                 lines 9291-9303
//   UpdateTreeListnewersimple21062012   lines 11850-12739
//   areascalene                         lines 12795-12819
//   calcLightLimitedProductiontree      lines 12864-12929
//   calcActualStocking                  lines 13650-13673
// ============================================================

// ============================================================
// METHOD: TreeArraySetUp
// Original lines in CCabala.cs: 3744-3890
// ============================================================
        private void TreeArraySetUp()
        {
            var neighbour = new double[10001];
            int cmonth;
            var CUMHT = default(double);
            var treecount = default(double);


            int numberofrowsbay;
            int numbercolsinterblock;
            int lastrow = 0;
            int lastcol = 0;
            int mycount = 0;

            double treehtodstart = 120d;

            numberRows = (int)Round(100d / interRow);
            numberCols = (int)Round(100d / intraRow);   // so we are going to loop on spacing - need to remove bays and spaces between blocks
            numberofrowsbay = Max(0, (int)Round(interbay / interRow) - 1); // this will round but should be multiple
            numbercolsinterblock = Max(0, (int)Round(interblock / intraRow) - 1);

            var loopTo = numberRows;
            for (row = 1; row <= loopTo; row++)
            {
                var loopTo1 = numberCols;
                for (col = 1; col <= loopTo1; col++)
                {
                    mycount += 1;
                    treeht[row, col] = 0d;
                    TreeNo[row, col] = mycount;
                    // TreeRow(mycount) = row
                    // TreeCol(mycount) = col
                    treesurvive[row, col] = 1;
                    treevol[row, col] = 0d;
                    treediam[row, col] = 0d;
                    MaxSpaceTree[row, col] = Sqrt(10000d / 0.0d);
                    MaxSpaceTreeLast[row, col] = MaxSpaceTree[row, col];
                    treelisthtodlast[row, col] = 1.3d;
                    treelisthtod[row, col] = 1.3d;

                    treeflag[row, col] = 0;
                    if (bnormdistseed)
                    {
                        treeht[row, col] = max(0d, ht + ht * seedvar * gasdev());  // use this if want normal distribution of heights
                    }
                    else
                    {
                        treeht[row, col] = max(0d, ht + ht * (0.5d - seedvar * VBMath.Rnd()));
                    }  // use this if want uniform distribution of heights

                    treehtlast[row, col] = treeht[row, col];

                    areatreelast[row, col] = 9d * interRow * intraRow;
                    treesurvive[row, col] = 1;

                    treevol[row, col] = fTreeVolume(treeht[row, col], treehtodstart, effectivestocking) / effectivestocking;
                    treediam[row, col] = treeht[row, col] / treehtodstart;
                    treediamlast[row, col] = treediam[row, col];
                    treeasw[row, col] = PI * Pow(treediam[row, col] / 2d, 2d);
                    treeasw[row, col] = 0.01d;
                    thintree[row, col] = 0;
                    vol = vol + treevol[row, col];
                    treecrownwidth[row, col] = crownRatio * treeht[row, col] * 0.5d;
                    treecrownwidthlast[row, col] = treecrownwidth[row, col];
                    CUMHT = CUMHT + treeht[row, col];
                    treecbalyear[row, col] = 0d;
                    leafarea[row, col] = L;
                    leafarealast[row, col] = leafarea[row, col];
                    treelcrown[row, col] = treeht[row, col];
                    treelcrownlast[row, col] = treelcrown[row, col];

                    if (varscalar == 0d)
                    {
                        treerandfac[row, col] = 1d;
                    }
                    else if (VBMath.Rnd(1f) < percentselfs / 100d)
                    {
                        treerandfac[row, col] = performself * (1d + (VBMath.Rnd(1f) * varscalarself - varscalarself / 2d));
                    }
                    else
                    {
                        treerandfac[row, col] = 1d + (VBMath.Rnd(1f) * varscalar - varscalar / 2d);
                    }

                    if (row > lastrow + rowsblock)
                    {
                        if (row <= lastrow + rowsblock + numberofrowsbay)
                        {
                            treesurvive[row, col] = 0;
                            treevol[row, col] = 0d;
                            treediam[row, col] = 0d;
                            treeht[row, col] = 0d;
                            leafarea[row, col] = 0d;
                            treecrownwidth[row, col] = 0d;
                            treecbalyear[row, col] = 0d;
                            treeasw[row, col] = 0d;
                            thintree[row, col] = 1;
                            treediamlast[row, col] = 0d;
                            treehtlast[row, col] = 0d;
                        }
                    }

                    if (col > lastcol + colsblock)
                    {
                        if (col <= lastcol + colsblock + numbercolsinterblock)
                        {
                            treesurvive[row, col] = 0;
                            treevol[row, col] = 0d;
                            treediam[row, col] = 0d;
                            treeht[row, col] = 0d;
                            leafarea[row, col] = 0d;
                            treecrownwidth[row, col] = 0d;
                            treecbalyear[row, col] = 0d;
                            treeasw[row, col] = 0d;
                            thintree[row, col] = 1;
                            treediamlast[row, col] = 0d;
                            treehtlast[row, col] = 0d;
                        }
                    }
                    for (cmonth = 1; cmonth <= 12; cmonth++)
                        resetMonthlyArray(cmonth);
                    for (cmonth = 1; cmonth <= 60; cmonth++)
                    {
                        if (treesurvive[row, col] == 1)
                        {
                            treecbal[row, col, cmonth] = 0.0001d; // initialise with small value
                        }
                        else
                        {
                            treecbal[row, col, cmonth] = 0d;
                        }
                    }
                    if (treesurvive[row, col] == 1)
                        treecount = treecount + 1d;
                    if (col == lastcol + colsblock + numbercolsinterblock)
                        lastcol = col;
                }

                RowThinned[row] = 0;
                if (row == lastrow + rowsblock + numberofrowsbay)
                    lastrow = row;
                lastcol = 0;
            }

            initspha = (int)Round(actualstocking);

        }

// ============================================================
// METHOD: fTreeVolume
// Original lines in CCabala.cs: 3897-3900
// ============================================================
        private double fTreeVolume(double Ht, double HtoD, double SPH)
        {
            return beta1 * Pow(Ht / 0.9d, beta2) * Pow(Pow(Ht / HtoD / 200d, 2d) * PI * SPH, beta3);
        }

// ============================================================
// METHOD: lightcalc
// Original lines in CCabala.cs: 5874-5890
// ============================================================
        private double lightcalc(ref double daylen, ref double hour_Renamed, ref double qday)
        {
            double lightcalcRet = default;
            double td, T;
            td = daylen * 24d;
            T = Abs(12d - hour_Renamed);
            if (T > td / 2d)
            {
                lightcalcRet = 0d;
            }
            else
            {
                lightcalcRet = 2.2d * qday * 1000000d * PI / (2d * td) * Cos(PI * T / td) / 3600d;
            } // this converts total to PAR

            return lightcalcRet;
        }

// ============================================================
// METHOD: RowThinningTreatment
// Original lines in CCabala.cs: 8209-8364
// ============================================================
        private void RowThinningTreatment(double thinproportion, double thintype)
        {
            int iii;


            commercialT = thintype;
            int nowrow;
            int nowcol;

            int outrow;

            double lostvol = 0d;
            var cumvolstart = default(double);

            double cumwssw = 0d;
            double cumwshw = 0d;
            double cumwbk = 0d;
            double cumwf = 0d;
            double cumwb = 0d;
            double cumwfr = 0d;
            double cumwcr = 0d;
            var loopTo = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo; nowrow++)
            {
                var loopTo1 = (int)Round(100d / initIntraRow);
                for (nowcol = 1; nowcol <= loopTo1; nowcol++)
                {
                    cumvolstart = cumvolstart + treevol[nowrow, nowcol];
                    cumwssw += wswtree[nowrow, nowcol];
                    cumwshw += Max(0d, wshwtree[nowrow, nowcol]);
                    cumwbk += wbktree[nowrow, nowcol];
                    cumwb += wbswtree[nowrow, nowcol];
                    cumwf += wftree[nowrow, nowcol];
                    cumwfr += WfrTree[nowrow, nowcol];
                    cumwcr += wcrhwtree[nowrow, nowcol] + wcrswtree[nowrow, nowcol];
                }
            }
            double voladjfac = vol / cumvolstart;


            voladjfac = 1d;

            silvicthinnedrowWssw = 0d;
            silvicthinnedrowWshw = 0d;
            silvicthinnedrowWbk = 0d;
            silvicthinnedrowWf = 0d;
            silvicthinnedrowWfr = 0d;
            silvicthinnedrowWb = 0d;
            silvicthinnedrowWcr = 0d;


            outrow = (int)Round(1d / thinproportion);
            var loopTo2 = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo2; nowrow++)
            {
                if (nowrow / (double)outrow == Conversion.Int(nowrow / (double)outrow))
                {
                    if (RowThinned[nowrow] == 1)
                        nowrow = nowrow + 1;
                    RowThinned[nowrow] = 1;
                    var loopTo3 = (int)Round(100d / initIntraRow);
                    for (nowcol = 1; nowcol <= loopTo3; nowcol++)
                    {
                        silvicthinnedwithinWssw = silvicthinnedwithinWssw + wswtree[nowrow, nowcol] / cumwssw * Wssw;
                        silvicthinnedwithinWshw = silvicthinnedwithinWshw + Max(0d, wshwtree[nowrow, nowcol]) / cumwshw * Wshw;
                        silvicthinnedwithinWbk = silvicthinnedwithinWbk + wbktree[nowrow, nowcol] / cumwbk * Wbk;
                        silvicthinnedwithinWf = silvicthinnedwithinWf + wftree[nowrow, nowcol] / cumwf * wf;
                        silvicthinnedwithinWfr = silvicthinnedwithinWfr + WfrTree[nowrow, nowcol] / cumwfr * Wfr;
                        silvicthinnedwithinWb = silvicthinnedwithinWb + wbswtree[nowrow, nowcol] / cumwb * Wb;
                        silvicthinnedwithinWcr = silvicthinnedwithinWcr + (wcrhwtree[nowrow, nowcol] + wcrswtree[nowrow, nowcol]) / cumwcr * Wcr;
                        wswtree[nowrow, nowcol] = 0d;
                        wshwtree[nowrow, nowcol] = 0d;
                        wbktree[nowrow, nowcol] = 0d;
                        wftree[nowrow, nowcol] = 0d;
                        WfrTree[nowrow, nowcol] = 0d;
                        wbswtree[nowrow, nowcol] = 0d;
                        wcrswtree[nowrow, nowcol] = 0d;
                        wcrhwtree[nowrow, nowcol] = 0d;
                        wswtree[nowrow, nowcol] = 0d;
                        wshwtree[nowrow, nowcol] = 0d;
                        wbktree[nowrow, nowcol] = 0d;
                        wftree[nowrow, nowcol] = 0d;
                        WfrTree[nowrow, nowcol] = 0d;
                        wbswtree[nowrow, nowcol] = 0d;
                        wcrswtree[nowrow, nowcol] = 0d;
                        wcrhwtree[nowrow, nowcol] = 0d;
                        treesurvive[nowrow, nowcol] = 0;
                        treediam[nowrow, nowcol] = 0d;
                        treeht[nowrow, nowcol] = 0d;
                        treehtlast[nowrow, nowcol] = 0d;
                        areatreelast[nowrow, nowcol] = 0d;
                        htinclast[nowrow, nowcol] = 0d;
                        treediam[nowrow, nowcol] = 0d;
                        leafarea[nowrow, nowcol] = 0d;
                        thintree[nowrow, nowcol] = 1;
                        treevol[nowrow, nowcol] = 0d;
                        treehtlast[nowrow, nowcol] = 0d;
                        treediamlast[nowrow, nowcol] = 0d;
                        leafarealast[nowrow, nowcol] = 0d;
                        treecrownwidth[nowrow, nowcol] = 0d;
                        treecrownwidthlast[nowrow, nowcol] = 0d;
                        aswtree[nowrow, nowcol] = 0d;
                        Ltree[nowrow, nowcol] = 0d;
                        treesurvive[nowrow, nowcol] = 0;
                        treecrownwidth[nowrow, nowcol] = 0d;
                        treecrownwidthlast[nowrow, nowcol] = 0d;
                        treeasw[nowrow, nowcol] = 0d;
                        treeflag[nowrow, nowcol] = 0;
                        lostvol += treevol[nowrow, nowcol] * voladjfac;
                        treesurvive[nowrow, nowcol] = 0;
                        treevol[nowrow, nowcol] = 0d;
                        treeht[nowrow, nowcol] = 0d;
                        treediam[nowrow, nowcol] = 0d;
                        thintree[nowrow, nowcol] = 1;
                        treevol[nowrow, nowcol] = 0d;
                        treehtlast[nowrow, nowcol] = 0d;
                        treediamlast[nowrow, nowcol] = 0d;
                        leafarea[nowrow, nowcol] = 0d;
                        treecrownwidth[nowrow, nowcol] = 0d;
                        treecrownwidthlast[nowrow, nowcol] = 0d;
                        treecbalyear[row, col] = 0d;

                    }
                }
            }


            vol = vol - lostvol;

            cumthinvol = cumthinvol + lostvol;


            var loopTo4 = fAgeMax();
            for (iii = 1; iii <= loopTo4; iii++)
                wfage[iii] = wfage[iii] * silvicthinnedrowWf / wf;

            spha = 0;

            var loopTo5 = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo5; nowrow++)
            {

                var loopTo6 = (int)Round(100d / initIntraRow);
                for (nowcol = 1; nowcol <= loopTo6; nowcol++)
                    spha += treesurvive[nowrow, nowcol];
            }


            interRow = 10000d / (intraRow * actualstocking * (1d - thinproportion));

            effectivestocking = 10000d / (intraRow * interRow);
            effectivestockingStored = effectivestocking;
            // actualstocking = (rowsblock * colsblock) * 10000 / (((rowsblock) * interRow + Math.Max(0.0R, interbay - interRow)) * ((colsblock) * intraRow + Math.Max(0.0R, interblock - intraRow)))
            thinproportion = 0d;
            lostvol = 0d;
        }

// ============================================================
// METHOD: withinThinningTreatment
// Original lines in CCabala.cs: 8390-8803
// ============================================================
        private void withinThinningTreatment(double thinproportion, double thinstems, ref double thintype, double minthinvol, bool IsCoppiceReduction = false)
        {


            int count;
            double smallest;
            double largest;
            double totthinvol;
            double cumvolstart;


            int nowrow;
            int nowcol;


            totthinvol = 0d;
            silvicthinnedrowWssw = 0d;
            silvicthinnedrowWshw = 0d;
            silvicthinnedrowWbk = 0d;
            silvicthinnedrowWf = 0d;
            silvicthinnedrowWfr = 0d;
            silvicthinnedrowWb = 0d;
            silvicthinnedrowWcr = 0d;
            cumvolstart = 0d;


            double gap;
            int thinnedtrees = 0;
            int removedtrees = 0;
            double cumwssw = 0d;
            double cumwshw = 0d;
            double cumwbk = 0d;
            double cumwf = 0d;
            double cumwb = 0d;
            double cumwfr = 0d;
            double cumwcr = 0d;
            var loopTo = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo; nowrow++)
            {
                var loopTo1 = (int)Round(100d / initIntraRow);
                for (nowcol = 1; nowcol <= loopTo1; nowcol++)
                {
                    cumvolstart = cumvolstart + treevol[nowrow, nowcol];
                    cumwssw += wswtree[nowrow, nowcol];
                    cumwshw += Max(0d, wshwtree[nowrow, nowcol]);
                    cumwbk += wbktree[nowrow, nowcol];
                    cumwb += wbswtree[nowrow, nowcol];
                    cumwf += wftree[nowrow, nowcol];
                    cumwfr += WfrTree[nowrow, nowcol];
                    cumwcr += wcrhwtree[nowrow, nowcol] + wcrswtree[nowrow, nowcol];
                }
            }

            double voladjfac = vol / cumvolstart;

            voladjfac = 1d;

            int i = 0;
            // Dim treesalive() As Integer

            // ReDim treesalive(spha)

            // Dim treesdiam() As Double

            // ReDim treesdiam(spha + 1)


            // thinnedtrees = Math.Max(0, CInt(spha * thinstems))
            thinnedtrees = Max(0, (int)Round(effectivestocking * thinstems));


            i = 1;

            removedtrees = 0;


            switch (withThinType)
            {
                case ThinningTypes.Random: // random uniform
                    {

                        // gap = CInt(1 / thinstems)
                        // gap = RoundupToValue(1 / thinstems, 1)

                        gap = RoundupToValue(groupSelect, 1d);  // first space removals evenly based on selection gap

                        if (!IsCoppiceReduction) // doesn't work for coppice because thinstems=0
                        {
                            gap = Max(RoundupToValue(1d / thinstems, 1d), gap);  // however ensure we don't remove too many trees
                        }

                        count = 0;

                        while (thinnedtrees > removedtrees)
                        {

                            // For nowrow = 1 To CInt(100 / initInterRow)
                            // If RowThinned(nowrow) = 0 Then
                            // For nowcol = 1 To CInt(100 / initIntraRow)
                            // If treesurvive(nowrow, nowcol) = 0 Then nowcol = nowcol + 1 ' so we are selecting groups of living trees only
                            // If treesurvive(nowrow, nowcol) = 1 Then  ' so first tree alive
                            // count = count + 1
                            // If thinnedtrees > removedtrees And count = gap Then
                            // totthinvol = totthinvol + treevol(nowrow, nowcol) * voladjfac
                            // removedtrees += 1
                            // deadtreereset(nowrow, nowcol, voladjfac)
                            // count = 0


                            nowrow = (int)Round(100d / initInterRow * VBMath.Rnd());
                            nowcol = (int)Round(100d / initIntraRow * VBMath.Rnd());
                            if (treesurvive[nowrow, nowcol] == 1)  // so first tree alive
                            {
                                if (thinnedtrees > removedtrees)
                                {
                                    totthinvol = totthinvol + treevol[nowrow, nowcol] * voladjfac;
                                    if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1)
                                    {
                                        thinnedtrees = thinnedtrees + 1;
                                        thinstems = thinnedtrees / effectivestocking;
                                    }

                                    removedtrees += 1;
                                    deadtreereset(ref nowrow, ref nowcol, voladjfac, ref cumwssw, ref cumwshw, ref cumwbk, ref cumwb, ref cumwf, ref cumwcr, ref cumwfr);
                                }
                            }

                        }

                        break;
                    }


                case ThinningTypes.GroupSelectFromBelow:
                    {


                        gap = RoundupToValue(groupSelect, 1d);  // first space removals evenly based on selection gap
                        if (!IsCoppiceReduction)
                        {
                            gap = Max(RoundupToValue(1d / thinstems, 1d), gap);  // however ensure we don't remove too many trees
                        }
                        count = 0;
                        int ggap;
                        ggap = (int)Round(gap);
                        smallest = 9999d;

                        var removerow = default(int);
                        var removecol = default(int);

                        while (thinnedtrees > removedtrees)
                        {
                            var loopTo2 = (int)Round(100d / initInterRow);
                            for (nowrow = 1; nowrow <= loopTo2; nowrow++)
                            {
                                if (RowThinned[nowrow] == 0)
                                {
                                    var loopTo3 = (int)Round(100d / initIntraRow);
                                    for (nowcol = 1; nowcol <= loopTo3; nowcol++)
                                    {

                                        if (treesurvive[nowrow, nowcol] == 0)
                                            nowcol = nowcol + 1; // so we are selecting groups of living trees only
                                        if (treesurvive[nowrow, nowcol] == 1)  // so first tree alive
                                        {

                                            if (treediam[nowrow, nowcol] < smallest)
                                            {
                                                smallest = treediam[nowrow, nowcol];
                                                removerow = nowrow;
                                                removecol = nowcol;
                                            }
                                            count = count + 1;

                                        }
                                        if (count == ggap & thinnedtrees > removedtrees)
                                        {
                                            totthinvol = totthinvol + treevol[removerow, removecol] * voladjfac;

                                            if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1)
                                            {
                                                thinnedtrees = thinnedtrees + 1;
                                                thinstems = thinnedtrees / effectivestocking;
                                            }

                                            removedtrees += 1;
                                            deadtreereset(ref removerow, ref removecol, voladjfac, ref cumwssw, ref cumwshw, ref cumwbk, ref cumwb, ref cumwf, ref cumwcr, ref cumwfr);
                                            smallest = 9999d;
                                            count = 0;

                                        }
                                    }
                                }
                            }
                        }
                        if (IsCoppiceReduction) // take thinvol off every stem
                        {
                            var loopTo4 = (int)Round(100d / initInterRow);
                            for (nowrow = 1; nowrow <= loopTo4; nowrow++)
                            {
                                var loopTo5 = (int)Round(100d / initIntraRow);
                                for (nowcol = 1; nowcol <= loopTo5; nowcol++)
                                {
                                    if (treesurvive[nowrow, nowcol] == 1)
                                    {
                                        totthinvol += treevol[nowrow, nowcol] * thinproportion;
                                        treevol[nowrow, nowcol] = (1d - thinproportion) * treevol[nowrow, nowcol] * voladjfac;
                                        treediam[nowrow, nowcol] = Sqrt(Pow(treevol[nowrow, nowcol] * spha / (beta1 * Pow(treeht[nowrow, nowcol] / 0.9d, beta2)), 1d / beta3) / spha / PI) * 100d;
                                        leafarea[nowrow, nowcol] = leafarea[nowrow, nowcol] * (1d - thinproportion);
                                        leafarealast[nowrow, nowcol] = leafarea[nowrow, nowcol];
                                        // L = L * (1 - thinproportion)
                                    }
                                }
                            }
                            // wf = wf * (1 - thinproportion)
                            // Wb = Wb * (1 - thinproportion)
                            silvicthinnedwithinWf = silvicthinnedwithinWf + (wf - silvicthinnedrowWf) * thinproportion;
                            silvicthinnedwithinWb = silvicthinnedwithinWb + (Wb - silvicthinnedrowWb) * thinproportion;

                        }

                        break;
                    }


                case ThinningTypes.GroupSelectFromAbove:
                    {


                        gap = RoundupToValue(groupSelect, 1d);  // first space removals evenly based on selection gap

                        if (!IsCoppiceReduction)
                        {
                            gap = Max(RoundupToValue(1d / thinstems, 1d), gap);  // however ensure we don't remove too many trees
                        }
                        count = 0;
                        int ggap;
                        ggap = (int)Round(gap);
                        largest = -999;

                        var removerow = default(int);
                        var removecol = default(int);

                        while (thinnedtrees > removedtrees)
                        {
                            var loopTo6 = (int)Round(100d / initInterRow);
                            for (nowrow = 1; nowrow <= loopTo6; nowrow++)
                            {
                                if (RowThinned[nowrow] == 0)
                                {
                                    var loopTo7 = (int)Round(100d / initIntraRow);
                                    for (nowcol = 1; nowcol <= loopTo7; nowcol++)
                                    {
                                        if (treesurvive[nowrow, nowcol] == 0)
                                            nowcol = nowcol + 1; // so we are selecting groups of living trees only
                                        if (treesurvive[nowrow, nowcol] == 1)  // so first tree alive
                                        {
                                            if (treediam[nowrow, nowcol] > largest)
                                            {
                                                largest = treediam[nowrow, nowcol];
                                                removerow = nowrow;
                                                removecol = nowcol;
                                            }
                                            count = count + 1;

                                        }

                                        if (count == ggap & thinnedtrees > removedtrees)
                                        {
                                            totthinvol = totthinvol + treevol[removerow, removecol] * voladjfac;

                                            if (totthinvol < minthinvol & thinnedtrees == removedtrees + 1)
                                            {
                                                thinnedtrees = thinnedtrees + 1;
                                                thinstems = thinnedtrees / effectivestocking;
                                            }

                                            removedtrees += 1;
                                            deadtreereset(ref removerow, ref removecol, voladjfac, ref cumwssw, ref cumwshw, ref cumwbk, ref cumwb, ref cumwf, ref cumwcr, ref cumwfr);
                                            largest = -999;
                                            count = 0;
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }


                case ThinningTypes.ThinBelowDiameter: // THIS WILL NOT BE AFFECTED BY ANYTHING BUT DIAMETER
                    {

                        var loopTo8 = (int)Round(100d / initInterRow);
                        for (nowrow = 1; nowrow <= loopTo8; nowrow++)
                        {
                            if (RowThinned[nowrow] == 0)
                            {
                                var loopTo9 = (int)Round(100d / initIntraRow);
                                for (nowcol = 1; nowcol <= loopTo9; nowcol++)
                                {
                                    if (treediam[nowrow, nowcol] < specified_Diameter)
                                    {
                                        if (treesurvive[nowrow, nowcol] == 1)
                                        {
                                            totthinvol = totthinvol + treevol[nowrow, nowcol] * voladjfac;
                                            removedtrees += 1;
                                            deadtreereset(ref nowrow, ref nowcol, voladjfac, ref cumwssw, ref cumwshw, ref cumwbk, ref cumwb, ref cumwf, ref cumwcr, ref cumwfr);
                                        }
                                    }

                                }
                            }
                        }

                        thinstems = Max(0d, removedtrees / (double)spha);
                        break;
                    }

                case ThinningTypes.ThinAboveDiameter:
                    {


                        var loopTo10 = (int)Round(100d / initInterRow);
                        for (nowrow = 1; nowrow <= loopTo10; nowrow++)
                        {
                            if (RowThinned[nowrow] == 0)
                            {
                                var loopTo11 = (int)Round(100d / initIntraRow);
                                for (nowcol = 1; nowcol <= loopTo11; nowcol++)
                                {
                                    if (treediam[nowrow, nowcol] > specified_Diameter)
                                    {
                                        if (treesurvive[nowrow, nowcol] == 1)
                                        {
                                            totthinvol = totthinvol + treevol[nowrow, nowcol] * voladjfac;
                                            removedtrees += 1;
                                            deadtreereset(ref nowrow, ref nowcol, voladjfac, ref cumwssw, ref cumwshw, ref cumwbk, ref cumwb, ref cumwf, ref cumwcr, ref cumwfr);

                                        }
                                    }

                                }
                            }
                        }

                        thinstems = Max(0d, removedtrees / (double)spha);
                        break;
                    }

            }


            double cumht = 0d;
            double cumdiam = 0d;
            double counttrees = 0d;
            double cumvol = 0d;

            // totthinvol = totthinvol * cumvolstart / vol ' adjust for individual tree diffs

            var loopTo12 = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo12; nowrow++)
            {
                var loopTo13 = (int)Round(100d / initIntraRow);
                for (nowcol = 1; nowcol <= loopTo13; nowcol++)
                {
                    cumht += treeht[nowrow, nowcol];
                    ba += Pow(treediam[nowrow, nowcol] / 200d, 2d) * PI;
                    cumdiam += treediam[nowrow, nowcol];
                    counttrees += treesurvive[nowrow, nowcol];
                    cumvol += treevol[nowrow, nowcol];
                }
            }


            // ht = cumht / counttrees
            // htLast = ht
            // diam = cumdiam / counttrees
            // diamLast = diam
            // vol = vol - totthinvol
            vol = cumvol;


            cumThinProp = cumThinProp * (1d - thinstems);
            cumthinvol = cumthinvol + totthinvol;
            intraRow = 10000d / (interRow * actualstocking * (1d - thinstems));
            effectivestocking = 10000d / (intraRow * interRow);
            effectivestockingStored = effectivestocking;
            // actualstocking = (rowsblock * colsblock) * 10000 / (((rowsblock) * interRow + Math.Max(0.0R, interbay - interRow)) * ((colsblock) * intraRow + Math.Max(0.0R, interblock - intraRow)))
            actualstocking = calcActualStocking(rowsblock, interRow, interbay, colsblock, intraRow, interblock);
            spha = 0;
            var loopTo14 = (int)Round(100d / initInterRow);
            for (nowrow = 1; nowrow <= loopTo14; nowrow++)
            {

                var loopTo15 = (int)Round(100d / initIntraRow);
                for (nowcol = 1; nowcol <= loopTo15; nowcol++)
                    spha += treesurvive[nowrow, nowcol];
            }

            var loopTo16 = fAgeMax();
            for (i = 1; i <= loopTo16; i++)
                // wfage(i) = wfage(i) * silvicthinnedwithinWf / wf
                wfage[i] = wfage[i] * (wf - silvicthinnedwithinWf) / wf;

            commercialT = thintype;

            thinstems = 0d;
            thinproportion = 0d;
            totthinvol = 0d;


        }

// ============================================================
// METHOD: deadtreereset
// Original lines in CCabala.cs: 8806-8847
// ============================================================
        private void deadtreereset(ref int nowrow, ref int nowcol, double byrefvolrat, ref double cumwsw, ref double cumshw, ref double cumwbk, ref double cumwb, ref double cumwf, ref double cumwcr, ref double cumwfr)
        {
            silvicthinnedwithinWssw = silvicthinnedwithinWssw + wswtree[nowrow, nowcol] / cumwsw * Wssw;
            silvicthinnedwithinWshw = silvicthinnedwithinWshw + Max(0d, wshwtree[nowrow, nowcol]) / cumshw * Wshw;
            silvicthinnedwithinWbk = silvicthinnedwithinWbk + wbktree[nowrow, nowcol] / cumwbk * Wbk;
            silvicthinnedwithinWf = silvicthinnedwithinWf + wftree[nowrow, nowcol] / cumwf * wf;
            silvicthinnedwithinWfr = silvicthinnedwithinWfr + WfrTree[nowrow, nowcol] / cumwfr * Wfr;
            silvicthinnedwithinWb = silvicthinnedwithinWb + wbswtree[nowrow, nowcol] / cumwb * Wb;
            silvicthinnedwithinWcr = silvicthinnedwithinWcr + (wcrhwtree[nowrow, nowcol] + wcrswtree[nowrow, nowcol]) / cumwcr * Wcr;
            wswtree[nowrow, nowcol] = 0d;
            wshwtree[nowrow, nowcol] = 0d;
            wbktree[nowrow, nowcol] = 0d;
            wftree[nowrow, nowcol] = 0d;
            WfrTree[nowrow, nowcol] = 0d;
            wbswtree[nowrow, nowcol] = 0d;
            wcrswtree[nowrow, nowcol] = 0d;
            wcrhwtree[nowrow, nowcol] = 0d;
            treesurvive[nowrow, nowcol] = 0;
            treediam[nowrow, nowcol] = 0d;
            treeht[nowrow, nowcol] = 0d;
            treehtlast[nowrow, nowcol] = 0d;
            areatreelast[nowrow, nowcol] = 0d;
            htinclast[nowrow, nowcol] = 0d;
            treediam[nowrow, nowcol] = 0d;
            leafarea[nowrow, nowcol] = 0d;
            thintree[nowrow, nowcol] = 1;
            treevol[nowrow, nowcol] = 0d;
            treehtlast[nowrow, nowcol] = 0d;
            treediamlast[nowrow, nowcol] = 0d;
            leafarealast[nowrow, nowcol] = 0d;
            treecrownwidth[nowrow, nowcol] = 0d;
            treecrownwidthlast[nowrow, nowcol] = 0d;
            aswtree[nowrow, nowcol] = 0d;
            Ltree[nowrow, nowcol] = 0d;
            treesurvive[nowrow, nowcol] = 0;
            treecrownwidth[nowrow, nowcol] = 0d;
            treecrownwidthlast[nowrow, nowcol] = 0d;
            treeasw[nowrow, nowcol] = 0d;
            treeflag[nowrow, nowcol] = 0;
            treecbalyear[row, col] = 0d;

        }

// ============================================================
// METHOD: radians
// Original lines in CCabala.cs: 9049-9054
// ============================================================
        private double radians(ref double x)
        {
            double radiansRet = default;
            radiansRet = PI / 180d * x;
            return radiansRet;
        }

// ============================================================
// METHOD: getAzimuthAndAlt
// Original lines in CCabala.cs: 9160-9208
// ============================================================
        private void getAzimuthAndAlt(double hin, double dayofyr, double lat)
        {
            // Get the solar elevation alphaS and azimuth psiS (degrees) at a time
            // h (hours) on the dayOfYear for a site of latitude Lat (degrees).
            // Based on Iqbal (An Introduction to Solar Radiation, Academic Press, 1983)
            // h = 12 hours.

            double HA;
            double delta;
            double cosHA;
            double sinLat;
            double cosLat;
            double sinDelta;
            double cosDelta;

            double sinAlpha;
            double cosAlpha;
            double cosPhi;
            double psiS;
            double alphaS;

            HA = 2d * PI * (12d - hin) / 24d;
            cosHA = Cos(HA);

            delta = -(23.45d * Cos(2d * PI * (dayofyr + 10d) / 365d)) * (PI / 180d);
            sinDelta = Sin(delta);
            cosDelta = Cos(delta);

            double mylat = lat * PI / 180d;
            sinLat = Sin(mylat);
            cosLat = Cos(mylat);

            sinAlpha = sinDelta * sinLat + cosDelta * cosLat * cosHA;
            cosAlpha = Sqrt(1d - Pow(sinAlpha, 2d));
            alphaS = Asin(sinAlpha) * 180d / PI;

            cosPhi = (sinAlpha * sinLat - sinDelta) / (cosAlpha * cosLat);
            cosPhi = Max(Min(cosPhi, 1d), -1);
            psiS = Acos(cosPhi) * 180d / PI;

            if (hin > 12d)
                psiS = 180d + psiS;
            else
                psiS = 180d - psiS;

            azimuth = psiS;
            solaralt = alphaS;

        }

// ============================================================
// METHOD: meanAllBranchLength
// Original lines in CCabala.cs: 9291-9303
// ============================================================
        private double meanAllBranchLength(ref double baX, ref double baY, ref double baZ, ref double btheta)
        {
            double meanAllBranchLengthRet = default;
            double BL2, BL1, BL3;
            double argbphi = 0d;
            BL1 = meanBranchLength(ref baX, ref baY, ref baZ, ref btheta, ref argbphi);
            double argbphi1 = 45d;
            BL2 = meanBranchLength(ref baX, ref baY, ref baZ, ref btheta, ref argbphi1);
            double argbphi2 = 90d;
            BL3 = meanBranchLength(ref baX, ref baY, ref baZ, ref btheta, ref argbphi2);
            meanAllBranchLengthRet = Pow(BL1 * BL2 * BL3, 1d / 3d);
            return meanAllBranchLengthRet;
        }

// ============================================================
// METHOD: UpdateTreeListnewersimple21062012
// Original lines in CCabala.cs: 11850-12739
// ============================================================
        private void UpdateTreeListnewersimple21062012(double stemincmonth, double ggrossmonth, double Rcrmonthin, double Rbmonthin, double Rfmonthin, double Rfrmonthin, double Rsmonthin, int mmonth, DateTime wdate, DateTime wcf, double aetmonth, double qmonth)
        {
            double LostStemMass;
            double CUMHT;
            double cumdiam;
            double sumproduction;
            double cumba;
            double treeWestR;
            double treeWestW;
            double aratio;
            double cumvol;
            double sumleafarea;
            double sumcrownl;
            var avhtaround = new double[101, 101];
            var rowcount = new int[4];
            var colcount = new int[4];
            double sratio;
            var Freespace = new double[101, 101];
            double gridsize;
            var crownfac = new double[101, 101];
            var rootcompwater = new double[101, 101];
            double totaltreesurfacearea;
            var treecrownvol = new double[101, 101];
            double totalcrownvolume;
            var areatree = new double[101, 101];
            double axtree1;
            double axtree2;
            var alphatree = new double[3];
            double fratio;
            double sumtreevol;
            double totalalloc;
            var alloctree = new double[101, 101];
            var neighbour = new double[10];
            int numberows;
            int numbercols;
            int cmonth;
            double LostTrees;
            double treeprod;
            int myrow;
            int mycol;
            double qtree;


            double Meanht;
            double Meandiam;

            int treecheck;

            var leftneighbour = default(int);
            var rightneighbour = default(int);
            var topneighbour = default(int);
            var bottomneighbour = default(int);
            var dleftneighbour = default(double);
            var drightneighbour = default(double);
            var dtopneighbour = default(double);
            var dbottomneighbour = default(double);
            var lleftneighbour = default(double);
            var lrightneighbour = default(double);
            var ltopneighbour = default(double);
            var lbottomneighbour = default(double);
            double diamleftneighbour;
            double diamrightneighbour;
            double diamtopneighbour;
            double diambottomneighbour;
            var toprightneighbour = default(int);
            var dtoprightneighbour = default(double);
            var ltoprightneighbour = default(double);
            double diamtoprightneighbour;
            var botrightneighbour = default(int);
            var dbotrightneighbour = default(double);
            var lbotrightneighbour = default(double);
            double diambotrightneighbour;
            var topleftneighbour = default(int);
            var dtopleftneighbour = default(double);
            var ltopleftneighbour = default(double);
            double diamtopleftneighbour;
            var botleftneighbour = default(int);
            var dbotleftneighbour = default(double);
            var lbotleftneighbour = default(double);
            double diambotleftneighbour;
            var hrightneighbour = default(double);
            var htopneighbour = default(double);
            var hbottomneighbour = default(double);
            var hleftneighbour = default(double);
            var hbotrightneighbour = default(double);
            var htopleftneighbour = default(double);
            var hbotleftneighbour = default(double);
            var htoprightneighbour = default(double);
            var crightneighbour = default(double);
            var ctopneighbour = default(double);
            var cbottomneighbour = default(double);
            var cleftneighbour = default(double);
            var cbotrightneighbour = default(double);
            var ctopleftneighbour = default(double);
            var cbotleftneighbour = default(double);
            var ctoprightneighbour = default(double);
            double Distance;
            int ii;
            double cumrad;
            double cumtreerad;
            double radnow;
            double hhour;
            double anglelower;
            double angleupper;
            double angleoffset;
            double nLad;

            double cumwswtree;
            double cumaswtree;
            double TREEBL;
            double cumwcrswtree;
            double cumwbswtree;
            double treehtnow;


            sumcrownl = 0d;
            sumleafarea = 0d;
            cumwcrswtree = 0d;
            cumwbswtree = 0d;

            LostStemMass = 0d;
            CUMHT = 0d;
            cumdiam = 0d;
            sumproduction = 0d;
            cumba = 0d;
            cumvol = 0d;
            totaltreesurfacearea = 0d;
            totalcrownvolume = 0d;
            sumtreevol = 0d;
            totalalloc = 0d;
            LostTrees = 0d;
            spha = 0;
            cumwswtree = 0d;
            cumaswtree = 0d;
            // sumtreevol = 0
            // sumleafarea = 0
            // cumwbswtree = 0
            // cumwcrswtree = 0


            var baseangle = new double[] { 180d, 0d, 90d, 270d, 135d, 45d, 225d, 315d };


            numberows = (int)Round(100d / initInterRow);
            numbercols = (int)Round(100d / initIntraRow);
            gridsize = initInterRow * initIntraRow;

            initspha = numberows * numbercols;

            var wnewdate = DateAndTime.DateAdd(DateInterval.Month, -1, wcf);


            var loopTo = numberows;
            for (row = 1; row <= loopTo; row++)
            {
                var loopTo1 = numbercols;
                for (col = 1; col <= loopTo1; col++)
                {

                    spha += treesurvive[row, col]; // count how many trees we have at the start, we update this at the end


                    treecheck = 0;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        myrow = myrow - 1;
                        Distance = Distance + initInterRow;
                        if (myrow == 0)
                            myrow = numberows;
                        leftneighbour = TreeNo[myrow, col];
                        dleftneighbour = Distance;
                        hleftneighbour = treehtlast[myrow, col];
                        lleftneighbour = leafarea[myrow, col];
                        diamleftneighbour = treediam[myrow, col];
                        cleftneighbour = treecrownwidth[myrow, col];

                        if (treesurvive[myrow, col] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }

                    treecheck = 0;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        myrow = myrow + 1;
                        Distance = Distance + initInterRow;
                        if (myrow > numberows)
                            myrow = 1;
                        rightneighbour = TreeNo[myrow, col];
                        drightneighbour = Distance;
                        hrightneighbour = treehtlast[myrow, col];
                        lrightneighbour = leafarea[myrow, col];
                        diamrightneighbour = treediam[myrow, col];
                        crightneighbour = treecrownwidth[myrow, col];
                        if (treesurvive[myrow, col] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }

                    treecheck = 0;
                    mycol = col;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol + 1;
                        Distance = Distance + initIntraRow;
                        if (mycol > numbercols)
                            mycol = 1;
                        topneighbour = TreeNo[row, mycol];
                        dtopneighbour = Distance;
                        htopneighbour = treehtlast[row, mycol];
                        ltopneighbour = leafarea[row, mycol];
                        diamtopneighbour = treediam[row, mycol];
                        ctopneighbour = treecrownwidth[row, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }


                    treecheck = 0;
                    mycol = col;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol - 1;
                        Distance = Distance + initIntraRow;
                        if (mycol == 0)
                            mycol = numbercols;
                        bottomneighbour = TreeNo[row, mycol];
                        dbottomneighbour = Distance;
                        hbottomneighbour = treehtlast[row, mycol];
                        lbottomneighbour = leafarea[row, mycol];
                        diambottomneighbour = treediam[row, mycol];
                        cbottomneighbour = treecrownwidth[row, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }

                    treecheck = 0;
                    mycol = col;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol + 1;
                        myrow = myrow + 1;
                        Distance = Distance + Pow(Pow(initIntraRow, 2d) + Pow(initInterRow, 2d), 0.5d);
                        if (mycol > numbercols)
                            mycol = 0;
                        if (myrow > numberows)
                            myrow = 0;
                        toprightneighbour = TreeNo[myrow, mycol];
                        dtoprightneighbour = Distance;
                        htoprightneighbour = treehtlast[myrow, mycol];
                        ltoprightneighbour = leafarea[myrow, mycol];
                        diamtoprightneighbour = treediam[row, mycol];
                        ctoprightneighbour = treecrownwidth[myrow, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= Pow(2d * Pow(100d, 2d), 0.5d))
                        {
                            treecheck = 1;
                        }
                    }


                    treecheck = 0;
                    mycol = col;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol + 1;
                        myrow = myrow - 1;
                        Distance = Distance + Pow(Pow(initIntraRow, 2d) + Pow(initInterRow, 2d), 0.5d);
                        if (mycol > numbercols)
                            mycol = 0;
                        if (myrow == 0)
                            myrow = numberows;
                        botrightneighbour = TreeNo[myrow, mycol];
                        dbotrightneighbour = Distance;
                        hbotrightneighbour = treehtlast[myrow, mycol];
                        lbotrightneighbour = leafarea[myrow, mycol];
                        diambotrightneighbour = treediam[row, mycol];
                        cbotrightneighbour = treecrownwidth[myrow, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= Pow(2d * Pow(100d, 2d), 0.5d))
                        {
                            treecheck = 1;
                        }
                    }


                    treecheck = 0;
                    mycol = col;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol - 1;
                        myrow = myrow - 1;
                        Distance = Distance + Pow(Pow(initIntraRow, 2d) + Pow(initInterRow, 2d), 0.5d);
                        if (mycol == 0)
                            mycol = numbercols;
                        if (myrow == 0)
                            myrow = numberows;
                        botleftneighbour = TreeNo[myrow, mycol];
                        dbotleftneighbour = Distance;
                        hbotleftneighbour = treehtlast[myrow, mycol];
                        lbotleftneighbour = leafarea[myrow, mycol];
                        diambotleftneighbour = treediam[row, mycol];
                        cbotleftneighbour = treecrownwidth[myrow, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }


                    treecheck = 0;
                    mycol = col;
                    myrow = row;
                    Distance = 0d;
                    while (treecheck == 0)
                    {
                        mycol = mycol - 1;
                        myrow = myrow + 1;
                        Distance = Distance + Pow(Pow(initIntraRow, 2d) + Pow(initInterRow, 2d), 0.5d);
                        if (mycol == 0)
                            mycol = numbercols;
                        if (myrow > numberows)
                            myrow = 0;
                        topleftneighbour = TreeNo[myrow, mycol];
                        dtopleftneighbour = Distance;
                        htopleftneighbour = treehtlast[myrow, mycol];
                        ltopleftneighbour = leafarea[myrow, mycol];
                        diamtopleftneighbour = treediam[row, mycol];
                        ctopleftneighbour = treecrownwidth[myrow, mycol];
                        if (treesurvive[row, mycol] == 1)
                            treecheck = 1;
                        if (Distance >= 100d)
                        {
                            treecheck = 1;
                        }
                    }

                    var neighbours = new int[] { leftneighbour, rightneighbour, topneighbour, bottomneighbour, topleftneighbour, toprightneighbour, botleftneighbour, botrightneighbour };
                    var dneighbours = new double[] { dleftneighbour, drightneighbour, dtopneighbour, dbottomneighbour, dtopleftneighbour, dtoprightneighbour, dbotleftneighbour, dbotrightneighbour };
                    var cneighbours = new double[] { cleftneighbour, crightneighbour, ctopneighbour, cbottomneighbour, ctopleftneighbour, ctoprightneighbour, cbotleftneighbour, cbotrightneighbour };
                    var lneighbours = new double[] { lleftneighbour, lrightneighbour, ltopneighbour, lbottomneighbour, ltopleftneighbour, ltoprightneighbour, lbotleftneighbour, lbotrightneighbour };
                    var hneighbours = new double[] { hleftneighbour, hrightneighbour, htopneighbour, hbottomneighbour, htopleftneighbour, htoprightneighbour, hbotleftneighbour, hbotrightneighbour };


                    double area1 = areascalene(dneighbours[0], dneighbours[4], cneighbours[0], cneighbours[4]);
                    double area2 = areascalene(dneighbours[4], dneighbours[2], cneighbours[4], cneighbours[2]);
                    double area3 = areascalene(dneighbours[2], dneighbours[5], cneighbours[2], cneighbours[5]);
                    double area4 = areascalene(dneighbours[5], dneighbours[1], cneighbours[5], cneighbours[1]);
                    double area5 = areascalene(dneighbours[1], dneighbours[7], cneighbours[1], cneighbours[7]);
                    double area6 = areascalene(dneighbours[7], dneighbours[3], cneighbours[7], cneighbours[3]);
                    double area7 = areascalene(dneighbours[3], dneighbours[6], cneighbours[3], cneighbours[6]);
                    double area8 = areascalene(dneighbours[6], dneighbours[0], cneighbours[6], cneighbours[0]);


                    MaxSpaceTree[row, col] = area1 + area2 + area3 + area4 + area5 + area6 + area7 + area8;


                    avhtaround[row, col] = (hleftneighbour + hrightneighbour + htopneighbour + hbottomneighbour + htopleftneighbour + htoprightneighbour + hbotleftneighbour + hbotrightneighbour) / 8d;


                    treelcrown[row, col] = max(0d, min(treeht[row, col], treeht[row, col] - greenht)); // m


                    if (treeht[row, col] > 0d)
                    {
                        treecrownwidth[row, col] = max(treecrownwidthlast[row, col], min(treecrownwidthlast[row, col] + 0.5d * htinclast[row, col], 0.5d * crownRatio * treeht[row, col], Sqrt(MaxSpaceTree[row, col]) / PI)); // m
                    }
                    // treecrownwidth(row, col) = max(treecrownwidthlast(row, col), min(treecrownwidthlast(row, col) + htinclast(row, col))) 'm
                    else
                    {
                        treecrownwidth[row, col] = treecrownwidthlast[row, col];
                    }

                    areatree[row, col] = PI * Pow(treecrownwidth[row, col], 2d);
                    totaltreesurfacearea += areatree[row, col];
                    treecrownvol[row, col] = 4d / 3d * PI * treelcrown[row, col] / 2d * Pow(treecrownwidth[row, col], 2d);
                    totalcrownvolume += treecrownvol[row, col];


                    // WfrTree(row, col) = (leafarea(row, col) * spla) * Wfr / wf ' leaf mass to fine root mass is a constant ratio assuming functional balance in kg

                    // allroots += WfrTree(row, col)


                    cumrad = 0d;
                    cumtreerad = 0d;


                    for (hhour = 1d; hhour <= 24d; hhour += 1d)
                    {
                        getAzimuthAndAlt(hhour, dayOfYear, lat);
                        azimuth = Round(azimuth, 0);
                        solaralt = Round(solaralt, 0);
                        azimuth = Round(90.0d - azimuth + rowdirection, 0);

                        if (solaralt > 0d)
                        {
                            radnow = lightcalc(ref h, ref hhour, ref qmonth);
                        }
                        else
                        {
                            radnow = 0d;
                        }

                        cumrad += radnow;
                        var hourtreerad = new double[9];
                        double minhourrad = 99999d;

                        for (ii = 0; ii <= 7; ii++)
                        {
                            // calc angles for edges of crown at widest part and see if neighbour is in line with the target tree
                            // - assume that shading is relevant to central part of our target not edge
                            angleoffset = 180d / PI * Atan(cneighbours[ii] / dneighbours[ii]);
                            anglelower = baseangle[ii] - angleoffset;
                            angleupper = baseangle[ii] + angleoffset;
                            if (anglelower <= azimuth & azimuth <= angleupper & treeht[row, col] < hneighbours[ii] & dneighbours[ii] < (hneighbours[ii] - treeht[row, col]) / Tan(solaralt * PI / 180d))
                            {
                                // now see if solar altitude is above shading tree - here we will assume light comp if tip of our tree is shaded in this hour
                                // assume shading tree leaf area uniform and assume path length is crownwidth/acos(solar altitude)
                                nLad = lneighbours[ii] / (4d / 3d * PI * Pow(cneighbours[ii], 2d) * Max(0.0d, hneighbours[ii] - greenht) / 2d); // this is the LAD assuming neighbour is an ellipoid
                                pathLength = cneighbours[ii] / Cos(radians(ref solaralt)); // this might be too harsh - this assumes that passing through max crown width
                                hourtreerad[ii] = radnow * Exp(-k * nLad * pathLength);
                            }

                            else
                            {
                                hourtreerad[ii] = radnow;
                            } // so that tree is not shading our tree

                            if (hourtreerad[ii] < minhourrad)
                                minhourrad = hourtreerad[ii]; // store the lowest value as this indicates our tree being shaded

                        }

                        cumtreerad = cumtreerad + minhourrad; // take the sum of lowest hourly light intesities as daily radiation

                    }

                    crownfac[row, col] = cumtreerad / cumrad;


                }
            }

            var loopTo2 = numberows;
            for (row = 1; row <= loopTo2; row++)
            {
                var loopTo3 = numbercols;
                for (col = 1; col <= loopTo3; col++)
                {


                    if (areatree[row, col] > 0d)
                    {
                        leafarea[row, col] = treesurvive[row, col] * L * 10000d * treecrownvol[row, col] / totalcrownvolume;    // m2 per tree
                    }
                    else
                    {
                        leafarea[row, col] = 0d;
                    }


                    if (areatree[row, col] > 0d)
                    {
                        Ltree[row, col] = leafarea[row, col] / areatree[row, col];
                        aswtree[row, col] = Max(0d, Pow(Ltree[row, col], Wfalloc2) * wfAlloc1ForCalc * Pow(treeht[row, col], Wfalloc3)) / spha;
                        wswtree[row, col] = Max(0d, aswtree[row, col] * (Max(0.01d, treeht[row, col] - treelcrown[row, col]) + 0.33d * treelcrown[row, col]) * density) / 1000d;
                        wcrswtree[row, col] = aswtree[row, col] * (treecrownwidth[row, col] + rootdepth) / 2d * branchTaper * density / 1000d;
                        double argbtheta = PI / 2d - branchangle * PI / 180d;
                        TREEBL = meanAllBranchLength(ref treecrownwidth[row, col], ref treelcrown[row, col], ref treecrownwidth[row, col], ref argbtheta);
                        if (wcrswtree[row, col] == 0d)
                            TREEBL = 0d;
                        wbswtree[row, col] = aswtree[row, col] * TREEBL * branchTaper * density / 1000d;
                    }
                    else
                    {
                        wswtree[row, col] = 0d;
                        wcrswtree[row, col] = 0d;
                        wbswtree[row, col] = 0d;

                    }

                    sumtreevol += treevol[row, col];
                    CUMHT += treeht[row, col];
                    sumleafarea += leafarea[row, col];
                    cumwswtree += wswtree[row, col];
                    cumaswtree += aswtree[row, col];
                    cumwbswtree += wbswtree[row, col];
                    cumwcrswtree += wcrswtree[row, col];


                }
            }


            var loopTo4 = numberows;
            for (row = 1; row <= loopTo4; row++)
            {
                var loopTo5 = numbercols;
                for (col = 1; col <= loopTo5; col++)
                {


                    double Pnscalar = crownfac[row, col];

                    if (avhtaround[row, col] > 0d)
                    {
                        axtree1 = Pnscalar * ax[1];
                        axtree2 = Pnscalar * ax[2];
                    }
                    else
                    {
                        axtree1 = ax[1];
                        axtree2 = ax[2];
                    }


                    if (treecrownwidth[row, col] > 0d & treesurvive[row, col] > 0 & crownfac[row, col] > 0d)
                    {

                        qtree = qmonth * crownfac[row, col];

                        treeprod = calcLightLimitedProductiontree(1d, axtree1, axtree2, alpha[1], alpha[2], leafarea[row, col], areatree[row, col], qtree);
                        production[row, col] = treerandfac[row, col] * areatree[row, col] * treeprod;
                    }
                    // If row = 1 Then
                    // If col = 2 Then
                    // row = row
                    // If production(row, col) = 0 Then Stop

                    else
                    {
                        production[row, col] = 0d;
                    }


                    sumproduction = sumproduction + production[row, col];


                }
            }


            var loopTo6 = numberows;
            for (row = 1; row <= loopTo6; row++)
            {
                var loopTo7 = numbercols;
                for (col = 1; col <= loopTo7; col++)
                {


                    if (sumproduction > 0d)
                    {
                        aratio = production[row, col] / sumproduction;
                    }
                    else
                    {
                        aratio = 0d;
                    }
                    if (sumleafarea > 0d)
                    {
                        fratio = leafarea[row, col] / sumleafarea;
                    }
                    else
                    {
                        fratio = 0d;
                    }
                    // sratio = treevol(row, col) / sumtreevol
                    if (cumwswtree > 0d)
                    {
                        sratio = wswtree[row, col] / cumwswtree;
                    }
                    else
                    {
                        sratio = 0d;
                    }
                    // rratio = WfrTree(row, col) / allroots
                    // Dim wratio As Double = treewaterdemand(row, col) / totalwaterdemand
                    double crswratio;
                    double bswratio;
                    wftree[row, col] = fratio * wf;
                    WfrTree[row, col] = fratio * Wfr;
                    wshwtree[row, col] = Max(0d, Ws * sratio - wswtree[row, col]);
                    wbktree[row, col] = Wbk * sratio;
                    wcrhwtree[row, col] = Max(0d, Wcrhw * sratio);
                    if (cumwcrswtree > 0d)
                    {
                        crswratio = wcrswtree[row, col] / cumwcrswtree;
                    }
                    else
                    {
                        crswratio = 0d;
                    }
                    if (cumwbswtree > 0d)
                    {
                        bswratio = wbswtree[row, col] / cumwbswtree;
                    }
                    else
                    {
                        bswratio = 0d;
                    }
                    wbswtree[row, col] = bswratio * Wb;
                    wcrswtree[row, col] = crswratio * Wcrsw;
                    transpiration[row, col] = aetmonth * aratio * 10000d; // here we assume A vs E constant


                    alloctree[row, col] = (aratio * ggrossmonth - (fratio * (Rfmonthin + lostWf) - fratio * (lostWfr + Rfrmonthin)) - sratio * (Rsmonthin + lostWssw) - bswratio * (Rbmonthin + lostWb) - crswratio * (Rcrmonthin + lostWcr)) / (1d + rC);


                    var loopTo8 = monthcstarve - 1;
                    for (cmonth = 1; cmonth <= loopTo8; cmonth++)
                        treecbal[row, col, cmonth] = treecbal[row, col, cmonth + 1];


                    treecbal[row, col, monthcstarve] = alloctree[row, col]; // use actual here to see which trees are eating into reserves

                    treecbalyear[row, col] = 0d;
                    var loopTo9 = monthcstarve;
                    for (cmonth = 1; cmonth <= loopTo9; cmonth++)
                        treecbalyear[row, col] = treecbalyear[row, col] + treecbal[row, col, cmonth];

                    totalalloc += alloctree[row, col];
                    // If row = 1 And col = 1 Then Stop
                }
            }


            CUMHT = 0d;
            cumdiam = 0d;
            cumba = 0d;
            cumvol = 0d;


            var loopTo10 = numberows;
            for (row = 1; row <= loopTo10; row++)
            {
                var loopTo11 = numbercols;
                for (col = 1; col <= loopTo11; col++)
                {


                    if (totalalloc > 0d)
                    {

                        treevol[row, col] = Max(0d, treevol[row, col] + alloctree[row, col] / totalalloc * stemincmonth);
                    }

                    else
                    {
                        treevol[row, col] = treevol[row, col];
                    }

                    cumvol += treevol[row, col];  // so cumvol is after allocation and should equal existing vol

                }
            }

            double newcumvol = 0d;
            double newcumdiam = 0d;
            double newcumht = 0d;

            var loopTo12 = numberows;
            for (row = 1; row <= loopTo12; row++)
            {
                var loopTo13 = numbercols;
                for (col = 1; col <= loopTo13; col++)
                {


                    treecbalyear[row, col] = max(0d, treecbalyear[row, col]);
                    if (treecbalyear[row, col] <= 0d)
                    {
                        treesurvive[row, col] = 0;
                        treeflag[row, col] = 1;
                    }


                    if (treevol[row, col] <= 0d & treeflag[row, col] == 0)
                    {
                        treesurvive[row, col] = 0;
                        treeflag[row, col] = 1;
                    }


                    if (treesurvive[row, col] == 0)
                        LostTrees += treevol[row, col];

                    if (0.9d * treeht[row, col] < greenht)
                    {
                        treesurvive[row, col] = 0;
                        treeflag[row, col] = 1;
                    }


                    // If alloctree(row, col) = 0 Then treesurvive(row, col) = 0

                    fratio = leafarea[row, col] / sumleafarea;
                    sratio = treevol[row, col] / cumvol;


                    if (treesurvive[row, col] > 0)
                    {
                        if (treevol[row, col] > 0d)
                        {
                            treeWestR = (wf * fratio * 1000d + Wb * fratio * 1000d) / (treevol[row, col] * density * 1000d + Wbk * sratio * 1000d);
                            treeWestW = wf * fratio * 1000d + Wb * fratio * 1000d + treevol[row, col] * density * 1000d + Wbk * sratio * 1000d;
                            treelisthtod[row, col] = 1.3d * Pow(treeWestR, -0.22d) * Pow(treeWestW, -0.136d);
                            double ratechangehtod = 0.02d; // proportion per month
                            if (treelisthtod[row, col] < treelisthtodlast[row, col] * (1d - ratechangehtod))
                            {
                                treelisthtod[row, col] = treelisthtodlast[row, col] * (1d - ratechangehtod);
                            }
                            if (treelisthtod[row, col] > treelisthtodlast[row, col] * (1d + ratechangehtod))
                            {
                                treelisthtod[row, col] = treelisthtodlast[row, col] * (1d + ratechangehtod);
                            }
                        }

                        else
                        {
                            treelisthtod[row, col] = 1d;
                        }
                    }


                    treelisthtodlast[row, col] = treelisthtod[row, col];

                    if (alloctree[row, col] / totalalloc * stemincmonth > 0d)
                    {
                        treehtnow = treesurvive[row, col] * Pow(0.9d * treevol[row, col] * spha * Pow(treelisthtod[row, col] * 200d, 2d * beta3) / (beta1 * Pow(PI * spha, beta3)), 1d / (beta2 + 2d * beta3));
                        treeht[row, col] = treehtnow;
                        treediam[row, col] = treesurvive[row, col] * treeht[row, col] / treelisthtod[row, col] / 0.9d;
                    }

                    else
                    {
                        treeht[row, col] = treehtlast[row, col] * treesurvive[row, col];
                        treediam[row, col] = treediamlast[row, col] * treesurvive[row, col];
                    }


                    treevol[row, col] = treesurvive[row, col] * treevol[row, col] * vol / cumvol; // make sure sum treevol(row,col) here is not different to vol

                    treeht[row, col] = treesurvive[row, col] * Pow(0.9d * treevol[row, col] * spha * Pow(treelisthtod[row, col] * 200d, 2d * beta3) / (beta1 * Pow(PI * spha, beta3)), 1d / (beta2 + 2d * beta3));

                    treediam[row, col] = treesurvive[row, col] * treeht[row, col] / treelisthtod[row, col] / 0.9d;

                    treediamlast[row, col] = treediam[row, col];
                    treehtlast[row, col] = treeht[row, col];


                    newcumvol += treevol[row, col];
                    newcumht += treeht[row, col];
                    newcumdiam += treediam[row, col];

                }
            }


            CUMHT = newcumht;
            cumdiam = newcumdiam;
            cumba = 0d;
            cumvol = newcumvol;
            spha = 0;
            selfThinnedWf += 0d; // wf * leafarea(row, col) / sumleafarea
            selfThinnedWfr += 0d; // Wfr * leafarea(row, col) / sumleafarea
            if (cumvol > 0d)
            {
                LostStemMass += (Wssw + Wshw) * LostTrees / cumvol;
                selfThinnedWssw += Wssw * LostTrees / cumvol;
                selfThinnedWshw += Wshw * LostTrees / cumvol;
                selfThinnedWbk += Wbk * LostTrees / cumvol;
                selfThinnedWb += 0d; // wbswtree(row, col) given no Wf lost then no Wbsw lost
                selfThinnedWcr += Wcr * LostTrees / cumvol;
                selfThinnedWcr += Wcrhw * LostTrees / cumvol; // no fineroots lost so all heartwood
            }

            var loopTo14 = numberows;
            for (row = 1; row <= loopTo14; row++)
            {
                var loopTo15 = numbercols;
                for (col = 1; col <= loopTo15; col++)
                {
                    if (treesurvive[row, col] == 0)
                    {
                        treevol[row, col] = 0d;

                        treeht[row, col] = 0d;
                        treediam[row, col] = 0d;
                        treehtlast[row, col] = 0d;
                        treediamlast[row, col] = 0d;
                        treevol[row, col] = 0d;
                        leafarea[row, col] = 0d;
                        treecrownwidth[row, col] = 0d;
                        treelcrown[row, col] = 0d;
                        areatree[row, col] = 0d;
                        treesurvivelast[row, col] = 0;
                        wswtree[row, col] = 0d;
                        wshwtree[row, col] = 0d;
                        wbktree[row, col] = 0d;
                        wftree[row, col] = 0d;
                        WfrTree[row, col] = 0d;
                        wbswtree[row, col] = 0d;
                        wcrswtree[row, col] = 0d;
                        wcrhwtree[row, col] = 0d;
                        treelcrown[row, col] = 0d;
                        treecbalyear[row, col] = 0d;
                    }

                    spha += treesurvive[row, col];
                    htinclast[row, col] = Max(0d, treeht[row, col] - treehtlast[row, col]);
                    cumba += Pow(treediam[row, col] / 100d / 2d, 2d) * PI;
                    sumcrownl += treelcrown[row, col];
                    treediamlast[row, col] = treediam[row, col];
                    treehtlast[row, col] = treeht[row, col];
                    areatreelast[row, col] = areatree[row, col];
                    leafarealast[row, col] = leafarea[row, col];
                    treelcrownlast[row, col] = treelcrown[row, col];
                    treecrownwidthlast[row, col] = treecrownwidth[row, col];


                }
            }


            if (spha == 0)
            {
                spha = 1;
                effectivestocking = 1d;
                actualstocking = 1d;
            }

            else
            {
                // intraRow = intraRow * Math.Sqrt(effectivestocking) / Math.Sqrt(spha)
                // interRow = interRow * Math.Sqrt(effectivestocking) / Math.Sqrt(spha)
                actualstocking = spha;
                intraRow = 10000d / (interRow * actualstocking);
                effectivestocking = 10000d / (intraRow * interRow);
                effectivestocking = 10000d / (intraRow * interRow);

                actualstocking = calcActualStocking(rowsblock, interRow, interbay, colsblock, intraRow, interblock);

            }


            Meanht = CUMHT / spha;
            Meandiam = cumdiam / spha;


            // ht = Meanht
            // diam = Meandiam
            // ba = cumba
            vol = cumvol;
            vollast[365] = vol;


            LostTrees = 0d;

        }

// ============================================================
// METHOD: areascalene
// Original lines in CCabala.cs: 12795-12819
// ============================================================
        public double areascalene(double distance1, double distance2, double crown1, double crown2)
        {
            double areascaleneRet = default;
            double t1a;
            double t1b;
            double t1c;
            double store;
            double st;


            t1a = distance1 - crown1;
            t1b = distance2 - crown2;
            if (t1a < t1b)
            {
                store = t1a;
                t1a = t1b;
                t1b = store;
            }
            double argx = 45d;
            double argx1 = 45d;
            t1c = Pow(Pow(t1a - t1b * Cos(radians(ref argx)), 2d) + Pow(t1b * Sin(radians(ref argx1)), 2d), 0.5d);
            st = (t1a + t1b + t1c) / 2d;
            areascaleneRet = Pow(st * (st - t1a) * (st - t1b) * (st - t1c), 0.5d);
            return areascaleneRet;
        }

// ============================================================
// METHOD: calcLightLimitedProductiontree
// Original lines in CCabala.cs: 12864-12929
// ============================================================
        private double calcLightLimitedProductiontree(double treefrac, double axtree1, double axtree2, double alpha1, double alpha2, double leaftree, double areatree, double qtree)
        {
            double calcLightLimitedProductiontreeRet = default;
            const double a1 = 0.22d;
            const double b1 = 0.74d;
            const double a2 = -0.18d;
            const double b2 = 0.5d;
            const int m = 0;

            int z;
            double f1;
            double f2;
            double qq;
            double gR;
            double gB;
            double gg;
            double axtree;
            var Acdtree = new double[3];
            double lightfractree;
            double alphatree;

            lightfractree = 1d - Exp(-k * leaftree / areatree);
            Qint = qtree * (1d - Exp(-k * leaftree / areatree));

            z = 1;
            for (z = 1; z <= 2; z++)
            {
                if (z == 1)
                {
                    axtree = axtree1;
                    alphatree = alpha1;
                }
                else
                {
                    axtree = axtree2;
                    alphatree = alpha1;
                }
                qq = PI * k * alphatree * qtree * Gamma / (2d * h * 86400d * (1 - m) * axtree);
                f1 = 1d + a1 * theta * (1d - theta) + b1 * Pow(theta, 2d) * Pow(1d - theta, 2d);
                f2 = a2 * theta + b2 * Pow(theta, 2d) + (1d - a2 - b2) * Pow(theta, 3d);
                if (qq < 1d)
                {
                    gR = 1d - 4d / (PI * Sqrt(1d - Pow(qq, 2d))) * Atan(Sqrt((1d - qq) / (1d + qq)));
                }
                else if (qq == 1d)
                {
                    gR = 1d - 2d / PI;
                }
                else
                {
                    gR = 1d - 2d / (PI * Sqrt(Pow(qq, 2d) - 1d)) * Log((1d + Sqrt((qq - 1d) / (qq + 1d))) / (1d - Sqrt((qq - 1d) / (qq + 1d))));
                }
                if (qq <= 1d)
                {
                    gB = 2d / PI * qq;
                }
                else
                {
                    gB = 1d + 2d / PI * (qq - Sqrt(Pow(qq, 2d) - 1d) - Asin(1d / qq));
                }
                gg = gR * f1 / (1d + f2 * (gR / gB - 1d));
                Acdtree[z] = lightfractree / k * axtree * h * 86400d * gg * 0.000001d;
            }
            calcLightLimitedProductiontreeRet = dmc * (Acdtree[1] + Acdtree[2]) / 2d;  // g/m2
            return calcLightLimitedProductiontreeRet;
        }

// ============================================================
// METHOD: calcActualStocking
// Original lines in CCabala.cs: 13650-13673
// ============================================================
        private double calcActualStocking(double rowsinblock, double interrowdist, double distbetweenbays, double colsinblock, double intrarowdist, double distbetweenblocks)
        {
            double calcActualStockingRet = default;
            double rowunit;
            double RowNo;
            double colunit;
            double colNo;

            if (distbetweenbays == 0d)
                distbetweenbays = interrowdist;

            if (distbetweenblocks == 0d)
                distbetweenblocks = intrarowdist;

            rowunit = (rowsinblock - 1d) * interrowdist + distbetweenbays;
            RowNo = rowsinblock;

            colunit = (colsinblock - 1d) * intrarowdist + distbetweenblocks;
            colNo = colsinblock;

            calcActualStockingRet = RowNo * 100d / rowunit * (colNo * 100d / colunit);
            return calcActualStockingRet;




// ============================================================
// NOTE: Three additional methods required by the above code
// ============================================================

// ============================================================
// METHOD: fAgeMax
// Original lines in CCabala.cs: 2089-2092
// Called by: RowThinningTreatment, withinThinningTreatment
// ============================================================
        private int fAgeMax()
        {
            return (int)Round(Conversion.Int(1d / gammaF * 365d));
        }


// ============================================================
// METHOD: gasdev
// Original lines in CCabala.cs: 9011-9031
// Called by: TreeArraySetUp (normal distribution of initial heights)
// NOTE: Uses VBMath.Rnd() — Visual Basic random number generator.
//       In R, replace with rnorm(1) for standard normal deviate.
// ============================================================
        private double gasdev()
        {
            double gasdevRet = default;

            double r, v1, v2, fac;


            do
            {
                v1 = 2d * (0.5d - VBMath.Rnd(1f));
                v2 = 2d * (0.5d - VBMath.Rnd(1f));
                r = Pow(v1, 2d) + Pow(v2, 2d);
            }
            while (r >= 1d);

            fac = Sqrt(-2 * Log(r) / r);
            gasdevRet = v2 * fac;
            return gasdevRet;

        }



// ============================================================
// METHOD: meanBranchLength
// Original lines in CCabala.cs: 9272-9290
// Called by: meanAllBranchLength (already extracted above)
// ============================================================
        private double meanBranchLength(ref double baX, ref double baY, ref double baZ, ref double btheta, ref double bphi)
        {
            double meanBranchLengthRet = default;
            double BM, bmmZ, bmmX, bmmY, bmm;
            bmmX = Cos(PI * btheta / 180d) * Cos(PI * bphi / 180d) / baX;
            bmmY = Sin(PI * btheta / 180d) / baY;
            bmmZ = Cos(PI * btheta / 180d) * Sin(PI * bphi / 180d) / baZ;
            bmm = Sqrt(Pow(bmmX, 2d) + Pow(bmmY, 2d) + Pow(bmmZ, 2d));
            BM = Sqrt(Pow(bmmX, 2d) + Pow(bmmZ, 2d)) / bmm;
            // Math.Asin(BM). '
            meanBranchLengthRet = 1d / 2d / bmm * (Sqrt(1d - Pow(BM, 2d)) + Asin(BM) / BM);
            return meanBranchLengthRet;
        }


// ════════════════════════════════════════════════════════════════════
// ║  BRANCH GEOMETRY                                                   ║
// ════════════════════════════════════════════════════════════════════

