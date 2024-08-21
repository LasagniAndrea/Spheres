using Fonet.DataTypes;

namespace Fonet.Fo.Properties
{
    internal class TextAltitudeMaker : ToBeImplementedProperty.Maker
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new TextAltitudeMaker(propName);
        }

        protected TextAltitudeMaker(string name) : base(name) { }

        public override bool IsInherited()
        {
            return false;
        }

        private Property m_defaultProp = null;

        public override Property Make(PropertyList propertyList)
        {
            if (m_defaultProp == null)
            {
                m_defaultProp = Make(propertyList, "use-font-metrics", propertyList.GetParentFObj());
            }
            return m_defaultProp;
        }

    }
}