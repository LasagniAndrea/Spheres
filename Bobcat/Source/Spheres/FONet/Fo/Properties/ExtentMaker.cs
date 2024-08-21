namespace Fonet.Fo.Properties
{
    internal class ExtentMaker : LengthProperty.Maker
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new ExtentMaker(propName);
        }

        protected ExtentMaker(string name) : base(name) { }


        public override bool IsInherited()
        {
            return true;
        }

        private Property m_defaultProp = null;

        public override Property Make(PropertyList propertyList)
        {
            if (m_defaultProp == null)
            {
                m_defaultProp = Make(propertyList, "0pt", propertyList.GetParentFObj());
            }
            return m_defaultProp;

        }

    }
}