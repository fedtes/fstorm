namespace FStorm
{
    public class CommandResult<TContext> where TContext : class
    {
        public CommandResult() { }

        public TContext Context = null!;

        public DataTable? Value;
    }


}