// This file contains a derived work based on routines and computations from the
// Naval Observatory Vector Astrometry Software (NOVAS), C Edition, developed by the
// U.S. Naval Observatory. NOVAS is public domain U.S. government work product.
// See https://aa.usno.navy.mil/software/novas/novas_info.php for the original software.
//
// Ported from NOVAS C Edition, Version 3.1: novascon.c (all physical/astronomical
// constants) and the "utility" layer of novas.c (vector math, coordinate-frame
// transforms, precession, aberration, light-time/deflection, refraction, and the
// object/observer constructor functions).

using System;

namespace NINA.Astrometry {

    public static partial class ManagedNovas {

        #region "novascon.c - Constants for use with NOVAS"

        /// <summary>TDB Julian date of epoch J2000.0.</summary>
        private const double T0 = 2451545.00000000;

        /// <summary>Speed of light in meters/second is a defining physical constant.</summary>
        private const double C = 299792458.0;

        /// <summary>Light-time for one astronomical unit (AU) in seconds, from DE-405.</summary>
        private const double AU_SEC = 499.0047838061;

        /// <summary>Speed of light in AU/day. Value is 86400 / AU_SEC.</summary>
        private const double C_AUDAY = 173.1446326846693;

        /// <summary>Astronomical unit in meters. Value is AU_SEC * C.</summary>
        private const double AU = 1.4959787069098932e+11;

        /// <summary>Astronomical Unit in kilometers.</summary>
        private const double AU_KM = 1.4959787069098932e+8;

        /// <summary>Heliocentric gravitational constant in meters^3 / second^2, from DE-405.</summary>
        private const double GS = 1.32712440017987e+20;

        /// <summary>Geocentric gravitational constant in meters^3 / second^2, from DE-405.</summary>
        private const double GE = 3.98600433e+14;

        /// <summary>Radius of Earth in meters from IERS Conventions (2003).</summary>
        private const double ERAD = 6378136.6;

        /// <summary>Earth ellipsoid flattening from IERS Conventions (2003). Value is 1 / 298.25642.</summary>
        private const double F = 0.003352819697896;

        /// <summary>Rotational angular velocity of Earth in radians/sec from IERS Conventions (2003).</summary>
        private const double ANGVEL = 7.2921150e-5;

        /// <summary>
        /// Reciprocal masses of solar system bodies, from DE-405 (Sun mass / body mass).
        /// RMASS[0] = Earth/Moon barycenter, RMASS[1] = Mercury, ..., RMASS[9] = Pluto,
        /// RMASS[10] = Sun, RMASS[11] = Moon.
        /// </summary>
        private static readonly double[] RMASS = {
            328900.561400, 6023600.0, 408523.71,
            332946.050895, 3098708.0, 1047.3486, 3497.898, 22902.98,
            19412.24, 135200000.0, 1.0, 27068700.387534
        };

        /// <summary>Value of 2 * pi in radians.</summary>
        private const double TWOPI = 6.283185307179586476925287;

        /// <summary>Number of arcseconds in 360 degrees.</summary>
        private const double ASEC360 = 1296000.0;

        private const double ASEC2RAD = 4.848136811095359935899141e-6;
        private const double DEG2RAD = 0.017453292519943296;
        private const double RAD2DEG = 57.295779513082321;

        // From novas.h: SIZE_OF_OBJ_NAME = 51, SIZE_OF_CAT_NAME = 4 (each includes the
        // null terminator in the C source; used below only as the C#-equivalent maximum
        // string lengths, i.e. SIZE_OF_OBJ_NAME - 1 = 50 and SIZE_OF_CAT_NAME - 1 = 3).

        /// <summary>
        /// Celestial pole offsets for high-precision applications, set via <see cref="CelPole"/>
        /// and consumed by <see cref="ETilt"/>. Mirrors the file-static globals 'PSI_COR' and
        /// 'EPS_COR' at the top of the real novas.c.
        /// </summary>
        private static double _psiCor = 0.0;
        private static double _epsCor = 0.0;

        #endregion

        #region "cal_date / julian_date / norm_ang / era"

        /// <summary>
        /// Computes a date on the Gregorian calendar given the Julian date. C version: 'cal_date'.
        /// </summary>
        public static void CalDate(double tjd, ref short year, ref short month, ref short day, ref double hour) {
            double djd = tjd + 0.5;
            long jd = (long)djd;

            hour = (djd % 1.0) * 24.0;

            long k = jd + 68569L;
            long n = 4L * k / 146097L;

            k = k - (146097L * n + 3L) / 4L;
            long m = 4000L * (k + 1L) / 1461001L;
            k = k - 1461L * m / 4L + 31L;

            month = (short)(80L * k / 2447L);
            day = (short)(k - 2447L * (long)month / 80L);
            k = (long)month / 11L;

            month = (short)((long)month + 2L - 12L * k);
            year = (short)(100L * (n - 49L) + m + k);
        }

        /// <summary>
        /// Computes the Julian date for a given calendar date (year, month, day, hour).
        /// C version: 'julian_date'.
        /// </summary>
        public static double JulianDate(short year, short month, short day, double hour) {
            long jd12h = (long)day - 32075L + 1461L * ((long)year + 4800L
                + ((long)month - 14L) / 12L) / 4L
                + 367L * ((long)month - 2L - ((long)month - 14L) / 12L * 12L)
                / 12L - 3L * (((long)year + 4900L + ((long)month - 14L) / 12L)
                / 100L) / 4L;

            double tjd = (double)jd12h - 0.5 + hour / 24.0;

            return tjd;
        }

        /// <summary>Normalize angle into the range 0 &lt;= angle &lt; (2 * pi). C version: 'norm_ang'.</summary>
        public static double NormAng(double angle) {
            double a = angle % TWOPI;
            if (a < 0.0) {
                a += TWOPI;
            }
            return a;
        }

        /// <summary>
        /// Returns the value of the Earth Rotation Angle (theta) for a given UT1 Julian date,
        /// in degrees. C version: 'era'.
        /// </summary>
        public static double Era(double jdHigh, double jdLow) {
            double thet1 = 0.7790572732640 + 0.00273781191135448 * (jdHigh - T0);
            double thet2 = 0.00273781191135448 * jdLow;
            double thet3 = (jdHigh % 1.0) + (jdLow % 1.0);

            double theta = ((thet1 + thet2 + thet3) % 1.0) * 360.0;
            if (theta < 0.0) {
                theta += 360.0;
            }

            return theta;
        }

        #endregion

        #region "tdb2tt / mean_obliq / fund_args / ee_ct / e_tilt / cel_pole / ira_equinox"

        /// <summary>
        /// Computes the Terrestrial Time (TT) Julian date corresponding to a Barycentric
        /// Dynamical Time (TDB) Julian date. C version: 'tdb2tt'.
        /// </summary>
        public static void Tdb2Tt(double tdbJd, ref double ttJd, ref double secdiff) {
            double t = (tdbJd - T0) / 36525.0;

            secdiff = 0.001657 * Math.Sin(628.3076 * t + 6.2401)
                + 0.000022 * Math.Sin(575.3385 * t + 4.2970)
                + 0.000014 * Math.Sin(1256.6152 * t + 6.1969)
                + 0.000005 * Math.Sin(606.9777 * t + 4.0212)
                + 0.000005 * Math.Sin(52.9691 * t + 0.4444)
                + 0.000002 * Math.Sin(21.3299 * t + 5.5431)
                + 0.000010 * t * Math.Sin(628.3076 * t + 4.2490);

            ttJd = tdbJd - secdiff / 86400.0;
        }

        /// <summary>Computes the mean obliquity of the ecliptic, in arcseconds. C version: 'mean_obliq'.</summary>
        public static double MeanObliq(double jdTdb) {
            double t = (jdTdb - T0) / 36525.0;

            double epsilon = ((((-0.0000000434 * t
                                 - 0.000000576) * t
                                 + 0.00200340) * t
                                 - 0.0001831) * t
                                 - 46.836769) * t + 84381.406;

            return epsilon;
        }

        /// <summary>
        /// Argument coefficients for the 't^0' term of the 'complementary terms' series.
        /// From novas.c 'ee_ct'.
        /// </summary>
        private static readonly short[,] _eeCt_ke0T = {
            {0,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  0,  0,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2, -2,  3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2, -2,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2, -2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2,  0,  3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  0,  0,  3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1,  0,  0, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  0,  0, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1,  2, -2,  3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1,  2, -2,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  4, -4,  4,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  1, -1,  1,  0, -8, 12,  0,  0,  0,  0,  0,  0},
            {0,  0,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2,  0,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  2,  0,  3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  2,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2, -2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1, -2,  2, -3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1, -2,  2, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  0,  0,  0,  0,  8,-13,  0,  0,  0,  0,  0, -1},
            {0,  0,  0,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {2,  0, -2,  0, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  0, -2,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  1,  2, -2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0,  0, -2, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  4, -2,  4,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {0,  0,  2, -2,  4,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0, -2,  0, -3,  0,  0,  0,  0,  0,  0,  0,  0,  0},
            {1,  0, -2,  0, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0}
        };

        /// <summary>Argument coefficients for the 't^1' term. From novas.c 'ee_ct'.</summary>
        private static readonly short[] _eeCt_ke1 = { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>Sine and cosine coefficients for the 't^0' term. From novas.c 'ee_ct'.</summary>
        private static readonly double[,] _eeCt_se0T = {
            {+2640.96e-6,          -0.39e-6},
            {  +63.52e-6,          -0.02e-6},
            {  +11.75e-6,          +0.01e-6},
            {  +11.21e-6,          +0.01e-6},
            {   -4.55e-6,          +0.00e-6},
            {   +2.02e-6,          +0.00e-6},
            {   +1.98e-6,          +0.00e-6},
            {   -1.72e-6,          +0.00e-6},
            {   -1.41e-6,          -0.01e-6},
            {   -1.26e-6,          -0.01e-6},
            {   -0.63e-6,          +0.00e-6},
            {   -0.63e-6,          +0.00e-6},
            {   +0.46e-6,          +0.00e-6},
            {   +0.45e-6,          +0.00e-6},
            {   +0.36e-6,          +0.00e-6},
            {   -0.24e-6,          -0.12e-6},
            {   +0.32e-6,          +0.00e-6},
            {   +0.28e-6,          +0.00e-6},
            {   +0.27e-6,          +0.00e-6},
            {   +0.26e-6,          +0.00e-6},
            {   -0.21e-6,          +0.00e-6},
            {   +0.19e-6,          +0.00e-6},
            {   +0.18e-6,          +0.00e-6},
            {   -0.10e-6,          +0.05e-6},
            {   +0.15e-6,          +0.00e-6},
            {   -0.14e-6,          +0.00e-6},
            {   +0.14e-6,          +0.00e-6},
            {   -0.14e-6,          +0.00e-6},
            {   +0.14e-6,          +0.00e-6},
            {   +0.13e-6,          +0.00e-6},
            {   -0.11e-6,          +0.00e-6},
            {   +0.11e-6,          +0.00e-6},
            {   +0.11e-6,          +0.00e-6}
        };

        /// <summary>Sine and cosine coefficients for the 't^1' term. From novas.c 'ee_ct'.</summary>
        private static readonly double[] _eeCt_se1 = { -0.87e-6, +0.00e-6 };

        /// <summary>
        /// Computes the "complementary terms" of the equation of the equinoxes, in radians.
        /// C version: 'ee_ct'.
        /// </summary>
        public static double EeCt(double jdHigh, double jdLow, short accuracy) {
            double[] fa = new double[14];
            double[] fa2 = new double[5];
            double cTerms;

            double t = ((jdHigh - T0) + jdLow) / 36525.0;

            if (accuracy == 0) {
                // Fundamental Arguments.
                fa[0] = NormAng((485868.249036 +
                                 (715923.2178 +
                                 (31.8792 +
                                 (0.051635 +
                                 (-0.00024470))
                                 * t) * t) * t) * t) * ASEC2RAD
                                 + (1325.0 * t % 1.0) * TWOPI;

                fa[1] = NormAng((1287104.793048 +
                                 (1292581.0481 +
                                 (-0.5532 +
                                 (+0.000136 +
                                 (-0.00001149))
                                 * t) * t) * t) * t) * ASEC2RAD
                                 + (99.0 * t % 1.0) * TWOPI;

                fa[2] = NormAng((335779.526232 +
                                 (295262.8478 +
                                 (-12.7512 +
                                 (-0.001037 +
                                 (0.00000417))
                                 * t) * t) * t) * t) * ASEC2RAD
                                 + (1342.0 * t % 1.0) * TWOPI;

                fa[3] = NormAng((1072260.703692 +
                                 (1105601.2090 +
                                 (-6.3706 +
                                 (0.006593 +
                                 (-0.00003169))
                                 * t) * t) * t) * t) * ASEC2RAD
                                 + (1236.0 * t % 1.0) * TWOPI;

                fa[4] = NormAng((450160.398036 +
                                 (-482890.5431 +
                                 (7.4722 +
                                 (0.007702 +
                                 (-0.00005939))
                                 * t) * t) * t) * t) * ASEC2RAD
                                 + (-5.0 * t % 1.0) * TWOPI;

                fa[5] = NormAng(4.402608842 + 2608.7903141574 * t);
                fa[6] = NormAng(3.176146697 + 1021.3285546211 * t);
                fa[7] = NormAng(1.753470314 + 628.3075849991 * t);
                fa[8] = NormAng(6.203480913 + 334.0612426700 * t);
                fa[9] = NormAng(0.599546497 + 52.9690962641 * t);
                fa[10] = NormAng(0.874016757 + 21.3299104960 * t);
                fa[11] = NormAng(5.481293872 + 7.4781598567 * t);
                fa[12] = NormAng(5.311886287 + 3.8133035638 * t);
                fa[13] = (0.024381750 + 0.00000538691 * t) * t;

                double s0 = 0.0;
                double s1 = 0.0;

                for (int i = 32; i >= 0; i--) {
                    double a = 0.0;
                    for (int j = 0; j < 14; j++) {
                        a += (double)_eeCt_ke0T[i, j] * fa[j];
                    }
                    s0 += (_eeCt_se0T[i, 0] * Math.Sin(a) + _eeCt_se0T[i, 1] * Math.Cos(a));
                }

                {
                    double a = 0.0;
                    for (int j = 0; j < 14; j++) {
                        a += (double)_eeCt_ke1[j] * fa[j];
                    }
                    s1 += (_eeCt_se1[0] * Math.Sin(a) + _eeCt_se1[1] * Math.Cos(a));
                }

                cTerms = (s0 + s1 * t);
            } else {
                // Low accuracy mode: terms smaller than 2 microarcseconds omitted.
                FundArgs(t, fa2);
                cTerms =
                    2640.96e-6 * Math.Sin(fa2[4])
                    + 63.52e-6 * Math.Sin(2.0 * fa2[4])
                    + 11.75e-6 * Math.Sin(2.0 * fa2[2] - 2.0 * fa2[3] + 3.0 * fa2[4])
                    + 11.21e-6 * Math.Sin(2.0 * fa2[2] - 2.0 * fa2[3] + fa2[4])
                    - 4.55e-6 * Math.Sin(2.0 * fa2[2] - 2.0 * fa2[3] + 2.0 * fa2[4])
                    + 2.02e-6 * Math.Sin(2.0 * fa2[2] + 3.0 * fa2[4])
                    + 1.98e-6 * Math.Sin(2.0 * fa2[2] + fa2[4])
                    - 1.72e-6 * Math.Sin(3.0 * fa2[4])
                    - 0.87e-6 * t * Math.Sin(fa2[4]);
            }

            cTerms *= ASEC2RAD;
            return cTerms;
        }

        private static short _eTilt_accuracyLast = 0;
        private static double _eTilt_jdLast = 0.0;
        private static double _eTilt_dp = 0.0;
        private static double _eTilt_de = 0.0;
        private static double _eTilt_cTerms = 0.0;

        /// <summary>
        /// Computes quantities related to the orientation of the Earth's rotation axis at
        /// Julian date 'jdTdb'. C version: 'e_tilt'. Calls into the nutation-series port
        /// (<c>NutationAngles</c>) expected in ManagedNovas.NutationAndCio.cs.
        /// </summary>
        public static void ETilt(double jdTdb, short accuracy, ref double mobl, ref double tobl, ref double ee, ref double dpsi, ref double deps) {
            double t = (jdTdb - T0) / 36525.0;

            short accDiff = (short)(accuracy - _eTilt_accuracyLast);

            if ((Math.Abs(jdTdb - _eTilt_jdLast) > 1.0e-8) || (accDiff != 0)) {
                NutationAngles(t, accuracy, out _eTilt_dp, out _eTilt_de);

                _eTilt_cTerms = EeCt(jdTdb, 0.0, accuracy) / ASEC2RAD;

                _eTilt_jdLast = jdTdb;
                _eTilt_accuracyLast = accuracy;
            }

            double dPsi = _eTilt_dp + _psiCor;
            double dEps = _eTilt_de + _epsCor;

            double meanOb = MeanObliq(jdTdb);

            double trueOb = meanOb + dEps;

            meanOb /= 3600.0;
            trueOb /= 3600.0;

            double eqEq = dPsi * Math.Cos(meanOb * DEG2RAD) + _eTilt_cTerms;
            eqEq /= 15.0;

            dpsi = dPsi;
            deps = dEps;
            ee = eqEq;
            mobl = meanOb;
            tobl = trueOb;
        }

        /// <summary>
        /// Specifies celestial pole offsets for high-precision applications. C version: 'cel_pole'.
        /// </summary>
        public static short CelPole(double tjd, short type, double dpole1, double dpole2) {
            short error = 0;

            switch (type) {
                case 1:
                    _psiCor = dpole1 * 1.0e-3;
                    _epsCor = dpole2 * 1.0e-3;
                    break;

                case 2: {
                    double dx = dpole1;
                    double dy = dpole2;

                    double t = (tjd - T0) / 36525.0;

                    double meanOb = MeanObliq(tjd);
                    double sinE = Math.Sin(meanOb * ASEC2RAD);

                    double x = (2004.190 * t) * ASEC2RAD;
                    double dz = -(x + 0.5 * x * x * x) * dx;

                    double[] dp1 = new double[3];
                    double[] dp2 = new double[3];
                    double[] dp3 = new double[3];

                    dp1[0] = dx * 1.0e-3 * ASEC2RAD;
                    dp1[1] = dy * 1.0e-3 * ASEC2RAD;
                    dp1[2] = dz * 1.0e-3 * ASEC2RAD;

                    FrameTie(dp1, 1, dp2);
                    Precession(T0, dp2, tjd, dp3);

                    _psiCor = (dp3[0] / sinE) / ASEC2RAD;
                    _epsCor = dp3[1] / ASEC2RAD;
                    break;
                }

                default:
                    error = 1;
                    break;
            }

            return error;
        }

        #endregion

        #region "radec2vector / vector2radec / starvectors / proper_motion / rad_vel"

        /// <summary>
        /// Converts equatorial spherical coordinates to a vector (equatorial rectangular
        /// coordinates). C version: 'radec2vector'.
        /// </summary>
        public static void Radec2Vector(double ra, double dec, double dist, double[] vector) {
            vector[0] = dist * Math.Cos(DEG2RAD * dec) * Math.Cos(DEG2RAD * 15.0 * ra);
            vector[1] = dist * Math.Cos(DEG2RAD * dec) * Math.Sin(DEG2RAD * 15.0 * ra);
            vector[2] = dist * Math.Sin(DEG2RAD * dec);
        }

        /// <summary>
        /// Converts a vector in equatorial rectangular coordinates to equatorial spherical
        /// coordinates. C version: 'vector2radec'.
        /// </summary>
        public static short Vector2Radec(double[] pos, ref double ra, ref double dec) {
            double xyproj = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1]);

            if ((xyproj == 0.0) && (pos[2] == 0)) {
                ra = 0.0;
                dec = 0.0;
                return 1;
            } else if (xyproj == 0.0) {
                ra = 0.0;
                dec = pos[2] < 0.0 ? -90.0 : 90.0;
                return 2;
            } else {
                ra = Math.Atan2(pos[1], pos[0]) / ASEC2RAD / 54000.0;
                dec = Math.Atan2(pos[2], xyproj) / ASEC2RAD / 3600.0;

                if (ra < 0.0) {
                    ra += 24.0;
                }
            }

            return 0;
        }

        /// <summary>Converts angular quantities for stars to vectors. C version: 'starvectors'.</summary>
        public static void Starvectors(NOVAS.CatalogueEntry star, double[] pos, double[] vel) {
            double paralx = star.Parallax;

            if (star.Parallax <= 0.0) {
                paralx = 1.0e-6;
            }

            double dist = 1.0 / Math.Sin(paralx * 1.0e-3 * ASEC2RAD);
            double r = star.RA * 15.0 * DEG2RAD;
            double d = star.Dec * DEG2RAD;
            double cra = Math.Cos(r);
            double sra = Math.Sin(r);
            double cdc = Math.Cos(d);
            double sdc = Math.Sin(d);

            pos[0] = dist * cdc * cra;
            pos[1] = dist * cdc * sra;
            pos[2] = dist * sdc;

            double k = 1.0 / (1.0 - star.RadialVelocity / C * 1000.0);

            double pmr = star.ProMoRA / (paralx * 365.25) * k;
            double pmd = star.ProMoDec / (paralx * 365.25) * k;
            double rvl = star.RadialVelocity * 86400.0 / AU_KM * k;

            vel[0] = -pmr * sra - pmd * sdc * cra + rvl * cdc * cra;
            vel[1] = pmr * cra - pmd * sdc * sra + rvl * cdc * sra;
            vel[2] = pmd * cdc + rvl * sdc;
        }

        /// <summary>
        /// Applies proper motion, including foreshortening effects, to a star's position.
        /// C version: 'proper_motion'.
        /// </summary>
        public static void ProperMotion(double jdTdb1, double[] pos, double[] vel, double jdTdb2, double[] pos2) {
            for (int j = 0; j < 3; j++) {
                pos2[j] = pos[j] + (vel[j] * (jdTdb2 - jdTdb1));
            }
        }

        private static bool _radVel_firstCall = true;
        private static double _radVel_c2;
        private static double _radVel_toms;
        private static double _radVel_toms2;

        /// <summary>
        /// Predicts the radial velocity of the observed object as it would be measured by
        /// spectroscopic means, in km/s. C version: 'rad_vel'.
        /// </summary>
        public static void RadVel(NOVAS.CelestialObject celObject, double[] pos, double[] vel, double[] velObs, double dObsGeo, double dObsSun, double dObjSun, ref double rv) {
            if (_radVel_firstCall) {
                _radVel_c2 = C * C;
                _radVel_toms = AU / 86400.0;
                _radVel_toms2 = _radVel_toms * _radVel_toms;

                _radVel_firstCall = false;
            }

            double[] v = new double[3];
            for (int i = 0; i < 3; i++) {
                v[i] = vel[i];
            }

            double ra, dec, radvel;

            switch (celObject.Type) {
                case 2:
                    ra = celObject.Star.RA;
                    dec = celObject.Star.Dec;
                    radvel = celObject.Star.RadialVelocity;

                    if (celObject.Star.Parallax <= 0.0) {
                        for (int i = 0; i < 3; i++) {
                            v[i] = 0.0;
                        }
                    }
                    break;

                default:
                    ra = 0.0;
                    dec = 0.0;
                    radvel = 0.0;
                    break;
            }

            double posmag = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]);

            double[] uk = new double[3];
            for (int i = 0; i < 3; i++) {
                uk[i] = pos[i] / posmag;
            }

            double v2 = (v[0] * v[0] + v[1] * v[1] + v[2] * v[2]) * _radVel_toms2;
            double vo2 = (velObs[0] * velObs[0] + velObs[1] * velObs[1] + velObs[2] * velObs[2]) * _radVel_toms2;

            double r = dObsGeo * AU;
            double phigeo = r > 1.0e6 ? GE / r : 0.0;

            r = dObsSun * AU;
            double phisun = r > 1.0e8 ? GS / r : 0.0;

            double rel;
            if ((dObsGeo != 0.0) || (dObsSun != 0.0)) {
                rel = 1.0 - (phigeo + phisun) / _radVel_c2 - 0.5 * vo2 / _radVel_c2;
            } else {
                rel = 1.0 - 1.550e-8;
            }

            double zobs1;

            switch (celObject.Type) {
                case 2: {
                    double rar = ra * 15.0 * DEG2RAD;
                    double dcr = dec * DEG2RAD;
                    double cosdec = Math.Cos(dcr);
                    double[] du = new double[3];
                    du[0] = uk[0] - (cosdec * Math.Cos(rar));
                    du[1] = uk[1] - (cosdec * Math.Sin(rar));
                    du[2] = uk[2] - Math.Sin(dcr);
                    double zc = radvel * 1.0e3 +
                        (v[0] * du[0] + v[1] * du[1] + v[2] * du[2]) * _radVel_toms;

                    double zb1 = 1.0 + zc / C;
                    double kvobs = (uk[0] * velObs[0] + uk[1] * velObs[1] + uk[2] * velObs[2]) * _radVel_toms;
                    zobs1 = zb1 * rel / (1.0 + kvobs / C);
                    break;
                }

                case 0:
                case 1:
                default: {
                    r = dObjSun * AU;
                    if ((r > 1.0e8) && (r < 1.0e16)) {
                        phisun = GS / r;
                    } else {
                        phisun = 0.0;
                    }

                    double kv = (uk[0] * vel[0] + uk[1] * vel[1] + uk[2] * vel[2]) * _radVel_toms;
                    double zb1 = (1.0 + kv / C) / (1.0 - phisun / _radVel_c2 - 0.5 * v2 / _radVel_c2);
                    double kvobs = (uk[0] * velObs[0] + uk[1] * velObs[1] + uk[2] * velObs[2]) * _radVel_toms;
                    zobs1 = zb1 * rel / (1.0 + kvobs / C);
                    break;
                }
            }

            rv = (zobs1 - 1.0) * C / 1000.0;
        }

        #endregion

        #region "bary2obs / d_light / grav_def / grav_vec / aberration"

        /// <summary>
        /// Moves the origin of coordinates from the solar system barycenter to the observer
        /// (or the geocenter). C version: 'bary2obs'.
        /// </summary>
        public static void Bary2Obs(double[] pos, double[] posObs, double[] pos2, ref double lighttime) {
            for (int j = 0; j < 3; j++) {
                pos2[j] = pos[j] - posObs[j];
            }

            lighttime = Math.Sqrt(pos2[0] * pos2[0] + pos2[1] * pos2[1] + pos2[2] * pos2[2]) / C_AUDAY;
        }

        /// <summary>
        /// Returns the difference in light-time, for a star, between the barycenter of the solar
        /// system and the observer (or the geocenter), in days. C version: 'd_light'.
        /// </summary>
        public static double DLight(double[] pos1, double[] posObs) {
            double dis = Math.Sqrt(pos1[0] * pos1[0] + pos1[1] * pos1[1] + pos1[2] * pos1[2]);

            double[] u1 = new double[3];
            u1[0] = pos1[0] / dis;
            u1[1] = pos1[1] / dis;
            u1[2] = pos1[2] / dis;

            double diflt = (posObs[0] * u1[0] + posObs[1] * u1[1] + posObs[2] * u1[2]) / C_AUDAY;

            return diflt;
        }

        /// <summary>
        /// Corrects a position vector for the deflection of light in the gravitational field of
        /// an arbitrary body. C version: 'grav_vec'. Not explicitly listed in scope, but required
        /// by <see cref="GravDef"/> and self-contained (pure vector math, no nutation/ephemeris
        /// dependency), so included here per the "prefer including small self-contained utilities"
        /// guidance.
        /// </summary>
        public static void GravVec(double[] pos1, double[] posObs, double[] posBody, double rmass, double[] pos2) {
            double[] pq = new double[3];
            double[] pe = new double[3];

            for (int i = 0; i < 3; i++) {
                pq[i] = posObs[i] + pos1[i] - posBody[i];
                pe[i] = posObs[i] - posBody[i];
            }

            double pmag = Math.Sqrt(pos1[0] * pos1[0] + pos1[1] * pos1[1] + pos1[2] * pos1[2]);
            double emag = Math.Sqrt(pe[0] * pe[0] + pe[1] * pe[1] + pe[2] * pe[2]);
            double qmag = Math.Sqrt(pq[0] * pq[0] + pq[1] * pq[1] + pq[2] * pq[2]);

            double[] phat = new double[3];
            double[] ehat = new double[3];
            double[] qhat = new double[3];

            for (int i = 0; i < 3; i++) {
                phat[i] = pos1[i] / pmag;
                ehat[i] = pe[i] / emag;
                qhat[i] = pq[i] / qmag;
            }

            double pdotq = phat[0] * qhat[0] + phat[1] * qhat[1] + phat[2] * qhat[2];
            double edotp = ehat[0] * phat[0] + ehat[1] * phat[1] + ehat[2] * phat[2];
            double qdote = qhat[0] * ehat[0] + qhat[1] * ehat[1] + qhat[2] * ehat[2];

            if (Math.Abs(edotp) > 0.99999999999) {
                for (int i = 0; i < 3; i++) {
                    pos2[i] = pos1[i];
                }
            } else {
                double fac1 = 2.0 * GS / (C * C * emag * AU * rmass);
                double fac2 = 1.0 + qdote;

                for (int i = 0; i < 3; i++) {
                    double p2i = phat[i] + fac1 * (pdotq * ehat[i] - edotp * qhat[i]) / fac2;
                    pos2[i] = p2i * pmag;
                }
            }
        }

        private static bool _gravDef_firstTime = true;
        private static short _gravDef_nbodiesLast = 0;
        private static readonly NOVAS.CelestialObject[] _gravDef_body = new NOVAS.CelestialObject[7];
        private static NOVAS.CelestialObject _gravDef_earth;

        /// <summary>
        /// Computes the total gravitational deflection of light for the observed object due to
        /// the major gravitating bodies in the solar system. C version: 'grav_def'. Calls into
        /// the ephemeris/orchestration port (<c>Ephemeris</c>) expected in
        /// ManagedNovas.Transforms.cs.
        /// </summary>
        public static short GravDef(double jdTdb, short locCode, short accuracy, double[] pos1, double[] posObs, double[] pos2) {
            string[] bodyName = { "Sun", "Jupiter", "Saturn", "Moon", "Venus", "Uranus", "Neptune" };
            short[] bodyNum = { 10, 5, 6, 11, 2, 7, 8 };

            short error = 0;

            double[] jd = new double[2];
            jd[1] = 0.0;

            for (int i = 0; i < 3; i++) {
                pos2[i] = pos1[i];
            }

            short nbodies = accuracy == 0 ? (short)3 : (short)1;

            if (_gravDef_firstTime || (nbodies != _gravDef_nbodiesLast)) {
                NOVAS.CatalogueEntry dummyStar = default;

                for (int i = 0; i < nbodies; i++) {
                    if (i == 0) {
                        MakeCatEntry("dummy", "   ", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, ref dummyStar);
                        MakeObject(0, 3, "Earth", dummyStar, ref _gravDef_earth);
                    }

                    if ((error = MakeObject(0, bodyNum[i], bodyName[i], dummyStar, ref _gravDef_body[i])) != 0) {
                        return (short)(error + 30);
                    }
                }

                _gravDef_firstTime = false;
                _gravDef_nbodiesLast = nbodies;
            }

            double tlt = Math.Sqrt(pos1[0] * pos1[0] + pos1[1] * pos1[1] + pos1[2] * pos1[2]) / C_AUDAY;

            for (int i = 0; i < nbodies; i++) {
                double[] pbody = new double[3];
                double[] vbody = new double[3];
                double[] pbodyo = new double[3];

                jd[0] = jdTdb;
                if (Ephemeris(jd, _gravDef_body[i], 0, accuracy, pbody, vbody) != 0) {
                    // solsys3.c (the no-external-file solar system solution this managed port
                    // uses instead of the real solsys1.c/JPL-ephemeris build - see project
                    // documentation) only provides Sun and Earth positions, not Jupiter/Saturn.
                    // The real Windows build can compute all three bodies' gravitational light
                    // deflection contributions; this port can only ever compute the Sun's (by far
                    // the dominant term - Jupiter/Saturn deflection is sub-milliarcsecond except
                    // extremely close to their own limbs). Skip this body's contribution rather
                    // than failing the whole calculation - deliberate accuracy/portability
                    // tradeoff, not a bug.
                    continue;
                }

                double lighttimeUnused = 0.0;
                Bary2Obs(pbody, posObs, pbodyo, ref lighttimeUnused);

                double dlt = DLight(pos2, pbodyo);

                double tclose = jdTdb;

                if (dlt > 0.0) {
                    tclose = jdTdb - dlt;
                }

                if (tlt < dlt) {
                    tclose = jdTdb - tlt;
                }

                jd[0] = tclose;
                if (Ephemeris(jd, _gravDef_body[i], 0, accuracy, pbody, vbody) != 0) {
                    continue;
                }

                GravVec(pos2, posObs, pbody, RMASS[bodyNum[i]], pos2);
            }

            error = 0;

            if (locCode != 0) {
                double[] pbody = new double[3];
                double[] vbody = new double[3];

                jd[0] = jdTdb;
                if ((error = Ephemeris(jd, _gravDef_earth, 0, accuracy, pbody, vbody)) != 0) {
                    return error;
                }

                GravVec(pos2, posObs, pbody, RMASS[3], pos2);
            }

            return error;
        }

        /// <summary>Corrects a position vector for aberration of light. C version: 'aberration'.</summary>
        public static void Aberration(double[] pos, double[] ve, double lighttime, double[] pos2) {
            double p1mag;

            if (lighttime == 0.0) {
                p1mag = Math.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]);
                lighttime = p1mag / C_AUDAY;
            } else {
                p1mag = lighttime * C_AUDAY;
            }

            double vemag = Math.Sqrt(ve[0] * ve[0] + ve[1] * ve[1] + ve[2] * ve[2]);
            double beta = vemag / C_AUDAY;
            double dot = pos[0] * ve[0] + pos[1] * ve[1] + pos[2] * ve[2];

            double cosd = dot / (p1mag * vemag);
            double gammai = Math.Sqrt(1.0 - beta * beta);
            double p = beta * cosd;
            double q = (1.0 + p / (1.0 + gammai)) * lighttime;
            double r = 1.0 + p;

            pos2[0] = (gammai * pos[0] + q * ve[0]) / r;
            pos2[1] = (gammai * pos[1] + q * ve[1]) / r;
            pos2[2] = (gammai * pos[2] + q * ve[2]) / r;
        }

        #endregion

        #region "precession / nutation / frame_tie"

        private static bool _precession_firstTime = true;
        private static double _precession_tLast = 0.0;
        private static double _precession_xx, _precession_yx, _precession_zx;
        private static double _precession_xy, _precession_yy, _precession_zy;
        private static double _precession_xz, _precession_yz, _precession_zz;

        /// <summary>
        /// Precesses equatorial rectangular coordinates from one epoch to another. One of the
        /// two epochs must be J2000.0. C version: 'precession'.
        /// </summary>
        public static short Precession(double jdTdb1, double[] pos1, double jdTdb2, double[] pos2) {
            short error = 0;

            if ((jdTdb1 != T0) && (jdTdb2 != T0)) {
                return (error = 1);
            }

            double t = (jdTdb2 - jdTdb1) / 36525.0;

            if (jdTdb2 == T0) {
                t = -t;
            }

            if ((Math.Abs(t - _precession_tLast) >= 1.0e-15) || _precession_firstTime) {
                double eps0 = 84381.406;

                double psia = ((((-0.0000000951 * t
                             + 0.000132851) * t
                             - 0.00114045) * t
                             - 1.0790069) * t
                             + 5038.481507) * t;

                double omegaa = ((((+0.0000003337 * t
                             - 0.000000467) * t
                             - 0.00772503) * t
                             + 0.0512623) * t
                             - 0.025754) * t + eps0;

                double chia = ((((-0.0000000560 * t
                             + 0.000170663) * t
                             - 0.00121197) * t
                             - 2.3814292) * t
                             + 10.556403) * t;

                eps0 = eps0 * ASEC2RAD;
                psia = psia * ASEC2RAD;
                omegaa = omegaa * ASEC2RAD;
                chia = chia * ASEC2RAD;

                double sa = Math.Sin(eps0);
                double ca = Math.Cos(eps0);
                double sb = Math.Sin(-psia);
                double cb = Math.Cos(-psia);
                double sc = Math.Sin(-omegaa);
                double cc = Math.Cos(-omegaa);
                double sd = Math.Sin(chia);
                double cd = Math.Cos(chia);

                _precession_xx = cd * cb - sb * sd * cc;
                _precession_yx = cd * sb * ca + sd * cc * cb * ca - sa * sd * sc;
                _precession_zx = cd * sb * sa + sd * cc * cb * sa + ca * sd * sc;
                _precession_xy = -sd * cb - sb * cd * cc;
                _precession_yy = -sd * sb * ca + cd * cc * cb * ca - sa * cd * sc;
                _precession_zy = -sd * sb * sa + cd * cc * cb * sa + ca * cd * sc;
                _precession_xz = sb * sc;
                _precession_yz = -sc * cb * ca - sa * cc;
                _precession_zz = -sc * cb * sa + cc * ca;

                _precession_tLast = t;
                _precession_firstTime = false;
            }

            if (jdTdb2 == T0) {
                pos2[0] = _precession_xx * pos1[0] + _precession_xy * pos1[1] + _precession_xz * pos1[2];
                pos2[1] = _precession_yx * pos1[0] + _precession_yy * pos1[1] + _precession_yz * pos1[2];
                pos2[2] = _precession_zx * pos1[0] + _precession_zy * pos1[1] + _precession_zz * pos1[2];
            } else {
                pos2[0] = _precession_xx * pos1[0] + _precession_yx * pos1[1] + _precession_zx * pos1[2];
                pos2[1] = _precession_xy * pos1[0] + _precession_yy * pos1[1] + _precession_zy * pos1[2];
                pos2[2] = _precession_xz * pos1[0] + _precession_yz * pos1[1] + _precession_zz * pos1[2];
            }

            return (error = 0);
        }

        private static bool _frameTie_computeMatrix = true;
        private static double _frameTie_xx, _frameTie_yx, _frameTie_zx;
        private static double _frameTie_xy, _frameTie_yy, _frameTie_zy;
        private static double _frameTie_xz, _frameTie_yz, _frameTie_zz;

        /// <summary>
        /// Transforms a vector between the dynamical reference system (mean equator and equinox
        /// of J2000.0) and the ICRS. C version: 'frame_tie'.
        /// </summary>
        public static void FrameTie(double[] pos1, short direction, double[] pos2) {
            const double xi0 = -0.0166170;
            const double eta0 = -0.0068192;
            const double da0 = -0.01460;

            if (_frameTie_computeMatrix) {
                _frameTie_xx = 1.0;
                _frameTie_yx = -da0 * ASEC2RAD;
                _frameTie_zx = xi0 * ASEC2RAD;
                _frameTie_xy = da0 * ASEC2RAD;
                _frameTie_yy = 1.0;
                _frameTie_zy = eta0 * ASEC2RAD;
                _frameTie_xz = -xi0 * ASEC2RAD;
                _frameTie_yz = -eta0 * ASEC2RAD;
                _frameTie_zz = 1.0;

                _frameTie_xx = 1.0 - 0.5 * (_frameTie_yx * _frameTie_yx + _frameTie_zx * _frameTie_zx);
                _frameTie_yy = 1.0 - 0.5 * (_frameTie_yx * _frameTie_yx + _frameTie_zy * _frameTie_zy);
                _frameTie_zz = 1.0 - 0.5 * (_frameTie_zy * _frameTie_zy + _frameTie_zx * _frameTie_zx);

                _frameTie_computeMatrix = false;
            }

            if (direction < 0) {
                pos2[0] = _frameTie_xx * pos1[0] + _frameTie_yx * pos1[1] + _frameTie_zx * pos1[2];
                pos2[1] = _frameTie_xy * pos1[0] + _frameTie_yy * pos1[1] + _frameTie_zy * pos1[2];
                pos2[2] = _frameTie_xz * pos1[0] + _frameTie_yz * pos1[1] + _frameTie_zz * pos1[2];
            } else {
                pos2[0] = _frameTie_xx * pos1[0] + _frameTie_xy * pos1[1] + _frameTie_xz * pos1[2];
                pos2[1] = _frameTie_yx * pos1[0] + _frameTie_yy * pos1[1] + _frameTie_yz * pos1[2];
                pos2[2] = _frameTie_zx * pos1[0] + _frameTie_zy * pos1[1] + _frameTie_zz * pos1[2];
            }
        }

        #endregion

        #region "ecl2equ_vec / equ2ecl_vec / equ2ecl / equ2gal / equ2hor"

        private static double _equ2EclVec_tLast = 0.0;
        private static double _equ2EclVec_ob2000 = 0.0;
        private static double _equ2EclVec_oblm;
        private static double _equ2EclVec_oblt;

        /// <summary>Converts an equatorial position vector to an ecliptic position vector. C version: 'equ2ecl_vec'.</summary>
        public static short Equ2EclVec(double jdTt, short coordSys, short accuracy, double[] pos1, double[] pos2) {
            short error = 0;

            double t = 0.0, secdiff = 0.0;
            Tdb2Tt(jdTt, ref t, ref secdiff);
            double jdTdb = jdTt + secdiff / 86400.0;

            double[] pos0 = new double[3];
            double obl;

            switch (coordSys) {
                case 0:
                case 1:
                    pos0[0] = pos1[0];
                    pos0[1] = pos1[1];
                    pos0[2] = pos1[2];
                    if (Math.Abs(jdTt - _equ2EclVec_tLast) > 1.0e-8) {
                        double x = 0.0, y = 0.0, z = 0.0;
                        ETilt(jdTdb, accuracy, ref _equ2EclVec_oblm, ref _equ2EclVec_oblt, ref x, ref y, ref z);
                        _equ2EclVec_tLast = jdTt;
                    }

                    switch (coordSys) {
                        case 0:
                            obl = _equ2EclVec_oblm * DEG2RAD;
                            break;
                        case 1:
                            obl = _equ2EclVec_oblt * DEG2RAD;
                            break;
                        default:
                            obl = 0.0;
                            break;
                    }
                    break;

                case 2:
                    FrameTie(pos1, 1, pos0);

                    if (_equ2EclVec_ob2000 == 0.0) {
                        double w = 0.0, x = 0.0, y = 0.0, z = 0.0;
                        ETilt(T0, accuracy, ref _equ2EclVec_oblm, ref w, ref x, ref y, ref z);
                        _equ2EclVec_ob2000 = _equ2EclVec_oblm;
                    }
                    obl = _equ2EclVec_ob2000 * DEG2RAD;
                    break;

                default:
                    return (error = 1);
            }

            pos2[0] = pos0[0];
            pos2[1] = pos0[1] * Math.Cos(obl) + pos0[2] * Math.Sin(obl);
            pos2[2] = -pos0[1] * Math.Sin(obl) + pos0[2] * Math.Cos(obl);

            return error;
        }

        private static double _ecl2EquVec_tLast = 0.0;
        private static double _ecl2EquVec_ob2000 = 0.0;
        private static double _ecl2EquVec_oblm;
        private static double _ecl2EquVec_oblt;

        /// <summary>Converts an ecliptic position vector to an equatorial position vector. C version: 'ecl2equ_vec'.</summary>
        public static short Ecl2EquVec(double jdTt, short coordSys, short accuracy, double[] pos1, double[] pos2) {
            short error = 0;

            double t = 0.0, secdiff = 0.0;
            Tdb2Tt(jdTt, ref t, ref secdiff);
            double jdTdb = jdTt + secdiff / 86400.0;

            double obl = 0.0;

            switch (coordSys) {
                case 0:
                case 1:
                    if (Math.Abs(jdTt - _ecl2EquVec_tLast) > 1.0e-8) {
                        double x = 0.0, y = 0.0, z = 0.0;
                        ETilt(jdTdb, accuracy, ref _ecl2EquVec_oblm, ref _ecl2EquVec_oblt, ref x, ref y, ref z);
                        _ecl2EquVec_tLast = jdTt;
                    }

                    switch (coordSys) {
                        case 0:
                            obl = _ecl2EquVec_oblm * DEG2RAD;
                            break;
                        case 1:
                            obl = _ecl2EquVec_oblt * DEG2RAD;
                            break;
                    }
                    break;

                case 2:
                    if (_ecl2EquVec_ob2000 == 0.0) {
                        double w = 0.0, x = 0.0, y = 0.0, z = 0.0;
                        ETilt(T0, accuracy, ref _ecl2EquVec_oblm, ref w, ref x, ref y, ref z);
                        _ecl2EquVec_ob2000 = _ecl2EquVec_oblm;
                    }
                    obl = _ecl2EquVec_ob2000 * DEG2RAD;
                    break;

                default:
                    return (error = 1);
            }

            pos2[0] = pos1[0];
            pos2[1] = pos1[1] * Math.Cos(obl) - pos1[2] * Math.Sin(obl);
            pos2[2] = pos1[1] * Math.Sin(obl) + pos1[2] * Math.Cos(obl);

            if (coordSys == 2) {
                double[] pos0 = { pos2[0], pos2[1], pos2[2] };
                FrameTie(pos0, -1, pos2);
            }

            return error;
        }

        /// <summary>Converts right ascension and declination to ecliptic longitude and latitude. C version: 'equ2ecl'.</summary>
        public static short Equ2Ecl(double jdTt, short coordSys, short accuracy, double ra, double dec, ref double elon, ref double elat) {
            short error;

            double r = ra * 15.0 * DEG2RAD;
            double d = dec * DEG2RAD;
            double[] pos1 = { Math.Cos(d) * Math.Cos(r), Math.Cos(d) * Math.Sin(r), Math.Sin(d) };
            double[] pos2 = new double[3];

            if ((error = Equ2EclVec(jdTt, coordSys, accuracy, pos1, pos2)) != 0) {
                return error;
            }

            double xyproj = Math.Sqrt(pos2[0] * pos2[0] + pos2[1] * pos2[1]);

            double e = xyproj > 0.0 ? Math.Atan2(pos2[1], pos2[0]) : 0.0;

            elon = e * RAD2DEG;
            if (elon < 0.0) {
                elon += 360.0;
            }

            e = Math.Atan2(pos2[2], xyproj);
            elat = e * RAD2DEG;

            return error;
        }

        /// <summary>Converts ICRS right ascension and declination to galactic longitude and latitude. C version: 'equ2gal'.</summary>
        public static void Equ2Gal(double rai, double deci, ref double glon, ref double glat) {
            double[,] ag = {
                {-0.0548755604, +0.4941094279, -0.8676661490},
                {-0.8734370902, -0.4448296300, -0.1980763734},
                {-0.4838350155, +0.7469822445, +0.4559837762}
            };

            double r = rai * 15.0 * DEG2RAD;
            double d = deci * DEG2RAD;
            double[] pos1 = { Math.Cos(d) * Math.Cos(r), Math.Cos(d) * Math.Sin(r), Math.Sin(d) };

            double[] pos2 = new double[3];
            pos2[0] = ag[0, 0] * pos1[0] + ag[1, 0] * pos1[1] + ag[2, 0] * pos1[2];
            pos2[1] = ag[0, 1] * pos1[0] + ag[1, 1] * pos1[1] + ag[2, 1] * pos1[2];
            pos2[2] = ag[0, 2] * pos1[0] + ag[1, 2] * pos1[1] + ag[2, 2] * pos1[2];

            double xyproj = Math.Sqrt(pos2[0] * pos2[0] + pos2[1] * pos2[1]);

            double g = xyproj > 0.0 ? Math.Atan2(pos2[1], pos2[0]) : 0.0;

            glon = g * RAD2DEG;
            if (glon < 0.0) {
                glon += 360.0;
            }

            g = Math.Atan2(pos2[2], xyproj);
            glat = g * RAD2DEG;
        }

        /// <summary>
        /// Transforms topocentric right ascension and declination to zenith distance and azimuth,
        /// optionally applying atmospheric refraction. C version: 'equ2hor'.
        /// </summary>
        public static void Equ2Hor(double jdUt1, double deltaT, short accuracy, double xp, double yp, ref NOVAS.OnSurface location, double ra, double dec, short refOption, ref double zd, ref double az, ref double rar, ref double decr) {
            rar = ra;
            decr = dec;

            double sinlat = Math.Sin(location.Latitude * DEG2RAD);
            double coslat = Math.Cos(location.Latitude * DEG2RAD);
            double sinlon = Math.Sin(location.Longitude * DEG2RAD);
            double coslon = Math.Cos(location.Longitude * DEG2RAD);
            double sindc = Math.Sin(dec * DEG2RAD);
            double cosdc = Math.Cos(dec * DEG2RAD);
            double sinra = Math.Sin(ra * 15.0 * DEG2RAD);
            double cosra = Math.Cos(ra * 15.0 * DEG2RAD);

            double[] uze = { coslat * coslon, coslat * sinlon, sinlat };
            double[] une = { -sinlat * coslon, -sinlat * sinlon, coslat };
            double[] uwe = { sinlon, -coslon, 0.0 };

            double[] uz = new double[3];
            double[] un = new double[3];
            double[] uw = new double[3];

            Ter2Cel(jdUt1, 0.0, deltaT, 1, accuracy, 1, xp, yp, uze, uz);
            Ter2Cel(jdUt1, 0.0, deltaT, 1, accuracy, 1, xp, yp, une, un);
            Ter2Cel(jdUt1, 0.0, deltaT, 1, accuracy, 1, xp, yp, uwe, uw);

            double[] p = { cosdc * cosra, cosdc * sinra, sindc };

            double pz = p[0] * uz[0] + p[1] * uz[1] + p[2] * uz[2];
            double pn = p[0] * un[0] + p[1] * un[1] + p[2] * un[2];
            double pw = p[0] * uw[0] + p[1] * uw[1] + p[2] * uw[2];

            double proj = Math.Sqrt(pn * pn + pw * pw);

            if (proj > 0.0) {
                az = -Math.Atan2(pw, pn) * RAD2DEG;
            }

            if (az < 0.0) {
                az += 360.0;
            }

            if (az >= 360.0) {
                az -= 360.0;
            }

            zd = Math.Atan2(proj, pz) * RAD2DEG;

            if (refOption != 0) {
                double zd0 = zd;
                double zd1;
                double refr;

                do {
                    zd1 = zd;
                    refr = Refract(ref location, refOption, zd);
                    zd = zd0 - refr;
                } while (Math.Abs(zd - zd1) > 3.0e-5);

                if ((refr > 0.0) && (zd > 3.0e-4)) {
                    double sinzd = Math.Sin(zd * DEG2RAD);
                    double coszd = Math.Cos(zd * DEG2RAD);
                    double sinzd0 = Math.Sin(zd0 * DEG2RAD);
                    double coszd0 = Math.Cos(zd0 * DEG2RAD);

                    double[] pr = new double[3];
                    for (int j = 0; j < 3; j++) {
                        pr[j] = ((p[j] - coszd0 * uz[j]) / sinzd0) * sinzd + uz[j] * coszd;
                    }

                    proj = Math.Sqrt(pr[0] * pr[0] + pr[1] * pr[1]);

                    if (proj > 0.0) {
                        rar = Math.Atan2(pr[1], pr[0]) * RAD2DEG / 15.0;
                    }

                    if (rar < 0.0) {
                        rar += 24.0;
                    }

                    if (rar >= 24.0) {
                        rar -= 24.0;
                    }

                    decr = Math.Atan2(pr[2], proj) * RAD2DEG;
                }
            }
        }

        #endregion

        #region "sidereal_time / ter2cel / cel2ter / spin / wobble / terra"

        private static double _siderealTime_ee = 0.0;
        private static double _siderealTime_jdLast = -99.0;

        /// <summary>
        /// Computes the Greenwich sidereal time, either mean or apparent, in hours. C version:
        /// 'sidereal_time'. Calls into the CIO-locator port (<c>CioLocation</c>, <c>CioBasis</c>)
        /// expected in ManagedNovas.NutationAndCio.cs.
        /// </summary>
        public static short SiderealTime(double jdHigh, double jdLow, double deltaT, short gstType, short method, short accuracy, ref double gst) {
            short error = 0;

            double[] unitx = { 1.0, 0.0, 0.0 };

            if ((accuracy < 0) || (accuracy > 1)) {
                return (error = 1);
            }

            double jdUt = jdHigh + jdLow;
            double jdTt = jdUt + (deltaT / 86400.0);
            double jdTdb = jdTt;
            double ttTemp = 0.0, secdiff = 0.0;
            Tdb2Tt(jdTdb, ref ttTemp, ref secdiff);
            jdTdb = jdTt + (secdiff / 86400.0);

            double t = (jdTdb - T0) / 36525.0;

            double theta = Era(jdHigh, jdLow);

            double eqeq;
            if (((gstType == 0) && (method == 0)) || ((gstType == 1) && (method == 1))) {
                if (Math.Abs(jdTdb - _siderealTime_jdLast) > 1.0e-8) {
                    double a = 0.0, b = 0.0, c = 0.0, d = 0.0;
                    ETilt(jdTdb, accuracy, ref a, ref b, ref _siderealTime_ee, ref c, ref d);
                    _siderealTime_jdLast = jdTdb;
                }
                eqeq = _siderealTime_ee * 15.0;
            } else {
                eqeq = 0.0;
            }

            switch (method) {
                case 0: {
                    double raCio = 0.0;
                    short refSys = 0;

                    if ((error = CioLocation(jdTdb, accuracy, out raCio, out refSys)) != 0) {
                        gst = 99.0;
                        return (short)(error + 10);
                    }

                    double[] x = new double[3];
                    double[] y = new double[3];
                    double[] z = new double[3];
                    CioBasis(jdTdb, raCio, refSys, accuracy, x, y, z);

                    double[] w1 = new double[3];
                    double[] w2 = new double[3];
                    double[] eq = new double[3];

                    Nutation(jdTdb, -1, accuracy, unitx, w1);
                    Precession(jdTdb, w1, T0, w2);
                    FrameTie(w2, -1, eq);

                    double haEq = theta - Math.Atan2((eq[0] * y[0] + eq[1] * y[1] +
                        eq[2] * y[2]), (eq[0] * x[0] + eq[1] * x[1] +
                        eq[2] * x[2])) * RAD2DEG;

                    haEq -= (eqeq / 240.0);

                    haEq = (haEq % 360.0) / 15.0;
                    if (haEq < 0.0) {
                        haEq += 24.0;
                    }
                    gst = haEq;
                    break;
                }

                case 1: {
                    double st = eqeq + 0.014506 +
                        ((((-0.0000000368 * t
                            - 0.000029956) * t
                            - 0.00000044) * t
                            + 1.3915817) * t
                            + 4612.156534) * t;

                    gst = ((st / 3600.0 + theta) % 360.0) / 15.0;

                    if (gst < 0.0) {
                        gst += 24.0;
                    }
                    break;
                }

                default:
                    gst = 99.0;
                    error = 2;
                    break;
            }

            return error;
        }

        /// <summary>
        /// Rotates a vector from the terrestrial to the celestial system. C version: 'ter2cel'.
        /// Calls into the CIO-locator port (<c>CioLocation</c>, <c>CioBasis</c>) expected in
        /// ManagedNovas.NutationAndCio.cs.
        /// </summary>
        public static short Ter2Cel(double jdUtHigh, double jdUtLow, double deltaT, short method, short accuracy, short option, double xp, double yp, double[] vec1, double[] vec2) {
            short error = 0;

            if ((accuracy < 0) || (accuracy > 1)) {
                return (error = 1);
            }

            double jdUt1 = jdUtHigh + jdUtLow;
            double jdTt = jdUt1 + (deltaT / 86400.0);

            double jdTdb = jdTt;
            double dummy = 0.0, secdiff = 0.0;
            Tdb2Tt(jdTdb, ref dummy, ref secdiff);
            jdTdb = jdTt + secdiff / 86400.0;

            double[] v1 = new double[3];
            double[] v2 = new double[3];

            switch (method) {
                case 0: {
                    if ((xp == 0.0) && (yp == 0.0)) {
                        v1[0] = vec1[0];
                        v1[1] = vec1[1];
                        v1[2] = vec1[2];
                    } else {
                        Wobble(jdTdb, 0, xp, yp, vec1, v1);
                    }

                    double raCio = 0.0;
                    short rs = 0;

                    if ((error = CioLocation(jdTdb, accuracy, out raCio, out rs)) != 0) {
                        return (short)(error + 10);
                    }

                    double[] x = new double[3];
                    double[] y = new double[3];
                    double[] z = new double[3];
                    if ((error = CioBasis(jdTdb, raCio, rs, accuracy, x, y, z)) != 0) {
                        return (short)(error + 20);
                    }

                    double theta = Era(jdUtHigh, jdUtLow);
                    Spin(-theta, v1, v2);

                    vec2[0] = x[0] * v2[0] + y[0] * v2[1] + z[0] * v2[2];
                    vec2[1] = x[1] * v2[0] + y[1] * v2[1] + z[1] * v2[2];
                    vec2[2] = x[2] * v2[0] + y[2] * v2[1] + z[2] * v2[2];
                    break;
                }

                case 1: {
                    if ((xp == 0.0) && (yp == 0.0)) {
                        for (int j = 0; j < 3; j++) {
                            v1[j] = vec1[j];
                        }
                    } else {
                        Wobble(jdTdb, 0, xp, yp, vec1, v1);
                    }

                    double gast = 0.0;
                    SiderealTime(jdUtHigh, jdUtLow, deltaT, 1, 1, accuracy, ref gast);
                    Spin(-gast * 15.0, v1, v2);

                    if (option == 1) {
                        vec2[0] = v2[0];
                        vec2[1] = v2[1];
                        vec2[2] = v2[2];
                    } else {
                        double[] v3 = new double[3];
                        double[] v4 = new double[3];
                        Nutation(jdTdb, -1, accuracy, v2, v3);
                        Precession(jdTdb, v3, T0, v4);
                        FrameTie(v4, -1, vec2);
                    }
                    break;
                }

                default:
                    error = 2;
                    break;
            }

            return error;
        }

        /// <summary>
        /// Rotates a vector from the celestial to the terrestrial system. C version: 'cel2ter'.
        /// Calls into the CIO-locator port (<c>CioLocation</c>, <c>CioBasis</c>) expected in
        /// ManagedNovas.NutationAndCio.cs.
        /// </summary>
        public static short Cel2Ter(double jdUtHigh, double jdUtLow, double deltaT, short method, short accuracy, short option, double xp, double yp, double[] vec1, double[] vec2) {
            short error = 0;

            if ((accuracy < 0) || (accuracy > 1)) {
                return (error = 1);
            }

            double jdUt1 = jdUtHigh + jdUtLow;
            double jdTt = jdUt1 + (deltaT / 86400.0);

            double jdTdb = jdTt;
            double dummy = 0.0, secdiff = 0.0;
            Tdb2Tt(jdTdb, ref dummy, ref secdiff);
            jdTdb = jdTt + secdiff / 86400.0;

            switch (method) {
                case 0: {
                    double raCio = 0.0;
                    short rs = 0;

                    if ((error = CioLocation(jdTdb, accuracy, out raCio, out rs)) != 0) {
                        return (short)(error + 10);
                    }

                    double[] x = new double[3];
                    double[] y = new double[3];
                    double[] z = new double[3];
                    if ((error = CioBasis(jdTdb, raCio, rs, accuracy, x, y, z)) != 0) {
                        return (short)(error + 20);
                    }

                    double[] v1 = new double[3];
                    v1[0] = x[0] * vec1[0] + x[1] * vec1[1] + x[2] * vec1[2];
                    v1[1] = y[0] * vec1[0] + y[1] * vec1[1] + y[2] * vec1[2];
                    v1[2] = z[0] * vec1[0] + z[1] * vec1[1] + z[2] * vec1[2];

                    double theta = Era(jdUtHigh, jdUtLow);
                    double[] v2 = new double[3];
                    Spin(theta, v1, v2);

                    if ((xp == 0.0) && (yp == 0.0)) {
                        vec2[0] = v2[0];
                        vec2[1] = v2[1];
                        vec2[2] = v2[2];
                    } else {
                        Wobble(jdTdb, 1, xp, yp, v2, vec2);
                    }

                    break;
                }

                case 1: {
                    double[] v1 = new double[3];
                    double[] v2 = new double[3];
                    double[] v3 = new double[3];
                    double[] v4 = new double[3];

                    if (option == 1) {
                        v3[0] = vec1[0];
                        v3[1] = vec1[1];
                        v3[2] = vec1[2];
                    } else {
                        FrameTie(vec1, 1, v1);
                        Precession(T0, v1, jdTdb, v2);
                        Nutation(jdTdb, 0, accuracy, v2, v3);
                    }

                    double gast = 0.0;
                    SiderealTime(jdUtHigh, jdUtLow, deltaT, 1, 1, accuracy, ref gast);
                    Spin(gast * 15.0, v3, v4);

                    if ((xp == 0.0) && (yp == 0.0)) {
                        for (int j = 0; j < 3; j++) {
                            vec2[j] = v4[j];
                        }
                    } else {
                        Wobble(jdTdb, 1, xp, yp, v4, vec2);
                    }

                    break;
                }

                default:
                    error = 2;
                    break;
            }

            return error;
        }

        private static double _spin_angLast = -999.0;
        private static double _spin_xx, _spin_yx, _spin_zx;
        private static double _spin_xy, _spin_yy, _spin_zy;
        private static double _spin_xz, _spin_yz, _spin_zz;

        /// <summary>
        /// Transforms a vector from one coordinate system to another with the same origin,
        /// rotated about the z-axis. C version: 'spin'.
        /// </summary>
        public static void Spin(double angle, double[] pos1, double[] pos2) {
            if (Math.Abs(angle - _spin_angLast) >= 1.0e-12) {
                double angr = angle * DEG2RAD;
                double cosang = Math.Cos(angr);
                double sinang = Math.Sin(angr);

                _spin_xx = cosang;
                _spin_yx = sinang;
                _spin_zx = 0.0;
                _spin_xy = -sinang;
                _spin_yy = cosang;
                _spin_zy = 0.0;
                _spin_xz = 0.0;
                _spin_yz = 0.0;
                _spin_zz = 1.0;

                _spin_angLast = angle;
            }

            pos2[0] = _spin_xx * pos1[0] + _spin_yx * pos1[1] + _spin_zx * pos1[2];
            pos2[1] = _spin_xy * pos1[0] + _spin_yy * pos1[1] + _spin_zy * pos1[2];
            pos2[2] = _spin_xz * pos1[0] + _spin_yz * pos1[1] + _spin_zz * pos1[2];
        }

        /// <summary>
        /// Corrects a vector in the ITRS for polar motion, transforming it to the terrestrial
        /// intermediate system (or the inverse). C version: 'wobble'.
        /// </summary>
        public static void Wobble(double tjd, short direction, double xp, double yp, double[] pos1, double[] pos2) {
            double xpole = xp * ASEC2RAD;
            double ypole = yp * ASEC2RAD;

            double t = (tjd - T0) / 36525.0;

            double sprime = -47.0e-6 * t;
            double tiolon = -sprime * ASEC2RAD;

            double sinx = Math.Sin(xpole);
            double cosx = Math.Cos(xpole);
            double siny = Math.Sin(ypole);
            double cosy = Math.Cos(ypole);
            double sinl = Math.Sin(tiolon);
            double cosl = Math.Cos(tiolon);

            double xx = cosx * cosl;
            double yx = sinx * siny * cosl + cosy * sinl;
            double zx = -sinx * cosy * cosl + siny * sinl;
            double xy = -cosx * sinl;
            double yy = -sinx * siny * sinl + cosy * cosl;
            double zy = sinx * cosy * sinl + siny * cosl;
            double xz = sinx;
            double yz = -cosx * siny;
            double zz = cosx * cosy;

            if (direction == 0) {
                pos2[0] = xx * pos1[0] + yx * pos1[1] + zx * pos1[2];
                pos2[1] = xy * pos1[0] + yy * pos1[1] + zy * pos1[2];
                pos2[2] = xz * pos1[0] + yz * pos1[1] + zz * pos1[2];
            } else {
                pos2[0] = xx * pos1[0] + xy * pos1[1] + xz * pos1[2];
                pos2[1] = yx * pos1[0] + yy * pos1[1] + yz * pos1[2];
                pos2[2] = zx * pos1[0] + zy * pos1[1] + zz * pos1[2];
            }
        }

        private static bool _terra_firstEntry = true;
        private static double _terra_eradKm;

        /// <summary>
        /// Computes the position and velocity vectors of a terrestrial observer with respect to
        /// the center of the Earth. C version: 'terra'.
        /// </summary>
        public static void Terra(ref NOVAS.OnSurface location, double st, double[] pos, double[] vel) {
            if (_terra_firstEntry) {
                _terra_eradKm = ERAD / 1000.0;
                _terra_firstEntry = false;
            }

            double df = 1.0 - F;
            double df2 = df * df;

            double phi = location.Latitude * DEG2RAD;
            double sinphi = Math.Sin(phi);
            double cosphi = Math.Cos(phi);
            double c = 1.0 / Math.Sqrt(cosphi * cosphi + df2 * sinphi * sinphi);
            double s = df2 * c;
            double htKm = location.Height / 1000.0;
            double ach = _terra_eradKm * c + htKm;
            double ash = _terra_eradKm * s + htKm;

            double stlocl = (st * 15.0 + location.Longitude) * DEG2RAD;
            double sinst = Math.Sin(stlocl);
            double cosst = Math.Cos(stlocl);

            pos[0] = ach * cosphi * cosst;
            pos[1] = ach * cosphi * sinst;
            pos[2] = ash * sinphi;

            vel[0] = -ANGVEL * ach * cosphi * sinst;
            vel[1] = ANGVEL * ach * cosphi * cosst;
            vel[2] = 0.0;

            for (int j = 0; j < 3; j++) {
                pos[j] /= AU_KM;
                vel[j] /= AU_KM;
                vel[j] *= 86400.0;
            }
        }

        #endregion

        #region "refract / limb_angle"

        /// <summary>
        /// Computes approximate atmospheric refraction in zenith distance, in degrees.
        /// C version: 'refract'.
        /// </summary>
        public static double Refract(ref NOVAS.OnSurface location, short refOption, double zdObs) {
            const double s = 9.1e3;
            double refr;

            if ((zdObs < 0.1) || (zdObs > 91.0)) {
                refr = 0.0;
            } else {
                double p, t;

                if (refOption == 2) {
                    p = location.Pressure;
                    t = location.Temperature;
                } else {
                    p = 1010.0 * Math.Exp(-location.Height / s);
                    t = 10.0;
                }

                double h = 90.0 - zdObs;
                double r = 0.016667 / Math.Tan((h + 7.31 / (h + 4.4)) * DEG2RAD);
                refr = r * (0.28 * p / (t + 273.0));
            }

            return refr;
        }

        private static bool _limbAngle_firstEntry = true;
        private static double _limbAngle_pi, _limbAngle_halfpi, _limbAngle_rade;

        /// <summary>
        /// Determines the angle of an object above or below the Earth's limb (horizon), assuming
        /// an airless spherical Earth. C version: 'limb_angle'.
        /// </summary>
        public static void LimbAngle(double[] posObj, double[] posObs, ref double limbAng, ref double nadirAng) {
            if (_limbAngle_firstEntry) {
                _limbAngle_pi = TWOPI / 2.0;
                _limbAngle_halfpi = _limbAngle_pi / 2.0;
                _limbAngle_rade = ERAD / AU;
                _limbAngle_firstEntry = false;
            }

            double disobj = Math.Sqrt(posObj[0] * posObj[0] + posObj[1] * posObj[1] + posObj[2] * posObj[2]);
            double disobs = Math.Sqrt(posObs[0] * posObs[0] + posObs[1] * posObs[1] + posObs[2] * posObs[2]);

            double aprad = disobs >= _limbAngle_rade ? Math.Asin(_limbAngle_rade / disobs) : _limbAngle_halfpi;

            double zdlim = _limbAngle_pi - aprad;

            double coszd = (posObj[0] * posObs[0] + posObj[1] * posObs[1] +
                posObj[2] * posObs[2]) / (disobj * disobs);

            double zdobj;
            if (coszd <= -1.0) {
                zdobj = _limbAngle_pi;
            } else if (coszd >= 1.0) {
                zdobj = 0.0;
            } else {
                zdobj = Math.Acos(coszd);
            }

            limbAng = (zdlim - zdobj) * RAD2DEG;
            nadirAng = (_limbAngle_pi - zdobj) / aprad;
        }

        #endregion

        #region "transform_cat / transform_hip"

        /// <summary>
        /// Transforms a star's catalog quantities for a change of epoch and/or equator and
        /// equinox. C version: 'transform_cat'.
        /// </summary>
        public static short TransformCat(short option, double dateIncat, NOVAS.CatalogueEntry incat, double dateNewcat, string newcatId, ref NOVAS.CatalogueEntry newcat) {
            short error = 0;

            double jdIncat = dateIncat < 10000.0 ? T0 + (dateIncat - 2000.0) * 365.25 : dateIncat;
            double jdNewcat = dateNewcat < 10000.0 ? T0 + (dateNewcat - 2000.0) * 365.25 : dateNewcat;

            double paralx = incat.Parallax;
            if (paralx <= 0.0) {
                paralx = 1.0e-6;
            }

            double dist = 1.0 / Math.Sin(paralx * 1.0e-3 * ASEC2RAD);
            double r = incat.RA * 54000.0 * ASEC2RAD;
            double d = incat.Dec * 3600.0 * ASEC2RAD;
            double cra = Math.Cos(r);
            double sra = Math.Sin(r);
            double cdc = Math.Cos(d);
            double sdc = Math.Sin(d);

            double[] pos1 = new double[3];
            pos1[0] = dist * cdc * cra;
            pos1[1] = dist * cdc * sra;
            pos1[2] = dist * sdc;

            double k = 1.0 / (1.0 - incat.RadialVelocity / C * 1000.0);

            double term1 = paralx * 365.25;
            double pmr = incat.ProMoRA / term1 * k;
            double pmd = incat.ProMoDec / term1 * k;
            double rvl = incat.RadialVelocity * 86400.0 / AU_KM * k;

            double[] vel1 = new double[3];
            vel1[0] = -pmr * sra - pmd * sdc * cra + rvl * cdc * cra;
            vel1[1] = pmr * cra - pmd * sdc * sra + rvl * cdc * sra;
            vel1[2] = pmd * cdc + rvl * sdc;

            double[] pos2 = new double[3];
            double[] vel2 = new double[3];

            if ((option == 1) || (option == 3)) {
                for (int j = 0; j < 3; j++) {
                    pos2[j] = pos1[j] + vel1[j] * (jdNewcat - jdIncat);
                    vel2[j] = vel1[j];
                }
            } else {
                for (int j = 0; j < 3; j++) {
                    pos2[j] = pos1[j];
                    vel2[j] = vel1[j];
                }
            }

            if ((option == 2) || (option == 3)) {
                for (int j = 0; j < 3; j++) {
                    pos1[j] = pos2[j];
                    vel1[j] = vel2[j];
                }
                if ((error = Precession(jdIncat, pos1, jdNewcat, pos2)) != 0) {
                    return error;
                }
                Precession(jdIncat, vel1, jdNewcat, vel2);
            }

            if (option == 4) {
                FrameTie(pos1, -1, pos2);
                FrameTie(vel1, -1, vel2);
            }

            if (option == 5) {
                FrameTie(pos1, 1, pos2);
                FrameTie(vel1, 1, vel2);
            }

            double xyproj = Math.Sqrt(pos2[0] * pos2[0] + pos2[1] * pos2[1]);

            r = xyproj > 0.0 ? Math.Atan2(pos2[1], pos2[0]) : 0.0;
            newcat.RA = r / ASEC2RAD / 54000.0;
            if (newcat.RA < 0.0) {
                newcat.RA += 24.0;
            }
            if (newcat.RA >= 24.0) {
                newcat.RA -= 24.0;
            }

            d = Math.Atan2(pos2[2], xyproj);
            newcat.Dec = d / ASEC2RAD / 3600.0;

            dist = Math.Sqrt(pos2[0] * pos2[0] + pos2[1] * pos2[1] + pos2[2] * pos2[2]);

            paralx = Math.Asin(1.0 / dist) / ASEC2RAD * 1000.0;
            newcat.Parallax = paralx;

            cra = Math.Cos(r);
            sra = Math.Sin(r);
            cdc = Math.Cos(d);
            sdc = Math.Sin(d);
            pmr = -vel2[0] * sra + vel2[1] * cra;
            pmd = -vel2[0] * cra * sdc - vel2[1] * sra * sdc + vel2[2] * cdc;
            rvl = vel2[0] * cra * cdc + vel2[1] * sra * cdc + vel2[2] * sdc;

            newcat.ProMoRA = pmr * paralx * 365.25 / k;
            newcat.ProMoDec = pmd * paralx * 365.25 / k;
            newcat.RadialVelocity = rvl * (AU_KM / 86400.0) / k;

            if (newcat.Parallax <= 1.01e-6) {
                newcat.Parallax = 0.0;
                newcat.RadialVelocity = incat.RadialVelocity;
            }

            // SIZE_OF_CAT_NAME - 1 = 3
            if (newcatId.Length > 3) {
                return 2;
            } else {
                newcat.Catalog = newcatId;
            }

            newcat.StarName = incat.StarName;
            newcat.StarNumber = incat.StarNumber;

            return error;
        }

        /// <summary>
        /// Converts Hipparcos catalog data at epoch J1991.25 to epoch J2000.0. C version:
        /// 'transform_hip'.
        /// </summary>
        public static void TransformHip(NOVAS.CatalogueEntry hipparcos, ref NOVAS.CatalogueEntry hip2000) {
            const double epochHip = 2448349.0625;

            NOVAS.CatalogueEntry scratch = default;

            scratch.StarName = hipparcos.StarName;
            scratch.StarNumber = hipparcos.StarNumber;
            scratch.Dec = hipparcos.Dec;
            scratch.ProMoRA = hipparcos.ProMoRA;
            scratch.ProMoDec = hipparcos.ProMoDec;
            scratch.Parallax = hipparcos.Parallax;
            scratch.RadialVelocity = hipparcos.RadialVelocity;

            scratch.Catalog = "SCR";

            scratch.RA = hipparcos.RA / 15.0;

            TransformCat(1, epochHip, scratch, T0, "HP2", ref hip2000);
        }

        #endregion

        #region "make_cat_entry / make_object / make_observer* / make_on_surface / make_in_space"

        /// <summary>
        /// Creates a 'cat_entry'-equivalent structure containing catalog data for a star or
        /// "star-like" object. C version: 'make_cat_entry'.
        /// </summary>
        public static short MakeCatEntry(string starName, string catalog, int starNum, double ra, double dec, double pmRa, double pmDec, double parallax, double radVel, ref NOVAS.CatalogueEntry star) {
            // SIZE_OF_OBJ_NAME - 1 = 50
            if (starName.Length > 50) {
                return 1;
            } else {
                star.StarName = starName;
            }

            // SIZE_OF_CAT_NAME - 1 = 3
            if (catalog.Length > 3) {
                return 2;
            } else {
                star.Catalog = catalog;
            }

            star.StarNumber = starNum;
            star.RA = ra;
            star.Dec = dec;
            star.ProMoRA = pmRa;
            star.ProMoDec = pmDec;
            star.Parallax = parallax;
            star.RadialVelocity = radVel;

            return 0;
        }

        /// <summary>
        /// Makes an 'object'-equivalent structure specifying the celestial object of interest.
        /// C version: 'make_object'.
        /// </summary>
        public static short MakeObject(short type, short number, string name, NOVAS.CatalogueEntry starData, ref NOVAS.CelestialObject celObj) {
            short error = 0;

            celObj.Type = 0;
            celObj.Number = 0;
            celObj.Name = "  ";
            celObj.Star.StarName = "  ";
            celObj.Star.Catalog = "  ";
            celObj.Star.StarNumber = 0;
            celObj.Star.RA = 0.0;
            celObj.Star.Dec = 0.0;
            celObj.Star.ProMoRA = 0.0;
            celObj.Star.ProMoDec = 0.0;
            celObj.Star.Parallax = 0.0;
            celObj.Star.RadialVelocity = 0.0;

            if ((type < 0) || (type > 2)) {
                return (error = 1);
            } else {
                celObj.Type = type;
            }

            if (type == 0) {
                if ((number <= 0) || (number > 11)) {
                    return (error = 2);
                }
            } else if (type == 1) {
                if (number <= 0) {
                    return (error = 2);
                }
            } else {
                number = 0;
            }

            celObj.Number = number;

            // SIZE_OF_OBJ_NAME - 1 = 50
            if (name.Length > 50) {
                return (error = 5);
            }

            char[] nameChars = new char[name.Length];
            for (int i = 0; i < name.Length; i++) {
                nameChars[i] = char.ToUpperInvariant(name[i]);
            }
            celObj.Name = new string(nameChars);

            if (type == 2) {
                celObj.Star.StarName = starData.StarName;
                celObj.Star.Catalog = starData.Catalog;
                celObj.Star.StarNumber = starData.StarNumber;
                celObj.Star.RA = starData.RA;
                celObj.Star.Dec = starData.Dec;
                celObj.Star.ProMoRA = starData.ProMoRA;
                celObj.Star.ProMoDec = starData.ProMoDec;
                celObj.Star.Parallax = starData.Parallax;
                celObj.Star.RadialVelocity = starData.RadialVelocity;
            }

            return error;
        }

        /// <summary>Makes an 'observer'-equivalent structure specifying the location of the observer. C version: 'make_observer'.</summary>
        public static short MakeObserver(short where, NOVAS.OnSurface obsSurface, NOVAS.InSpace obsSpace, ref NOVAS.Observer obs) {
            short error = 0;

            obs.Where = where;
            obs.OnSurf.Latitude = 0.0;
            obs.OnSurf.Longitude = 0.0;
            obs.OnSurf.Height = 0.0;
            obs.OnSurf.Temperature = 0.0;
            obs.OnSurf.Pressure = 0.0;
            obs.NearEarth.ScPos = new double[] { 0.0, 0.0, 0.0 };
            obs.NearEarth.ScVel = new double[] { 0.0, 0.0, 0.0 };

            switch (where) {
                case 0:
                    break;

                case 1:
                    obs.OnSurf.Latitude = obsSurface.Latitude;
                    obs.OnSurf.Longitude = obsSurface.Longitude;
                    obs.OnSurf.Height = obsSurface.Height;
                    obs.OnSurf.Temperature = obsSurface.Temperature;
                    obs.OnSurf.Pressure = obsSurface.Pressure;
                    break;

                case 2:
                    obs.NearEarth.ScPos[0] = obsSpace.ScPos[0];
                    obs.NearEarth.ScPos[1] = obsSpace.ScPos[1];
                    obs.NearEarth.ScPos[2] = obsSpace.ScPos[2];
                    obs.NearEarth.ScVel[0] = obsSpace.ScVel[0];
                    obs.NearEarth.ScVel[1] = obsSpace.ScVel[1];
                    obs.NearEarth.ScVel[2] = obsSpace.ScVel[2];
                    break;

                default:
                    error = 1;
                    break;
            }

            return error;
        }

        /// <summary>Makes an 'observer'-equivalent structure for an observer at the geocenter. C version: 'make_observer_at_geocenter'.</summary>
        public static void MakeObserverAtGeocenter(ref NOVAS.Observer obsAtGeocenter) {
            double[] satPos = { 0.0, 0.0, 0.0 };
            double[] satVel = { 0.0, 0.0, 0.0 };

            NOVAS.InSpace satState = default;
            NOVAS.OnSurface surfaceLoc = default;

            MakeInSpace(satPos, satVel, ref satState);
            MakeOnSurface(0.0, 0.0, 0.0, 0.0, 0.0, ref surfaceLoc);

            obsAtGeocenter.Where = 0;
            obsAtGeocenter.OnSurf = surfaceLoc;
            obsAtGeocenter.NearEarth = satState;
        }

        /// <summary>Makes an 'observer'-equivalent structure for an observer on the surface of the Earth. C version: 'make_observer_on_surface'.</summary>
        public static void MakeObserverOnSurface(double latitude, double longitude, double height, double temperature, double pressure, ref NOVAS.Observer obsOnSurface) {
            double[] satPos = { 0.0, 0.0, 0.0 };
            double[] satVel = { 0.0, 0.0, 0.0 };

            NOVAS.InSpace satState = default;
            NOVAS.OnSurface surfaceLoc = default;

            MakeInSpace(satPos, satVel, ref satState);
            MakeOnSurface(latitude, longitude, height, temperature, pressure, ref surfaceLoc);

            obsOnSurface.Where = 1;
            obsOnSurface.OnSurf = surfaceLoc;
            obsOnSurface.NearEarth = satState;
        }

        /// <summary>Makes an 'observer'-equivalent structure for an observer on a near-Earth spacecraft. C version: 'make_observer_in_space'.</summary>
        public static void MakeObserverInSpace(double[] scPos, double[] scVel, ref NOVAS.Observer obsInSpace) {
            const double latitude = 0.0;
            const double longitude = 0.0;
            const double height = 0.0;
            const double temperature = 0.0;
            const double pressure = 0.0;

            NOVAS.InSpace satState = default;
            NOVAS.OnSurface surfaceLoc = default;

            MakeInSpace(scPos, scVel, ref satState);
            MakeOnSurface(latitude, longitude, height, temperature, pressure, ref surfaceLoc);

            obsInSpace.Where = 2;
            obsInSpace.OnSurf = surfaceLoc;
            obsInSpace.NearEarth = satState;
        }

        /// <summary>Makes an 'on_surface'-equivalent structure specifying the location and weather for an observer on the surface of the Earth. C version: 'make_on_surface'.</summary>
        public static void MakeOnSurface(double latitude, double longitude, double height, double temperature, double pressure, ref NOVAS.OnSurface obsSurface) {
            obsSurface.Latitude = latitude;
            obsSurface.Longitude = longitude;
            obsSurface.Height = height;
            obsSurface.Temperature = temperature;
            obsSurface.Pressure = pressure;
        }

        /// <summary>Makes an 'in_space'-equivalent structure specifying the position and velocity of an observer on a near-Earth spacecraft. C version: 'make_in_space'.</summary>
        public static void MakeInSpace(double[] scPos, double[] scVel, ref NOVAS.InSpace obsSpace) {
            obsSpace.ScPos = new double[3];
            obsSpace.ScVel = new double[3];

            obsSpace.ScPos[0] = scPos[0];
            obsSpace.ScPos[1] = scPos[1];
            obsSpace.ScPos[2] = scPos[2];

            obsSpace.ScVel[0] = scVel[0];
            obsSpace.ScVel[1] = scVel[1];
            obsSpace.ScVel[2] = scVel[2];
        }

        #endregion

        #region "Cross-file dependencies expected from sibling partial-class files"

        // The following members are ported by other engineers working concurrently in the same
        // partial class (ManagedNovas), in separate files. Signatures below are best-effort
        // guesses based on the real NOVAS C prototypes (novas.h / nutation.h) using this file's
        // own ref/array conventions; they are NOT implemented here and could not be
        // compile-verified against this file alone since those files did not exist yet at the
        // time this file was written (confirmed via `ls ManagedNovas.*.cs` on the Mac).
        //
        // Expected in ManagedNovas.NutationAndCio.cs (nutation.c / cio_file.c port):
        //   static void NutationAngles(double t, short accuracy, out double dpsi, out double deps)
        //   static short CioLocation(double jdTdb, short accuracy, out double raCio, out short refSys)
        //   static short CioBasis(double jdTdb, double raCio, short refSys, short accuracy, double[] x, double[] y, double[] z)
        //   static short CioArray(double jdTdb, long nPts, RaOfCio[] cio)
        //
        // Expected in ManagedNovas.Transforms.cs (solsys3.c / place-chain port):
        //   static short Ephemeris(double[] jd, NOVAS.CelestialObject celObj, short origin, short accuracy, double[] pos, double[] vel)
        //
        // Both sibling files now exist and were confirmed to match the above signatures exactly,
        // EXCEPT NutationAngles/CioLocation use 'out' (not 'ref') for their output parameters —
        // this file's calls were updated accordingly. See the final report for other
        // cross-file naming/signature mismatches found (StarVectors renamed to Starvectors to
        // match ManagedNovas.Transforms.cs's call site; MakeObject's 'type' parameter is 'short'
        // per this file's C-faithful convention, but Transforms.cs calls it with NOVAS.ObjectType
        // in a few places - a mismatch in the OTHER file that the coordinator will need to
        // reconcile there, not here).

        #endregion
    }
}
