namespace Fonet.Fo.Properties
{
    internal class MarkerClassNameMaker : StringProperty.Maker
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new MarkerClassNameMaker(propName);
        }

        protected MarkerClassNameMaker(string name) : base(name) { }


        public override bool IsInherited()
        {
            return false;
        }

        private Property m_defaultProp = null;

        public override Property Make(PropertyList propertyList)
        {
            if (m_defaultProp == null)
            {
                m_defaultProp = Make(propertyList, "", propertyList.GetParentFObj());
            }
            return m_defaultProp;

        }

    }
}