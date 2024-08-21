namespace Fonet.Fo.Pagination
{
    using System.Collections;

    internal class Root : FObj
    {
        new internal class Maker : FObj.Maker
        {
            public override FObj Make(FObj parent, PropertyList propertyList)
            {
                return new Root(parent, propertyList);
            }
        }

        new public static FObj.Maker GetMaker()
        {
            return new Maker();
        }

        private LayoutMasterSet layoutMasterSet;

        private readonly ArrayList pageSequences;

        private int runningPageNumberCounter = 0;

        protected internal Root(FObj parent, PropertyList propertyList)
            : base(parent, propertyList)
        {
            this.name = "fo:root";
            pageSequences = new ArrayList();
            if (parent != null)
            {
                throw new FonetException("root must be root element");
            }
        }

        protected internal int GetRunningPageNumberCounter()
        {
            return this.runningPageNumberCounter;
        }

        protected internal void SetRunningPageNumberCounter(int count)
        {
            this.runningPageNumberCounter = count;
        }

        public int GetPageSequenceCount()
        {
            return pageSequences.Count;
        }

        public PageSequence GetSucceedingPageSequence(PageSequence current)
        {
            int currentIndex = pageSequences.IndexOf(current);
            if (currentIndex == -1)
            {
                return null;
            }
            if (currentIndex < (pageSequences.Count - 1))
            {
                return (PageSequence)pageSequences[currentIndex + 1];
            }
            else
            {
                return null;
            }
        }

        public LayoutMasterSet GetLayoutMasterSet()
        {
            return this.layoutMasterSet;
        }

        public void SetLayoutMasterSet(LayoutMasterSet layoutMasterSet)
        {
            this.layoutMasterSet = layoutMasterSet;
        }

    }
}