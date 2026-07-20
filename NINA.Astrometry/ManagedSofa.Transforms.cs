// This file contains a derived work based on routines and computations from the
// IAU SOFA (Standards of Fundamental Astronomy) software collection, used under
// license. It does not itself constitute software provided by and/or endorsed by
// SOFA. See http://www.iausofa.org for the original software and full license terms.
//
// Per SOFA license condition (c): routine names in this derived work do not use
// the "iau" or "sofa" prefix used by the original SOFA routines they are based on.

using System;

namespace NINA.Astrometry {

    public static partial class ManagedSofa {

        /// <summary>
        /// Apply aberration to transform natural direction into proper direction.
        /// Ported from SOFA/SOFA/src/ab.c (iauAb).
        /// </summary>
        /// <param name="pnat">double[3] natural direction to the source (unit vector)</param>
        /// <param name="v">double[3] observer barycentric velocity in units of c</param>
        /// <param name="s">double distance between the Sun and the observer (au)</param>
        /// <param name="bm1">double sqrt(1-|v|^2): reciprocal of Lorenz factor</param>
        /// <param name="ppr">double[3] proper direction to source (unit vector) (returned)</param>
        public static void Ab(double[] pnat, double[] v, double s, double bm1, double[] ppr) {
            /* Schwarzschild radius of the Sun (au) */
            const double SRS = 1.97412574336e-8;

            int i;
            double pdv, w1, w2, r2, w, r;
            double[] p = new double[3];

            pdv = Pdp(pnat, v);
            w1 = 1.0 + pdv / (1.0 + bm1);
            w2 = SRS / s;
            r2 = 0.0;
            for (i = 0; i < 3; i++) {
                w = pnat[i] * bm1 + w1 * v[i] + w2 * (v[i] - pdv * pnat[i]);
                p[i] = w;
                r2 = r2 + w * w;
            }
            r = Math.Sqrt(r2);
            for (i = 0; i < 3; i++) {
                ppr[i] = p[i] / r;
            }
        }

        /// <summary>
        /// Apply light deflection by a solar-system body, as part of transforming
        /// coordinate direction into natural direction.
        /// Ported from SOFA/SOFA/src/ld.c (iauLd).
        /// </summary>
        /// <param name="bm">double mass of the gravitating body (solar masses)</param>
        /// <param name="p">double[3] direction from observer to source (unit vector)</param>
        /// <param name="q">double[3] direction from body to source (unit vector)</param>
        /// <param name="e">double[3] direction from body to observer (unit vector)</param>
        /// <param name="em">double distance from body to observer (au)</param>
        /// <param name="dlim">double deflection limiter</param>
        /// <param name="p1">double[3] observer to deflected source (unit vector) (returned)</param>
        public static void Ld(double bm, double[] p, double[] q, double[] e, double em, double dlim, double[] p1) {
            /* Schwarzschild radius of the Sun (au) */
            const double SRS = 1.97412574336e-8;

            int i;
            double qdqpe, w;
            double[] qpe = new double[3];
            double[] eq = new double[3];
            double[] peq = new double[3];

            /* q . (q + e). */
            for (i = 0; i < 3; i++) {
                qpe[i] = q[i] + e[i];
            }
            qdqpe = Pdp(q, qpe);

            /* 2 x G x bm / ( em x c^2 x ( q . (q + e) ) ). */
            w = bm * SRS / em / Math.Max(qdqpe, dlim);

            /* p x (e x q). */
            Pxp(e, q, eq);
            Pxp(p, eq, peq);

            /* Apply the deflection. */
            for (i = 0; i < 3; i++) {
                p1[i] = p[i] + w * peq[i];
            }
        }

        /// <summary>
        /// Deflection of starlight by the Sun.
        /// Ported from SOFA/SOFA/src/ldsun.c (iauLdsun).
        /// </summary>
        /// <param name="p">double[3] direction from observer to star (unit vector)</param>
        /// <param name="e">double[3] direction from Sun to observer (unit vector)</param>
        /// <param name="em">double distance from Sun to observer (au)</param>
        /// <param name="p1">double[3] observer to deflected star (unit vector) (returned)</param>
        public static void Ldsun(double[] p, double[] e, double em, double[] p1) {
            double em2, dlim;

            /* Deflection limiter (smaller for distant observers). */
            em2 = em * em;
            if (em2 < 1.0) em2 = 1.0;
            dlim = 1e-6 / (em2 > 1.0 ? em2 : 1.0);

            /* Apply the deflection. */
            Ld(1.0, p, p, e, em, dlim, p1);
        }

        /// <summary>
        /// Proper motion and parallax.
        /// Ported from SOFA/SOFA/src/pmpx.c (iauPmpx).
        /// </summary>
        /// <param name="rc">double ICRS RA at catalog epoch (radians)</param>
        /// <param name="dc">double ICRS Dec at catalog epoch (radians)</param>
        /// <param name="pr">double RA proper motion (radians/year)</param>
        /// <param name="pd">double Dec proper motion (radians/year)</param>
        /// <param name="px">double parallax (arcsec)</param>
        /// <param name="rv">double radial velocity (km/s, +ve if receding)</param>
        /// <param name="pmt">double proper motion time interval (SSB, Julian years)</param>
        /// <param name="pob">double[3] SSB to observer vector (au)</param>
        /// <param name="pco">double[3] coordinate direction (BCRS unit vector) (returned)</param>
        public static void Pmpx(double rc, double dc, double pr, double pd, double px, double rv, double pmt, double[] pob, double[] pco) {
            const double DAYSEC = 86400.0;
            const double DJY = 365.25;
            const double DJM = 365250.0;
            const double DAU = 149597870.7e3;
            const double CMPS = 299792458.0;
            const double AULT = DAU / CMPS;

            /* Km/s to au/year */
            double VF = DAYSEC * DJM / DAU;

            /* Light time for 1 au, Julian years */
            double AULTY = AULT / DAYSEC / DJY;

            int i;
            double sr, cr, sd, cd, x, y, z, dt, pxr, w, pdz;
            double[] p = new double[3];
            double[] pm = new double[3];

            /* Spherical coordinates to unit vector (and useful functions). */
            sr = Math.Sin(rc);
            cr = Math.Cos(rc);
            sd = Math.Sin(dc);
            cd = Math.Cos(dc);
            p[0] = x = cr * cd;
            p[1] = y = sr * cd;
            p[2] = z = sd;

            /* Proper motion time interval (y) including Roemer effect. */
            dt = pmt + Pdp(p, pob) * AULTY;

            /* Space motion (radians per year). */
            pxr = px * DAS2R;
            w = VF * rv * pxr;
            pdz = pd * z;
            pm[0] = -pr * y - pdz * cr + w * x;
            pm[1] = pr * x - pdz * sr + w * y;
            pm[2] = pd * cd + w * z;

            /* Coordinate direction of star (unit vector, BCRS). */
            for (i = 0; i < 3; i++) {
                p[i] += dt * pm[i] - pxr * pob[i];
            }
            Pn(p, out w, pco);
        }

        /// <summary>
        /// Position and velocity of a terrestrial observing station.
        /// Ported from SOFA/SOFA/src/pvtob.c (iauPvtob).
        /// </summary>
        /// <param name="elong">double longitude (radians, east +ve)</param>
        /// <param name="phi">double latitude (geodetic, radians)</param>
        /// <param name="hm">double height above ref. ellipsoid (geodetic, m)</param>
        /// <param name="xp">double coordinate of the pole (radians)</param>
        /// <param name="yp">double coordinate of the pole (radians)</param>
        /// <param name="sp">double the TIO locator s' (radians)</param>
        /// <param name="theta">double Earth rotation angle (radians)</param>
        /// <param name="pv">double[2,3] position/velocity vector (m, m/s, CIRS) (returned)</param>
        public static void Pvtob(double elong, double phi, double hm, double xp, double yp, double sp, double theta, double[,] pv) {
            const double DAYSEC = 86400.0;

            /* Earth rotation rate in radians per UT1 second */
            double OM = 1.00273781191135448 * D2PI / DAYSEC;

            double[] xyzm = new double[3];
            double[,] rpm = new double[3, 3];
            double[] xyz = new double[3];
            double x, y, z, s, c;

            /* Geodetic to geocentric transformation (WGS84). */
            Gd2gc(1, elong, phi, hm, xyzm);

            /* Polar motion and TIO position. */
            Pom00(xp, yp, sp, rpm);
            Trxp(rpm, xyzm, xyz);
            x = xyz[0];
            y = xyz[1];
            z = xyz[2];

            /* Functions of ERA. */
            s = Math.Sin(theta);
            c = Math.Cos(theta);

            /* Position. */
            pv[0, 0] = c * x - s * y;
            pv[0, 1] = s * x + c * y;
            pv[0, 2] = z;

            /* Velocity. */
            pv[1, 0] = OM * (-s * x - c * y);
            pv[1, 1] = OM * (c * x - s * y);
            pv[1, 2] = 0.0;
        }

        /// <summary>
        /// Form the matrix of polar motion for a given date, IAU 2000.
        /// Ported from SOFA/SOFA/src/pom00.c (iauPom00).
        ///
        /// NOTE: iauPom00 is not one of the 25 functions assigned to this file, nor is
        /// it part of either of the other two engineers' assigned function lists. It is
        /// a small dependency of Pvtob (which IS assigned here), built only from Ir,
        /// Rx, Ry, Rz (all already ported in ManagedSofa.Vectors.cs), so it is included
        /// here as a private helper rather than left unimplemented. Flagged for the
        /// coordinator in case ownership should move.
        /// </summary>
        /// <param name="xp">double coordinate of the pole (radians)</param>
        /// <param name="yp">double coordinate of the pole (radians)</param>
        /// <param name="sp">double the TIO locator s' (radians)</param>
        /// <param name="rpom">double[3,3] polar-motion matrix (returned)</param>
        private static void Pom00(double xp, double yp, double sp, double[,] rpom) {
            /* Construct the matrix. */
            Ir(rpom);
            Rz(sp, rpom);
            Ry(-xp, rpom);
            Rx(-yp, rpom);
        }

        /// <summary>
        /// For a geocentric observer, prepare star-independent astrometry parameters
        /// for transformations between ICRS and GCRS coordinates. The Earth ephemeris
        /// is supplied by the caller.
        /// Ported from SOFA/SOFA/src/apcg.c (iauApcg).
        /// </summary>
        public static void Apcg(double date1, double date2, double[,] ebpv, double[] ehp, AstromContext astrom) {
            /* Geocentric observer */
            double[,] pv = new double[2, 3] { { 0.0, 0.0, 0.0 }, { 0.0, 0.0, 0.0 } };

            /* Compute the star-independent astrometry parameters. */
            Apcs(date1, date2, pv, ebpv, ehp, astrom);
        }

        /// <summary>
        /// For an observer whose geocentric position and velocity are known, prepare
        /// star-independent astrometry parameters for transformations between ICRS and
        /// GCRS. The Earth ephemeris is supplied by the caller.
        /// Ported from SOFA/SOFA/src/apcs.c (iauApcs).
        /// </summary>
        public static void Apcs(double date1, double date2, double[,] pv, double[,] ebpv, double[] ehp, AstromContext astrom) {
            const double DAYSEC = 86400.0;
            const double DJY = 365.25;
            const double DJ00 = 2451545.0;
            const double DAU = 149597870.7e3;
            const double CMPS = 299792458.0;
            const double AULT = DAU / CMPS;

            /* au/d to m/s */
            double AUDMS = DAU / DAYSEC;

            /* Light time for 1 au (day) */
            double CR = AULT / DAYSEC;

            int i;
            double dp, dv, v2, w;
            double[] pb = new double[3];
            double[] vb = new double[3];
            double[] ph = new double[3];

            /* Time since reference epoch, years (for proper motion calculation). */
            astrom.Pmt = ((date1 - DJ00) + date2) / DJY;

            /* Adjust Earth ephemeris to observer. */
            for (i = 0; i < 3; i++) {
                dp = pv[0, i] / DAU;
                dv = pv[1, i] / AUDMS;
                pb[i] = ebpv[0, i] + dp;
                vb[i] = ebpv[1, i] + dv;
                ph[i] = ehp[i] + dp;
            }

            /* Barycentric position of observer (au). */
            Cp(pb, astrom.Eb);

            /* Heliocentric direction and distance (unit vector and au). */
            Pn(ph, out astrom.Em, astrom.Eh);

            /* Barycentric vel. in units of c, and reciprocal of Lorenz factor. */
            v2 = 0.0;
            for (i = 0; i < 3; i++) {
                w = vb[i] * CR;
                astrom.V[i] = w;
                v2 += w * w;
            }
            astrom.Bm1 = Math.Sqrt(1.0 - v2);

            /* Reset the NPB matrix. */
            Ir(astrom.Bpn);
        }

        /// <summary>
        /// For a terrestrial observer, prepare star-independent astrometry parameters
        /// for transformations between ICRS and geocentric CIRS coordinates. The Earth
        /// ephemeris and CIP/CIO are supplied by the caller.
        /// Ported from SOFA/SOFA/src/apci.c (iauApci).
        /// </summary>
        public static void Apci(double date1, double date2, double[,] ebpv, double[] ehp, double x, double y, double s, AstromContext astrom) {
            /* Star-independent astrometry parameters for geocenter. */
            Apcg(date1, date2, ebpv, ehp, astrom);

            /* CIO based BPN matrix. */
            C2ixys(x, y, s, astrom.Bpn);
        }

        /// <summary>
        /// For a terrestrial observer, prepare star-independent astrometry parameters
        /// for transformations between ICRS and geocentric CIRS coordinates. The caller
        /// supplies the date, and SOFA models are used to predict the Earth ephemeris
        /// and CIP/CIO.
        /// Ported from SOFA/SOFA/src/apci13.c (iauApci13).
        /// </summary>
        public static void Apci13(double date1, double date2, AstromContext astrom, out double eo) {
            double[,] ehpv = new double[2, 3];
            double[,] ebpv = new double[2, 3];
            double[,] r = new double[3, 3];
            double x, y, s;

            /* Earth barycentric & heliocentric position/velocity (au, au/d). */
            Epv00(date1, date2, ehpv, ebpv);

            /* Form the equinox based BPN matrix, IAU 2006/2000A. */
            Pnm06a(date1, date2, r);

            /* Extract CIP X,Y. */
            Bpn2xy(r, out x, out y);

            /* Obtain CIO locator s. */
            s = S06(date1, date2, x, y);

            /* Compute the star-independent astrometry parameters. */
            double[] ehp = new double[] { ehpv[0, 0], ehpv[0, 1], ehpv[0, 2] };
            Apci(date1, date2, ebpv, ehp, x, y, s, astrom);

            /* Equation of the origins. */
            eo = Eors(r, s);
        }

        /// <summary>
        /// For a terrestrial observer, prepare star-independent astrometry parameters
        /// for transformations between ICRS and observed coordinates. The caller
        /// supplies the Earth ephemeris, the Earth rotation information and the
        /// refraction constants as well as the site coordinates.
        /// Ported from SOFA/SOFA/src/apco.c (iauApco).
        /// </summary>
        public static void Apco(double date1, double date2, double[,] ebpv, double[] ehp,
                                 double x, double y, double s, double theta,
                                 double elong, double phi, double hm,
                                 double xp, double yp, double sp,
                                 double refa, double refb, AstromContext astrom) {
            double[,] r = new double[3, 3];
            double a, b, eral, c;
            double[,] pvc = new double[2, 3];
            double[,] pv = new double[2, 3];

            /* Form the rotation matrix, CIRS to apparent [HA,Dec]. */
            Ir(r);
            Rz(theta + sp, r);
            Ry(-xp, r);
            Rx(-yp, r);
            Rz(elong, r);

            /* Solve for local Earth rotation angle. */
            a = r[0, 0];
            b = r[0, 1];
            eral = (a != 0.0 || b != 0.0) ? Math.Atan2(b, a) : 0.0;
            astrom.Eral = eral;

            /* Solve for polar motion [X,Y] with respect to local meridian. */
            a = r[0, 0];
            c = r[0, 2];
            astrom.Xpl = Math.Atan2(c, Math.Sqrt(a * a + b * b));
            a = r[1, 2];
            b = r[2, 2];
            astrom.Ypl = (a != 0.0 || b != 0.0) ? -Math.Atan2(a, b) : 0.0;

            /* Adjusted longitude. */
            astrom.Along = Anpm(eral - theta);

            /* Functions of latitude. */
            astrom.Sphi = Math.Sin(phi);
            astrom.Cphi = Math.Cos(phi);

            /* Refraction constants. */
            astrom.Refa = refa;
            astrom.Refb = refb;

            /* Disable the (redundant) diurnal aberration step. */
            astrom.Diurab = 0.0;

            /* CIO based BPN matrix. */
            C2ixys(x, y, s, r);

            /* Observer's geocentric position and velocity (m, m/s, CIRS). */
            Pvtob(elong, phi, hm, xp, yp, sp, theta, pvc);

            /* Rotate into GCRS. */
            Trxpv(r, pvc, pv);

            /* ICRS <-> GCRS parameters. */
            Apcs(date1, date2, pv, ebpv, ehp, astrom);

            /* Store the CIO based BPN matrix. */
            Cr(r, astrom.Bpn);
        }

        /// <summary>
        /// For a terrestrial observer, prepare star-independent astrometry parameters
        /// for transformations between ICRS and observed coordinates. The caller
        /// supplies UTC, site coordinates, ambient air conditions and observing
        /// wavelength, and SOFA models are used to obtain the Earth ephemeris, CIP/CIO
        /// and refraction constants.
        /// Ported from SOFA/SOFA/src/apco13.c (iauApco13).
        /// </summary>
        /// <returns>int status: +1 = dubious year; 0 = OK; -1 = unacceptable date</returns>
        public static int Apco13(double utc1, double utc2, double dut1,
                                  double elong, double phi, double hm, double xp, double yp,
                                  double phpa, double tc, double rh, double wl,
                                  AstromContext astrom, out double eo) {
            int j;
            double tai1, tai2, tt1, tt2, ut11, ut12, x, y, s, theta, sp;
            double refa = 0.0, refb = 0.0;
            double[,] ehpv = new double[2, 3];
            double[,] ebpv = new double[2, 3];
            double[,] r = new double[3, 3];

            eo = 0.0;

            /* UTC to other time scales. */
            j = Utctai(utc1, utc2, out tai1, out tai2);
            if (j < 0) return -1;
            j = Taitt(tai1, tai2, out tt1, out tt2);
            j = Utcut1(utc1, utc2, dut1, out ut11, out ut12);
            if (j < 0) return -1;

            /* Earth barycentric & heliocentric position/velocity (au, au/d). */
            Epv00(tt1, tt2, ehpv, ebpv);

            /* Form the equinox based BPN matrix, IAU 2006/2000A. */
            Pnm06a(tt1, tt2, r);

            /* Extract CIP X,Y. */
            Bpn2xy(r, out x, out y);

            /* Obtain CIO locator s. */
            s = S06(tt1, tt2, x, y);

            /* Earth rotation angle. */
            theta = Era00(ut11, ut12);

            /* TIO locator s'. */
            sp = Sp00(tt1, tt2);

            /* Refraction constants A and B. */
            Refco(phpa, tc, rh, wl, ref refa, ref refb);

            /* Compute the star-independent astrometry parameters. */
            double[] ehp = new double[] { ehpv[0, 0], ehpv[0, 1], ehpv[0, 2] };
            Apco(tt1, tt2, ebpv, ehp, x, y, s, theta,
                 elong, phi, hm, xp, yp, sp, refa, refb, astrom);

            /* Equation of the origins. */
            eo = Eors(r, s);

            /* Return any warning status. */
            return j;
        }

        /// <summary>
        /// Quick ICRS, epoch J2000.0, to CIRS transformation, given precomputed
        /// star-independent astrometry parameters.
        /// Ported from SOFA/SOFA/src/atciq.c (iauAtciq).
        /// </summary>
        public static void Atciq(double rc, double dc, double pr, double pd, double px, double rv,
                                  AstromContext astrom, out double ri, out double di) {
            double[] pco = new double[3];
            double[] pnat = new double[3];
            double[] ppr = new double[3];
            double[] pi = new double[3];
            double w;

            /* Proper motion and parallax, giving BCRS coordinate direction. */
            Pmpx(rc, dc, pr, pd, px, rv, astrom.Pmt, astrom.Eb, pco);

            /* Light deflection by the Sun, giving BCRS natural direction. */
            Ldsun(pco, astrom.Eh, astrom.Em, pnat);

            /* Aberration, giving GCRS proper direction. */
            Ab(pnat, astrom.V, astrom.Em, astrom.Bm1, ppr);

            /* Bias-precession-nutation, giving CIRS proper direction. */
            Rxp(astrom.Bpn, ppr, pi);

            /* CIRS RA,Dec. */
            C2s(pi, out w, out di);
            ri = Anp(w);
        }

        /// <summary>
        /// Quick CIRS RA,Dec to ICRS astrometric place, given the star-independent
        /// astrometry parameters.
        /// Ported from SOFA/SOFA/src/aticq.c (iauAticq).
        /// </summary>
        public static void Aticq(double ri, double di, AstromContext astrom, out double rc, out double dc) {
            int j, i;
            double w, r2, r;
            double[] pi = new double[3];
            double[] ppr = new double[3];
            double[] pnat = new double[3];
            double[] pco = new double[3];
            double[] d = new double[3];
            double[] before = new double[3];
            double[] after = new double[3];

            /* CIRS RA,Dec to Cartesian. */
            S2c(ri, di, pi);

            /* Bias-precession-nutation, giving GCRS proper direction. */
            Trxp(astrom.Bpn, pi, ppr);

            /* Aberration, giving GCRS natural direction. */
            Zp(d);
            for (j = 0; j < 2; j++) {
                r2 = 0.0;
                for (i = 0; i < 3; i++) {
                    w = ppr[i] - d[i];
                    before[i] = w;
                    r2 += w * w;
                }
                r = Math.Sqrt(r2);
                for (i = 0; i < 3; i++) {
                    before[i] /= r;
                }
                Ab(before, astrom.V, astrom.Em, astrom.Bm1, after);
                r2 = 0.0;
                for (i = 0; i < 3; i++) {
                    d[i] = after[i] - before[i];
                    w = ppr[i] - d[i];
                    pnat[i] = w;
                    r2 += w * w;
                }
                r = Math.Sqrt(r2);
                for (i = 0; i < 3; i++) {
                    pnat[i] /= r;
                }
            }

            /* Light deflection by the Sun, giving BCRS coordinate direction. */
            Zp(d);
            for (j = 0; j < 5; j++) {
                r2 = 0.0;
                for (i = 0; i < 3; i++) {
                    w = pnat[i] - d[i];
                    before[i] = w;
                    r2 += w * w;
                }
                r = Math.Sqrt(r2);
                for (i = 0; i < 3; i++) {
                    before[i] /= r;
                }
                Ldsun(before, astrom.Eh, astrom.Em, after);
                r2 = 0.0;
                for (i = 0; i < 3; i++) {
                    d[i] = after[i] - before[i];
                    w = pnat[i] - d[i];
                    pco[i] = w;
                    r2 += w * w;
                }
                r = Math.Sqrt(r2);
                for (i = 0; i < 3; i++) {
                    pco[i] /= r;
                }
            }

            /* ICRS astrometric RA,Dec. */
            C2s(pco, out w, out dc);
            rc = Anp(w);
        }

        /// <summary>
        /// Quick CIRS to observed place transformation.
        /// Ported from SOFA/SOFA/src/atioq.c (iauAtioq).
        /// </summary>
        public static void Atioq(double ri, double di, AstromContext astrom,
                                  out double aob, out double zob, out double hob, out double dob, out double rob) {
            /* Minimum cos(alt) and sin(alt) for refraction purposes */
            const double CELMIN = 1e-6;
            const double SELMIN = 0.05;

            double[] v = new double[3];
            double x, y, z, sx, cx, sy, cy, xhd, yhd, zhd, f,
                   xhdt, yhdt, zhdt, xaet, yaet, zaet, azobs, r, tz, w, del,
                   cosdel, xaeo, yaeo, zaeo, zdobs, hmobs, dcobs, raobs;

            /* CIRS RA,Dec to Cartesian -HA,Dec. */
            S2c(ri - astrom.Eral, di, v);
            x = v[0];
            y = v[1];
            z = v[2];

            /* Polar motion. */
            sx = Math.Sin(astrom.Xpl);
            cx = Math.Cos(astrom.Xpl);
            sy = Math.Sin(astrom.Ypl);
            cy = Math.Cos(astrom.Ypl);
            xhd = cx * x + sx * z;
            yhd = sx * sy * x + cy * y - cx * sy * z;
            zhd = -sx * cy * x + sy * y + cx * cy * z;

            /* Diurnal aberration. */
            f = (1.0 - astrom.Diurab * yhd);
            xhdt = f * xhd;
            yhdt = f * (yhd + astrom.Diurab);
            zhdt = f * zhd;

            /* Cartesian -HA,Dec to Cartesian Az,El (S=0,E=90). */
            xaet = astrom.Sphi * xhdt - astrom.Cphi * zhdt;
            yaet = yhdt;
            zaet = astrom.Cphi * xhdt + astrom.Sphi * zhdt;

            /* Azimuth (N=0,E=90). */
            azobs = (xaet != 0.0 || yaet != 0.0) ? Math.Atan2(yaet, -xaet) : 0.0;

            /* ---------- */
            /* Refraction */
            /* ---------- */

            /* Cosine and sine of altitude, with precautions. */
            r = Math.Sqrt(xaet * xaet + yaet * yaet);
            r = r > CELMIN ? r : CELMIN;
            z = zaet > SELMIN ? zaet : SELMIN;

            /* A*tan(z)+B*tan^3(z) model, with Newton-Raphson correction. */
            tz = r / z;
            w = astrom.Refb * tz * tz;
            del = (astrom.Refa + w) * tz /
                  (1.0 + (astrom.Refa + 3.0 * w) / (z * z));

            /* Apply the change, giving observed vector. */
            cosdel = 1.0 - del * del / 2.0;
            f = cosdel - del * z / r;
            xaeo = xaet * f;
            yaeo = yaet * f;
            zaeo = cosdel * zaet + del * r;

            /* Observed ZD. */
            zdobs = Math.Atan2(Math.Sqrt(xaeo * xaeo + yaeo * yaeo), zaeo);

            /* Az/El vector to HA,Dec vector (both right-handed). */
            v[0] = astrom.Sphi * xaeo + astrom.Cphi * zaeo;
            v[1] = yaeo;
            v[2] = -astrom.Cphi * xaeo + astrom.Sphi * zaeo;

            /* To spherical -HA,Dec. */
            C2s(v, out hmobs, out dcobs);

            /* Right ascension (with respect to CIO). */
            raobs = astrom.Eral + hmobs;

            /* Return the results. */
            aob = Anp(azobs);
            zob = zdobs;
            hob = -hmobs;
            dob = dcobs;
            rob = Anp(raobs);
        }

        /// <summary>
        /// Quick observed place to CIRS, given the star-independent astrometry
        /// parameters.
        /// Ported from SOFA/SOFA/src/atoiq.c (iauAtoiq).
        /// </summary>
        /// <param name="type">type of coordinates: "R", "H" or "A" (only the first character is significant)</param>
        public static void Atoiq(string type, double ob1, double ob2, AstromContext astrom, out double ri, out double di) {
            /* Minimum sin(alt) for refraction purposes */
            const double SELMIN = 0.05;

            char c;
            double c1, c2, sphi, cphi, ce, xaeo, yaeo, zaeo,
                   xmhdo, ymhdo, zmhdo, az, sz, zdo, refa, refb, tz, dref,
                   zdt, xaet, yaet, zaet, xmhda, ymhda, zmhda,
                   f, xhd, yhd, zhd, sx, cx, sy, cy, hma;
            double[] v = new double[3];

            /* Coordinate type. */
            c = (type != null && type.Length > 0) ? type[0] : '\0';

            /* Coordinates. */
            c1 = ob1;
            c2 = ob2;

            /* Sin, cos of latitude. */
            sphi = astrom.Sphi;
            cphi = astrom.Cphi;

            /* Standardize coordinate type. */
            if (c == 'r' || c == 'R') {
                c = 'R';
            } else if (c == 'h' || c == 'H') {
                c = 'H';
            } else {
                c = 'A';
            }

            /* If Az,ZD, convert to Cartesian (S=0,E=90). */
            if (c == 'A') {
                ce = Math.Sin(c2);
                xaeo = -Math.Cos(c1) * ce;
                yaeo = Math.Sin(c1) * ce;
                zaeo = Math.Cos(c2);
            } else {
                /* If RA,Dec, convert to HA,Dec. */
                if (c == 'R') c1 = astrom.Eral - c1;

                /* To Cartesian -HA,Dec. */
                S2c(-c1, c2, v);
                xmhdo = v[0];
                ymhdo = v[1];
                zmhdo = v[2];

                /* To Cartesian Az,El (S=0,E=90). */
                xaeo = sphi * xmhdo - cphi * zmhdo;
                yaeo = ymhdo;
                zaeo = cphi * xmhdo + sphi * zmhdo;
            }

            /* Azimuth (S=0,E=90). */
            az = (xaeo != 0.0 || yaeo != 0.0) ? Math.Atan2(yaeo, xaeo) : 0.0;

            /* Sine of observed ZD, and observed ZD. */
            sz = Math.Sqrt(xaeo * xaeo + yaeo * yaeo);
            zdo = Math.Atan2(sz, zaeo);

            /*
            ** Refraction
            ** ----------
            */

            /* Fast algorithm using two constant model. */
            refa = astrom.Refa;
            refb = astrom.Refb;
            tz = sz / (zaeo > SELMIN ? zaeo : SELMIN);
            dref = (refa + refb * tz * tz) * tz;
            zdt = zdo + dref;

            /* To Cartesian Az,ZD. */
            ce = Math.Sin(zdt);
            xaet = Math.Cos(az) * ce;
            yaet = Math.Sin(az) * ce;
            zaet = Math.Cos(zdt);

            /* Cartesian Az,ZD to Cartesian -HA,Dec. */
            xmhda = sphi * xaet + cphi * zaet;
            ymhda = yaet;
            zmhda = -cphi * xaet + sphi * zaet;

            /* Diurnal aberration. */
            f = (1.0 + astrom.Diurab * ymhda);
            xhd = f * xmhda;
            yhd = f * (ymhda - astrom.Diurab);
            zhd = f * zmhda;

            /* Polar motion. */
            sx = Math.Sin(astrom.Xpl);
            cx = Math.Cos(astrom.Xpl);
            sy = Math.Sin(astrom.Ypl);
            cy = Math.Cos(astrom.Ypl);
            v[0] = cx * xhd + sx * sy * yhd - sx * cy * zhd;
            v[1] = cy * yhd + sy * zhd;
            v[2] = sx * xhd - cx * sy * yhd + cx * cy * zhd;

            /* To spherical -HA,Dec. */
            C2s(v, out hma, out di);

            /* Right ascension. */
            ri = Anp(astrom.Eral + hma);
        }

        /// <summary>
        /// Transform ICRS star data, epoch J2000.0, to CIRS.
        /// Ported from SOFA/SOFA/src/atci13.c (iauAtci13).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Atci13 / CelestialToIntermediate).
        /// </summary>
        public static void Atci13(double rc, double dc, double pr, double pd, double px, double rv,
                                   double date1, double date2, ref double ri, ref double di, ref double eo) {
            AstromContext astrom = new AstromContext();
            double localRi, localDi, localEo;

            /* The transformation parameters. */
            Apci13(date1, date2, astrom, out localEo);

            /* ICRS (epoch J2000.0) to CIRS. */
            Atciq(rc, dc, pr, pd, px, rv, astrom, out localRi, out localDi);

            ri = localRi;
            di = localDi;
            eo = localEo;
        }

        /// <summary>
        /// Transform star RA,Dec from geocentric CIRS to ICRS astrometric.
        /// Ported from SOFA/SOFA/src/atic13.c (iauAtic13).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Atic13 / IntermediateToCelestial).
        /// </summary>
        public static void Atic13(double ri, double di, double date1, double date2, ref double rc, ref double dc, ref double eo) {
            AstromContext astrom = new AstromContext();
            double localRc, localDc, localEo;

            /* Star-independent astrometry parameters. */
            Apci13(date1, date2, astrom, out localEo);

            /* CIRS to ICRS astrometric. */
            Aticq(ri, di, astrom, out localRc, out localDc);

            rc = localRc;
            dc = localDc;
            eo = localEo;
        }

        /// <summary>
        /// ICRS RA,Dec to observed place. The caller supplies UTC, site coordinates,
        /// ambient air conditions and observing wavelength.
        /// Ported from SOFA/SOFA/src/atco13.c (iauAtco13).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Atco13 / CelestialToTopocentric), except the return type is widened
        /// from short to int (see report note).
        /// </summary>
        public static int Atco13(double rc, double dc, double pr, double pd, double px, double rv,
                                  double utc1, double utc2, double dut1,
                                  double elong, double phi, double hm, double xp, double yp,
                                  double phpa, double tc, double rh, double wl,
                                  ref double aob, ref double zob, ref double hob, ref double dob, ref double rob, ref double eo) {
            int j;
            AstromContext astrom = new AstromContext();
            double ri, di;
            double localEo;

            /* Star-independent astrometry parameters. */
            j = Apco13(utc1, utc2, dut1, elong, phi, hm, xp, yp,
                       phpa, tc, rh, wl, astrom, out localEo);
            eo = localEo;

            /* Abort if bad UTC. */
            if (j < 0) return j;

            /* Transform ICRS to CIRS. */
            Atciq(rc, dc, pr, pd, px, rv, astrom, out ri, out di);

            /* Transform CIRS to observed. */
            double localAob, localZob, localHob, localDob, localRob;
            Atioq(ri, di, astrom, out localAob, out localZob, out localHob, out localDob, out localRob);
            aob = localAob;
            zob = localZob;
            hob = localHob;
            dob = localDob;
            rob = localRob;

            /* Return OK/warning status. */
            return j;
        }

        /// <summary>
        /// Observed place at a groundbased site to ICRS astrometric RA,Dec. The caller
        /// supplies UTC, site coordinates, ambient air conditions and observing
        /// wavelength.
        /// Ported from SOFA/SOFA/src/atoc13.c (iauAtoc13).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Atoc13 / TopocentricToCelestial), except the return type is widened
        /// from short to int (see report note).
        /// </summary>
        public static int Atoc13(string type, double ob1, double ob2,
                                  double utc1, double utc2, double dut1,
                                  double elong, double phi, double hm, double xp, double yp,
                                  double phpa, double tc, double rh, double wl,
                                  ref double rc, ref double dc) {
            int j;
            AstromContext astrom = new AstromContext();
            double eo, ri, di;
            double localRc, localDc;

            /* Star-independent astrometry parameters. */
            j = Apco13(utc1, utc2, dut1, elong, phi, hm, xp, yp,
                       phpa, tc, rh, wl, astrom, out eo);

            /* Abort if bad UTC. */
            if (j < 0) return j;

            /* Transform observed to CIRS. */
            Atoiq(type, ob1, ob2, astrom, out ri, out di);

            /* Transform CIRS to ICRS. */
            Aticq(ri, di, astrom, out localRc, out localDc);
            rc = localRc;
            dc = localDc;

            /* Return OK/warning status. */
            return j;
        }

        /// <summary>
        /// Horizon to equatorial coordinates: transform azimuth and altitude to hour
        /// angle and declination.
        /// Ported from SOFA/SOFA/src/ae2hd.c (iauAe2hd).
        ///
        /// NOTE: the real SOFA iauAe2hd is void (no status return). The existing
        /// SOFA.cs P/Invoke wrapper declares SOFA_Ae2hd with a `short` return, which
        /// does not correspond to any value the native function actually produces.
        /// This port faithfully mirrors the real (void) C signature; see report note
        /// for how to reconcile the public SOFA.cs wrapper.
        /// </summary>
        public static void Ae2hd(double azimuth, double altitude, double latitude, ref double hourAngle, ref double declination) {
            double sa, ca, se, ce, sp, cp, x, y, z, r;

            /* Useful trig functions. */
            sa = Math.Sin(azimuth);
            ca = Math.Cos(azimuth);
            se = Math.Sin(altitude);
            ce = Math.Cos(altitude);
            sp = Math.Sin(latitude);
            cp = Math.Cos(latitude);

            /* HA,Dec unit vector. */
            x = -ca * ce * sp + se * cp;
            y = -sa * ce;
            z = ca * ce * cp + se * sp;

            /* To spherical. */
            r = Math.Sqrt(x * x + y * y);
            hourAngle = (r != 0.0) ? Math.Atan2(y, x) : 0.0;
            declination = Math.Atan2(z, r);
        }

        /// <summary>
        /// Equatorial to horizon coordinates: transform hour angle and declination to
        /// azimuth and altitude.
        /// Ported from SOFA/SOFA/src/hd2ae.c (iauHd2ae).
        ///
        /// NOTE: the real SOFA iauHd2ae is void (no status return), same caveat as
        /// Ae2hd above regarding the existing SOFA.cs P/Invoke wrapper's `short` return.
        /// </summary>
        public static void Hd2ae(double hourAngle, double declination, double latitude, ref double azimuth, ref double altitude) {
            double sh, ch, sd, cd, sp, cp, x, y, z, r, a;

            /* Useful trig functions. */
            sh = Math.Sin(hourAngle);
            ch = Math.Cos(hourAngle);
            sd = Math.Sin(declination);
            cd = Math.Cos(declination);
            sp = Math.Sin(latitude);
            cp = Math.Cos(latitude);

            /* Az,Alt unit vector. */
            x = -ch * cd * sp + sd * cp;
            y = -sh * cd;
            z = ch * cd * cp + sd * sp;

            /* To spherical. */
            r = Math.Sqrt(x * x + y * y);
            a = (r != 0.0) ? Math.Atan2(y, x) : 0.0;
            azimuth = (a < 0.0) ? a + D2PI : a;
            altitude = Math.Atan2(z, r);
        }

        /// <summary>
        /// Equation of the origins, IAU 2006 precession and IAU 2000A nutation.
        /// Ported from SOFA/SOFA/src/eo06a.c (iauEo06a).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Eo06a).
        /// </summary>
        public static double Eo06a(double date1, double date2) {
            double[,] r = new double[3, 3];
            double x, y, s, eo;

            /* Classical nutation x precession x bias matrix. */
            Pnm06a(date1, date2, r);

            /* Extract CIP coordinates. */
            Bpn2xy(r, out x, out y);

            /* The CIO locator, s. */
            s = S06(date1, date2, x, y);

            /* Solve for the EO. */
            eo = Eors(r, s);

            return eo;
        }

        /// <summary>
        /// Determine the constants A and B in the atmospheric refraction model
        /// dZ = A tan Z + B tan^3 Z.
        /// Ported from SOFA/SOFA/src/refco.c (iauRefco).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_iauRefco / RefractionConstants). This one matches exactly: the real
        /// iauRefco is void, same as the existing wrapper.
        /// </summary>
        public static void Refco(double phpa, double tc, double rh, double wl, ref double refa, ref double refb) {
            bool optic;
            double p, t, r, w, ps, pw, tk, wlsq, gamma, beta;

            /* Decide whether optical/IR or radio case: switch at 100 microns. */
            optic = (wl <= 100.0);

            /* Restrict parameters to safe values. */
            t = Math.Max(tc, -150.0);
            t = Math.Min(t, 200.0);
            p = Math.Max(phpa, 0.0);
            p = Math.Min(p, 10000.0);
            r = Math.Max(rh, 0.0);
            r = Math.Min(r, 1.0);
            w = Math.Max(wl, 0.1);
            w = Math.Min(w, 1e6);

            /* Water vapour pressure at the observer. */
            if (p > 0.0) {
                ps = Math.Pow(10.0, (0.7859 + 0.03477 * t) /
                                     (1.0 + 0.00412 * t)) *
                     (1.0 + p * (4.5e-6 + 6e-10 * t * t));
                pw = r * ps / (1.0 - (1.0 - r) * ps / p);
            } else {
                pw = 0.0;
            }

            /* Refractive index minus 1 at the observer. */
            tk = t + 273.15;
            if (optic) {
                wlsq = w * w;
                gamma = ((77.53484e-6 +
                          (4.39108e-7 + 3.666e-9 / wlsq) / wlsq) * p
                          - 11.2684e-6 * pw) / tk;
            } else {
                gamma = (77.6890e-6 * p - (6.3938e-6 - 0.375463 / tk) * pw) / tk;
            }

            /* Formula for beta from Stone, with empirical adjustments. */
            beta = 4.4474e-6 * tk;
            if (!optic) beta -= 0.0074 * pw * beta;

            /* Refraction constants from Green. */
            refa = gamma * (1.0 - beta);
            refb = -gamma * (beta - gamma / 2.0);
        }

        /// <summary>
        /// Encode date and time fields into 2-part Julian Date (or in the case of UTC
        /// a quasi-JD form that includes special provision for leap seconds).
        /// Ported from SOFA/SOFA/src/dtf2d.c (iauDtf2d).
        ///
        /// Drop-in-compatible signature for the existing SOFA.cs P/Invoke wrapper
        /// (SOFA_Dtf2d), except the return type is widened from short to int (see
        /// report note).
        /// </summary>
        public static int Dtf2d(string scale, int iy, int im, int id, int ihr, int imn, double sec, ref double d1, ref double d2) {
            const double DAYSEC = 86400.0;

            int js, iy2, im2, id2;
            double dj, w, day, seclim, dat0, dat12, dat24, dleap, time;

            /* Today's Julian Day Number. */
            js = Cal2jd(iy, im, id, out dj, out w);
            if (js != 0) return js;
            dj += w;

            /* Day length and final minute length in seconds (provisional). */
            day = DAYSEC;
            seclim = 60.0;

            /* Deal with the UTC leap second case. */
            if (scale == "UTC") {
                /* TAI-UTC at 0h today. */
                js = Dat(iy, im, id, 0.0, out dat0);
                if (js < 0) return js;

                /* TAI-UTC at 12h today (to detect drift). */
                js = Dat(iy, im, id, 0.5, out dat12);
                if (js < 0) return js;

                /* TAI-UTC at 0h tomorrow (to detect jumps). */
                js = Jd2cal(dj, 1.5, out iy2, out im2, out id2, out w);
                if (js != 0) return js;
                js = Dat(iy2, im2, id2, 0.0, out dat24);
                if (js < 0) return js;

                /* Any sudden change in TAI-UTC between today and tomorrow. */
                dleap = dat24 - (2.0 * dat12 - dat0);

                /* If leap second day, correct the day and final minute lengths. */
                day += dleap;
                if (ihr == 23 && imn == 59) seclim += dleap;

                /* End of UTC-specific actions. */
            }

            /* Validate the time. */
            if (ihr >= 0 && ihr <= 23) {
                if (imn >= 0 && imn <= 59) {
                    if (sec >= 0.0) {
                        if (sec >= seclim) {
                            js += 2;
                        }
                    } else {
                        js = -6;
                    }
                } else {
                    js = -5;
                }
            } else {
                js = -4;
            }
            if (js < 0) return js;

            /* The time in days. */
            time = (60.0 * ((double)(60 * ihr + imn)) + sec) / day;

            /* Return the date and time. */
            d1 = dj;
            d2 = time;

            /* Status. */
            return js;
        }
    }

    /// <summary>
    /// Star-independent astrometry parameters, ported from the iauASTROM struct
    /// defined in SOFA/SOFA/src/sofa.h. Implemented as a class (not a struct) for
    /// simplicity with the array-typed fields.
    ///
    /// Field-name mapping to the original C struct (PascalCase used per C#
    /// convention; the underlying quantities and units are unchanged):
    ///   pmt -> Pmt, eb -> Eb, eh -> Eh, em -> Em, v -> V, bm1 -> Bm1, bpn -> Bpn,
    ///   along -> Along, phi -> Phi, xpl -> Xpl, ypl -> Ypl, sphi -> Sphi,
    ///   cphi -> Cphi, diurab -> Diurab, eral -> Eral, refa -> Refa, refb -> Refb.
    /// </summary>
    public class AstromContext {
        /// <summary>PM time interval (SSB, Julian years)</summary>
        public double Pmt;

        /// <summary>SSB to observer (vector, au)</summary>
        public double[] Eb = new double[3];

        /// <summary>Sun to observer (unit vector)</summary>
        public double[] Eh = new double[3];

        /// <summary>distance from Sun to observer (au)</summary>
        public double Em;

        /// <summary>barycentric observer velocity (vector, c)</summary>
        public double[] V = new double[3];

        /// <summary>sqrt(1-|v|^2): reciprocal of Lorenz factor</summary>
        public double Bm1;

        /// <summary>bias-precession-nutation matrix</summary>
        public double[,] Bpn = new double[3, 3];

        /// <summary>longitude + s' + dERA(DUT) (radians)</summary>
        public double Along;

        /// <summary>geodetic latitude (radians)</summary>
        public double Phi;

        /// <summary>polar motion xp wrt local meridian (radians)</summary>
        public double Xpl;

        /// <summary>polar motion yp wrt local meridian (radians)</summary>
        public double Ypl;

        /// <summary>sine of geodetic latitude</summary>
        public double Sphi;

        /// <summary>cosine of geodetic latitude</summary>
        public double Cphi;

        /// <summary>magnitude of diurnal aberration vector</summary>
        public double Diurab;

        /// <summary>"local" Earth rotation angle (radians)</summary>
        public double Eral;

        /// <summary>refraction constant A (radians)</summary>
        public double Refa;

        /// <summary>refraction constant B (radians)</summary>
        public double Refb;
    }
}
