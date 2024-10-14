namespace FStorm
{
    public class PaginationClause {
        public PaginationClause(long? top, long? skip) {
            Top = top;
            Skip = skip;
        }

        public long? Top { get; }
        public long? Skip { get; }
    }
}