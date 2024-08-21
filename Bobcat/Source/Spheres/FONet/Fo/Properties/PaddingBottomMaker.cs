using System.Text;

namespace Fonet.Fo.Properties
{
    internal class PaddingBottomMaker : GenericPadding
    {
        new public static PropertyMaker Maker(string propName)
        {
            return new PaddingBottomMaker(propName);
        }

        protected PaddingBottomMaker(string name) : base(name) { }


        public override Property Compute(PropertyList propertyList)
        {
            FObj parentFO = propertyList.GetParentFObj();
            StringBuilder sbExpr = new StringBuilder();
            sbExpr.Append("padding-");
            sbExpr.Append(propertyList.WmAbsToRel(PropertyList.BOTTOM));

            Property p = propertyList.GetExplicitOrShorthandProperty(sbExpr.ToString());

            if (p != null)
            {
                p = ConvertProperty(p, propertyList, parentFO);
            }

            return p;
        }

    }
}