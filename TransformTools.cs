using System;
using System.Windows;

namespace SpatialTutorial
{
    // using generic sphere mercator (radius is 1)
    public static class TransformTools
    {
        const double EARTH_HALF_CIRC = Math.PI;
        const double EARTH_CIRCUM = EARTH_HALF_CIRC * 2.0;

        public static Point WgsToSphereMercator(Point point)
        {
            double x = point.X * Math.PI / 180.0;
            double y = Math.Log(Math.Tan(Math.PI / 4.0 + point.Y * Math.PI / 360.0));

            return new Point(x, y);
        }

        public static Point SphereMercatorToWgs(Point point)
        {
            double x = (180 / Math.PI) * point.X;
            double y = (360 / Math.PI) * (Math.Atan(Math.Exp(point.Y)) - (Math.PI / 4));

            return new Point(x, y);
        }
        
        public static Rect TileToSphereMercator(int x, int y, int z)
        {
            double arc = EARTH_CIRCUM / (1 << z);
            double x1 = EARTH_HALF_CIRC - x * arc;
            double y1 = EARTH_HALF_CIRC - y * arc;
            double x2 = EARTH_HALF_CIRC - (x + 1) * arc;
            double y2 = EARTH_HALF_CIRC - (y + 1) * arc;

            return new Rect(new Point(-x1, y2), new Point(-x2, y1));
        }

        public static Rect TileToWgs(int x, int y, int z)
        {
            var rect = TileToSphereMercator(x, y, z);
            return new Rect(SphereMercatorToWgs(rect.TopLeft), SphereMercatorToWgs(rect.BottomRight));
        }

        public static Point MercatorToImage(Rect mercatorRect, Size size, Point mercatorPoint)
        {
            return new Point(
              (mercatorPoint.X - mercatorRect.Left) / (mercatorRect.Right - mercatorRect.Left) * size.Width,
              size.Height - (mercatorPoint.Y - mercatorRect.Top) / (mercatorRect.Bottom - mercatorRect.Top) * size.Height);
        }

        public static Point WgsToTile(int x, int y, int z, Point wgsPoint)
        {
            return MercatorToImage(TileToSphereMercator(x, y, z), new Size(256, 256), WgsToSphereMercator(wgsPoint));
        }
    }
}