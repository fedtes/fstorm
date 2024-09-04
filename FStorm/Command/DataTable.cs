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


    public class HRange
    {
        public HRange(EdmPath EntityPath, List<VRange> VRanges) {
            this.EntityPath = EntityPath;
            this.VRanges = VRanges;
        }
        public List<VRange> VRanges;

        public EdmPath EntityPath;

        public override string ToString()
        {
            return $"{EntityPath.ToString()}[{VRanges.Count()}]";
        }
    }

    public class VRange
    {
        /// <summary>
        /// Zero based
        /// </summary>
        public List<int> RowIndexes;

        public List<EdmPath> Columns;

        public EdmPath Key;

        public object KeyValue;

        public override string ToString()
        {
            return $"{Key.ToString()} [{RowIndexes.Count()}; {Columns.Count()}]";
        }
    }

    public class DataTable: IList<Row>
    {
        public Row this[int index] { get => ((IList<Row>)Rows)[index]; set => ((IList<Row>)Rows)[index] = value; }

        public List<Row> Rows { get; } = new List<Row>();

        public List<HRange> GetHRanges() {
            List<HRange> result = new List<HRange>();

            var colGroups = Columns
                .Where(x => x.GetType() == typeof(EdmResourcePath))
                .GroupBy(x => x - 1)
                .OrderBy(x => x.Key.Count()).ToList();

            foreach (var colGroup in colGroups)
            {

                List<VRange> groups = new List<VRange>();
                var _cols = colGroup.ToList();
                var _key = colGroup.ToList().First(x=> x.IsPathToKey());
                var _keyValues = this.Where(x=> x.ContainsKey(_key)).Select(x => x[_key]).Distinct();

                foreach (var kv in _keyValues)
                {
                    if (kv is null ) continue;
                    List<int> _rowIdx = new List<int>();
                    foreach (var r in this)
                    {
                        if (r[_key] != null && r[_key]!.Equals(kv)) _rowIdx.Add(this.IndexOf(r));
                    }
                    VRange group = new VRange() {
                        Columns = _cols,
                        Key = _key,
                        KeyValue = kv,
                        RowIndexes = _rowIdx
                    };

                    groups.Add(group);
                }

                result.Add(new HRange(colGroup.Key, groups));
            }

            return result;
        }


        /// <summary>
        /// List all columns in the data table. TODO avoid null exception in DataTable is empty.
        /// </summary>
        public List<EdmPath> Columns {get => this.First().Keys.ToList() ;}
        
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



    public class DataObjects : IList<DataObject>
    {
        private List<DataObject> objs = new List<DataObject>();

        public DataObjects(){}

        public DataObjects(DataTable source)
        {
           var ranges = source.GetHRanges(); 
            ProcessRange(source, ranges);
        }

        public override string ToString()
        {
            return $"Count({objs.Count})";
        }

        private void ProcessRange(DataTable source, List<HRange> ranges) 
        {
            
            foreach (var vr in ranges.First().VRanges)
            {
                var _do = new DataObject();
                foreach (var col in vr.Columns)
                {
                    _do.Add(col.Last().Identifier, source[vr.RowIndexes.First()][col]);
                }
                ProcessRange(source, ranges.Skip(1).ToList(), _do, vr);
                this.Add(_do);
            } 
        }

        private void ProcessRange(DataTable source, List<HRange> ranges, DataObject @do, VRange vrange) 
        {
            DataObjects enumProp = new DataObjects();
            HRange current = ranges.First();
            foreach (var g in ranges.First().VRanges.Where(x => x.RowIndexes.Intersect(vrange.RowIndexes).Any()))
            {
                var _do = new DataObject();
                foreach (var col in g.Columns)
                {
                    _do.Add(col.Last().Identifier, source[g.RowIndexes.First()][col]);
                }
                enumProp.Add(_do);

                if (ranges.Count() > 1) {
                    // go nested
                    int idx =1;
                    while (ranges.Count() > idx && ranges.Skip(1).First().EntityPath.Count() == current.EntityPath.Count())
                    {
                        idx++;
                    }
                    if (ranges.Count() > idx) ProcessRange(source, ranges.Skip(1).ToList(), _do, g);
                }
            }
            @do.Add(ranges.First().EntityPath.Last().Identifier, enumProp);
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
        private Dictionary<string, object?> properties = new Dictionary<string, object?>();

        public DataObject() {}

        public DataObject(DataTable source, int rowIndex)
        {

            var colGroups = source.Columns
                .Where(x => x.GetType()==typeof(EdmResourcePath))
                .GroupBy(x=> x - 1)
                .OrderBy(x => x.Key.Count()).ToList();

            var minLen = colGroups.Select(x=> x.Key.Count()).Min();

            ConvertDataTable(source, rowIndex, null, colGroups);
        }

        private void ConvertDataTable(DataTable source, int rowIndex, EdmPath? Key, List<IGrouping<EdmPath, EdmPath>> colGroups)
        {
            foreach (var item in colGroups.First())
            {
                // convert to properties
                var propName = item.Last().Identifier;
                var propValue = source[rowIndex][item];
                properties.Add(propName, propValue);
            }
        }

        public override string ToString()
        {
            var k = properties[":key"];
            return $":key{k} properties({properties.Count})";
        }

        #region "IDictionary"

        public object? this[string key] { get => ((IDictionary<string, object?>)properties)[key]; set => ((IDictionary<string, object?>)properties)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, object?>)properties).Keys;

        public ICollection<object?> Values => ((IDictionary<string, object?>)properties).Values;

        public int Count => ((ICollection<KeyValuePair<string, object?>>)properties).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, object?>>)properties).IsReadOnly;

        public void Add(string key, object? value)
        {
            ((IDictionary<string, object?>)properties).Add(key, value);
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            ((ICollection<KeyValuePair<string, object?>>)properties).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, object?>>)properties).Clear();
        }

        public bool Contains(KeyValuePair<string, object?> item)
        {
            return ((ICollection<KeyValuePair<string, object?>>)properties).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object?>)properties).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object?>>)properties).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object?>>)properties).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object?>)properties).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            return ((ICollection<KeyValuePair<string, object?>>)properties).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        {
            return ((IDictionary<string, object?>)properties).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)properties).GetEnumerator();
        }
#endregion

    }

}