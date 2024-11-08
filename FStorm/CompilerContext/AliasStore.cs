namespace FStorm
{
    public class AliasStore
    {
        private readonly string Prefix;

        private int index = 0;

        public AliasStore()
        {
            Prefix = "P";
        }

        public AliasStore(string Prefix)
        {
            this.Prefix = Prefix;
        }

        private Dictionary<EdmPath, string> aliases = new Dictionary<EdmPath, string>();

        private string ComputeNewAlias(EdmPath path)
        {
            index++;
            return $"{Prefix}{index}";
        }

        public string AddOrGet(EdmPath path)
        {
            if (!Contains(path))
            {
                var a = ComputeNewAlias(path);
                aliases.Add(path, a);
            }
            return aliases[path];
        }

        public bool Contains(EdmPath path)
        {
            return aliases.ContainsKey(path);
        }

        internal EdmPath? TryGet(string v)
        {
            return aliases.FirstOrDefault(x => x.Value == v).Key;
        }
    }
}