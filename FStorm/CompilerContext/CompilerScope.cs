namespace FStorm
{
    public class CompilerScope
    {
        public const string ROOT = "main";
        public const string AND = "and";
        public const string NOT = "not";
        public const string OR = "or";
        public const string NO_SCOPE = "noscope";
        public const string VARIABLE = "var";
        public const string ANY = "any";
        public const string ALL = "all";
        public const string EXPAND = "expand";

        internal readonly IQueryBuilder? Query;
        internal readonly string ScopeType;
        internal readonly Variable? Variable;
        internal readonly AliasStore Aliases;
        internal bool HasFromClause = false;

        internal CompilerScope(string scopeType)
        {
            ScopeType = scopeType;
            Query = null;
            Aliases = new AliasStore();
        }
        internal CompilerScope(string scopeType, IQueryBuilder query)
        {
            ScopeType = scopeType;
            Query = query;
            Aliases = new AliasStore();
        }

        internal CompilerScope(string scopeType, IQueryBuilder query, AliasStore? aliasStore)
        {
            ScopeType = scopeType;
            Query = query;
            Aliases = aliasStore ?? new AliasStore();
        }

        internal CompilerScope(string scopeType, IQueryBuilder query, Variable variable)
        {
            ScopeType = scopeType;
            Query = query;
            this.Variable = variable;
            Aliases = new AliasStore();
        }

        public override string ToString()
        {
            return ScopeType;
        }
    }
}