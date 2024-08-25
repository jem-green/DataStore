using DatastoreLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatastoreTest
{
    internal class Methods
    {
        #region Fields

        static string _name = String.Empty;
        static string _path = String.Empty;
        static bool _reset = false;
        private static PersistentDatastore? _datastore;

        #endregion

        internal Methods()
        {
            _name = "method";
            _path = "";
            _reset = true;
        }

        #region Constructors
        #region Methods

        internal void Run()
        {

            string filenamepath = Path.Join(_path, _name) + ".dbf";
            if (File.Exists(filenamepath))
            {
                File.Delete(_name + ".dbf");
                File.Delete(_name + ".idx");
            }

            // Test the PersistentDatastore private methods

            _datastore = new PersistentDatastore(_path, _name);

            /*

            object obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Create", ppd, new object[2] { 0, "start" });

            
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Create", ppd, new object[2] { 1, "next" });
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Create", ppd, new object[2] { 2, "end" });
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Delete", ppd, new object[1] { 1 });
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Create", ppd, new object[2] { 1, "next" });
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Update", ppd, new object[2] { 1, "to" });
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Update", ppd, new object[2] { 1, "longer" });

            for (int i = 0; i < 3; i++)
            {
                obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Read", ppd, new object[1] { i });
                string s = obj.ToString();
                Console.WriteLine(s);
            }
            obj = RunInstanceMethod(typeof(Dictionary.PersistentDictionary<int, string>), "Close", ppd, null);


            */


            _datastore.New();
            _datastore.Open();
            if (_reset == true)
            {
                _datastore.Reset();
            }
            _datastore.Add("id", "Int32", 0);
            _datastore.Add(new PersistentDatastore.FieldType("name", TypeCode.String, 10,false));

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
            // 0, id=101, name="Jeremy"
            // 1, id=1, name="laura"

            List<KeyValuePair<string, object>> update = new List<KeyValuePair<string, object>>();
            update.Add(new KeyValuePair<string, object>("id", 101));
            update.Add(new KeyValuePair<string, object>("name", "Jeremy"));
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

        private static object RunStaticMethod(System.Type t, string strMethod, object[] aobjParams)
        {
            BindingFlags eFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            return RunMethod(t, strMethod, null, aobjParams, eFlags);
        }

        private static object RunInstanceMethod(System.Type t, string strMethod, object objInstance, object[] aobjParams)
        {
            BindingFlags eFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return RunMethod(t, strMethod, objInstance, aobjParams, eFlags);
        }

        #endregion
        #region Private

        private static object RunMethod(System.Type t, string strMethod, object objInstance, object[] aobjParams, BindingFlags eFlags)
        {
            MethodInfo m;
            try
            {
                m = t.GetMethod(strMethod, eFlags);
                if (m == null)
                {
                    throw new ArgumentException("There is no method '" + strMethod + "' for type '" + t.ToString() + "'.");
                }

                object objRet = m.Invoke(objInstance, aobjParams);
                return objRet;
            }
            catch
            {
                throw;
            }
        }

        #endregion
    }
}
