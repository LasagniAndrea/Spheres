namespace Fonet.Fo.Properties
{
    internal class BackgroundImageMaker : StringProperty.Maker
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new BackgroundImageMaker(propName);
        }

        protected BackgroundImageMaker(string name) : base(name) { }

        public override bool IsInherited()
        {
            return false;
        }

        private Property m_defaultProp = null;

        public override Property Make(PropertyList propertyList)
        {
            if (m_defaultProp == null)
            {
                m_defaultProp = Make(propertyList, "none", propertyList.GetParentFObj());
            }
            return m_defaultProp;
        }
    }
}