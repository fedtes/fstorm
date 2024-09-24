using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FStorm
{

    public class Row : IDictionary<EdmPath, object?>
    {
        private Dictionary<EdmPath, object?> _cells { get; } = new Dictionary<EdmPath, object?>();
        
        public Dictionary<EdmPath, object?> Cells { get => this._cells.Where(x => !x.Key.IsPathToKey()).ToDictionary(x => x.Key,x => x.Value); }
        
        public List<KeyValuePair<EdmPath, object?>> GetKeys() => _cells.Where(x => x.Key.IsPathToKey()).ToList();

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
        private List<EdmPath> _Columns = new List<EdmPath>();

        private readonly EdmPath root;

        public Row this[int index] { get => ((IList<Row>)Rows)[index]; set => ((IList<Row>)Rows)[index] = value; }

        public List<Row> Rows { get; } = new List<Row>();

        public DataTable(EdmPath root){
            this.root = root;
        }

        /// <summary>
        /// List all columns in the data table. TODO avoid null exception in DataTable is empty.
        /// </summary>
        public List<EdmPath> Columns {get => _Columns;}

        public void AddColumn(EdmPath col) {
            if (!_Columns.Any(x => x == col)) {
                _Columns.Add(col);
            }
        }

        public Row CreateRow() {
            var r = new Row();
            _Columns.ForEach(x => r.Add(x,null));
            this.Add(r);
            return r;
        }

        public List<EdmPath> SortedColumns() {
            List<EdmPath> c = new List<EdmPath>();
            _Columns.Where(x => typeof(EdmPath)==x.GetType()).ToList().ForEach(x => c.Add(x));
            _Columns.Where(x => typeof(EdmPath)!=x.GetType()).ToList().ForEach(x => c.Add(x));

            bool LeftIsGreaterToRight (EdmPath l, EdmPath r) {
                if (typeof(EdmPath)==r.GetType() && typeof(EdmPath)!=l.GetType()) return false;
                if (typeof(EdmPath)!=r.GetType() && typeof(EdmPath)==l.GetType()) return true;
                if (l.Count() > r.Count()) return true;
                if (l.Count() < r.Count()) return false;
                if (l.IsPathToKey() && !r.IsPathToKey()) return false;
                if (!l.IsPathToKey() && r.IsPathToKey()) return true;
                return false;
            };

            //bubble sort. can be upgraded
            for (int i = 0; i < c.Count; i++)
            {
                for (int j = 0; j < c.Count - i - 1; j++)
                {
                    if (LeftIsGreaterToRight(c[j], c[j+1])) {
                        var temp = c[j];
                        c[j] = c[j+1];
                        c[j+1] = temp;
                    }
                }
            }
            return c;
        }


        public DataObjects ToDataObjects(DataTableIterator? iterator = null, int depth = 0) {
            var _iterator = iterator?? new DataTableIterator(this);
            var result = new DataObjects();
            DataObject? current = null;
            bool pk_found = false;

            do {

                if (!pk_found && _iterator.Value.Key.IsPathToKey()) 
                {
                    if (_iterator.Value.Value != null)
                    {
                        if (!result.Any(x=> x.primaryKey.Equals(_iterator.Value.Value))) 
                        {
                            result.Add(new DataObject() {primaryKey = _iterator.Value.Value});
                        }
                        current = result.First(x=> x.primaryKey.Equals(_iterator.Value.Value));
                        pk_found = true;
                    }
                } 
                else 
                {

                    if (_iterator.PreValue != null && _iterator.Value.Key.Count() > _iterator.PreValue.Value.Key.Count()) 
                    {
                        if (current != null && !current.ContainsKey((_iterator.Value.Key - 1).Last().Identifier)) 
                        {
                            current.Add((_iterator.Value.Key - 1).Last().Identifier, ToDataObjects(_iterator, depth + 1));
                        } 
                        else if (current != null && current.ContainsKey((_iterator.Value.Key - 1).Last().Identifier)) {
                            current[(_iterator.Value.Key - 1).Last().Identifier] = (current[(_iterator.Value.Key - 1).Last().Identifier] as DataObjects)!.Concat(ToDataObjects(_iterator,depth + 1));
                        }
                    } 
                    else if (_iterator.PreValue != null && (_iterator.Value.Key-1) != (_iterator.PreValue.Value.Key-1)) 
                    {
                        return result;
                    } 
                    else 
                    {
                        if (current != null && !current.ContainsKey(_iterator.Value.Key.Last().Identifier)) 
                        {
                            current.Add(_iterator.Value.Key.Last().Identifier, _iterator.Value.Value);
                        }
                    }
                }

                if (_iterator.IsLastCellRow()) 
                {
                    if (depth > 0) return result;
                    pk_found = false;
                } 
            } while (_iterator.Next());

            return result;
        }


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

    public class DataTableIterator
    {
        private readonly DataTable data;
        private int row = 0;
        private int col = 0;

        private List<EdmPath> cols;

        internal DataTableIterator(DataTable data)
        {
            this.data = data;
            cols = data.SortedColumns();
        }

        internal int Col {get => col;}
        internal int Row {get => row;}

        KeyValuePair<EdmPath, object?>? preValue;
        internal KeyValuePair<EdmPath, object?> Value {get => new KeyValuePair<EdmPath, object?>(cols[col], data[row][cols[col]]); }
        internal KeyValuePair<EdmPath, object?>? PreValue {get => preValue; }

        internal bool IsLastCellRow() => col == cols.Count - 1;

        internal bool Next() {
            preValue = Value;
            col++;
            if (col >= cols.Count){
                col=0;
                row++;
            }
            return row < data.Count;
        }
    }


    public class DataObjects : IList<DataObject>
    {
        private List<DataObject> objs = new List<DataObject>();

        public DataObjects Concat(DataObjects values) {
            objs = objs.Concat(values.objs).ToList();
            return this;
        }

        public override string ToString()
        {
            return $"Count({objs.Count})";
        }

        #region "IList"
        public DataObject this[int index] { get => ((IList<DataObject>)objs)[index]; set => ((IList<DataObject>)objs)[index] = value; }

        public int Count => ((ICollection<DataObject>)objs).Count;

        public bool IsReadOnly => ((ICollection<DataObject>)objs).IsReadOnly;

        public void Add(DataObject item)
        {
            ((ICollection<DataObject>)objs).Add(item);
        }

        public void Clear()
        {
            ((ICollection<DataObject>)objs).Clear();
        }

        public bool Contains(DataObject item)
        {
            return ((ICollection<DataObject>)objs).Contains(item);
        }

        public void CopyTo(DataObject[] array, int arrayIndex)
        {
            ((ICollection<DataObject>)objs).CopyTo(array, arrayIndex);
        }

        public IEnumerator<DataObject> GetEnumerator()
        {
            return ((IEnumerable<DataObject>)objs).GetEnumerator();
        }

        public int IndexOf(DataObject item)
        {
            return ((IList<DataObject>)objs).IndexOf(item);
        }

        public void Insert(int index, DataObject item)
        {
            ((IList<DataObject>)objs).Insert(index, item);
        }

        public bool Remove(DataObject item)
        {
            return ((ICollection<DataObject>)objs).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<DataObject>)objs).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)objs).GetEnumerator();
        }
#endregion
    }


    public class DataObject : IDictionary<string, object?>
    {
        internal Dictionary<string, object?> Properties = new Dictionary<string, object?>();

        internal object primaryKey = null!;

        internal DataObject() {}

        public override string ToString()
        {
            return $":key {primaryKey} properties({Properties.Count})";
        }

        #region "IDictionary"

        public object? this[string key] { get => ((IDictionary<string, object?>)Properties)[key]; set => ((IDictionary<string, object?>)Properties)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, object?>)Properties).Keys;

        public ICollection<object?> Values => ((IDictionary<string, object?>)Properties).Values;

        public int Count => ((ICollection<KeyValuePair<string, object?>>)Properties).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, object?>>)Properties).IsReadOnly;

        public void Add(string key, object? value)
        {
            ((IDictionary<string, object?>)Properties).Add(key, value);
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            ((ICollection<KeyValuePair<string, object?>>)Properties).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, object?>>)Properties).Clear();
        }

        public bool Contains(KeyValuePair<string, object?> item)
        {
            return ((ICollection<KeyValuePair<string, object?>>)Properties).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object?>)Properties).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object?>>)Properties).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object?>>)Properties).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object?>)Properties).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            return ((ICollection<KeyValuePair<string, object?>>)Properties).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        {
            return ((IDictionary<string, object?>)Properties).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Properties).GetEnumerator();
        }
#endregion

    }

}