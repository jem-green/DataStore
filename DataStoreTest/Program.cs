using DatastoreLibrary;
using DatastoreTest;

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
            Function f = new Function();
            f.Run();

            //Performance p = new Performance();
            //p.Run();

        }
        #endregion
    }
}