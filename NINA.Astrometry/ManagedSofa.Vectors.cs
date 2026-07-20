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

        // Constants from SOFA/SOFA/src/sofam.h (values reproduced exactly, unrounded).

        /// <summary>Pi</summary>
        private const double DPI = 3.141592653589793238462643;

        /// <summary>2Pi</summary>
        private const double D2PI = 6.283185307179586476925287;

        /// <summary>Radians to degrees</summary>
        private const double DR2D = 57.29577951308232087679815;

        /// <summary>Degrees to radians</summary>
        private const double DD2R = 1.745329251994329576923691e-2;

        /// <summary>Radians to arcseconds</summary>
        private const double DR2AS = 206264.8062470963551564734;

        /// <summary>Arcseconds to radians</summary>
        private const double DAS2R = 4.848136811095359935899141e-6;

        /// <summary>Seconds of time to radians</summary>
        private const double DS2R = 7.272205216643039903848712e-5;

        /// <summary>Arcseconds in a full circle</summary>
        private const double TURNAS = 1296000.0;

        /// <summary>Milliarcseconds to radians</summary>
        private const double DMAS2R = DAS2R / 1e3;

        /// <summary>Reference ellipsoid identifiers (Note: values have no significance outside this code).</summary>
        private const int WGS84 = 1;

        private const int GRS80 = 2;
        private const int WGS72 = 3;

        /// <summary>
        /// dsign(A,B) - magnitude of A with sign of B (double)
        /// </summary>
        private static double DSign(double a, double b) {
            return b < 0.0 ? -Math.Abs(a) : Math.Abs(a);
        }

        /// <summary>
        /// Initialize an r-matrix to the identity matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix (returned)</param>
        public static void Ir(double[,] r) {
            r[0, 0] = 1.0;
            r[0, 1] = 0.0;
            r[0, 2] = 0.0;
            r[1, 0] = 0.0;
            r[1, 1] = 1.0;
            r[1, 2] = 0.0;
            r[2, 0] = 0.0;
            r[2, 1] = 0.0;
            r[2, 2] = 1.0;
        }

        /// <summary>
        /// Zero a p-vector.
        /// </summary>
        /// <param name="p">double[3] zero p-vector (returned)</param>
        public static void Zp(double[] p) {
            p[0] = 0.0;
            p[1] = 0.0;
            p[2] = 0.0;
        }

        /// <summary>
        /// Copy a p-vector.
        /// </summary>
        /// <param name="p">double[3] p-vector to be copied</param>
        /// <param name="c">double[3] copy (returned)</param>
        public static void Cp(double[] p, double[] c) {
            c[0] = p[0];
            c[1] = p[1];
            c[2] = p[2];
        }

        /// <summary>
        /// Copy an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix to be copied</param>
        /// <param name="c">double[3][3] copy (returned)</param>
        public static void Cr(double[,] r, double[,] c) {
            // iauCr copies row by row via iauCp(r[0],c[0]) etc; equivalent direct element copy below.
            c[0, 0] = r[0, 0];
            c[0, 1] = r[0, 1];
            c[0, 2] = r[0, 2];
            c[1, 0] = r[1, 0];
            c[1, 1] = r[1, 1];
            c[1, 2] = r[1, 2];
            c[2, 0] = r[2, 0];
            c[2, 1] = r[2, 1];
            c[2, 2] = r[2, 2];
        }

        /// <summary>
        /// Transpose an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix</param>
        /// <param name="rt">double[3][3] transpose (returned)</param>
        public static void Tr(double[,] r, double[,] rt) {
            double[,] wm = new double[3, 3];
            int i, j;

            for (i = 0; i < 3; i++) {
                for (j = 0; j < 3; j++) {
                    wm[i, j] = r[j, i];
                }
            }
            Cr(wm, rt);
        }

        /// <summary>
        /// Rotate an r-matrix about the x-axis.
        /// </summary>
        /// <param name="phi">double angle (radians)</param>
        /// <param name="r">double[3][3] r-matrix, rotated (given and returned)</param>
        public static void Rx(double phi, double[,] r) {
            double s, c, a10, a11, a12, a20, a21, a22;

            s = Math.Sin(phi);
            c = Math.Cos(phi);

            a10 = c * r[1, 0] + s * r[2, 0];
            a11 = c * r[1, 1] + s * r[2, 1];
            a12 = c * r[1, 2] + s * r[2, 2];
            a20 = -s * r[1, 0] + c * r[2, 0];
            a21 = -s * r[1, 1] + c * r[2, 1];
            a22 = -s * r[1, 2] + c * r[2, 2];

            r[1, 0] = a10;
            r[1, 1] = a11;
            r[1, 2] = a12;
            r[2, 0] = a20;
            r[2, 1] = a21;
            r[2, 2] = a22;
        }

        /// <summary>
        /// Rotate an r-matrix about the y-axis.
        /// </summary>
        /// <param name="theta">double angle (radians)</param>
        /// <param name="r">double[3][3] r-matrix, rotated (given and returned)</param>
        public static void Ry(double theta, double[,] r) {
            double s, c, a00, a01, a02, a20, a21, a22;

            s = Math.Sin(theta);
            c = Math.Cos(theta);

            a00 = c * r[0, 0] - s * r[2, 0];
            a01 = c * r[0, 1] - s * r[2, 1];
            a02 = c * r[0, 2] - s * r[2, 2];
            a20 = s * r[0, 0] + c * r[2, 0];
            a21 = s * r[0, 1] + c * r[2, 1];
            a22 = s * r[0, 2] + c * r[2, 2];

            r[0, 0] = a00;
            r[0, 1] = a01;
            r[0, 2] = a02;
            r[2, 0] = a20;
            r[2, 1] = a21;
            r[2, 2] = a22;
        }

        /// <summary>
        /// Rotate an r-matrix about the z-axis.
        /// </summary>
        /// <param name="psi">double angle (radians)</param>
        /// <param name="r">double[3][3] r-matrix, rotated (given and returned)</param>
        public static void Rz(double psi, double[,] r) {
            double s, c, a00, a01, a02, a10, a11, a12;

            s = Math.Sin(psi);
            c = Math.Cos(psi);

            a00 = c * r[0, 0] + s * r[1, 0];
            a01 = c * r[0, 1] + s * r[1, 1];
            a02 = c * r[0, 2] + s * r[1, 2];
            a10 = -s * r[0, 0] + c * r[1, 0];
            a11 = -s * r[0, 1] + c * r[1, 1];
            a12 = -s * r[0, 2] + c * r[1, 2];

            r[0, 0] = a00;
            r[0, 1] = a01;
            r[0, 2] = a02;
            r[1, 0] = a10;
            r[1, 1] = a11;
            r[1, 2] = a12;
        }

        /// <summary>
        /// Multiply a p-vector by an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix</param>
        /// <param name="p">double[3] p-vector</param>
        /// <param name="rp">double[3] r * p (returned)</param>
        public static void Rxp(double[,] r, double[] p, double[] rp) {
            double w;
            double[] wrp = new double[3];
            int i, j;

            /* Matrix r * vector p. */
            for (j = 0; j < 3; j++) {
                w = 0.0;
                for (i = 0; i < 3; i++) {
                    w += r[j, i] * p[i];
                }
                wrp[j] = w;
            }

            /* Return the result. */
            Cp(wrp, rp);
        }

        /// <summary>
        /// Multiply a pv-vector by an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix</param>
        /// <param name="pv">double[2][3] pv-vector</param>
        /// <param name="rpv">double[2][3] r * pv (returned)</param>
        public static void Rxpv(double[,] r, double[,] pv, double[,] rpv) {
            double[] p0 = new double[] { pv[0, 0], pv[0, 1], pv[0, 2] };
            double[] p1 = new double[] { pv[1, 0], pv[1, 1], pv[1, 2] };
            double[] rp0 = new double[3];
            double[] rp1 = new double[3];

            Rxp(r, p0, rp0);
            Rxp(r, p1, rp1);

            rpv[0, 0] = rp0[0]; rpv[0, 1] = rp0[1]; rpv[0, 2] = rp0[2];
            rpv[1, 0] = rp1[0]; rpv[1, 1] = rp1[1]; rpv[1, 2] = rp1[2];
        }

        /// <summary>
        /// Multiply a p-vector by the transpose of an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix</param>
        /// <param name="p">double[3] p-vector</param>
        /// <param name="trp">double[3] r^T * p (returned)</param>
        public static void Trxp(double[,] r, double[] p, double[] trp) {
            double[,] tr = new double[3, 3];

            /* Transpose of matrix r. */
            Tr(r, tr);

            /* Matrix tr * vector p -> vector trp. */
            Rxp(tr, p, trp);
        }

        /// <summary>
        /// Multiply a pv-vector by the transpose of an r-matrix.
        /// </summary>
        /// <param name="r">double[3][3] r-matrix</param>
        /// <param name="pv">double[2][3] pv-vector</param>
        /// <param name="trpv">double[2][3] r^T * pv (returned)</param>
        public static void Trxpv(double[,] r, double[,] pv, double[,] trpv) {
            double[,] tr = new double[3, 3];

            /* Transpose of matrix r. */
            Tr(r, tr);

            /* Matrix tr * vector pv -> vector trpv. */
            Rxpv(tr, pv, trpv);
        }

        /// <summary>
        /// p-vector outer (=vector=cross) product.
        /// </summary>
        /// <param name="a">double[3] first p-vector</param>
        /// <param name="b">double[3] second p-vector</param>
        /// <param name="axb">double[3] a x b (returned)</param>
        public static void Pxp(double[] a, double[] b, double[] axb) {
            double xa, ya, za, xb, yb, zb;

            xa = a[0];
            ya = a[1];
            za = a[2];
            xb = b[0];
            yb = b[1];
            zb = b[2];
            axb[0] = ya * zb - za * yb;
            axb[1] = za * xb - xa * zb;
            axb[2] = xa * yb - ya * xb;
        }

        /// <summary>
        /// p-vector inner (=scalar=dot) product.
        /// </summary>
        /// <param name="a">double[3] first p-vector</param>
        /// <param name="b">double[3] second p-vector</param>
        /// <returns>double a . b</returns>
        public static double Pdp(double[] a, double[] b) {
            double w;

            w = a[0] * b[0]
              + a[1] * b[1]
              + a[2] * b[2];

            return w;
        }

        /// <summary>
        /// Convert a p-vector into modulus and unit vector.
        /// </summary>
        /// <param name="p">double[3] p-vector</param>
        /// <param name="r">double modulus (returned)</param>
        /// <param name="u">double[3] unit vector (returned)</param>
        public static void Pn(double[] p, out double r, double[] u) {
            double w;

            /* Obtain the modulus and test for zero. */
            w = Pm(p);
            if (w == 0.0) {

                /* Null vector. */
                Zp(u);

            } else {

                /* Unit vector. */
                Sxp(1.0 / w, p, u);
            }

            /* Return the modulus. */
            r = w;
        }

        /// <summary>
        /// Modulus of p-vector.
        /// </summary>
        /// <param name="p">double[3] p-vector</param>
        /// <returns>double modulus</returns>
        public static double Pm(double[] p) {
            return Math.Sqrt(p[0] * p[0] + p[1] * p[1] + p[2] * p[2]);
        }

        /// <summary>
        /// Multiply a p-vector by a scalar.
        /// </summary>
        /// <param name="s">double scalar</param>
        /// <param name="p">double[3] p-vector</param>
        /// <param name="sp">double[3] s * p (returned)</param>
        public static void Sxp(double s, double[] p, double[] sp) {
            sp[0] = s * p[0];
            sp[1] = s * p[1];
            sp[2] = s * p[2];
        }

        /// <summary>
        /// Normalize angle into the range 0 &lt;= a &lt; 2pi.
        /// </summary>
        /// <param name="a">double angle (radians)</param>
        /// <returns>double angle in range 0-2pi</returns>
        public static double Anp(double a) {
            double w;

            w = a % D2PI;
            if (w < 0) w += D2PI;

            return w;
        }

        /// <summary>
        /// Normalize angle into the range -pi &lt;= a &lt; +pi.
        /// </summary>
        /// <param name="a">double angle (radians)</param>
        /// <returns>double angle in range +/-pi</returns>
        public static double Anpm(double a) {
            double w;

            w = a % D2PI;
            if (Math.Abs(w) >= DPI) w -= DSign(D2PI, a);

            return w;
        }

        /// <summary>
        /// P-vector to spherical coordinates.
        /// </summary>
        /// <param name="p">double[3] p-vector</param>
        /// <param name="theta">double longitude angle (radians) (returned)</param>
        /// <param name="phi">double latitude angle (radians) (returned)</param>
        public static void C2s(double[] p, out double theta, out double phi) {
            double x, y, z, d2;

            x = p[0];
            y = p[1];
            z = p[2];
            d2 = x * x + y * y;

            theta = (d2 == 0.0) ? 0.0 : Math.Atan2(y, x);
            phi = (z == 0.0) ? 0.0 : Math.Atan2(z, Math.Sqrt(d2));
        }

        /// <summary>
        /// Convert spherical coordinates to Cartesian.
        /// </summary>
        /// <param name="theta">double longitude angle (radians)</param>
        /// <param name="phi">double latitude angle (radians)</param>
        /// <param name="c">double[3] direction cosines (returned)</param>
        public static void S2c(double theta, double phi, double[] c) {
            double cp;

            cp = Math.Cos(phi);
            c[0] = Math.Cos(theta) * cp;
            c[1] = Math.Sin(theta) * cp;
            c[2] = Math.Sin(phi);
        }

        /// <summary>
        /// Angular separation between two p-vectors.
        /// </summary>
        /// <param name="a">double[3] first p-vector (not necessarily unit length)</param>
        /// <param name="b">double[3] second p-vector (not necessarily unit length)</param>
        /// <returns>double angular separation (radians, always positive)</returns>
        public static double Sepp(double[] a, double[] b) {
            double[] axb = new double[3];
            double ss, cs, s;

            /* Sine of angle between the vectors, multiplied by the two moduli. */
            Pxp(a, b, axb);
            ss = Pm(axb);

            /* Cosine of the angle, multiplied by the two moduli. */
            cs = Pdp(a, b);

            /* The angle. */
            s = ((ss != 0.0) || (cs != 0.0)) ? Math.Atan2(ss, cs) : 0.0;

            return s;
        }

        /// <summary>
        /// Angular separation between two sets of spherical coordinates.
        /// </summary>
        /// <param name="al">double first longitude (radians)</param>
        /// <param name="ap">double first latitude (radians)</param>
        /// <param name="bl">double second longitude (radians)</param>
        /// <param name="bp">double second latitude (radians)</param>
        /// <returns>double angular separation (radians)</returns>
        public static double Seps(double al, double ap, double bl, double bp) {
            double[] ac = new double[3];
            double[] bc = new double[3];
            double s;

            /* Spherical to Cartesian. */
            S2c(al, ap, ac);
            S2c(bl, bp, bc);

            /* Angle between the vectors. */
            s = Sepp(ac, bc);

            return s;
        }

        /// <summary>
        /// Transform geodetic coordinates to geocentric using the specified reference ellipsoid.
        /// </summary>
        /// <param name="n">int ellipsoid identifier (Note 1)</param>
        /// <param name="elong">double longitude (radians, east +ve)</param>
        /// <param name="phi">double latitude (geodetic, radians)</param>
        /// <param name="height">double height above ellipsoid (geodetic)</param>
        /// <param name="xyz">double[3] geocentric vector (returned)</param>
        /// <returns>int status: 0 = OK, -1 = illegal identifier, -2 = illegal case</returns>
        public static int Gd2gc(int n, double elong, double phi, double height, double[] xyz) {
            int j;
            double a, f;

            /* Obtain reference ellipsoid parameters. */
            j = Eform(n, out a, out f);

            /* If OK, transform longitude, geodetic latitude, height to x,y,z. */
            if (j == 0) {
                j = Gd2gce(a, f, elong, phi, height, xyz);
                if (j != 0) j = -2;
            }

            /* Deal with any errors. */
            if (j != 0) Zp(xyz);

            /* Return the status. */
            return j;
        }

        /// <summary>
        /// Transform geodetic coordinates to geocentric for a reference ellipsoid of specified form.
        /// </summary>
        /// <param name="a">double equatorial radius</param>
        /// <param name="f">double flattening</param>
        /// <param name="elong">double longitude (radians, east +ve)</param>
        /// <param name="phi">double latitude (geodetic, radians)</param>
        /// <param name="height">double height above ellipsoid (geodetic)</param>
        /// <param name="xyz">double[3] geocentric vector (returned)</param>
        /// <returns>int status: 0 = OK, -1 = illegal case</returns>
        public static int Gd2gce(double a, double f, double elong, double phi, double height, double[] xyz) {
            double sp, cp, w, d, ac, asv, r;

            /* Functions of geodetic latitude. */
            sp = Math.Sin(phi);
            cp = Math.Cos(phi);
            w = 1.0 - f;
            w = w * w;
            d = cp * cp + w * sp * sp;
            if (d <= 0.0) return -1;
            ac = a / Math.Sqrt(d);
            asv = w * ac;

            /* Geocentric vector. */
            r = (ac + height) * cp;
            xyz[0] = r * Math.Cos(elong);
            xyz[1] = r * Math.Sin(elong);
            xyz[2] = (asv + height) * sp;

            /* Success. */
            return 0;
        }

        /// <summary>
        /// Earth reference ellipsoids.
        /// </summary>
        /// <param name="n">int ellipsoid identifier (Note 1)</param>
        /// <param name="a">double equatorial radius (meters) (returned)</param>
        /// <param name="f">double flattening (returned)</param>
        /// <returns>int status: 0 = OK, -1 = illegal identifier</returns>
        public static int Eform(int n, out double a, out double f) {
            /* Look up a and f for the specified reference ellipsoid. */
            switch (n) {

                case WGS84:
                    a = 6378137.0;
                    f = 1.0 / 298.257223563;
                    break;

                case GRS80:
                    a = 6378137.0;
                    f = 1.0 / 298.257222101;
                    break;

                case WGS72:
                    a = 6378135.0;
                    f = 1.0 / 298.26;
                    break;

                default:

                    /* Invalid identifier. */
                    a = 0.0;
                    f = 0.0;
                    return -1;
            }

            /* OK status. */
            return 0;
        }
    }
}
