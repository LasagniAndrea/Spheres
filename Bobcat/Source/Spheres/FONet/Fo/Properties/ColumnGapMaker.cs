namespace Fonet.Fo.Properties
{
    internal class ColumnGapMaker : LengthProperty.Maker
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new ColumnGapMaker(propName);
        }

        protected ColumnGapMaker(string name) : base(name) { }


        public override bool IsInherited()
        {
            return false;
        }

        protected override bool IsAutoLengthAllowed()
        {
            return true;
        }

        private Property m_defaultProp = null;

        public override Property Make(PropertyList propertyList)
        {
            if (m_defaultProp == null)
            {
                m_defaultProp = Make(propertyList, "0.25in", propertyList.GetParentFObj());
            }
            return m_defaultProp;

        }

    }
}