using DatastoreLibrary;

namespace DatastoreTests

{
    class Program
    {
        #region Fields

        static string _name = "datastore";
        static string _path = "";
        static bool _reset = true;
        private static PersistentDatastore? _datastore;

        #endregion
        #region Constructors

        static void Main(string[] args)
        {
            string filenamepath = Path.Join(_path, _name);
            if (File.Exists(filenamepath + ".dbf"))
            {
                File.Delete(filenamepath + ".dbf");
                File.Delete(filenamepath + ".idx");
            }
            _datastore = new PersistentDatastore(_path, _name);
            _datastore.New();
            _datastore.Open();
            if (_reset == true)
            {
                _datastore.Reset();
            }
            _datastore.Add("id", "Int64", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", "string", 10));

            // Create data
            // 0, id=0, name="hello"

            List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 0));
            create.Add(new KeyValuePair<string, object>("name", "hello"));
            _datastore.Create(create);
            PrintAll(_datastore.Read());

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="laura"

            create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 1));
            create.Add(new KeyValuePair<string, object>("name", "laura"));
            _datastore.Create(create);
            PrintAll(_datastore.Read());

            // Update data
            // 0, id=101, name="jeremy"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "jeremy"));
            _datastore.Update(update, 0);
            PrintAll(_datastore.Read());

            // insert data
            // 0, id=2, name="ash"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
            insert.Add(new KeyValuePair<string, object>("id", 2));
            insert.Add(new KeyValuePair<string, object>("name", "Ash"));
            _datastore.Insert(insert, 0);
            PrintAll(_datastore.Read());

        }

        #endregion
        #region Methods

        static void Print(List<KeyValuePair<string, object>> record)
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

        static void PrintAll(List<List<KeyValuePair<string, object>>> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
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