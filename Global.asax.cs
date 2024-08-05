using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Configuration;
using System.Data.SQLite;
using System.Net;
using System.Web.Hosting;

namespace SpatialTutorial
{
    public class Global : System.Web.HttpApplication
    {
        public static string SpatiaLitePath;
        public static string SpatiaLiteNativeDll = "libspatialite-4.dll";
        public static SQLiteConnection cn;

   
        protected void Application_Start(object sender, EventArgs e)
        {
            // set PATH for SpatialLite depending on processor
            var mod_spatialite_folderPath = (IntPtr.Size == 4) ?
                 "mod_spatialite-5.1.0-win-x86" : "mod_spatialite-5.1.0-win-amd64";

            string path =
                HostingEnvironment.MapPath("~/SpatialLite/") + mod_spatialite_folderPath + ";" +
                Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            cn = new SQLiteConnection(@"Data Source=|DATADIRECTORY|\db.sqlite;Version=3;");
            cn.Open();

            // load spatiallite extension
            cn.LoadExtension("mod_spatialite");
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}