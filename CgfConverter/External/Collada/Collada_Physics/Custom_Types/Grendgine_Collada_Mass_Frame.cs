using System;
using System.Xml;
using System.Xml.Serialization;
namespace grendgine_collada
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(ElementName = "mass_frame", Namespace = "http://www.collada.org/2005/11/COLLADASchema", IsNullable = true)]
    public partial class Grendgine_Collada_Mass_Frame
    {

        [XmlElement(ElementName = "rotate")]
        public Grendgine_Collada_Rotate[] Rotate;


        [XmlElement(ElementName = "translate")]
        public Grendgine_Collada_Translate[] Translate;
    }
}

