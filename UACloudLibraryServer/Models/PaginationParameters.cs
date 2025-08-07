
namespace AdminShell
{
    using System;

    public class PaginationParameters
    {
        private const int MaxResultSize = 5000;

        private int _cursor;
        private int _limit;

        public PaginationParameters(string cursor, int limit)
        {
            _cursor = string.IsNullOrEmpty(cursor) || !int.TryParse(cursor, out var parsedCursor) ? 0 : parsedCursor;

            if (limit < 0)
            {
                throw new ArgumentException("Limit");
            }

            if (limit == 0)
            {
                limit = MaxResultSize;
            }

            _limit = limit;
        }

        public int Limit
        {
            get => _limit;
            set => _limit = value;
        }

        public int Cursor
        {
            get => _cursor;
            set => _cursor = value;
        }
    }
}
