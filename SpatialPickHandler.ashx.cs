using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Globalization;
using System.Configuration;
using SpatialTutorial.XRouteServiceReference;
using System.Data.SQLite;
using System.Net;
using SpatialTutorial;

namespace Ptvag.Dawn.SilverMap.Web
{
    /// <summary>
    /// Summary description for SpatialPickHandler
    /// </summary>
    public class SpatialPickHandler : IHttpHandler
    {
        //static SQLiteConnection cn;
        static SpatialPickHandler()
        {
            //cn = new SQLiteConnection(@"Data Source=|DATADIRECTORY|\db.sqlite;Version=3;");
            //cn.Open();
            //SQLiteCommand cm = new SQLiteCommand(String.Format("SELECT load_extension('{0}');", SpatialTutorial.Global.SpatiaLiteNativeDll), cn);
            //cm.ExecuteNonQuery();
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                //Parse request parameters
                double lat, lon;
                if (!double.TryParse(context.Request.Params["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                    throw (new ArgumentException("Invalid parameter"));
                if (!double.TryParse(context.Request.Params["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                    throw (new ArgumentException("Invalid parameter"));

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // get the isochrone horizon
                var xroute = new XRouteWSService();
                xroute.Credentials = new NetworkCredential("xtok", SpatialTutorial.Properties.Settings.Default.token);
                //xroute.ClientCredentials.UserName.UserName = "xtok";
                //xroute.ClientCredentials.UserName.Password = SpatialTutorial.Properties.Settings.Default.token;
                var isoResult = xroute.calculateIsochrones(
                    new WaypointDesc
                    {
                        fuzzyRadius = 99999999,
                        linkType = LinkType.FUZZY_LINKING,
                        wrappedCoords = new Point[] { new Point { point = new PlainPoint { x = lon, y = lat } } }
                    },
                    null,
                    new IsochroneOptions
                        {
                            polygonCalculationMode = PolygonCalculationMode.NODE_BASED,
                            isoDetail = IsochroneDetail.POLYS_ONLY,
                            expansionDesc = new ExpansionDescription
                            {
                                expansionType = ExpansionType.EXP_TIME,
                                wrappedHorizons = new int[] { 1800 }
                            }
                        },
                        new CallerContext
                        {
                            wrappedProperties = new CallerContextProperty[]
                    {
                        new CallerContextProperty{key = "CoordFormat", value = "OG_GEODECIMAL"},
                        new CallerContextProperty{key = "ResponseGeometry", value = "WKT"},
                        new CallerContextProperty{key = "Profile", value = "carfast"},
                    }
                        });

                var isoLine = isoResult.wrappedIsochrones[0].polys.wkt;
                string isoType;
                var isoJson = WktToGeoJson(isoLine, out isoType);

                // convert line string to polygon
                var text = isoLine.Replace("LINESTRING (", "POLYGON ((");
                text = text.Replace(")", "))");

                // get the uinion of all polygons intersecting the horzizon
                var strSql = string.Format(
                    "SELECT AsText(GUnion(geometry)), Sum(households), Sum(Category * households)/Sum(households) FROM 'Germany 5-digit postcode areas 2012' " +
                    "inner join MyData on MyData.Id = 'Germany 5-digit postcode areas 2012'.id " +
                    "WHERE Intersects(GeomFromText('" + text + "'), geometry)=1"
                );

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string type;

                        if (reader[1] == DBNull.Value)
                        {
                            // no result - return empty json
                            context.Response.ContentType = "text/json";
                            context.Response.Write(@"{  ""error"": """ + "Cannot find any reachable regions!" + @"""}");
                            return;
                        }

                        int households = System.Convert.ToInt32(reader[1]);
                        double avgKKat = System.Convert.ToDouble(reader[2]);
                        string str = reader.GetString(0);

                        string regionType;
                        string reginJson = WktToGeoJson(str, out regionType);

                        // buiold response
                        context.Response.ContentType = "text/json";
                        context.Response.Write(@"{""coveredRegion"":
{
  ""type"": """ + regionType + @""",
  ""name"": """ + string.Format(CultureInfo.InvariantCulture, "{0:0,0}", households) + " households<br>"
                    + string.Format(CultureInfo.InvariantCulture, "{0:n}", avgKKat) + " avg. power" + @""",
  ""coordinates"": [" + reginJson + @"]
},
""isoHorizon"":
{
  ""type"": """ + isoType + @""",
  ""coordinates"": [" + isoJson + @"]
}}");
                        return;
                    }
                }

                // no result - return empty json
                context.Response.ContentType = "text/json";
                context.Response.Write("{}");
            }
            catch (Exception ex)
            {
                // no result - return empty json
                context.Response.ContentType = "text/json";
                context.Response.Write(@"{  ""error"": """ + ex.Message + @"""}");
            }
        }

        public string WktToGeoJson(string str, out string type)
        {
            // convert wkt to GeoJson
            if (str.StartsWith("LINESTRING"))
            {
                type = "LineString";
                str = str
                    .Replace("LINESTRING", "")
                    .Trim();
            }
            else if (str.StartsWith("POLYGON"))
            {
                type = "Polygon";
                str = str
                    .Replace("POLYGON", "")
                    .Trim();
            }
            else
            {
                type = "MultiPolygon";
                str = str
                    .Replace("MULTIPOLYGON", "")
                    .Trim();
            }

            str = str
                .Replace(", ", "],[")
                .Replace(" ", ",")
                .Replace("(", "[")
                .Replace(")", "]")
                .Replace(",", ", ");

            return str;
        }
        public bool IsReusable
        {
            get { return true; }
        }
    }
}