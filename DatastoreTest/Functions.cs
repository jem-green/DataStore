using DatastoreLibrary;

namespace DatastoreTests

{
    internal class Functions
    {
        #region Fields

        static string _name = String.Empty;
        static string _path = String.Empty;
        static bool _reset = false;
        private static PersistentDatastore? _datastore;

        #endregion
        #region Constructors

        internal Functions()
        {
            _name = "function";
            _path = "";
            _reset = true;
        }

        internal void Run()
        {
            string filenamepath = Path.Join(_path, _name) + ".dbf";
            if (File.Exists(filenamepath))
            {
                File.Delete(_name + ".dbf");
                File.Delete(_name + ".idx");
            }
            _datastore = new PersistentDatastore(_path, _name);
            _datastore.New();
            _datastore.Open();
            if (_reset == true)
            {
                _datastore.Reset();
            }
            _datastore.Add("id", "Int32", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", TypeCode.String, 10, true));
            _datastore.Index();

            // Create data
            // 0, id=0, name="hello"

            List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 0));
            create.Add(new KeyValuePair<string, object>("name", "hello"));
            _datastore.Create(create);
            PrintAll(_datastore.Read());

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="Laura"

            create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 1));
            create.Add(new KeyValuePair<string, object>("name", "Laura"));
            _datastore.Create(create);
            PrintAll(_datastore.Read());

            // Update data
            // 0, id=101, name="Jeremy"
            // 1, id=1, name="Laura"

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "Jeremy"));
            _datastore.Update(update, 0);
            PrintAll(_datastore.Read());

            // insert data
            // 0, id=2, name="Ash"
            // 1, id=101, name="Jeremy"
            // 2, id=1, name="Laura"

            List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
            insert.Add(new KeyValuePair<string, object>("id", 2));
            insert.Add(new KeyValuePair<string, object>("name", "Ash"));
            _datastore.InsertAt(insert, 0);
            PrintAll(_datastore.Read());

            // Seek data
            // 1, id=1, name="Laura"

            int row = _datastore.Find("Jeremy");
            Print(_datastore.Read(row));

            Console.WriteLine("");

        }
        #endregion
        #region Methods
        private void Print(List<KeyValuePair<string, object>> record)
        {
            for (int j = 0; j < record.Count; j++)
            {
                Console.Write("\"" + record[j].Key + "\"");
                Console.Write("=");
                TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                switch (typeCode)
                {
                    case TypeCode.String:
                        {
                            Console.Write("\"" + record[j].Value + "\"");
                            break;
                        }
                    default:
                        {
                            Console.Write(record[j].Value);
                            break;
                        }
                }
                if (j < record.Count - 1)
                {
                    Console.Write(",");
                }
            }
            Console.Write("\r\n");
        }

        private void PrintAll(List<List<KeyValuePair<string, object>>> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                Console.Write(i + ", ");
                List<KeyValuePair<string, object>> record = records[i];
                for (int j = 0; j < record.Count; j++)
                {
                    Console.Write("\"" + record[j].Key + "\"");
                    Console.Write("=");
                    TypeCode typeCode = Convert.GetTypeCode(record[j].Value);
                    switch (typeCode)
                    {
                        case TypeCode.String:
                            {
                                Console.Write("\"" + record[j].Value + "\"");
                                break;
                            }
                        default:
                            {
                                Console.Write(record[j].Value);
                                break;
                            }
                    }
                    if (j < record.Count - 1)
                    {
                        Console.Write(",");
                    }
                }
                Console.Write("\r\n");
            }
            Console.Write("\r\n");
        }
        #endregion
    }
}