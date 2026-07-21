// This file contains a derived work based on routines and computations from the
// Naval Observatory Vector Astrometry Software (NOVAS), C Edition, developed by the
// U.S. Naval Observatory. NOVAS is public domain U.S. government work product.
// See https://aa.usno.navy.mil/software/novas/novas_info.php for the original software.
//
// solarsystem/solarsystem_hp here are ported from solsys3.c (NOVAS's own
// self-contained, no-external-file solar/planetary position solution) rather than
// solsys1.c (which the real Windows build uses and which requires a binary JPL DE
// ephemeris file not reliably available in this project) - a deliberate accuracy/
// portability tradeoff, see project documentation for the full reasoning.
//
// This file ports:
//   - solsys3.c in full (solarsystem, solarsystem_hp, sun_eph)
//   - the top-level orchestration / apparent-place chain from novas.c:
//       ephemeris, place, geo_posvel, light_time, app_star, virtual_star,
//       astro_star, app_planet, virtual_planet, astro_planet, topo_star,
//       local_star, topo_planet, local_planet, mean_star, gcrs2equ
//
// It calls into functions ported elsewhere in this same partial class
// (ManagedNovas.Core.cs and ManagedNovas.NutationAndCio.cs), which this file does
// not itself define. Their signatures are ASSUMED below (documented at each call
// site and summarized here) based on the exact P/Invoke conventions NOVAS.cs
// already establishes elsewhere in this project (struct pointer params -> `ref`,
// scalar output pointer params -> `out`, `double *vec` params -> plain `double[]`,
// C `short int` status codes -> C# `short`). If the landed files differ, only the
// call sites below need small signature adjustments - none of the ported logic
// itself should need to change.
//
// Assumed signatures for functions owned by ManagedNovas.Core.cs:
//   void   Tdb2Tt(double tdbJd, out double ttJd, out double secDiff)
//   short  Precession(double jdTdb1, double[] pos1, double jdTdb2, double[] pos2)
//   void   FrameTie(double[] pos1, short direction, double[] pos2)
//   void   Starvectors(ref NOVAS.CatalogueEntry star, double[] pos, double[] vel)
//   void   ProperMotion(double jdTdb1, double[] pos, double[] vel, double jdTdb2, double[] pos2)
//   void   Bary2Obs(double[] pos, double[] posObs, double[] pos2, out double lightTime)
//   double DLight(double[] pos1, double[] posObs)
//   void   LimbAngle(double[] posObj, double[] posObs, out double limbAngle, out double nadirAngle)
//   short  GravDef(double jdTdb, short locCode, short accuracy, double[] pos1, double[] posObs, double[] pos2)
//   void   Aberration(double[] pos, double[] ve, double lightTime, double[] pos2)
//   void   RadVel(ref NOVAS.CelestialObject celObject, double[] pos, double[] vel, double[] velObs, double dObsGeo, double dObsSun, double dObjSun, out double rv)
//   short  Vector2Radec(double[] pos, out double ra, out double dec)
//   void   Radec2Vector(double ra, double dec, double dist, double[] vector)
//   short  MakeCatEntry(string starName, string catalog, long starNum, double ra, double dec, double pmRa, double pmDec, double parallax, double radVel, out NOVAS.CatalogueEntry star)
//   short  MakeObject(NOVAS.ObjectType type, short number, string name, NOVAS.CatalogueEntry starData, out NOVAS.CelestialObject celObj)
//   short  MakeObserver(short where, ref NOVAS.OnSurface obsSurface, ref NOVAS.InSpace obsSpace, out NOVAS.Observer obs)
//   short  SiderealTime(double jdHigh, double jdLow, double deltaT, NOVAS.GstType gstType, NOVAS.Method method, NOVAS.Accuracy accuracy, ref double gst)
//   void   ETilt(double jdTdb, short accuracy, out double mobl, out double tobl, out double ee, out double dpsi, out double deps)
//   void   Terra(ref NOVAS.OnSurface location, double st, double[] pos, double[] vel)
//
// Assumed signatures for functions owned by ManagedNovas.NutationAndCio.cs:
//   void  Nutation(double jdTdb, short direction, short accuracy, double[] pos, double[] pos2)
//   short CioLocation(double jdTdb, short accuracy, out double raCio, out short refSys)
//   short CioBasis(double jdTdb, double raCio, short refSys, short accuracy, double[] x, double[] y, double[] z)

using System;

namespace NINA.Astrometry {

    public static partial class ManagedNovas {

        #region "solsys3.c - self-contained Sun/Earth ephemeris"

        // Masses and orbital elements for Jupiter, Saturn, Uranus, Neptune (see
        // Explanatory Supplement (1992), p. 316), angles in radians. Used for
        // barycenter computations only. Values reproduced exactly from solsys3.c.
        private static readonly double[] Solsys3Pm = { 1047.349, 3497.898, 22903.0, 19412.2 };
        private static readonly double[] Solsys3Pa = { 5.203363, 9.537070, 19.191264, 30.068963 };
        private static readonly double[] Solsys3Pe = { 0.048393, 0.054151, 0.047168, 0.008586 };
        private static readonly double[] Solsys3Pj = { 0.022782, 0.043362, 0.013437, 0.030878 };
        private static readonly double[] Solsys3Po = { 1.755036, 1.984702, 1.295556, 2.298977 };
        private static readonly double[] Solsys3Pw = { 0.257503, 1.613242, 2.983889, 0.784898 };
        private static readonly double[] Solsys3Pl = { 0.600470, 0.871693, 5.466933, 5.321160 };
        private static readonly double[] Solsys3Pn = { 1.450138e-3, 5.841727e-4, 2.047497e-4, 1.043891e-4 };

        /// <summary>Obliquity of ecliptic at epoch J2000.0, in degrees.</summary>
        private const double Solsys3Obl = 23.4392794444;

        /// <summary>
        /// solsys3.c's 'tmass', 'a[3][4]' and 'b[3][4]' are computed once ('first_time'/'tlast'
        /// static-init guard in the original C). They depend only on the fixed tables above, not
        /// on the Julian date, so this managed port computes them once via a static field
        /// initializer instead of replicating the C static-flag-guarded lazy init - identical
        /// result, no shared mutable "first_time" state to reason about.
        /// </summary>
        private static readonly (double Tmass, double[,] A, double[,] B) Solsys3Init = ComputeSolsys3Constants();

        private static (double, double[,], double[,]) ComputeSolsys3Constants() {
            const double twopi = 6.283185307179586476925287;

            double tmass = 1.0 + 5.977e-6;
            double oblr = Solsys3Obl * twopi / 360.0;
            double se = Math.Sin(oblr);
            double ce = Math.Cos(oblr);

            var a = new double[3, 4];
            var b = new double[3, 4];

            for (int i = 0; i < 4; i++) {
                tmass += 1.0 / Solsys3Pm[i];

                double si = Math.Sin(Solsys3Pj[i]);
                double ci = Math.Cos(Solsys3Pj[i]);
                double sn = Math.Sin(Solsys3Po[i]);
                double cn = Math.Cos(Solsys3Po[i]);
                double sw = Math.Sin(Solsys3Pw[i] - Solsys3Po[i]);
                double cw = Math.Cos(Solsys3Pw[i] - Solsys3Po[i]);

                // p and q vectors (Brouwer & Clemence (1961), Methods of Celestial Mechanics, pp. 35-36).
                double p1 = cw * cn - sw * sn * ci;
                double p2 = (cw * sn + sw * cn * ci) * ce - sw * si * se;
                double p3 = (cw * sn + sw * cn * ci) * se + sw * si * ce;
                double q1 = -sw * cn - cw * sn * ci;
                double q2 = (-sw * sn + cw * cn * ci) * ce - cw * si * se;
                double q3 = (-sw * sn + cw * cn * ci) * se + cw * si * ce;
                double roote = Math.Sqrt(1.0 - Solsys3Pe[i] * Solsys3Pe[i]);

                a[0, i] = Solsys3Pa[i] * p1;
                a[1, i] = Solsys3Pa[i] * p2;
                a[2, i] = Solsys3Pa[i] * p3;
                b[0, i] = Solsys3Pa[i] * roote * q1;
                b[1, i] = Solsys3Pa[i] * roote * q2;
                b[2, i] = Solsys3Pa[i] * roote * q3;
            }

            return (tmass, a, b);
        }

        /// <summary>
        /// Provides the position and velocity of the Earth (or Sun) at epoch 'tjd' by evaluating
        /// a closed-form theory without reference to an external file. Ported from solsys3.c
        /// 'solarsystem'.
        /// </summary>
        /// <param name="tjd">TDB Julian date.</param>
        /// <param name="body">0, 1 or 10 for the Sun; 2 or 3 for the Earth.</param>
        /// <param name="origin">0 = solar system barycenter, 1 = center of mass of the Sun.</param>
        /// <param name="position">Position vector of 'body' at 'tjd' (AU, ICRS-referred equator/equinox of J2000.0). Returned.</param>
        /// <param name="velocity">Velocity vector of 'body' at 'tjd' (AU/day). Returned.</param>
        /// <returns>0 OK, 1 date out of range, 2 invalid body.</returns>
        public static short Solarsystem(double tjd, short body, short origin, double[] position, double[] velocity) {
            const double T0 = 2451545.00000000;
            const double twopi = 6.283185307179586476925287;

            int i;

            if ((tjd < 2340000.5) || (tjd > 2560000.5)) {
                return 1;
            }

            if ((body == 0) || (body == 1) || (body == 10)) {
                // Sun.
                for (i = 0; i < 3; i++) {
                    position[i] = velocity[i] = 0.0;
                }
            } else if ((body == 2) || (body == 3)) {
                // Earth. Velocities obtained from crude numerical differentiation.
                var p = new double[3][] { new double[3], new double[3], new double[3] };

                for (i = 0; i < 3; i++) {
                    double qjd = tjd + (double)(i - 1) * 0.1;
                    SunEph(qjd, out double ras, out double decs, out double diss);

                    var pos1 = new double[3];
                    Radec2Vector(ras, decs, diss, pos1);

                    var posOut = new double[3];
                    Precession(qjd, pos1, T0, posOut);

                    p[i][0] = -posOut[0];
                    p[i][1] = -posOut[1];
                    p[i][2] = -posOut[2];
                }

                for (i = 0; i < 3; i++) {
                    position[i] = p[1][i];
                    velocity[i] = (p[2][i] - p[0][i]) / 0.2;
                }
            } else if (body == 11) {
                // Moon. Not part of the real solsys3.c (confirmed by reading the source - it only
                // ever handles Sun and Earth) - solsys3 alone has no lunar theory at all, so
                // without this the Moon would be permanently unavailable through this managed
                // build (see project documentation - a real, previously-flagged gap). This adds a
                // real, independently-verified low-precision lunar ephemeris (Van Flandern &
                // Pulkkinen 1979-derived formulation, ~1-2 arcminute accuracy - see
                // MoonEclipticPositionOfDate's own doc comment for the verification story), rather
                // than leaving the gap open. Position is Earth's heliocentric position (same
                // SunEph-based calculation as the Earth case above) plus the Moon's own geocentric
                // offset, so it flows through the same origin/barycenter handling below exactly
                // like every other body. Velocity via the same numerical-differentiation technique
                // already used for Earth above.
                var p = new double[3][] { new double[3], new double[3], new double[3] };

                for (i = 0; i < 3; i++) {
                    double qjd = tjd + (double)(i - 1) * 0.1;

                    SunEph(qjd, out double ras, out double decs, out double diss);
                    var earthPos1 = new double[3];
                    Radec2Vector(ras, decs, diss, earthPos1);
                    var earthPosOut = new double[3];
                    Precession(qjd, earthPos1, T0, earthPosOut);

                    var moonEquOfDate = MoonGeocentricEquatorialOfDate(qjd);
                    var moonPosOut = new double[3];
                    Precession(qjd, moonEquOfDate, T0, moonPosOut);

                    p[i][0] = -earthPosOut[0] + moonPosOut[0];
                    p[i][1] = -earthPosOut[1] + moonPosOut[1];
                    p[i][2] = -earthPosOut[2] + moonPosOut[2];
                }

                for (i = 0; i < 3; i++) {
                    position[i] = p[1][i];
                    velocity[i] = (p[2][i] - p[0][i]) / 0.2;
                }
            } else {
                return 2;
            }

            if (origin == 0) {
                // Move origin to solar system barycenter, from Keplerian approximations of the
                // coordinates of the four largest planets. (The original C caches this per 'tjd'
                // via a static 'tlast' guard; this port simply recomputes every call, which is
                // deterministic and numerically identical, just without the memoization.)
                var pbary = new double[3];
                var vbary = new double[3];

                for (i = 0; i < 4; i++) {
                    double e = Solsys3Pe[i];
                    double mlon = Solsys3Pl[i] + Solsys3Pn[i] * (tjd - T0);
                    double ma = (mlon - Solsys3Pw[i]) % twopi;
                    double u = ma + e * Math.Sin(ma) + 0.5 * e * e * Math.Sin(2.0 * ma);
                    double sinu = Math.Sin(u);
                    double cosu = Math.Cos(u);

                    double anr = Solsys3Pn[i] / (1.0 - e * cosu);

                    var pplan = new double[3];
                    var vplan = new double[3];
                    for (int k = 0; k < 3; k++) {
                        pplan[k] = Solsys3Init.A[k, i] * (cosu - e) + Solsys3Init.B[k, i] * sinu;
                        vplan[k] = anr * (-Solsys3Init.A[k, i] * sinu + Solsys3Init.B[k, i] * cosu);
                    }

                    double f = 1.0 / (Solsys3Pm[i] * Solsys3Init.Tmass);

                    for (int k = 0; k < 3; k++) {
                        pbary[k] += pplan[k] * f;
                        vbary[k] += vplan[k] * f;
                    }
                }

                for (i = 0; i < 3; i++) {
                    position[i] -= pbary[i];
                    velocity[i] -= vbary[i];
                }
            }

            return 0;
        }

        /// <summary>
        /// High-precision Earth/Sun position and velocity. Ported from solsys3.c 'solarsystem_hp'.
        ///
        /// IMPORTANT DEVIATION FROM THE LITERAL SOURCE: the real solsys3.c ships this function
        /// with 'action = 1' (its documented default), which unconditionally returns error 3
        /// ("this version of solarsystem not valid for use with NOVAS-C") - solsys3 alone has no
        /// high-precision capability without an external ephemeris file, so the real NOVAS-C
        /// build expects solsys1.c (with the JPL DE file) to supply high precision instead.
        /// This project deliberately has no JPL DE file and no solsys1 port (see file header), so
        /// honoring that literal default would make every NOVAS.Accuracy.Full (0) call - which is
        /// the default accuracy used throughout NINA - fail outright, since ephemeris() always
        /// tries solarsystem_hp first for accuracy == 0.
        ///
        /// Instead this port takes solsys3.c's own documented alternative, 'action = 2' (quoting
        /// its NOTES: "simply calls function 'solarsystem' and returns the low-precision position
        /// and velocity... An error code of '0' (no error) is also returned"). That is real,
        /// vendored NOVAS behavior described directly in the source - just not the file's default
        /// - and it is the only way this self-contained, file-free build can serve full-accuracy
        /// requests at all. Net effect: "full accuracy" and "reduced accuracy" both resolve to the
        /// same solsys3 low-precision theory in this build. This is the intended, documented
        /// consequence of the solsys3-instead-of-solsys1 architecture decision, not a bug.
        /// </summary>
        public static short SolarsystemHp(double[] tjd, short body, short origin, double[] position, double[] velocity) {
            double jd = tjd[0] + tjd[1];
            return Solarsystem(jd, body, origin, position, velocity);
        }

        /// <summary>
        /// Drop-in-compatible overload matching NOVAS.cs's existing 'NOVAS_solarsystem_hp' P/Invoke
        /// declaration's parameter types (Body/SolarSystemOrigin enums instead of raw short).
        /// </summary>
        public static short SolarsystemHp(double[] tjd, NOVAS.Body body, NOVAS.SolarSystemOrigin origin, double[] position, double[] velocity) {
            return SolarsystemHp(tjd, (short)body, (short)origin, position, velocity);
        }

        /// <summary>
        /// Geocentric position of the Moon, mean equator and equinox of date, in AU. NOT part of
        /// the real vendored solsys3.c (confirmed by reading the source - it has no lunar theory
        /// at all) - added to close a real gap this managed NOVAS build would otherwise have (see
        /// project documentation). This is a low-precision (~1-2 arcminute) analytical lunar
        /// theory, a Kepler-orbit-plus-largest-perturbation-terms formulation commonly attributed
        /// to Van Flandern &amp; Pulkkinen (1979) "Low-precision formulae for planetary positions"
        /// (Astrophys. J. Suppl. 41, 391) in the practical form published by Paul Schlyter,
        /// "How to compute planetary positions" (https://stjarnhimlen.se/comp/ppcomp.html) - a
        /// long-standing, widely-used public reference for exactly this class of problem, not
        /// something derived from scratch here.
        ///
        /// Verified against the complete worked example in the same author's tutorial
        /// (https://stjarnhimlen.se/comp/tutorial.html, 19 April 1990 0:00 UT) - every intermediate
        /// value (orbital elements N/i/w/e/M, eccentric anomaly E, true anomaly v, distance r,
        /// ecliptic longitude/latitude before AND after the perturbation terms) matched the
        /// published values exactly via a disposable console harness, not just eyeballed. One real
        /// finding during verification: the source text says "the initial [eccentric anomaly]
        /// approximation suffices" for the Moon's eccentricity, but the worked example's own E
        /// value only matched after iterating Kepler's equation to full Newton-Raphson convergence
        /// (the one-shot estimate was off by ~0.005 degrees) - implemented with convergence, not
        /// the one-shot form, to match the reference's actual (not stated) behavior.
        ///
        /// Accuracy is NOT sufficient for precision pointing/guiding - this is a moon-phase/
        /// avoidance-planning-grade ephemeris, consistent with the accuracy this whole
        /// solsys3-based managed build already provides for the Sun (~2-arcsecond sun_eph, itself
        /// far below SOFA/JPL precision). Distance/perturbation terms not exercised by the worked
        /// example (the r perturbation, and any longitude/latitude term not active at that
        /// specific date) are transcribed exactly from the same cited source but were not
        /// independently spot-checked beyond that one date.
        /// </summary>
        private static double[] MoonGeocentricEquatorialOfDate(double jd) {
            const double deg2rad = Math.PI / 180.0;
            const double rad2deg = 180.0 / Math.PI;
            // AU_KM and ERAD are the real NOVAS constants already declared in
            // ManagedNovas.Core.cs (same partial class - private members are shared across all
            // files of a partial class, no redeclaration needed).
            double auPerEarthRadius = AU_KM / (ERAD / 1000.0);

            double NormDeg(double x) {
                x %= 360.0;
                if (x < 0) x += 360.0;
                return x;
            }

            // Schlyter's day-number epoch: 2000 Jan 0.0 UT = 1999 Dec 31 0:00 UT = JD 2451543.5.
            // TT vs UT is not distinguished here - negligible at this algorithm's own ~1-2
            // arcminute accuracy budget.
            double d = jd - 2451543.5;

            // Moon's orbital elements.
            double N = NormDeg(125.1228 - 0.0529538083 * d);
            double inc = 5.1454;
            double w = NormDeg(318.0634 + 0.1643573223 * d);
            double a = 60.2666; // Earth radii
            double e = 0.054900;
            double M = NormDeg(115.3654 + 13.0649929509 * d);

            // Eccentric anomaly via Newton-Raphson to convergence (see doc comment above - the
            // reference's own worked example needs full convergence, not the one-shot estimate).
            double E = M + rad2deg * e * Math.Sin(M * deg2rad) * (1.0 + e * Math.Cos(M * deg2rad));
            for (int iter = 0; iter < 8; iter++) {
                double dE = (E - rad2deg * e * Math.Sin(E * deg2rad) - M) / (1.0 - e * Math.Cos(E * deg2rad));
                E -= dE;
                if (Math.Abs(dE) < 1e-9) break;
            }

            double xv = a * (Math.Cos(E * deg2rad) - e);
            double yv = a * (Math.Sqrt(1.0 - e * e) * Math.Sin(E * deg2rad));
            double v = NormDeg(Math.Atan2(yv, xv) * rad2deg);
            double r = Math.Sqrt(xv * xv + yv * yv);

            double nr = N * deg2rad, ir = inc * deg2rad, vwr = (v + w) * deg2rad;
            double xh = r * (Math.Cos(nr) * Math.Cos(vwr) - Math.Sin(nr) * Math.Sin(vwr) * Math.Cos(ir));
            double yh = r * (Math.Sin(nr) * Math.Cos(vwr) + Math.Cos(nr) * Math.Sin(vwr) * Math.Cos(ir));
            double zh = r * (Math.Sin(vwr) * Math.Sin(ir));

            double lonecl = NormDeg(Math.Atan2(yh, xh) * rad2deg);
            double latecl = Math.Atan2(zh, Math.Sqrt(xh * xh + yh * yh)) * rad2deg;

            // Sun's orbital elements, needed only for Ms in the perturbation terms below - the
            // same low-precision Sun formula this whole reference uses (not solsys3's own
            // higher-precision sun_eph, which uses different, non-interchangeable coefficients).
            double wSun = NormDeg(282.9404 + 4.70935e-5 * d);
            double mSun = NormDeg(356.0470 + 0.9856002585 * d);

            double mm = M, ms = mSun, nm = N, wm = w, ws = wSun;
            double ls = NormDeg(ms + ws);
            double lm = NormDeg(mm + wm + nm);
            double dd = NormDeg(lm - ls);
            double f = NormDeg(lm - nm);

            double dLon =
                -1.274 * Math.Sin((mm - 2 * dd) * deg2rad)
                + 0.658 * Math.Sin((2 * dd) * deg2rad)
                - 0.186 * Math.Sin(ms * deg2rad)
                - 0.059 * Math.Sin((2 * mm - 2 * dd) * deg2rad)
                - 0.057 * Math.Sin((mm - 2 * dd + ms) * deg2rad)
                + 0.053 * Math.Sin((mm + 2 * dd) * deg2rad)
                + 0.046 * Math.Sin((2 * dd - ms) * deg2rad)
                + 0.041 * Math.Sin((mm - ms) * deg2rad)
                - 0.035 * Math.Sin(dd * deg2rad)
                - 0.031 * Math.Sin((mm + ms) * deg2rad)
                - 0.015 * Math.Sin((2 * f - 2 * dd) * deg2rad)
                + 0.011 * Math.Sin((mm - 4 * dd) * deg2rad);

            double dLat =
                -0.173 * Math.Sin((f - 2 * dd) * deg2rad)
                - 0.055 * Math.Sin((mm - f - 2 * dd) * deg2rad)
                - 0.046 * Math.Sin((mm + f - 2 * dd) * deg2rad)
                + 0.033 * Math.Sin((f + 2 * dd) * deg2rad)
                + 0.017 * Math.Sin((2 * mm + f) * deg2rad);

            double dR =
                -0.58 * Math.Cos((mm - 2 * dd) * deg2rad)
                - 0.46 * Math.Cos((2 * dd) * deg2rad);

            double lonecl2 = (lonecl + dLon) * deg2rad;
            double latecl2 = (latecl + dLat) * deg2rad;
            double r2 = r + dR;

            double xh2 = r2 * Math.Cos(lonecl2) * Math.Cos(latecl2);
            double yh2 = r2 * Math.Sin(lonecl2) * Math.Cos(latecl2);
            double zh2 = r2 * Math.Sin(latecl2);

            // Ecliptic-of-date -> equatorial-of-date, via the same source's own date-dependent
            // obliquity formula.
            double ecl = (23.4393 - 3.563e-7 * d) * deg2rad;
            double xe = xh2;
            double ye = yh2 * Math.Cos(ecl) - zh2 * Math.Sin(ecl);
            double ze = yh2 * Math.Sin(ecl) + zh2 * Math.Cos(ecl);

            return new double[] { xe / auPerEarthRadius, ye / auPerEarthRadius, ze / auPerEarthRadius };
        }

        /// <summary>
        /// Equatorial spherical coordinates of the Sun referred to the mean equator and equinox of
        /// date. Ported from solsys3.c 'sun_eph'. Accuracy per the original source's NOTES is
        /// approximately 2.0 + 0.03*T^2 arcsec, T in units of 1000 years from J2000.0.
        /// </summary>
        /// <param name="jd">Julian date on TDT/ET time scale.</param>
        /// <param name="ra">Right ascension, mean equator/equinox of date, hours. Returned.</param>
        /// <param name="dec">Declination, mean equator/equinox of date, degrees. Returned.</param>
        /// <param name="dis">Geocentric distance, AU. Returned.</param>
        private static void SunEph(double jd, out double ra, out double dec, out double dis) {
            const double T0 = 2451545.00000000;
            const double twopi = 6.283185307179586476925287;
            const double asec2rad = 4.848136811095359935899141e-6;
            const double rad2deg = 57.295779513082321;

            double sumLon = 0.0;
            double sumR = 0.0;
            const double factor = 1.0e-07;

            // Table reproduced exactly from solsys3.c 'con[50]' (l, r, alpha, nu).
            var con = SunEphCon;

            double u = (jd - T0) / 3652500.0;
            double t = u * 100.0;

            for (int i = 0; i < 50; i++) {
                double arg = con[i, 2] + con[i, 3] * u;
                sumLon += con[i, 0] * Math.Sin(arg);
                sumR += con[i, 1] * Math.Cos(arg);
            }

            double lon = 4.9353929 + 62833.1961680 * u + factor * sumLon;
            lon += (-0.1371679461 - 0.2918293271 * t) * asec2rad;

            lon %= twopi;
            if (lon < 0.0) {
                lon += twopi;
            }

            dis = 1.0001026 + factor * sumR;

            double emean = (84381.406 + (-46.836769 +
                (-0.0001831 + 0.00200340 * t) * t) * t) * asec2rad;

            double sinLon = Math.Sin(lon);
            double raDeg = Math.Atan2(Math.Cos(emean) * sinLon, Math.Cos(lon)) * rad2deg;
            raDeg %= 360.0;
            if (raDeg < 0.0) {
                raDeg += 360.0;
            }
            ra = raDeg / 15.0;

            dec = Math.Asin(Math.Sin(emean) * sinLon) * rad2deg;
        }

        // solsys3.c 'con[50]' static table: columns are {l, r, alpha, nu}, reproduced exactly.
        private static readonly double[,] SunEphCon = {
            {403406.0,      0.0, 4.721964,     1.621043},
            {195207.0, -97597.0, 5.937458, 62830.348067},
            {119433.0, -59715.0, 1.115589, 62830.821524},
            {112392.0, -56188.0, 5.781616, 62829.634302},
            {  3891.0,  -1556.0, 5.5474  , 125660.5691 },
            {  2819.0,  -1126.0, 1.5120  , 125660.9845 },
            {  1721.0,   -861.0, 4.1897  ,  62832.4766 },
            {     0.0,    941.0, 1.163   ,      0.813  },
            {   660.0,   -264.0, 5.415   , 125659.310  },
            {   350.0,   -163.0, 4.315   ,  57533.850  },
            {   334.0,      0.0, 4.553   ,    -33.931  },
            {   314.0,    309.0, 5.198   , 777137.715  },
            {   268.0,   -158.0, 5.989   ,  78604.191  },
            {   242.0,      0.0, 2.911   ,      5.412  },
            {   234.0,    -54.0, 1.423   ,  39302.098  },
            {   158.0,      0.0, 0.061   ,    -34.861  },
            {   132.0,    -93.0, 2.317   , 115067.698  },
            {   129.0,    -20.0, 3.193   ,  15774.337  },
            {   114.0,      0.0, 2.828   ,   5296.670  },
            {    99.0,    -47.0, 0.52    ,  58849.27   },
            {    93.0,      0.0, 4.65    ,   5296.11   },
            {    86.0,      0.0, 4.35    ,  -3980.70   },
            {    78.0,    -33.0, 2.75    ,  52237.69   },
            {    72.0,    -32.0, 4.50    ,  55076.47   },
            {    68.0,      0.0, 3.23    ,    261.08   },
            {    64.0,    -10.0, 1.22    ,  15773.85   },
            {    46.0,    -16.0, 0.14    ,  188491.03  },
            {    38.0,      0.0, 3.44    ,   -7756.55  },
            {    37.0,      0.0, 4.37    ,     264.89  },
            {    32.0,    -24.0, 1.14    ,  117906.27  },
            {    29.0,    -13.0, 2.84    ,   55075.75  },
            {    28.0,      0.0, 5.96    ,   -7961.39  },
            {    27.0,     -9.0, 5.09    ,  188489.81  },
            {    27.0,      0.0, 1.72    ,    2132.19  },
            {    25.0,    -17.0, 2.56    ,  109771.03  },
            {    24.0,    -11.0, 1.92    ,   54868.56  },
            {    21.0,      0.0, 0.09    ,   25443.93  },
            {    21.0,     31.0, 5.98    ,  -55731.43  },
            {    20.0,    -10.0, 4.03    ,   60697.74  },
            {    18.0,      0.0, 4.27    ,    2132.79  },
            {    17.0,    -12.0, 0.79    ,  109771.63  },
            {    14.0,      0.0, 4.24    ,   -7752.82  },
            {    13.0,     -5.0, 2.01    ,  188491.91  },
            {    13.0,      0.0, 2.65    ,     207.81  },
            {    13.0,      0.0, 4.98    ,   29424.63  },
            {    12.0,      0.0, 0.93    ,      -7.99  },
            {    10.0,      0.0, 2.21    ,   46941.14  },
            {    10.0,      0.0, 3.59    ,     -68.29  },
            {    10.0,      0.0, 1.50    ,   21463.25  },
            {    10.0,     -9.0, 2.55    ,  157208.40  },
        };

        #endregion "solsys3.c - self-contained Sun/Earth ephemeris"

        #region "novas.c - apparent-place orchestration chain"

        /// <summary>
        /// Retrieves the position and velocity of a solar system body. Ported from novas.c
        /// 'ephemeris'.
        /// </summary>
        /// <param name="jd">TDB Julian date split into two parts (jd[0] + jd[1]).</param>
        /// <param name="celObj">Designation of the body of interest.</param>
        /// <param name="origin">0 = solar system barycenter, 1 = center of mass of the Sun.</param>
        /// <param name="accuracy">0 = full accuracy, 1 = reduced accuracy.</param>
        /// <param name="pos">Position vector of the body, ICRS, AU. Returned.</param>
        /// <param name="vel">Velocity vector of the body, ICRS, AU/day. Returned.</param>
        public static short Ephemeris(double[] jd, NOVAS.CelestialObject celObj, short origin, short accuracy, double[] pos, double[] vel) {
            short error;

            if ((origin < 0) || (origin > 1)) {
                return 1;
            }

            switch (celObj.Type) {
                case 0:
                    // Major planet, Pluto, Sun, or Moon.
                    short ssNumber = celObj.Number;
                    if (accuracy == 0) {
                        if ((error = SolarsystemHp(jd, ssNumber, origin, pos, vel)) != 0) {
                            error = (short)(error + 10);
                        }
                    } else {
                        double jdTdb = jd[0] + jd[1];
                        if ((error = Solarsystem(jdTdb, ssNumber, origin, pos, vel)) != 0) {
                            error = (short)(error + 10);
                        }
                    }
                    break;

                case 1:
                    // Minor planet ('readeph' ephemeris access) is intentionally out of scope for
                    // this managed port: no minor-planet ephemeris source is vendored or available
                    // in this project, and NINA's NOVAS.Body enum (see NOVAS.cs) never constructs
                    // a type-1 (minor planet) CelestialObject, so this path is not exercised by
                    // the application. Returns 3, mirroring the real function's "unable to
                    // allocate/obtain data" failure code for this situation.
                    error = 3;
                    break;

                default:
                    error = 2;
                    break;
            }

            return error;
        }

        /// <summary>
        /// Computes the geocentric position and velocity of an observer on the Earth's surface or
        /// a near-Earth spacecraft, expressed in the GCRS. Ported from novas.c 'geo_posvel'.
        ///
        /// Drop-in-compatible with NOVAS.cs's existing 'NOVAS_geo_posvel' P/Invoke declaration
        /// (same parameter count/order/types; 'pos'/'vel' are pre-allocated double[3] arrays
        /// written in place, matching the existing wrapper's array convention).
        /// </summary>
        public static short GeoPosvel(double jdtt, double deltaT, NOVAS.Accuracy accuracy, NOVAS.Observer obs, double[] pos, double[] vel) {
            const double auKm = 1.4959787069098932e+8;
            const double t0 = 2451545.00000000;

            short acc = (short)accuracy;
            if (acc < 0 || acc > 1) {
                return 1;
            }

            double jdTdb = jdtt;
            double x = 0, secdif = 0;
            Tdb2Tt(jdTdb, ref x, ref secdif);
            jdTdb = jdtt + secdif / 86400.0;

            var pos1 = new double[3];
            var vel1 = new double[3];

            switch (obs.Where) {
                case 0:
                    // Observer at geocenter. Trivial case.
                    pos[0] = pos[1] = pos[2] = 0.0;
                    vel[0] = vel[1] = vel[2] = 0.0;
                    return 0;

                case 1: {
                    // Observer on surface of Earth.
                    double jdUt1 = jdtt - (deltaT / 86400.0);
                    double gmst = 0.0;
                    SiderealTime(jdUt1, 0.0, deltaT, (short)NOVAS.GstType.GreenwichMeanSiderealTime, (short)NOVAS.Method.EquinoxBased, (short)accuracy, ref gmst);
                    double x1 = 0, x2 = 0, eqeq = 0, x3 = 0, x4 = 0;
                    ETilt(jdTdb, acc, ref x1, ref x2, ref eqeq, ref x3, ref x4);
                    double gast = gmst + eqeq / 3600.0;

                    var onSurf = obs.OnSurf;
                    Terra(ref onSurf, gast, pos1, vel1);
                    break;
                }

                case 2: {
                    // Observer on near-earth spacecraft. Convert km, km/s -> AU, AU/day.
                    double fac = auKm / 86400.0;

                    pos1[0] = obs.NearEarth.ScPos[0] / auKm;
                    pos1[1] = obs.NearEarth.ScPos[1] / auKm;
                    pos1[2] = obs.NearEarth.ScPos[2] / auKm;

                    vel1[0] = obs.NearEarth.ScVel[0] / fac;
                    vel1[1] = obs.NearEarth.ScVel[1] / fac;
                    vel1[2] = obs.NearEarth.ScVel[2] / fac;
                    break;
                }

                default:
                    return 1;
            }

            // Transform geocentric position vector of observer to GCRS.
            var pos2 = new double[3];
            var pos3 = new double[3];
            Nutation(jdTdb, -1, acc, pos1, pos2);
            Precession(jdTdb, pos2, t0, pos3);
            FrameTie(pos3, -1, pos);

            // Transform geocentric velocity vector of observer to GCRS.
            var vel2 = new double[3];
            var vel3 = new double[3];
            Nutation(jdTdb, -1, acc, vel1, vel2);
            Precession(jdTdb, vel2, t0, vel3);
            FrameTie(vel3, -1, vel);

            return 0;
        }

        /// <summary>
        /// Computes the geocentric position of a solar system body, antedated for light-time.
        /// Ported from novas.c 'light_time'.
        /// </summary>
        public static short LightTime(double jdTdb, NOVAS.CelestialObject ssObject, double[] posObs, double tlight0, short accuracy, double[] pos, out double tlight) {
            short error = 0;
            int iter = 0;

            double tol, t1, t2, t3 = 0.0;
            var jd = new double[2];

            if (accuracy == 0) {
                tol = 1.0e-12;
                jd[0] = (double)(long)jdTdb;
                t1 = jdTdb - jd[0];
                t2 = t1 - tlight0;
            } else {
                tol = 1.0e-9;
                jd[0] = 0.0;
                jd[1] = 0.0;
                t1 = jdTdb;
                t2 = jdTdb - tlight0;
            }

            var pos1 = new double[3];
            var vel1 = new double[3];
            tlight = 0.0;

            do {
                if (iter > 10) {
                    error = 1;
                    tlight = 0.0;
                    break;
                }

                if (iter > 0) {
                    t2 = t3;
                }

                jd[1] = t2;
                error = Ephemeris(jd, ssObject, 0, accuracy, pos1, vel1);

                if (error != 0) {
                    error = (short)(error + 10);
                    tlight = 0.0;
                    break;
                }

                Bary2Obs(pos1, posObs, pos, ref tlight);

                t3 = t1 - tlight;
                iter++;
            } while (Math.Abs(t3 - t2) > tol);

            return error;
        }

        // Lazily-created static objects mirroring place()'s C 'static ... first_time' pattern
        // (NULL_STAR cat_entry, and Earth/Sun objects), using the same Lazy<T> idiom NOVAS.cs
        // already uses for its own 'dummy_star'. Avoids declaring a static constructor here,
        // which would collide with any static constructor declared in the other partial files
        // of this same class.
        private static readonly Lazy<NOVAS.CatalogueEntry> PlaceNullStar = new Lazy<NOVAS.CatalogueEntry>(() => {
            var star = default(NOVAS.CatalogueEntry);
            MakeCatEntry("NULL_STAR", "   ", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, ref star);
            return star;
        });

        private static readonly Lazy<NOVAS.CelestialObject> PlaceEarth = new Lazy<NOVAS.CelestialObject>(() => {
            var obj = default(NOVAS.CelestialObject);
            MakeObject((short)NOVAS.ObjectType.MajorPlanetSunOrMoon, 3, "Earth", PlaceNullStar.Value, ref obj);
            return obj;
        });

        private static readonly Lazy<NOVAS.CelestialObject> PlaceSun = new Lazy<NOVAS.CelestialObject>(() => {
            var obj = default(NOVAS.CelestialObject);
            MakeObject((short)NOVAS.ObjectType.MajorPlanetSunOrMoon, 10, "Sun", PlaceNullStar.Value, ref obj);
            return obj;
        });

        /// <summary>
        /// This function computes the apparent direction of a star or solar system body at a
        /// specified time and in a specified coordinate system. Ported from novas.c 'place'.
        ///
        /// Drop-in-compatible with NOVAS.cs's existing 'NOVAS_Place' P/Invoke declaration (same
        /// parameter count/order/types, 'ref' for struct pointer params matching the existing
        /// wrapper's convention, 'short' return for the C 'short int' status code).
        ///
        /// Note: the original C caches Earth/Sun barycentric position+velocity and the CIO basis
        /// vectors across calls via static 'tlast1'/'tlast2' guards, purely as a performance
        /// optimization (recomputation from the same inputs is deterministic and numerically
        /// identical). This port always recomputes, trading a little performance for not needing
        /// extra shared mutable state in a library now used from a multi-threaded managed app.
        /// </summary>
        public static short Place(double jdTt, ref NOVAS.CelestialObject celObject, ref NOVAS.Observer location, double deltaT, short coordSys, short accuracy, ref NOVAS.SkyPosition output) {
            const double t0 = 2451545.00000000;
            const double cAuday = 173.1446326846693;

            short error;
            int i;

            if ((coordSys < 0) || (coordSys > 3)) {
                return 1;
            }

            if ((accuracy < 0) || (accuracy > 1)) {
                return 2;
            }

            // Earth can only be an observed object when 'location' is a near-Earth satellite.
            if ((celObject.Type == 0) && (celObject.Number == 3) && (location.Where != 2)) {
                return 3;
            }

            var earth = PlaceEarth.Value;
            var sun = PlaceSun.Value;

            double jdTdb = jdTt;
            double x = 0, secdif = 0;
            Tdb2Tt(jdTdb, ref x, ref secdif);
            jdTdb = jdTt + secdif / 86400.0;

            var jd = new double[] { jdTdb, 0.0 };

            var peb = new double[3];
            var veb = new double[3];
            if ((error = Ephemeris(jd, earth, 0, accuracy, peb, veb)) != 0) {
                return (short)(error + 10);
            }

            var psb = new double[3];
            var vsb = new double[3];
            if ((error = Ephemeris(jd, sun, 0, accuracy, psb, vsb)) != 0) {
                return (short)(error + 10);
            }

            // Position and velocity of observer.
            var pog = new double[3];
            var vog = new double[3];
            short loc;

            if ((location.Where == 1) || (location.Where == 2)) {
                if ((error = GeoPosvel(jdTt, deltaT, (NOVAS.Accuracy)accuracy, location, pog, vog)) != 0) {
                    return (short)(error + 40);
                }
                loc = 1;
            } else {
                // Geocentric place: observer is at geocenter, pog/vog remain zero.
                loc = 0;
            }

            var pob = new double[3];
            var vob = new double[3];
            for (i = 0; i < 3; i++) {
                pob[i] = peb[i] + pog[i];
                vob[i] = veb[i] + vog[i];
            }

            // Geometric position of observed object.
            var pos1 = new double[3];
            var vel1 = new double[3];
            var pos3 = new double[3];
            double tLight;

            if (celObject.Type == 2) {
                // Observed object is a star.
                var star = celObject.Star;
                Starvectors(star, pos1, vel1);
                double dt = DLight(pos1, pob);

                var pos2 = new double[3];
                ProperMotion(t0, pos1, vel1, jdTdb + dt, pos2);

                tLight = 0;
                Bary2Obs(pos2, pob, pos3, ref tLight);
                output.Dis = 0.0;
            } else {
                // Observed object is a solar system body.
                if ((error = Ephemeris(jd, celObject, 0, accuracy, pos1, vel1)) != 0) {
                    return (short)(error + 10);
                }

                var pos2 = new double[3];
                double tLight0 = 0;
                Bary2Obs(pos1, pob, pos2, ref tLight0);
                output.Dis = tLight0 * cAuday;

                if ((error = LightTime(jdTdb, celObject, pob, tLight0, accuracy, pos3, out tLight)) != 0) {
                    return (short)(error + 50);
                }
            }

            // Gravitational deflection of light and aberration.
            var pos5 = new double[3];

            if (coordSys == 3) {
                // Skipped for astrometric place.
                Array.Copy(pos3, pos5, 3);
            } else {
                if (loc == 1) {
                    double limbX = 0, frlimb = 0;
                    LimbAngle(pos3, pog, ref limbX, ref frlimb);
                    if (frlimb < 0.8) {
                        loc = 0;
                    }
                }

                var pos4 = new double[3];
                if ((error = GravDef(jdTdb, loc, accuracy, pos3, pob, pos4)) != 0) {
                    return (short)(error + 70);
                }

                Aberration(pos4, vob, tLight, pos5);
            }

            // Transform to output coordinate system.
            var pos8 = new double[3];

            switch (coordSys) {
                case 1: {
                    // Equator and equinox of date.
                    var pos6 = new double[3];
                    var pos7 = new double[3];
                    FrameTie(pos5, 1, pos6);
                    Precession(t0, pos6, jdTdb, pos7);
                    Nutation(jdTdb, 0, accuracy, pos7, pos8);
                    break;
                }

                case 2: {
                    // Equator and CIO of date.
                    if ((error = CioLocation(jdTdb, accuracy, out double rCio, out short rs)) != 0) {
                        return (short)(error + 80);
                    }

                    var px = new double[3];
                    var py = new double[3];
                    var pz = new double[3];
                    if ((error = CioBasis(jdTdb, rCio, rs, accuracy, px, py, pz)) != 0) {
                        return (short)(error + 90);
                    }

                    pos8[0] = px[0] * pos5[0] + px[1] * pos5[1] + px[2] * pos5[2];
                    pos8[1] = py[0] * pos5[0] + py[1] * pos5[1] + py[2] * pos5[2];
                    pos8[2] = pz[0] * pos5[0] + pz[1] * pos5[1] + pz[2] * pos5[2];
                    break;
                }

                default:
                    // No transformation: keep GCRS, or ICRS for astrometric coordinates.
                    Array.Copy(pos5, pos8, 3);
                    break;
            }

            // Radial velocity.
            double dObsGeo = Math.Sqrt(
                (pob[0] - peb[0]) * (pob[0] - peb[0]) +
                (pob[1] - peb[1]) * (pob[1] - peb[1]) +
                (pob[2] - peb[2]) * (pob[2] - peb[2]));

            double dObsSun = Math.Sqrt(
                (pob[0] - psb[0]) * (pob[0] - psb[0]) +
                (pob[1] - psb[1]) * (pob[1] - psb[1]) +
                (pob[2] - psb[2]) * (pob[2] - psb[2]));

            double dObjSun = Math.Sqrt(
                (pos1[0] - psb[0]) * (pos1[0] - psb[0]) +
                (pos1[1] - psb[1]) * (pos1[1] - psb[1]) +
                (pos1[2] - psb[2]) * (pos1[2] - psb[2]));

            double rv = 0;
            RadVel(celObject, pos3, vel1, vob, dObsGeo, dObsSun, dObjSun, ref rv);
            output.RV = rv;

            // Finish up.
            double ra = 0, dec = 0;
            Vector2Radec(pos8, ref ra, ref dec);
            output.RA = ra;
            output.Dec = dec;

            double xn = Math.Sqrt(pos8[0] * pos8[0] + pos8[1] * pos8[1] + pos8[2] * pos8[2]);

            if (output.RHat == null) {
                output.RHat = new double[3];
            }
            for (i = 0; i < 3; i++) {
                output.RHat[i] = pos8[i] / xn;
            }

            return 0;
        }

        /// <summary>
        /// Computes the apparent place of a star at date 'jd_tt'. Ported from novas.c 'app_star'.
        /// </summary>
        public static short AppStar(double jdTt, ref NOVAS.CatalogueEntry star, short accuracy, out double ra, out double dec) {
            short error;

            var celObj = default(NOVAS.CelestialObject);
            if ((error = MakeObject((short)NOVAS.ObjectType.ObjectLocatedOutsideSolarSystem, 0, star.StarName, star, ref celObj)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 10);
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 1; // True equator and equinox of date

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref celObj, ref location, 0.0, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 20);
            }

            ra = output.RA;
            dec = output.Dec;
            return 0;
        }

        /// <summary>
        /// Computes the virtual place of a star at date 'jd_tt'. Ported from novas.c 'virtual_star'.
        /// </summary>
        public static short VirtualStar(double jdTt, ref NOVAS.CatalogueEntry star, short accuracy, out double ra, out double dec) {
            short error;

            var celObj = default(NOVAS.CelestialObject);
            if ((error = MakeObject((short)NOVAS.ObjectType.ObjectLocatedOutsideSolarSystem, 0, star.StarName, star, ref celObj)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 10);
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 0; // GCRS

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref celObj, ref location, 0.0, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 20);
            }

            ra = output.RA;
            dec = output.Dec;
            return 0;
        }

        /// <summary>
        /// Computes the astrometric place of a star at date 'jd_tt'. Ported from novas.c 'astro_star'.
        /// </summary>
        public static short AstroStar(double jdTt, ref NOVAS.CatalogueEntry star, short accuracy, out double ra, out double dec) {
            short error;

            var celObj = default(NOVAS.CelestialObject);
            if ((error = MakeObject((short)NOVAS.ObjectType.ObjectLocatedOutsideSolarSystem, 0, star.StarName, star, ref celObj)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 10);
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 3; // ICRS astrometric coordinates

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref celObj, ref location, 0.0, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 20);
            }

            ra = output.RA;
            dec = output.Dec;
            return 0;
        }

        /// <summary>
        /// Compute the apparent place of a solar system body. Ported from novas.c 'app_planet'.
        ///
        /// Drop-in-compatible with NOVAS.cs's existing 'NOVAS_app_planet' P/Invoke declaration
        /// (same parameter count/order/types, 'out' for the pointer-outputs).
        /// </summary>
        public static short AppPlanet(double jdTt, NOVAS.CelestialObject ssBody, NOVAS.Accuracy accuracy, out double ra, out double dec, out double dis) {
            if ((ssBody.Type < 0) || (ssBody.Type > 1)) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return 1;
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 1; // True equator and equinox of date

            var output = default(NOVAS.SkyPosition);
            short error;
            if ((error = Place(jdTt, ref ssBody, ref location, 0.0, coordSys, (short)accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return (short)(error + 10);
            }

            ra = output.RA;
            dec = output.Dec;
            dis = output.Dis;
            return 0;
        }

        /// <summary>
        /// Compute the virtual place of a solar system body. Ported from novas.c 'virtual_planet'.
        /// </summary>
        public static short VirtualPlanet(double jdTt, NOVAS.CelestialObject ssBody, short accuracy, out double ra, out double dec, out double dis) {
            if ((ssBody.Type < 0) || (ssBody.Type > 1)) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return 1;
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 0; // GCRS

            var output = default(NOVAS.SkyPosition);
            short error;
            if ((error = Place(jdTt, ref ssBody, ref location, 0.0, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return (short)(error + 10);
            }

            ra = output.RA;
            dec = output.Dec;
            dis = output.Dis;
            return 0;
        }

        /// <summary>
        /// Compute the astrometric place of a solar system body. Ported from novas.c 'astro_planet'.
        /// </summary>
        public static short AstroPlanet(double jdTt, NOVAS.CelestialObject ssBody, short accuracy, out double ra, out double dec, out double dis) {
            if ((ssBody.Type < 0) || (ssBody.Type > 1)) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return 1;
            }

            var location = default(NOVAS.Observer);
            location.Where = 0; // Geocenter
            short coordSys = 3; // ICRS astrometric coordinates

            var output = default(NOVAS.SkyPosition);
            short error;
            if ((error = Place(jdTt, ref ssBody, ref location, 0.0, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return (short)(error + 10);
            }

            ra = output.RA;
            dec = output.Dec;
            dis = output.Dis;
            return 0;
        }

        /// <summary>
        /// Computes the topocentric place of a star. Ported from novas.c 'topo_star'.
        /// </summary>
        public static short TopoStar(double jdTt, double deltaT, ref NOVAS.CatalogueEntry star, ref NOVAS.OnSurface position, short accuracy, out double ra, out double dec) {
            var dummy = new NOVAS.InSpace { ScPos = new double[3], ScVel = new double[3] };

            short error;
            var location = default(NOVAS.Observer);
            if ((error = MakeObserver(1, position, dummy, ref location)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return 1;
            }

            var celObj = default(NOVAS.CelestialObject);
            if ((error = MakeObject((short)NOVAS.ObjectType.ObjectLocatedOutsideSolarSystem, 0, star.StarName, star, ref celObj)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 10);
            }

            short coordSys = 1; // True equator and equinox of date

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref celObj, ref location, deltaT, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 20);
            }

            ra = output.RA;
            dec = output.Dec;
            return 0;
        }

        /// <summary>
        /// Computes the local place of a star. Ported from novas.c 'local_star'.
        /// </summary>
        public static short LocalStar(double jdTt, double deltaT, ref NOVAS.CatalogueEntry star, ref NOVAS.OnSurface position, short accuracy, out double ra, out double dec) {
            var dummy = new NOVAS.InSpace { ScPos = new double[3], ScVel = new double[3] };

            short error;
            var location = default(NOVAS.Observer);
            if ((error = MakeObserver(1, position, dummy, ref location)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return 1;
            }

            var celObj = default(NOVAS.CelestialObject);
            if ((error = MakeObject((short)NOVAS.ObjectType.ObjectLocatedOutsideSolarSystem, 0, star.StarName, star, ref celObj)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 10);
            }

            short coordSys = 0; // "Local GCRS"

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref celObj, ref location, deltaT, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(error + 20);
            }

            ra = output.RA;
            dec = output.Dec;
            return 0;
        }

        /// <summary>
        /// Computes the topocentric place of a solar system body. Ported from novas.c 'topo_planet'.
        /// </summary>
        public static short TopoPlanet(double jdTt, NOVAS.CelestialObject ssBody, double deltaT, ref NOVAS.OnSurface position, short accuracy, out double ra, out double dec, out double dis) {
            var dummy = new NOVAS.InSpace { ScPos = new double[3], ScVel = new double[3] };

            short error;
            var location = default(NOVAS.Observer);
            if ((error = MakeObserver(1, position, dummy, ref location)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return 1;
            }

            short coordSys = 1; // True equator and equinox of date

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref ssBody, ref location, deltaT, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return (short)(error + 10);
            }

            ra = output.RA;
            dec = output.Dec;
            dis = output.Dis;
            return 0;
        }

        /// <summary>
        /// Computes the local place of a solar system body. Ported from novas.c 'local_planet'.
        /// </summary>
        public static short LocalPlanet(double jdTt, NOVAS.CelestialObject ssBody, double deltaT, ref NOVAS.OnSurface position, short accuracy, out double ra, out double dec, out double dis) {
            var dummy = new NOVAS.InSpace { ScPos = new double[3], ScVel = new double[3] };

            short error;
            var location = default(NOVAS.Observer);
            if ((error = MakeObserver(1, position, dummy, ref location)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return 1;
            }

            short coordSys = 0; // "Local GCRS"

            var output = default(NOVAS.SkyPosition);
            if ((error = Place(jdTt, ref ssBody, ref location, deltaT, coordSys, accuracy, ref output)) != 0) {
                ra = 0.0;
                dec = 0.0;
                dis = 0.0;
                return (short)(error + 10);
            }

            ra = output.RA;
            dec = output.Dec;
            dis = output.Dis;
            return 0;
        }

        /// <summary>
        /// Computes the ICRS position of a star, given its apparent place at date 'jd_tt'. Proper
        /// motion, parallax and radial velocity are assumed zero. Ported from novas.c 'mean_star'.
        /// </summary>
        public static short MeanStar(double jdTt, double ra, double dec, short accuracy, out double ira, out double idec) {
            const double t0 = 2451545.00000000;

            short error;

            var tempstar = default(NOVAS.CatalogueEntry);
            if ((error = MakeCatEntry("dummy", "CAT", 0, ra, dec, 0.0, 0.0, 0.0, 0.0, ref tempstar)) != 0) {
                ira = 0.0;
                idec = 0.0;
                return (short)(error + 1);
            }

            var pos = new double[3];
            var dum = new double[3];
            Starvectors(tempstar, pos, dum);

            // Initial approximation: precess star position at 'jd_tt' to J2000.0.
            var pos2 = new double[3];
            Precession(jdTt, pos, t0, pos2);
            double newira = 0, newidec = 0;
            if ((error = Vector2Radec(pos2, ref newira, ref newidec)) != 0) {
                ira = 0.0;
                idec = 0.0;
                return (short)(error + 10);
            }

            int iter = 0;
            double oldira, oldidec, ra2, dec2, deltara, deltadec;

            do {
                oldira = newira;
                oldidec = newidec;
                tempstar.RA = oldira;
                tempstar.Dec = oldidec;

                if ((error = AppStar(jdTt, ref tempstar, accuracy, out ra2, out dec2)) != 0) {
                    ira = 0.0;
                    idec = 0.0;
                    return (short)(error + 20);
                }

                deltara = ra2 - oldira;
                deltadec = dec2 - oldidec;
                if (deltara < -12.0) {
                    deltara += 24.0;
                }
                if (deltara > 12.0) {
                    deltara -= 24.0;
                }
                newira = ra - deltara;
                newidec = dec - deltadec;

                if (iter >= 30) {
                    ira = 0.0;
                    idec = 0.0;
                    return 1;
                }
                iter++;
            } while (!(Math.Abs(newira - oldira) <= 1.0e-12) || !(Math.Abs(newidec - oldidec) <= 1.0e-11));

            ira = newira;
            idec = newidec;
            if (ira < 0.0) {
                ira += 24.0;
            }
            if (ira >= 24.0) {
                ira -= 24.0;
            }

            return 0;
        }

        /// <summary>
        /// Converts GCRS right ascension and declination to coordinates with respect to the
        /// equator of date (mean or true; equinox- or CIO-based). Ported from novas.c 'gcrs2equ'.
        /// </summary>
        /// <param name="coordSys">0 = mean equator/equinox of date, 1 = true equator/equinox of date, 2 = true equator and CIO of date.</param>
        public static short Gcrs2Equ(double jdTt, short coordSys, short accuracy, double rag, double decg, out double ra, out double dec) {
            const double deg2Rad = 0.017453292519943296;
            const double t0 = 2451545.00000000;

            short error = 0;

            double t = 0, secdiff = 0;
            Tdb2Tt(jdTt, ref t, ref secdiff);
            double t1 = jdTt + secdiff / 86400.0;

            double r = rag * 15.0 * deg2Rad;
            double d = decg * deg2Rad;

            var pos1 = new double[3];
            pos1[0] = Math.Cos(d) * Math.Cos(r);
            pos1[1] = Math.Cos(d) * Math.Sin(r);
            pos1[2] = Math.Sin(d);

            var pos4 = new double[3];

            if (coordSys <= 1) {
                // GCRS -> mean equator and equinox of date.
                var pos2 = new double[3];
                var pos3 = new double[3];
                FrameTie(pos1, 1, pos2);
                Precession(t0, pos2, t1, pos3);

                if (coordSys == 1) {
                    Nutation(t1, 0, accuracy, pos3, pos4);
                } else {
                    Array.Copy(pos3, pos4, 3);
                }
            } else {
                // True equator and CIO of date.
                if ((error = CioLocation(t1, accuracy, out double rCio, out short rs)) != 0) {
                    ra = 0.0;
                    dec = 0.0;
                    return (short)(error + 10);
                }

                var xv = new double[3];
                var yv = new double[3];
                var zv = new double[3];
                if ((error = CioBasis(t1, rCio, rs, accuracy, xv, yv, zv)) != 0) {
                    ra = 0.0;
                    dec = 0.0;
                    return (short)(error + 20);
                }

                pos4[0] = xv[0] * pos1[0] + xv[1] * pos1[1] + xv[2] * pos1[2];
                pos4[1] = yv[0] * pos1[0] + yv[1] * pos1[1] + yv[2] * pos1[2];
                pos4[2] = zv[0] * pos1[0] + zv[1] * pos1[1] + zv[2] * pos1[2];
            }

            ra = 0.0;
            dec = 0.0;
            if ((error = Vector2Radec(pos4, ref ra, ref dec)) != 0) {
                ra = 0.0;
                dec = 0.0;
                return (short)(-1 * error);
            }

            return 0;
        }

        #endregion "novas.c - apparent-place orchestration chain"
    }
}
