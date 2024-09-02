using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FStorm
{

    public class Row : IDictionary<EdmPath, object?>
    {
        private Dictionary<EdmPath, object?> _cells { get; } = new Dictionary<EdmPath, object?>();
        
        public Dictionary<EdmPath, object?> Cells { get => this._cells.Where(x => !x.Key.IsPathToKey()).ToDictionary(x => x.Key,x => x.Value); }
        
        #region "IDictionary"
        public object? this[EdmPath key] { get => ((IDictionary<EdmPath, object?>)_cells)[key]; set => ((IDictionary<EdmPath, object?>)_cells)[key] = value; }

        public ICollection<EdmPath> Keys => ((IDictionary<EdmPath, object?>)_cells).Keys;

        public ICollection<object?> Values => ((IDictionary<EdmPath, object?>)_cells).Values;

        public int Count => ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).IsReadOnly;

        public void Add(EdmPath key, object? value)
        {
            ((IDictionary<EdmPath, object?>)_cells).Add(key, value);
        }

        public void Add(KeyValuePair<EdmPath, object?> item)
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).Clear();
        }

        public bool Contains(KeyValuePair<EdmPath, object?> item)
        {
            return ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).Contains(item);
        }

        public bool ContainsKey(EdmPath key)
        {
            return ((IDictionary<EdmPath, object?>)_cells).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<EdmPath, object?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<EdmPath, object?>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<EdmPath, object?>>)_cells).GetEnumerator();
        }

        public bool Remove(EdmPath key)
        {
            return ((IDictionary<EdmPath, object?>)_cells).Remove(key);
        }

        public bool Remove(KeyValuePair<EdmPath, object?> item)
        {
            return ((ICollection<KeyValuePair<EdmPath, object?>>)_cells).Remove(item);
        }

        public bool TryGetValue(EdmPath key, [MaybeNullWhen(false)] out object? value)
        {
            return ((IDictionary<EdmPath, object?>)_cells).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_cells).GetEnumerator();
        }
        #endregion
    }


    public class DataTable: IList<Row>
    {
        public Row this[int index] { get => ((IList<Row>)Rows)[index]; set => ((IList<Row>)Rows)[index] = value; }

        public List<Row> Rows { get; } = new List<Row>();

        
        
        #region "IList"
        public int Count => ((ICollection<Row>)Rows).Count;

        public bool IsReadOnly => ((ICollection<Row>)Rows).IsReadOnly;

        public void Add(Row item)
        {
            ((ICollection<Row>)Rows).Add(item);
        }

        public void Clear()
        {
            ((ICollection<Row>)Rows).Clear();
        }

        public bool Contains(Row item)
        {
            return ((ICollection<Row>)Rows).Contains(item);
        }

        public void CopyTo(Row[] array, int arrayIndex)
        {
            ((ICollection<Row>)Rows).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Row> GetEnumerator()
        {
            return ((IEnumerable<Row>)Rows).GetEnumerator();
        }

        public int IndexOf(Row item)
        {
            return ((IList<Row>)Rows).IndexOf(item);
        }

        public void Insert(int index, Row item)
        {
            ((IList<Row>)Rows).Insert(index, item);
        }

        public bool Remove(Row item)
        {
            return ((ICollection<Row>)Rows).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Row>)Rows).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Rows).GetEnumerator();
        }
        #endregion
    }

}