using DatastoreLibrary;

namespace DataStoreTests

{
    class Program
    {

        #region Fields

        static string _name = "datastore1";
        static string _path = "";
        static bool _reset = true;
        private static PersistentDatastore? _datastore;

        #endregion

        static void Main(string[] args)
        {
            string filenamepath = Path.Join(_path, _name) + ".dbf";
            if (File.Exists(filenamepath))
            {
                File.Delete(_name + ".dbf");
                File.Delete(_name + ".idx");
            }
            _datastore = new PersistentDatastore(_path, _name, _reset);
            //_datastore = new PersistentDatastore(_path, _name, false);
            _datastore.Add("id", "Int32", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", "string", 10));

            // Create data
            // 0, id=0, name="hello"

            List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 0));
            create.Add(new KeyValuePair<string, object>("name", "hello"));
            _datastore.Create(create);
            Print(_datastore.Read(0));

            // Create more data
            // 0, id=0, name="hello"
            // 1, id=1, name="laura"

            create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 1));
            create.Add(new KeyValuePair<string, object>("name", "laura"));
            _datastore.Create(create);
            Print(_datastore.Read(0));

            // Update data
            // 0, id=101, name="jeremy"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "jeremy"));
            _datastore.Update(update, 0);
            Print(_datastore.Read(0));

            // insert data
            // 0, id=2, name="ash"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> insert = new List<KeyValuePair<string, object>>();
            insert.Add(new KeyValuePair<string, object>("id", 2));
            insert.Add(new KeyValuePair<string, object>("name", "Ash"));
            _datastore.Insert(insert,0);
            Print(_datastore.Read(0));
        }

        static void Print(Dictionary<string, object> record)
        {
                int j = 0;
            foreach (KeyValuePair<string, object> kvp in record)
            {
                Console.Write("\"" + kvp.Key + "\"");
                Console.Write("=");
                TypeCode typeCode = Convert.GetTypeCode(kvp.Value);
                switch (typeCode)
                {
                    case TypeCode.String:
                        {
                            Console.Write("\"" + kvp.Value + "\"");
                            break;
                        }
                    default:
                        {
                            Console.Write(kvp.Value);
                            break;
                        }
                }
                j++;
                if (j < record.Count - 1)
                {
                    Console.Write(",");
                }
            }
        }

        static void PrintAll(List<Dictionary<string, object>> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                Dictionary<string, object> record = records[i];
                int j = 0;
                foreach (KeyValuePair<string, object> kvp in record)
                {
                    Console.Write("\"" + kvp.Key + "\"");
                    Console.Write("=");
                    TypeCode typeCode = Convert.GetTypeCode(kvp.Value);
                    switch (typeCode)
                    {
                        case TypeCode.String:
                            {
                                Console.Write("\"" + kvp.Value + "\"");
                                break;
                            }
                        default:
                            {
                                Console.Write(kvp.Value);
                                break;
                            }
                    }
                    j++;
                    if (j < record.Count - 1)
                    {
                        Console.Write(",");
                    }
                }
                Console.Write("\r\n");
            }

        }

    }
}