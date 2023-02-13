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

            List<KeyValuePair<string, object>> create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 0));
            create.Add(new KeyValuePair<string, object>("name", "hello"));
            _datastore.Create(create);

            create = new List<KeyValuePair<string, object>>();
            create.Add(new KeyValuePair<string, object>("id", 1));
            create.Add(new KeyValuePair<string, object>("name", "laura"));
            _datastore.Create(create);

            // Update data

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "jeremy"));
            _datastore.Update(0, update);

            // output the results

            List<List<KeyValuePair<string, object>>> records = _datastore.Read(0, false);

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
        }
    }
}