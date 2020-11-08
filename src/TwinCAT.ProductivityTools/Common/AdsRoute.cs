using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCAT.Remote
{
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class TcConfig
        {

            private TcConfigRoute[] remoteConnectionsField;

            [System.Xml.Serialization.XmlArrayItemAttribute("Route", IsNullable = false)]
            public TcConfigRoute[] RemoteConnections
            {
                get
                {
                    return this.remoteConnectionsField;
                }
                set
                {
                    this.remoteConnectionsField = value;
                }
            }
        }

        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class TcConfigRoute
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string NetId { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

}
