using System.Collections;
using System.Text.RegularExpressions;

namespace FStorm
{
    public class EdmPath : IEnumerable<EdmPath>
    {
        static Regex entitySetIdentifier = new Regex("^(?'entity'[_a-zA-Z][.a-zA-Z0-9_]{0,127}$)");
        static Regex singleEntityIdentifier = new Regex("^(?'entity'[_a-zA-Z][.a-zA-Z0-9_]{0,127})[(](?'id'[a-zA-Z0-9_]{1,128})[)]$");
        static Regex quotedSingleEntityIdentifier = new Regex("^(?'entity'[_a-zA-Z][.a-zA-Z0-9_]{0,127})[(]'(?'id'[a-zA-Z0-9_]{1,128})'[)]$");

        readonly string? value;
        EdmPath[] _tokens;

        public EdmPath()
        {
            _tokens = new EdmPath[0];
            value = null;
        }

        public EdmPath(params string[] tokens){
            _tokens = tokens.Select(x => new EdmPath(x)).ToArray();
        }

        public EdmPath(string token)
        {
            if (!entitySetIdentifier.IsMatch(token) && !singleEntityIdentifier.IsMatch(token) && !quotedSingleEntityIdentifier.IsMatch(token))
            {
                throw new ArgumentException("Invalid format");
            }
            value = token;
            _tokens = new EdmPath[] {this};
        }

        public EdmPath(EdmPath[] tokens){
            if (tokens == null) 
                throw new ArgumentNullException(nameof(tokens));
            _tokens = tokens;
        }

        public static EdmPath operator +(EdmPath x, EdmPath y) 
        { 
            if (x.Count() == 0)
                return y;
            if (y.Count() == 0)
                return x;

            return new EdmPath(x._tokens.Concat(y._tokens).ToArray()); 
        }

        public bool HasValue() => value != null;

        public bool HasId() => HasValue() && (singleEntityIdentifier.IsMatch(value) || quotedSingleEntityIdentifier.IsMatch(value));

        public string GetEntityName()
        {
            if (!HasValue())
                throw new ArgumentException("This path element don't have a single value");

            if (entitySetIdentifier.IsMatch(value))
                return entitySetIdentifier.Match(value).Groups["entity"].Value;

            if (singleEntityIdentifier.IsMatch(value))
                return singleEntityIdentifier.Match(value).Groups["entity"].Value;

            if (quotedSingleEntityIdentifier.IsMatch(value))
                return quotedSingleEntityIdentifier.Match(value).Groups["entity"].Value;

            throw new ArgumentException("This path element don't have a single value");
        }

        public string GetId()
        {
            if (!HasId()) 
                throw new ArgumentException("This path element don't have an id");

            if (singleEntityIdentifier.IsMatch(value)) 
                return singleEntityIdentifier.Match(value).Groups["id"].Value;

            if (quotedSingleEntityIdentifier.IsMatch(value))
                return quotedSingleEntityIdentifier.Match(value).Groups["id"].Value;

            throw new ArgumentException("This path element don't have an id");
        }

        public override string ToString()
        {
            if (_tokens.Length == 0) return string.Empty;
            if (_tokens.Length == 1) return (value??String.Empty);
            return String.Join("/", _tokens.Select(x => x.ToString()).Where(x => !String.IsNullOrEmpty(x)));
        }

        public IEnumerator<EdmPath> GetEnumerator()
        {
            return ((IEnumerable<EdmPath>)_tokens).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }
    }

}