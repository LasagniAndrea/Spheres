using System.Text;

namespace Fonet.Fo.Properties
{
    internal class BorderStartStyleMaker : GenericBorderStyle
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new BorderStartStyleMaker(propName);
        }

        protected BorderStartStyleMaker(string name) : base(name) { }


        public override bool IsCorrespondingForced(PropertyList propertyList)
        {
            StringBuilder sbExpr = new StringBuilder
            {
                Length = 0
            };
            sbExpr.Append("border-");
            sbExpr.Append(propertyList.WmRelToAbs(PropertyList.START));
            sbExpr.Append("-style");
            if (propertyList.GetExplicitProperty(sbExpr.ToString()) != null)
            {
                return true;
            }

            return false;
        }


        public override Property Compute(PropertyList propertyList)
        {
            FObj parentFO = propertyList.GetParentFObj();
            StringBuilder sbExpr = new StringBuilder();
            sbExpr.Append("border-");
            sbExpr.Append(propertyList.WmRelToAbs(PropertyList.START));
            sbExpr.Append("-style");
            Property p = propertyList.GetExplicitOrShorthandProperty(sbExpr.ToString());

            if (p != null)
            {
                p = ConvertProperty(p, propertyList, parentFO);
            }

            return p;
        }

    }
}