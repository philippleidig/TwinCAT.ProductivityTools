using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TwinCAT.Remote
{
    public static class AmsRouter
    {
        public static IEnumerable<TcConfigRoute> ListRoutes()
        {
            string staticRoutes = @"C:\TwinCAT\3.1\Target\StaticRoutes.xml";

            XmlSerializer serializer = new XmlSerializer(typeof(TcConfig));
            TcConfig routes;

            using (FileStream fs = new FileStream(staticRoutes, FileMode.Open))
            {
                
                routes = (TcConfig)serializer.Deserialize(fs);
            }

            return routes.RemoteConnections;
        }

    }
}
