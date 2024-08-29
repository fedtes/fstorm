using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FStorm
{

    public class Row : IDictionary<EdmPath, object?>
    {
        public Dictionary<EdmPath, object?> Cells { get; } = new Dictionary<EdmPath, object?>();
        
        
        
        #region "IDictionary"
        public object? this[EdmPath key] { get => ((IDictionary<EdmPath, object?>)Cells)[key]; set => ((IDictionary<EdmPath, object?>)Cells)[key] = value; }

        public ICollection<EdmPath> Keys => ((IDictionary<EdmPath, object?>)Cells).Keys;

        public ICollection<object?> Values => ((IDictionary<EdmPath, object?>)Cells).Values;

        public int Count => ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).IsReadOnly;

        public void Add(EdmPath key, object? value)
        {
            ((IDictionary<EdmPath, object?>)Cells).Add(key, value);
        }

        public void Add(KeyValuePair<EdmPath, object?> item)
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).Clear();
        }

        public bool Contains(KeyValuePair<EdmPath, object?> item)
        {
            return ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).Contains(item);
        }

        public bool ContainsKey(EdmPath key)
        {
            return ((IDictionary<EdmPath, object?>)Cells).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<EdmPath, object?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<EdmPath, object?>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<EdmPath, object?>>)Cells).GetEnumerator();
        }

        public bool Remove(EdmPath key)
        {
            return ((IDictionary<EdmPath, object?>)Cells).Remove(key);
        }

        public bool Remove(KeyValuePair<EdmPath, object?> item)
        {
            return ((ICollection<KeyValuePair<EdmPath, object?>>)Cells).Remove(item);
        }

        public bool TryGetValue(EdmPath key, [MaybeNullWhen(false)] out object? value)
        {
            return ((IDictionary<EdmPath, object?>)Cells).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Cells).GetEnumerator();
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