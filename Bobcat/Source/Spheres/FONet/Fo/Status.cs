namespace Fonet.Fo
{
    internal struct Status
    {
        private readonly int code;

        public const int OK = 1;
        public const int AREA_FULL_NONE = 2;
        public const int AREA_FULL_SOME = 3;
        public const int FORCE_PAGE_BREAK = 4;
        public const int FORCE_PAGE_BREAK_EVEN = 5;
        public const int FORCE_PAGE_BREAK_ODD = 6;
        public const int FORCE_COLUMN_BREAK = 7;
        public const int KEEP_WITH_NEXT = 8;

        public Status(int code)
        {
            this.code = code;
        }

        public int GetCode()
        {
            return this.code;
        }

        public bool IsIncomplete()
        {
            return ((this.code != OK) && (this.code != KEEP_WITH_NEXT));
        }

        public bool LaidOutNone()
        {
            return (this.code == AREA_FULL_NONE);
        }

        public bool IsPageBreak()
        {
            return ((this.code == FORCE_PAGE_BREAK)
                || (this.code == FORCE_PAGE_BREAK_EVEN)
                || (this.code == FORCE_PAGE_BREAK_ODD));
        }

    }
}