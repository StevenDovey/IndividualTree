/*

    ITGM.cs

    Implements a representation of a forest stand comprised of stand level
    variables and a tree list. An individual tree growth model is used to
    project the stand thru time.

    See Shula, R.G. SGM Coop reports Nos 58, 59, 60,

    Copyright (c) 2010 New Zealand Forest Research Institute. All rights reserved.

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

using Scion.Common.Utilities;

using Scion.Forecaster.Repository;


namespace Scion.Forecaster.StandGrower
{
    internal sealed class ITGM : IGrowthModel
    {
        #region Constants

        private enum GrowthDirection
        {
            Forward,
            Backward
        }

        // Warning limits
        private const double BasalWarnMin     =    0.0;
        private const double BasalWarnMax     =   99.9;
        private const double StockingWarnMin  =   50.0;
        private const double StockingWarnMax  = 5500.0;
        private const double HeightWarnMin    =    0.0;
        private const double HeightWarnMax    =   99.0;
        private const double SiteIndexWarnMin =    1.5;
        private const double SiteIndexWarnMax =   89.4;
        private const double AgeWarnMin       =    5.0;
        private const double AgeWarnMax       =   80.0;

        private const double MinMortalityAdjustment = -100.0;
        private const double MaxMortalityAdjustment = 100.0;

        private const string Database           = "";
        private const string GeneticGainAllowed = "Not allowed";
        private const string BackwardsGrowth    = "Not allowed";
        private const string HeightModelNo      = "None";
        private const string ITGMDescription    = "Individual Tree Growth Model";

        // Property descriptions
        private const string RegionDescription           = "Region code.";
        private const string NitrogenScoreDescription    = "Nitrogen score [1-7], Hunter et al 1991 index";
        private const string PhosphorousScoreDescription = "Phosphorous score [1-7], Hunter et al 1991 index";
        private const string MortalityDescription        = "Mortality Adjustment percentage [-100 to 100]";
        private const string MonthlyRainFallDescription  = "Monthly rainfall for Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec";

        internal static readonly string[] RegionValues = {
                                                              ModelConstants.GrowthModelNames.SandsName,
                                                              ModelConstants.GrowthModelNames.CLAYSFTName,
                                                              ModelConstants.GrowthModelNames.CNIName,
                                                              ModelConstants.GrowthModelNames.CANTName,
                                                              ModelConstants.GrowthModelNames.HBayName,
                                                              ModelConstants.GrowthModelNames.NelsonName,
                                                              ModelConstants.GrowthModelNames.SouthName,
                                                              ModelConstants.GrowthModelNames.WestlandName
                                                          };

        #endregion

        #region Private member variables:

        private readonly IPublisher _publisher;
        private IProperties _properties;

        #endregion

        public ITGM( IPublisher publisher )
        {
            _publisher = publisher;
            _properties = CreatePropertyBag();
        }

        #region Implementation of ICloneable:

        public object Clone()
        {
            ITGM clone = (ITGM)MemberwiseClone();
            clone._properties = (IProperties)_properties.Clone();
            return clone;
        }

        #endregion

        #region Implementation of IGrowthModel:

        public string Name
        {
            get { return ModelConstants.GrowthModelNames.ITGMName; }
        }

        public string Description
        {
            get { return ITGMDescription; }
        }

        public string Species
        {
            get { return ModelConstants.PropertyValues.SpeciesPRad; }
        }

        public string Details
        {
            get
            {
                string modelDetails =
                    Utils.FormatModelInfomation( Name, Species, Description, Database,
                                                 GeneticGainAllowed, BackwardsGrowth, HeightModelNo ) +
                    Utils.FormatModelLimit( BasalWarnMin, BasalWarnMax,
                                            HeightWarnMin, HeightWarnMax,
                                            StockingWarnMin, StockingWarnMax,
                                            SiteIndexWarnMin, SiteIndexWarnMax,
                                            AgeWarnMin, AgeWarnMax );
                return modelDetails;
            }
        }

        public IProperties Properties
        {
            get { return _properties; }
        }

        public void InitializeStand( DateTime date, double heightAdjustedAge, IModelledCrop crop, IModelledSite site, IRegime regime )
        {
            Utils.CheckLimits( _publisher, this, crop, site, heightAdjustedAge,
                               BasalWarnMin, BasalWarnMax,
                               StockingWarnMin, StockingWarnMax,
                               HeightWarnMin, HeightWarnMax,
                               AgeWarnMin, AgeWarnMax );
            // FIXME: breast height should be a property on growth model and the function set maintainer
            //        should remove it in the getGrowthModelProoperties() and add it in the setData()
            // IAtlasUser user = ServiceManager.getInstance().getSession().getAtlasUser();
            // IAtlasConfig atlasConfig = ServiceManager.getInstance().getCommonEntities().getAtlasConfig();
            // double breastHt = Double.parseDouble(atlasConfig.getProperty(user,"BREAST_HEIGHT"));

            crop.Object = CreateIGMStand( date, heightAdjustedAge, crop, site );
        }

        private IGMStand CreateIGMStand( DateTime date, double heightAdjustedAge, IModelledCrop crop, IModelledSite site )
        {
            // Don't check site index here as it's not used
            // FIXME: J.G. 11/2007 Plant year and plant month look wrong - the date passed is the current date.
            //                     Should use properties PLANT_YEAR and PLANT_MONTH.
            int plantYear = date.Year;
            int plantMonth = date.Month;
            double currentAge = heightAdjustedAge;
            double altitude = site.Altitude;
            int region = GrowthRegion.NameToIndex( _properties[ ModelConstants.PropertyNames.Region ] );
            NPScore nScore = new NPScore( _properties[ ModelConstants.PropertyNames.NitrogenScore ],    "Nitrogen" );
            NPScore pScore = new NPScore( _properties[ ModelConstants.PropertyNames.PhosphorousScore ], "Phosphorus" );
            double mortalityAdjust = 1.0 + Double.Parse( _properties[ ModelConstants.PropertyNames.MortalityAdjust ] ) / 100.0;
            IGMStand igmStand = new IGMStand( crop, region, plantYear, plantMonth, currentAge,
                                              ( nScore.Score ), ( pScore.Score ), altitude, mortalityAdjust );
            MonthlyRainfall rainfall = new MonthlyRainfall( _properties[ ModelConstants.PropertyNames.MonthlyRainFall ] );
            for ( int i = 0; i < 12; i++ )
            {
                igmStand.SetRain( i, rainfall.MonthlyAmount( i + 1 ) );
            }
            return igmStand;
        }

        public void GrowForwardOneYear( DateTime date, double heightAdjustedAge, IModelledCrop crop, IModelledSite site )
        {
            GrowOneYear( date, heightAdjustedAge, crop, site, GrowthDirection.Forward );
        }

        public void GrowBackOneYear( DateTime date, double heightAdjustedAge, IModelledCrop crop, IModelledSite site )
        {
            throw new ApplicationException( " Growth model \"" + Name + "\" cannot grow backwards." );
        }

        #endregion

        #region PropertyBag :

        private static IPropertyBag CreatePropertyBag()
        {
            const string DefaultRegion           = "";
            const string DefaultNitrogenScore    = "";
            const string DefaultPhosphorousScore = "";
            const string DefaultMonthlyRainfall  = "0,0,0,0,0,0,0,0,0,0,0,0";
            const string DefaultMortalityAdjust  = "0.0";

            IList<IPropertySpec> propertySpecs = new List<IPropertySpec> 
            {
                Utilities.NewPropertySpec( ModelConstants.PropertyNames.Region,           typeof( string ), "Model", RegionDescription,           DefaultRegion,           null, typeof( RegionConverter ) ),
                Utilities.NewPropertySpec( ModelConstants.PropertyNames.NitrogenScore,    typeof( string ), "Model", NitrogenScoreDescription,    DefaultNitrogenScore,    null, typeof( NPConverter )     ),
                Utilities.NewPropertySpec( ModelConstants.PropertyNames.PhosphorousScore, typeof( string ), "Model", PhosphorousScoreDescription, DefaultPhosphorousScore, null, typeof( NPConverter )     ),
                Utilities.NewPropertySpec( ModelConstants.PropertyNames.MonthlyRainFall,  typeof( string ), "Model", MonthlyRainFallDescription,  DefaultMonthlyRainfall  ),
                Utilities.NewPropertySpec( ModelConstants.PropertyNames.MortalityAdjust,  typeof( string ), "Model", MortalityDescription,  DefaultMortalityAdjust  )
            };
            IPropertyBag propertyBag = Utilities.NewPropertyBag( propertySpecs, ModelConstants.PropertyNames.MonthlyRainFall );

            propertyBag[ ModelConstants.PropertyNames.Region           ] = DefaultRegion;
            propertyBag[ ModelConstants.PropertyNames.NitrogenScore    ] = DefaultNitrogenScore;
            propertyBag[ ModelConstants.PropertyNames.PhosphorousScore ] = DefaultPhosphorousScore;
            propertyBag[ ModelConstants.PropertyNames.MonthlyRainFall  ] = DefaultMonthlyRainfall;
            propertyBag[ ModelConstants.PropertyNames.MortalityAdjust  ] = DefaultMortalityAdjust;

            return propertyBag;
        }

        #endregion

        #region Private methods :

        private void GrowOneYear( DateTime date, double heightAdjustedAge, IModelledCrop crop, IModelledSite site, GrowthDirection direction )
        {
            Utils.CheckLimits( _publisher, this, crop, site, heightAdjustedAge,
                               BasalWarnMin, BasalWarnMax,
                               StockingWarnMin, StockingWarnMax,
                               HeightWarnMin, HeightWarnMax,
                               AgeWarnMin, AgeWarnMax );

            if ( site.Altitude < 0 || site.Altitude > 900 )
            {
                throw new ApplicationException( string.Format( "Altitude {0} outside ITGM model range of 0-900", site.Altitude ) );
            }
            if ( crop.Object == null )
            {
                // May be null if interpolate or do an event which alters state of crop (e.g. prune or thin).
                crop.Object = CreateIGMStand( date, heightAdjustedAge, crop, site );
            }
            IGMStand igmStand = (IGMStand)crop.Object;
            igmStand.GrowForwardOneYear( crop );
            ModelledCrop modelledCrop = (ModelledCrop)crop;
            modelledCrop.MeanTopHeight = igmStand.MeanTopHeight;
        }

        #endregion
    }

    #region Rainfall property handling

    // [TypeConverter( typeof(RainfallConverter))]
    public class MonthlyRainfall
    {
        private const string RainSeparator = ",";
        private const double MaxRainfall = 500.0;

        private readonly string[] _monthRain;

        public MonthlyRainfall()
        {
            _monthRain = new string[12];
        }

        public MonthlyRainfall( string delimitedString )
        {
            char[] separator = { RainSeparator[ 0 ] };
            _monthRain = delimitedString.Split( separator );
            if ( _monthRain.GetUpperBound( 0 ) != 11 )
            {
                throw new ApplicationException( "Rainfall string must contain a list of 12 values" );
            }
        }

        public double MonthlyAmount( int calendarMonth )
        {
            if ( calendarMonth < 1 || calendarMonth > 12 )
            {
                throw new ApplicationException( "MonthlyRainfall: Calendar month must be 1-12" );
            }
            return Double.Parse( _monthRain[ calendarMonth - 1 ] );
        }

        [DescriptionAttribute( "January rainfall (mm)" )]
        public string Month01
        {
            get { return _monthRain[ 0 ]; }
            set { _monthRain[ 0 ] = ValidRain( value, "January" ); }
        }

        [DescriptionAttribute( "February rainfall (mm)" )]
        public string Month02
        {
            get { return _monthRain[ 1 ]; }
            set { _monthRain[ 1 ] = ValidRain( value, "February" ); }
        }

        [DescriptionAttribute( "March rainfall (mm)" )]
        public string Month03
        {
            get { return _monthRain[ 2 ]; }
            set { _monthRain[ 2 ] = ValidRain( value, "March" ); }
        }

        [DescriptionAttribute( "April rainfall (mm)" )]
        public string Month04
        {
            get { return _monthRain[ 3 ]; }
            set { _monthRain[ 3 ] = ValidRain( value, "April" ); }
        }

        [DescriptionAttribute( "May rainfall (mm)" )]
        public string Month05
        {
            get { return _monthRain[ 4 ]; }
            set { _monthRain[ 4 ] = ValidRain( value, "May" ); }
        }

        [DescriptionAttribute( "June rainfall (mm)" )]
        public string Month06
        {
            get { return _monthRain[ 5 ]; }
            set { _monthRain[ 5 ] = ValidRain( value, "June" ); }
        }

        [DescriptionAttribute( "July rainfall (mm)" )]
        public string Month07
        {
            get { return _monthRain[ 6 ]; }
            set { _monthRain[ 6 ] = ValidRain( value, "July" ); }
        }

        [DescriptionAttribute( "August rainfall (mm)" )]
        public string Month08
        {
            get { return _monthRain[ 7 ]; }
            set { _monthRain[ 7 ] = ValidRain( value, "August" ); }
        }

        [DescriptionAttribute( "September rainfall (mm)" )]
        public string Month09
        {
            get { return _monthRain[ 8 ]; }
            set { _monthRain[ 8 ] = ValidRain( value, "September" ); }
        }

        [DescriptionAttribute( "October rainfall (mm)" )]
        public string Month10
        {
            get { return _monthRain[ 9 ]; }
            set { _monthRain[ 9 ] = ValidRain( value, "October" ); }
        }

        [DescriptionAttribute( "November rainfall (mm)" )]
        public string Month11
        {
            get { return _monthRain[ 10 ]; }
            set { _monthRain[ 10 ] = ValidRain( value, "November" ); }
        }

        [DescriptionAttribute( "December rainfall (mm)" )]
        public string Month12
        {
            get { return _monthRain[ 11 ]; }
            set { _monthRain[ 11 ] = ValidRain( value, "December" ); }
        }

        public override string ToString()
        {
            const string Sep = RainSeparator;
            string s = string.Format( "{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}{17}{18}{19}{20}{21}{22}",
                                      Month01, Sep, Month02, Sep, Month03, Sep, Month04, Sep, Month05, Sep, Month06, Sep,
                                      Month07, Sep, Month08, Sep, Month09, Sep, Month10, Sep, Month11, Sep, Month12 );
            return s;
        }

        public static MonthlyRainfall Parse( string delimitedString )
        {
            char[] separator = { RainSeparator[ 0 ] };
            string[] rain = delimitedString.Split( separator );
            if ( rain.GetUpperBound( 0 ) != 11 )
            {
                throw new ApplicationException( "Rainfall property must contain a list of 12 values" );
            }
            MonthlyRainfall monthlyRain = new MonthlyRainfall();
            monthlyRain.Month01 = rain[ 0 ];
            monthlyRain.Month02 = rain[ 1 ];
            monthlyRain.Month03 = rain[ 2 ];
            monthlyRain.Month04 = rain[ 3 ];
            monthlyRain.Month05 = rain[ 4 ];
            monthlyRain.Month06 = rain[ 5 ];
            monthlyRain.Month07 = rain[ 6 ];
            monthlyRain.Month08 = rain[ 7 ];
            monthlyRain.Month09 = rain[ 8 ];
            monthlyRain.Month10 = rain[ 9 ];
            monthlyRain.Month11 = rain[ 10 ];
            monthlyRain.Month12 = rain[ 11 ];
            return monthlyRain;
        }

        private static string ValidRain( string rainfall, string monthName )
        {
            double dvalue;
            bool isValid = Double.TryParse( rainfall, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out dvalue );
            if ( isValid )
            {
                isValid = dvalue >= 0 && dvalue <= MaxRainfall;
            }
            if ( isValid )
            {
                return rainfall;
            }
            throw new ApplicationException( string.Format( "Invalid rainfall [{0}] for {1}. A number between zero and {2} is required",
                                                           rainfall, monthName, MaxRainfall ) );
        }
    }

    #endregion

    #region N and P score property handling

    internal class NPConverter : StringConverter
    {
        public override bool GetStandardValuesSupported( ITypeDescriptorContext context )
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues( ITypeDescriptorContext context )
        {
            return new StandardValuesCollection( NPScore.NPValueDescriptions );
        }

        public override bool GetStandardValuesExclusive( ITypeDescriptorContext context )
        {
            return true;
        }
    }

    public class NPScore
    {
        public static readonly string[] NPValueDescriptions = {
                                                                    "1 - deficient",
                                                                    "2 - marginal (high probability of deficiency)",
                                                                    "3 - marginal (medium probability of deficiency)",
                                                                    "4 - marginal (low probability of deficiency)",
                                                                    "5 - satisfactory (low probability of deficiency)",
                                                                    "6 - satisfactory (medium probability of deficiency)",
                                                                    "7 - satisfactory (high probability of deficiency)"
                                                              };

        private const int MinNPScore = 0; // FIXME MM Sep 05: Should really be 1 but test code sets to zero!
        private const int MaxNPScore = 7;

        private int _score;
        private string _NorP = "N/P";

        public NPScore( string valueDescription, string typeNorP )
        {
            ValueDescription = valueDescription;
            TypeNorP = typeNorP;
        }

        public string ValueDescription
        {
            get { return NPValueDescriptions[ _score - 1 ]; }
            set { _score = NumericScore( value ); }
        }

        public string TypeNorP
        {
            get { return _NorP; }
            set { _NorP = value; }
        }

        public int Score
        {
            get { return _score; }
            set { _score = NumericScore( value.ToString() ); }
        }

        private int NumericScore( string s )
        {
            if ( s.IndexOf( "-" ) > 0 )
            {
                // value is in form "n - description" so just take the "n" part
                s = s.Substring( 0, s.IndexOf( "-" ) );
            }
            try
            {
                double dValue = double.Parse( s );
                int iValue = (int)dValue;
                if ( iValue == dValue && iValue >= MinNPScore && iValue <= MaxNPScore )
                {
                    return iValue;
                }
            }
            catch {}
            string msg = string.Format( "Invalid {0} score [{1}]. An integer from {2} to {3} is required.",
                                        TypeNorP, s, MinNPScore, MaxNPScore );
            throw new ApplicationException( "ITGM property error: " + msg );
        }
    }

    #endregion

    #region Region property handling

    internal class RegionConverter : StringConverter
    {
        public override bool GetStandardValuesSupported( ITypeDescriptorContext context )
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues( ITypeDescriptorContext context )
        {
            return new StandardValuesCollection( ITGM.RegionValues );
        }

        public override bool GetStandardValuesExclusive( ITypeDescriptorContext context )
        {
            return true;
        }
    }

    public class GrowthRegion
    {
        private string _name;

        public GrowthRegion()
        {
            _name = "";
        }

        [DescriptionAttribute( "Name of region" )]
        public string Name
        {
            get { return _name; }
            set
            {
                if ( NameToIndex( value ) < 0 )
                {
                    throw new ApplicationException( "Region code " + value + " unsupported" );
                }
                _name = value;
            }
        }

        public int Index
        {
            get { return NameToIndex( _name ); }
        }

        public static int NameToIndex( string name )
        {
            for ( int i = 0; i <= IGMStand.RegionNames.GetUpperBound( 0 ); i++ )
            {
                if ( name.ToUpper().Equals( IGMStand.RegionNames[ i ].ToUpper() ) )
                {
                    return i;
                }
            }
            return -1;
        }
    }

    #endregion

    internal class IGMStand : ICloneable
    {
        #region Constants :

        private const int TplotWhole = 0;
        private const int IGMSands = 0;
        private const int IGMClays = 1;
        private const int IGMCni = 2;
        private const int IGMCant = 3;
        private const int IGMHBay = 4;
        private const int IGMNelson = 5;
        private const int IGMSouth = 6;
        private const int IGMWestland = 7;

        public static readonly string[] RegionNames = {
                                                           ModelConstants.GrowthModelNames.SandsName,
                                                           ModelConstants.GrowthModelNames.CLAYSFTName,
                                                           ModelConstants.GrowthModelNames.CNIName,
                                                           ModelConstants.GrowthModelNames.CANTName,
                                                           ModelConstants.GrowthModelNames.HBayName,
                                                           ModelConstants.GrowthModelNames.NelsonName,
                                                           ModelConstants.GrowthModelNames.SouthName,
                                                           ModelConstants.GrowthModelNames.WestlandName
                                                       };

        private static readonly int[] IGMRegionSet = {
                                                           IGMSands, IGMClays, IGMCni, IGMCant, IGMHBay, IGMNelson, IGMSouth, IGMWestland
                                                       };

        private static readonly int[] Months = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        private static readonly int[] Spring = { 8, 9, 10 };
        private static readonly int[] Summer = { 11, 0, 1 };

        private const double PotBaseAge = 20.0;
        private const int TopTrees = 100;

        #endregion

        private double _meanTopHeight;
        private readonly int _region;
        private readonly int _plantYear;
        private readonly int _plantMonth;
        private double _currentAge;
        private readonly double _altitude;
        private readonly double _nScore;
        private readonly double _pScore;
        private readonly double _mortalityAdjust; // adjustment to survival function

        private readonly double[] _rainfall = new double[12];

        public IGMStand( IModelledCrop crop, int region, int plantYear, int plantMonth, double currentAge,
                         double nScore, double pScore, double altitude, double mortalityAdjust )
        {
            _region = region;
            _plantYear = plantYear;
            _plantMonth = plantMonth;
            _currentAge = currentAge;
            _nScore = nScore;
            _pScore = pScore;
            _altitude = altitude;
            _mortalityAdjust = mortalityAdjust;
            for ( int i = 0; i < 12; i++ )
            {
                _rainfall[ i ] = MissingValues.DoubleMissingValue;
            }
        }

        #region Implementation of ICloneable :

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Internal methods :

        internal double MeanTopHeight
        {
            get { return _meanTopHeight; }
        }

        internal void SetRain( int month, double rain )
        {
            if ( ( month < 0 ) || ( month > 11 ) )
            {
                throw new ApplicationException( "IGM Stand: - Month index (" + month + ") out of range (0..11)" );
            }
            _rainfall[ month ] = rain;
        }

        internal void GrowForwardOneYear( IModelledCrop crop )
        {
            double targetAge = _currentAge + 1.0;

            // Check everything needed is set
            string msg = CheckRainfallSet();
            if ( msg != "" )
            {
                throw new ApplicationException( "IGM Stand.growForwardOneYear: error in rainfall: " + msg );
            }

            // Calculate some stand stats from tree list
            double baseN = crop.Stocking;
            double baseG = crop.BasalArea;
            double baseQMD = crop.MeanDBH / 10.0; // for consistency with original implementation which used cm
            double baseMTD = crop.MeanTopDiameter / 10.0; // ditto
            double baseMTH = crop.MeanTopHeight;

            int stemIdx = 0;
            double[] dbhIncs = new double[crop.Stems.Count];
            double[] weightings = new double[crop.Stems.Count];
            foreach ( ModelledStem stem in crop.Stems )
            {
                double stemDBH = stem.DBH;
                double stemHeight = stem.Height;
                double stemWeighting = stem.Weighting;

                double baLarge = crop.BasalAreaAbove( stemDBH );
                // Start with survival (weighting)
                double pSurvival = ProbSurvive( _region, baseN, baseQMD, stemDBH / 10.0, _currentAge, baseG );
                weightings[ stemIdx ] = stemWeighting * Math.Pow( pSurvival, _mortalityAdjust );

                // Now dbh
                dbhIncs[ stemIdx ] = 10.0 * DbhInc( _region, stemDBH / 10.0, stemHeight, baLarge,
                                                    _currentAge, baseN, baseQMD, baseG, baseMTH,
                                                    Rain( Months ), Rain( Spring ), Rain( Summer ),
                                                    _nScore, _pScore );

                // Add predicted increment directly
                stem.Height += DirectIncHeightFn( _region, stemDBH / 10, stemHeight, _currentAge, baLarge, baseMTD );
                if ( stem.Height < 1.4 )
                {
                    stem.Height = 1.4;
                }
                stemIdx++;
            }
            for ( int i = 0; i < stemIdx; i++ )
            {
                ( (ModelledStem)crop.Stems[ i ] ).DBH += dbhIncs[ i ];
                ( (ModelledStem)crop.Stems[ i ] ).Weighting = weightings[ i ];
            }

            double newMTH = crop.MeanTopHeight;
            _meanTopHeight = newMTH;

            _currentAge = targetAge;
        }

        #endregion

        #region Private methods :

        private double Rain( int[] months )
        {
            double rainfall = 0.0;

            for ( int i = 0; i < 12; i++ )
            {
                bool found = false;
                for ( int j = 0; j < months.GetLength( 0 ); j++ )
                {
                    if ( months[ j ] == i )
                    {
                        found = true;
                        break;
                    }
                }
                if ( found )
                {
                    rainfall += _rainfall[ i ];
                }
            }
            return rainfall;
        }

        private static int RienekeSdi( double n, double qmd )
        {
            // dbh in cm
            int rieneke = (int)( 1.0147 * Math.Pow( 10.0, Math.Log10( n ) + 1.605 * Math.Log10( qmd ) - 2.250 ) + 0.5 );
            // can use to guard agains rieneke_sdi <=0, as ln(<=0) DNE
            if ( rieneke <= 0.0 )
            {
                rieneke = 1; //  bound at 1.0 to give SDI= 1
            }
            return rieneke;
        }

        private static double PotentialHt( int region, double ht1, double age1, double age2 )
        {
            const double HpitBh = 1.37;
            double a, b;
            switch ( region )
            {
                case IGMSands:
                    a = 4.05147152;
                    b = -11.35737229;
                    break;

                case IGMClays:
                    a = 4.14133802;
                    b = -26.17231510;
                    break;

                case IGMCni:
                    a = 4.55730091;
                    b = -12.90751890;
                    break;

                case IGMCant:
                    a = 8.645489457;
                    b = -8.758747536;
                    break;

                case IGMHBay:
                    a = 4.67375242;
                    b = -11.00566216;
                    break;

                case IGMNelson:
                    a = 5.011867093;
                    b = -9.641850166;
                    break;

                case IGMSouth:
                    a = 5.26233285;
                    b = -10.23676846;
                    break;

                case IGMWestland:
                    a = 4.05950415;
                    b = -24.80459209;
                    break;

                default:
                    throw new ApplicationException( "IGM Stand: " + region + " - Invalid region" );
            }
            double result = HpitBh + Math.Exp( a + b * Math.Exp( Math.Log( ( Math.Log( ht1 - HpitBh ) - a ) / b )
                                                                 * Math.Log( age2 ) / Math.Log( age1 ) ) );
            return result;
        }

        private static double ChangePdbh( int region, double dbh1, double age1, double age2 )
        {
            double a, b;
            switch ( region )
            {
                case IGMSands:
                    a = 7.585688848;
                    b = -7.090152348;
                    break;

                case IGMClays:
                    a = 6.76974764;
                    b = -17.99991912;
                    break;

                case IGMCni:
                    a = 7.443019620;
                    b = -5.591267444;
                    break;

                case IGMCant:
                    a = 8.767677736;
                    b = -5.913716578;
                    break;

                case IGMHBay:
                    a = 7.454444083;
                    b = -9.625008349;
                    break;

                case IGMNelson:
                    a = 7.645297489;
                    b = -7.652838205;
                    break;

                case IGMSouth:
                    a = 7.81591067;
                    b = -10.50216636;
                    break;

                case IGMWestland:
                    a = 6.90926642;
                    b = -16.60303168;
                    break;

                default:
                    throw new ApplicationException( "IGM Stand: " + region + " - Invalid region" );
            }

            double pdbh2 = (int)( Math.Exp( a + b * Math.Exp( Math.Log( ( Math.Log( dbh1 * 10.0 ) - a ) / b ) *
                                                              Math.Log( age2 ) / Math.Log( age1 ) ) ) + 0.5 );
            return ( pdbh2 - dbh1 * 10.0 ) / 10.0; //to cm
        }

        private static double ChangePMTD( int region, double dbh1, double age1, double age2 )
        {
            const double PmtdBound = 0.001;
            double a, b;
            switch ( region )
            {
                case IGMSands:
                    a = 3.917162149;
                    b = -5.435622138;
                    break;

                case IGMClays:
                    a = 4.09046176;
                    b = -10.46210647;
                    break;

                case IGMCni:
                    a = b = MissingValues.DoubleMissingValue;
                    break;

                case IGMCant:
                    a = b = MissingValues.DoubleMissingValue;
                    break;

                case IGMHBay:
                    a = 4.327007014;
                    b = -6.790573715;
                    break;

                case IGMNelson:
                    a = 4.423091661;
                    b = -8.345960217;
                    break;

                case IGMSouth:
                    a = b = MissingValues.DoubleMissingValue;
                    break;

                case IGMWestland:
                    a = b = MissingValues.DoubleMissingValue;
                    break;

                default:
                    throw new ApplicationException( "IGM Stand: " + region + " - Invalid region" );
            }
            double part = ( Math.Log( dbh1 ) - a ) / b;
            if ( part < PmtdBound )
            {
                part = PmtdBound;
            }
            double pdbh2 = Math.Exp( a + b * Math.Exp( Math.Log( part ) * Math.Log( age2 ) / Math.Log( age1 ) ) );
            return pdbh2 - dbh1;
        }

        private static double CalcProb( double a0, double a1, double a2, double a3, double a4,
                                        int region, double n, double qmd, double dbh, double age )
        {
            return 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Log( RienekeSdi( n, qmd ) )
                                           + a2 * ChangePMTD( region, dbh, age, age + 1 )
                                           + a3 * Math.Pow( dbh / qmd, a4 ) ) );
        }

        private static double ProbSurvive( int region, double n, double qmd, double dbh, double age, double g )
        {
            double a0, a1, a2, a3, a4;
            double prob;

            switch ( region )
            {
                case IGMSands:
                    a0 = -10.72208175;
                    a1 = 1.52145029;
                    a2 = 0.16317220;
                    a3 = -4.60461630;
                    a4 = 1.13832131;
                    prob = CalcProb( a0, a1, a2, a3, a4, region, n, qmd, dbh, age );
                    break;

                case IGMClays:
                    a0 = -22.75898099;
                    a1 = 3.12334159;
                    a2 = 0.28486687;
                    a3 = -5.71097836;
                    a4 = 2.13913197;
                    prob = CalcProb( a0, a1, a2, a3, a4, region, n, qmd, dbh, age );
                    break;

                case IGMCni:
                    a0 = -8.787965743;
                    a1 = 1.477285949;
                    a2 = -0.292327055;
                    a3 = -4.529255275;
                    a4 = 1.194661794;
                    prob = 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Log( RienekeSdi( n, qmd ) )
                                                   + a2 * Math.Sqrt( dbh )
                                                   + a3 * Math.Pow( dbh / qmd, a4 ) ) );
                    break;

                case IGMCant:
                    prob = 1.0;
                    break;

                case IGMHBay:
                    a0 = -10.28405957;
                    a1 = 1.24749751;
                    a2 = -0.03177930;
                    a3 = -3.87188406;
                    a4 = 2.13966506;
                    prob = 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Log( RienekeSdi( n, qmd ) )
                                                   + a2 * ChangePMTD( region, dbh, age, age + 1 )
                                                   + a3 * Math.Pow( dbh / qmd, a4 ) ) );
                    break;

                case IGMNelson:
                    a0 = -2.661304797;
                    a1 = 0.069701572;
                    a2 = -1.068531758;
                    a3 = -3.032364523;
                    a4 = 1.772830767;
                    prob = 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Sqrt( RienekeSdi( n, qmd ) )
                                                   + a2 * ChangePMTD( region, dbh, age, age + 1 )
                                                   + a3 * Math.Pow( dbh / qmd, a4 ) ) );
                    break;

                case IGMSouth: // REMEMBER: SET RAINFALL TO 78MM FOR EACH MONTH
                    a0 = -11.47471960;
                    a1 = 3.63960522;
                    a2 = -0.56252998;
                    a3 = -6.36901793;
                    prob = 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Log( g )
                                                   + a2 * Math.Log( qmd ) + a3 * dbh / qmd ) );
                    break;

                case IGMWestland:
                    // NB use the changePMTD function for Nelson
                    a0 = -1.828739986;
                    a1 = 0.041866658;
                    a2 = -1.318999009;
                    a3 = -2.936535781;
                    a4 = 2.055335111;
                    prob = 1.0 / ( 1.0 + Math.Exp( a0 + a1 * Math.Sqrt( RienekeSdi( n, qmd ) )
                                                   + a2 * ChangePMTD( IGMNelson, dbh, age, age + 1 )
                                                   + a3 * Math.Pow( dbh / qmd, a4 ) ) );
                    break;
                default:
                    throw new ApplicationException( "IGM Stand: " + region + " - Invalid region" );
            }

            if ( ( prob <= 0 ) || ( prob > 1 ) )
            {
                string msg = string.Format( "IGM Stand: region {0} - Bad survival probability {1}", region, prob );
                throw new ApplicationException( msg );
            }

            return prob;
        }

        private static double DbhInc( int region, double dbh, double ht, double balarge, double age, double n,
                                      double qmd, double g, double mth, double rainTotal, double rainSpring,
                                      double rainSummer, double nitrogen, double phosphorus )
        {
            double a0, a1, a2, a3, a4;
            double chgpdbh;
            double inc;

            switch ( region )
            {
                case IGMSands:
                    a0 = 6.995867585;
                    a1 = 0.001265804;
                    a2 = 0.024236242;
                    a3 = 0.022952765;
                    a4 = -1.096183884;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * chgpdbh * chgpdbh * 100.0
                                    + a2 * PotentialHt( region, ht, age, PotBaseAge )
                                    + a3 * nitrogen * phosphorus * Math.Log( dbh * 10 )
                                    + a4 * Math.Log( RienekeSdi( n, qmd ) ) );
                    break;

                case IGMClays:
                    a0 = 1.10359482;
                    a1 = -34.79649743;
                    a2 = 0.02933947;
                    a3 = 0.08881261;
                    a4 = 0.43573804;
                    double relspace = 1000.0 / ( mth * Math.Sqrt( n ) );
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * ( balarge / ( dbh * 10 ) ) * ( balarge / ( dbh * 10 ) )
                                    + a2 * nitrogen
                                    + a3 * relspace * relspace
                                    + a4 * Math.Log( chgpdbh * 10 ) );
                    break;

                case IGMCni:
                    a0 = 5.62029648;
                    a1 = 0.23624032;
                    a2 = -20.33089162;
                    a3 = -0.65340901;
                    a4 = 0.02907101;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * Math.Log( chgpdbh * chgpdbh * 100.0 )
                                    + a2 * ( balarge / ( dbh * 10 ) ) * ( balarge / ( dbh * 10.0 ) )
                                    + a3 * Math.Log( RienekeSdi( n, qmd ) )
                                    + a4 * ( n / 100.0 ) * Math.Log( ( dbh / qmd ) * ( dbh / qmd ) ) );
                    break;

                case IGMCant:
                    a0 = 3.70985227;
                    a1 = 0.00219193;
                    a2 = -28.09253115;
                    a3 = -0.27954330;
                    a4 = 0.17432788;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * chgpdbh * chgpdbh * 100.0
                                    + a2 * ( balarge / ( dbh * 10 ) ) * ( balarge / ( dbh * 10.0 ) )
                                    + a3 * Math.Log( RienekeSdi( n, qmd ) )
                                    + a4 * rainTotal / ( dbh * 10.0 ) );
                    break;

                case IGMHBay:
                    a0 = 1.873821009;
                    a1 = 0.659312226;
                    a2 = -5.290432331;
                    a3 = -0.324759500;
                    a4 = -0.686514067;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    double rdStnd = (int)( g / Math.Sqrt( qmd ) + 0.5 );
                    if ( rdStnd <= 0.0 )
                    {
                        rdStnd = 1.0; // bound at 1.0 to give rdStnd= 1
                    }
                    inc = Math.Exp( a0 + a1 * Math.Log( chgpdbh * chgpdbh * 100.0 )
                                    + a2 * balarge / ( dbh * 10.0 )
                                    + a3 * Math.Log( rdStnd )
                                    + a4 * Math.Log( PotentialHt( region, ht, age, PotBaseAge ) ) );
                    break;

                case IGMNelson:
                    a0 = 3.031587923;
                    a1 = 0.395219905;
                    a2 = -8.879682909;
                    a3 = -0.199956549;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * Math.Log( chgpdbh * chgpdbh * 100.0 )
                                    + a2 * ( balarge / ( dbh * 10.0 ) ) * ( balarge / ( dbh * 10.0 ) )
                                    + a3 * Math.Log( RienekeSdi( n, qmd ) * RienekeSdi( n, qmd ) ) );
                    break;

                case IGMSouth:
                    a0 = 6.10742954;
                    a1 = 0.27822652;
                    a2 = -21.04187356;
                    a3 = -0.77824359;
                    a4 = 1.42534529;
                    inc = Math.Exp( a0 + a1 * Math.Log( PotentialHt( region, ht, age, PotBaseAge ) )
                                    + a2 * ( balarge / ( dbh * 10.0 ) ) * ( balarge / ( dbh * 10.0 ) )
                                    + a3 * Math.Log( RienekeSdi( n, qmd ) )
                                    + a4 * Math.Sqrt( ( rainSpring + rainSummer ) / 1000.0 ) );
                    break;

                case IGMWestland:
                    a0 = 0.58929599;
                    a1 = 0.41212971;
                    a2 = -28.33830927;
                    a3 = -0.00412158;
                    chgpdbh = ChangePdbh( region, dbh, age, age + 1.0 );
                    inc = Math.Exp( a0 + a1 * Math.Log( chgpdbh * chgpdbh * 100.0 )
                                    + a2 * ( balarge / ( dbh * 10.0 ) ) * ( balarge / ( dbh * 10.0 ) )
                                    + a3 * Math.Log( RienekeSdi( n, qmd ) * RienekeSdi( n, qmd ) ) );
                    break;

                default:
                    throw new ApplicationException( "IGM Stand: " + region + " - Invalid region" );
            }

            return inc / 10.0;
        }

        private static double DirectIncHeightFn( int region, double dbh, double ht,
                                                 double ageOfHt, double baLarge, double mtd )
        {
            double b0;
            double b1 = 0.029945726;
            double b2 = -0.738212634;
            double b3 = 0.029422499;
            double b4 = -0.367425918;
            double b5 = 0.0;
            switch ( region )
            {
                case IGMSands:
                    b0 = -1.464467613;
                    break;

                case IGMClays:
                    b0 = -1.196043518;
                    break;

                case IGMCni:
                    b0 = -0.977934668;
                    break;

                case IGMCant:
                    b0 = -1.299877303;
                    break;

                case IGMHBay:
                    b0 = -1.070528892;
                    break;

                case IGMNelson:
                    b0 = -1.115954513;
                    break;

                case IGMSouth:
                    b0 = -1.145538682;
                    break;

                case IGMWestland: // fitted seperately from other regions
                    b0 = -1.07448940;
                    b1 = 0.0;
                    b2 = -1.05357063;
                    b3 = 0.03915247;
                    b4 = -11.07650424;
                    b5 = 0.02288922;
                    break;

                default:
                    throw new ApplicationException( "IGM Stand: Invalid region: " + region );
            }
            double t1 = b1 * Math.Log( dbh * 10.0 ) * Math.Log( dbh * 10.0 );
            double t2 = b2 * ht * ht / 1000.0;
            double t3 = b3 * PotentialHt( region, ht, ageOfHt, PotBaseAge );
            double t4 = b4 * baLarge / ( dbh * 10.0 );
            double t5 = b5 * mtd;
            double t = b0 + t1 + t2 + t3 + t4 + t5;
            return Math.Exp( t );
        }

        private string CheckRainfallSet()
        {
            for ( int i = 0; i < 12; i++ )
            {
                if ( MissingValues.IsMissingValue( _rainfall[ i ] ) )
                {
                    return "Monthly rainfall not set for month " + i;
                }
            }
            return "";
        }

        private const string DecimalPlaces0 = "{0:0;#0}";
        private const string DecimalPlaces1 = "{0:0.0;#0.0}";
        private const string DecimalPlaces2 = "{0:0.00;#0.00}";
        private const string DecimalPlaces3 = "{0:0.000;#0.000}";

        private void Print( IModelledCrop crop )
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine( "Stand" );

                Console.WriteLine( "Region: " + _region );
                Console.WriteLine( "Plant Year: " + _plantYear );
                Console.WriteLine( "Plant Month: " + _plantMonth );
                Console.WriteLine( "Current Age: " + _currentAge );
                Console.WriteLine( "Nitrogen: " + _nScore );
                Console.WriteLine( "Phosphorus: " + _pScore );
                Console.WriteLine( "Altitude: " + _altitude );
                Console.WriteLine( "Mortality: " + _mortalityAdjust );

                for ( int i = 0; i < 12; i++ )
                {
                    Console.WriteLine( "Rainfall: " + _rainfall[ i ] );
                }

                Console.WriteLine( "N: " + crop.Stocking );
                Console.WriteLine( "G: " + crop.BasalArea );
                Console.WriteLine( "QMD: " + crop.MeanDBH / 10.0 ); // cm for consistency with original
                Console.WriteLine( "MTD: " + crop.MeanTopDiameter / 10.0 ); // ditto
                Console.WriteLine( "MTH: " + crop.MeanTopHeight );

                int tree = 0;
                foreach ( IModelledStem stem in crop.Stems )
                {
                    Console.WriteLine(
                                      string.Format( "Tree {0}", tree ) + ":\t" +
                                      string.Format( DecimalPlaces1, stem.DBH / 10.0 ) + "\t\t" +
                                      string.Format( DecimalPlaces1, stem.Height / 1000.0 ) + "\t\t" +
                                      string.Format( DecimalPlaces3, stem.Weighting ) );
                    tree++;
                }
            }
            catch {}
        }

        #endregion
    }
}