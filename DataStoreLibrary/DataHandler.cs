using System;

using System.IO;
using System.Reflection;


namespace DatastoreLibrary
{
    internal class DataHandler
    {
        /*
        Solve problem of passing the PersistentDataTable to the other
        classes, so nest the class 

        Data file
        =========

                  1         2
        012345678901234567890
        ~--+-¬+~--+-¬.......~--+
           |  |   |            |
           |  |   |            +- Start of data
           |  |   +- Property (6 + name)..
           |  +- Field (1) 
           +- Header (6)

        +-----------------------+ <- 0
        | Header (6)            | 
        +-----------------------+ <- 6
        | Fields (1)            |
        +-----------------------+ <- 7 [_start]
        | Property (6 + name)   |
        |          ...          |
        +-----------------------+ <- xx [_top]
        |          ...          |
        +-----------------------+ <- yy [_data]
        |          data         |     
        |          ...          |  
        +-----------------------+ <- zz [_pointer]

        Header
        ------
        
        00 - unsigned int16 - number of records _size
        00 - unsigned int16 - pointer to current record _pointer
        00 - unsigned int16 - pointer to start of data _data
        
        Field
        -----
         
        0 - unsigned byte - number of fields (0-255) _items

          
        Field Properties
        ----------------

        The offset is needed if the field is shortened.

        0 - unsigned byte - Offset assuming a field name is less than 255 characters (0-255)
        0 - unsigned byte - Flag 0 = normal, 1 = deleted, 2 = spare
        0 - unsigned byte - Field order
        0 - unsigned byte - Field type enum value (0-255)
        0 - unsigned byte - If string or blob set the length (0-255)
        0 - unsigned byte - Primary key 0 - No, 1 - Yes (0-1)
        00 - LEB128 - Length of element handled by the binary writer and reader in LEB128 format
        bytes - string
        ...

        From the type definition
        
        Empty	    0 - A null reference.
        Object	    1 - A general type representing any reference or value type not explicitly represented by another TypeCode.
        DBNull	    2 - A database null (column) value.
        Boolean	    3 - A simple type representing Boolean values of true or false.
        Char	    4 - An integral type representing unsigned 16-bit integers with values between 0 and 65535. The set of possible values for the Char type corresponds to the Unicode character set.
        SByte	    5 - An integral type representing signed 8-bit integers with values between -128 and 127.
        Byte	    6 - An integral type representing unsigned 8-bit integers with values between 0 and 255.
        Int16	    7 - An integral type representing signed 16-bit integers with values between -32768 and 32767.
        UInt16	    8 - An integral type representing unsigned 16-bit integers with values between 0 and 65535.
        Int32	    9 - An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
        UInt32	    10 - An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
        Int64	    11 - An integral type representing signed 64-bit integers with values between -9223372036854775808 and 9223372036854775807.
        UInt64	    12 - An integral type representing unsigned 64-bit integers with values between 0 and 18446744073709551615.
        Single	    13 - A floating point type representing values ranging from approximately 1.5 x 10 -45 to 3.4 x 10 38 with a precision of 7 digits.
        Double	    14 - A floating point type representing values ranging from approximately 5.0 x 10 -324 to 1.7 x 10 308 with a precision of 15-16 digits.
        Decimal	    15 - A simple type representing values ranging from 1.0 x 10 -28 to approximately 7.9 x 10 28 with 28-29 significant digits.
        DateTime	16 - A type representing a date and time value.
        ?
        String      18 - A sealed class type representing Unicode character strings.
         
        The offset is needed if the field is shortened.

        0 - unsigned byte - Offset assuming a field name is less than 255 characters (0-255)
        0 - unsigned byte - Flag 0 = normal, 1 = deleted, 2 = spare
        0 - unsigned byte - Field order
        0 - unsigned byte - Field type enum value (0-255)
        0 - unsigned byte - If string or blob set the length (0-255)
        0 - unsigned byte - Primary key 0 - No, 1 - Yes (0-1)
        00 - LEB128 - Length of element handled by the binary writer and reader in LEB128 format
        bytes - string
        ...
        
        data
        ----
        
        0 - unsigned byte - flag 0 = normal, 1 = deleted, 2 = spare
        
        1 - string
        00 - LEB128 - Length of element handled by the binary writer and reader in LEB128 format
        bytes - string
        
        2 - bool
        0 - unsigned byte - 0 = false, 1 = true
         
        3 - int
        0000 - 32 bit - defaults to zero
         
        4 - double
        000000000 - 64 bit - defaults to zero
        
        5 - blob
        0000 - unsigned int32 - Length of blob
        bytes - data
        
        The date repeats for each field, there is no record separator
        
        Index file
        ==========

                  1
        01234567890
        ~+
         |  
         +- Header (2)

        +-----------------------+ <- 0
        | Header (2)            | 
        +-----------------------+ <- 2 [_begin]
        |         index         |     
        |          ...          |  
        +-----------------------+ <- zz 

        header
        ------

        00 - unsigned int16 - length of the key 
        //00 - LEB128 - Length of keyname handled by the binary writer and reader in LEB128 format
        //bytes - string

        index
        -----

        bytes - key (key length)
        00 - unsigned int16 - pointer to data
        00 - unsigned int16 - length of data
        ...
        bytes - key (key length)
        00 - unsigned int16 - pointer to data + 1 
        00 - unsigned int16 - length of data + 1
        
        */

        #region Fields

        private string _path = "";
        private string _name = "";
        private string _index = "";

        private readonly object _lockObject = new Object();
        private UInt16 _size = 0;               // number of elements
        private readonly UInt16 _start = 7;     // Pointer to the start of the field area
        private UInt16 _pointer = 7;            // Pointer to current element offset from the data area
        private UInt16 _data = 7;               // pointer to start of data area
        private byte _items = 0;                // number of field property items
        private Property[] _properties;         // Cache of fields
        private readonly UInt16 _begin = 2;     // Pointer to the beginning of the index area
        
        /// <summary>
        /// Field properties
        /// </summary>
        internal struct Property
        {
            string _name;
            byte _flag;
            byte _order;
            TypeCode _type;
            sbyte _length;
            bool _primary;

            internal Property(string name, byte flag, byte order, TypeCode type, sbyte length, bool primary)
            {
                _name = name;
                _flag = flag;
                _order = order;
                _type = type;
                _length = length;
                _primary = primary;
            }

            internal byte Flag
            {
                set
                {
                    _flag = value;
                }
                get
                {
                    return (_flag);
                }
            }

            internal sbyte Length
            {
                set
                {
                    _length = value;
                }
                get
                {
                    return (_length);
                }
            }

            internal string Name
            {
                set
                {
                    _name = value;
                }
                get
                {
                    return (_name);
                }
            }

            internal byte Order
            {
                set
                {
                    _order = value;
                }
                get
                {
                    return (_order);
                }
            }

            internal bool Primary
            {
                set
                {
                    _primary = value;
                }
                get
                {
                    return (_primary);
                }
            }

            internal TypeCode Type
            {
                set
                {
                    _type = value;
                }
                get
                {
                    return (_type);
                }
            }

            public override string ToString()
            {
                string s = _name + "," + _type;
                if (_length > 0)
                {
                    s = s + "[" + _length + "]";
                }
                return (s);
            }
        }

        #endregion
        #region Constructor

        /// <summary>
        /// Create default with path, name location
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        internal DataHandler(string path, string name)
        {
            _path = path;
            _name = name;
            _index = name;
        }

        internal DataHandler(string path, string name, string index)
        {
            _path = path;
            _name = name;
            _index = index;
        }

        #endregion
        #region Properties

        internal string Path
        {
            set
            {
                _path = value;
            }
            get
            {
                return (_path);
            }
        }

        internal string Name
        {
            set
            {
                _name = value;
            }
            get
            {
                return (_name);
            }
        }

        internal string Index
        {
            set
            {
                _index = value;
            }
            get
            {
                return (_index);
            }
        }

        internal UInt16 Size
        {
            get
            {
                return (_size);
            }
        }

        internal byte Items
        {
            get
            {
                return (_items);
            }
        }

        #endregion
        #region Methods

        // General methods (OCRN)
        // Open -
        // Close - 
        // Reset -
        // Index -
        // New   -
        //
        // Field methods (ARSG)
        // Add -
        // Remove -
        // Set -
        // Get -
        // 
        // Record methods (CRUD) 
        // Create -
        // Read -
        // Update -
        // Delete - 

        #region General Methods

        /// <summary>
        /// Open an existing database file
        /// </summary>
        internal bool Open()
        {
            bool open = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            if (File.Exists(filenamePath + ".dbf") == true)
            {
                // Assume we only need to read the data and not the index

                BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                
                binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);                      // Move to position of the current
                _size = binaryReader.ReadUInt16();                                      // Read in the size of data
                _pointer = binaryReader.ReadUInt16();                                   // Read in the current record
                _data = binaryReader.ReadUInt16();                                      // Read in the data pointer
                _items = binaryReader.ReadByte();                                       // Read in the number of fields

                Array.Resize(ref _properties, _items);
                UInt16 pointer = 0;
                for (int count = 0; count < _items; count++)
                {
                    binaryReader.BaseStream.Seek(_start + pointer, SeekOrigin.Begin);   // Move to the field as may have been updated
                    byte offset = binaryReader.ReadByte();                              // Read the field offset
                    byte flag = binaryReader.ReadByte();                                // Read the status flag
                    byte order = binaryReader.ReadByte();                               // Read the field order
                    TypeCode typeCode = (TypeCode)binaryReader.ReadByte();              // Read the field Type
                    sbyte length = binaryReader.ReadSByte();                            // Read the field Length
                    bool primary = false;                                               // Read if the primary key
                    if (binaryReader.ReadByte() == 1)
                    {
                        primary = true;
                    }
                    string name = binaryReader.ReadString();                            // Read the field Name
                    if (flag == 0)  // Not deleted or spare so add rather than skip
                    {
                        Property field = new Property(name, flag, order, typeCode, length, primary);
                        _properties[count] = field;
                    }
                    pointer = (UInt16)(pointer + offset);
                }
                binaryReader.Close();
                open = true;
            }
            return (open);
        }

        /// <summary>
        /// Create a new database file
        /// </summary>
        internal bool New()
        {
            bool @new = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);

            if (File.Exists(filenamePath + ".dbf") == false)
            {
                BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.OpenOrCreate));
                binaryWriter.Seek(0, SeekOrigin.Begin); // Move to start of the file

                _size = 0;                                  // Reset the number of elements
                _pointer = 0;                               // Offset from the data areas so zero
                _data = _start;                             // Start of the field area 3 x 16 bit + 1 x 8 bit = 7 bytes
                _items = 0;                                 // Number of fields

                binaryWriter.Write(_size);                  // Write the size of data
                binaryWriter.Write(_pointer);               // Write pointer to new current record offset from the data area
                binaryWriter.Write(_data);                  // Write pointer to new data area
                binaryWriter.Write(_items);                 // write new number of fields
                binaryWriter.BaseStream.SetLength(7);       // Fix the size as we are resetting
                binaryWriter.Close();

                // Create the index

                BinaryWriter indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.OpenOrCreate));
                indexWriter.Write((UInt16)2);
                indexWriter.BaseStream.SetLength(2);
                indexWriter.Close();

                @new = true;
            }
            return (@new);
        }

        /// <summary>
        /// Reset the database file and clear any index files
        /// </summary>
        internal bool Reset()
        {
            bool reset = false;
            // Reset the file
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);

            if (File.Exists(filenamePath + ".dbf") == true)
            {
                BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.OpenOrCreate));
                binaryWriter.Seek(0, SeekOrigin.Begin); // Move to start of the file

                _size = 0;                                  // Reset the number of elements
                _pointer = 0;                               // Offset from the data areas so zero
                _data = _start;                             // Start of the field area 3 x 16 bit + 1 x 8 bit = 
                _items = 0;                                 // Number of fields

                binaryWriter.Write(_size);                  // Write the size of data
                binaryWriter.Write(_pointer);               // Write pointer to new current record offset from the data area
                binaryWriter.Write(_data);                  // Write pointer to new data area
                binaryWriter.Write(_items);                 // write new number of fields
                binaryWriter.BaseStream.SetLength(7);       // Fix the size as we are resetting
                binaryWriter.Close();

                // Recreate the index

                BinaryWriter indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.OpenOrCreate));
                indexWriter.Write((UInt16)2);
                indexWriter.BaseStream.SetLength(2);
                indexWriter.Close();

                // Clear the field cache

                Array.Resize(ref _properties, _items);

                reset = true;
            }
            return (reset);
        }

        /// <summary>
        /// Clear the database file and clear any index files
        /// </summary>
        internal bool Clear()
        {
            bool clear;
            // Reset the file
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);
            BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
            binaryWriter.Seek(0, SeekOrigin.Begin); // Move to start of the file

            _size = 0;                                  // Zero the sizw of the data
            _pointer = 0;                               // Offset from the data areas so zero

            binaryWriter.Write(_size);                  // Write the size of data
            binaryWriter.Write(_pointer);               // Write pointer to new current record offset from the data area
            binaryWriter.BaseStream.SetLength(_data);   // Fix the size as we are resetting
            binaryWriter.Close();

            // Re-create the index

            BinaryWriter indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.Open));
            indexWriter.Write((UInt16)2);
            indexWriter.BaseStream.SetLength(2);
            indexWriter.Close();
            clear = true;
            return (clear);
        }

        /// <summary>
        /// Delete the database file and index file
        /// </summary>
        internal bool Close()
        {
            bool close = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);

            if (File.Exists(filenamePath + ".dbf") == true)
            {
                // Need to delete both data and index
                File.Delete(filenamePath + ".dbf");
                // Assumption here is the the index also exists
                if (File.Exists(indexPath + ".idx") == true)
                {
                    File.Delete(indexPath + ".idx");
                }
                close = true;
            }
            return (close);
        }

        #endregion
        #region Column methods

        /// <summary>
        /// Add a new database field
        /// </summary>
        /// <param name="field"></param>
        internal void Add(Property field)
        {
            string filenamePath = System.IO.Path.Combine(_path, _name);
            lock (_lockObject)
            {
                // need to calculate the space needed
                //
                // Field
                //
                // 0 - unsigned byte - offset
                // 0 - unsigned byte - flag 0 = normal, 1 = deleted, 2 = Spare
                // 0 - unsigned byte - Field order (0-255)
                // 0 - unsigned byte - Field type enum value (0-255)
                // 0 - unsigned byte - If string or blob set the length (0-255)
                // 0 - unsigned byte - Primary key 0 - No, 1 - Yes (0-1)
                // 00 - leb128 - Length of element handled by the binary writer and reader in LEB128 format
                // bytes - string
                // ...
                // The structure repeats
                //

                if (_size == 0)
                {
                    if (field.Name.Length > 0)
                    {
                        if (field.Type != TypeCode.Empty)
                        {

                            byte order = 0;
                            TypeCode typeCode = field.Type;

                            // Update the local cache

                            Array.Resize(ref _properties, _items + 1);
                            _properties[_items] = field;

                            // Calcualte the data size

                            int offset = 0;
                            int length = field.Name.Length;
                            offset = offset + 6 + LEB128.Size(length) + length;     // 6 = number of bytes before string name

                            // move the data

                            // this would need to shift the data area upwards to accomodate the
                            // new field entry. Seems like a design problem, but may be somthing
                            // to start with assuming that fields are not generally added later. Could
                            // create some spare space when we initialise the database so fields
                            // can get added into the space.

                            // Add the new field

                            BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                            binaryWriter.Seek(_data, SeekOrigin.Begin);

                            byte flag = 0;                                  // Normal
                            binaryWriter.Write((byte)offset);               // write the offset to next field
                            binaryWriter.Write(flag);                       // write the field Flag
                            binaryWriter.Write(order);                      // write the field Order
                            binaryWriter.Write((byte)typeCode);             // write the field Type
                            binaryWriter.Write((sbyte)field.Length);        // write the field Length
                            if (field.Primary == true)                      // write the primary key indicator (byte)
                            {
                                binaryWriter.Write((byte)1);
                            }
                            else
                            {
                                binaryWriter.Write((byte)0);
                            }

                            binaryWriter.Write(field.Name);                 // Write the field Name

                            _data = (UInt16)(_data + offset);               // The data area is moved up as fields are added
                            _items = (byte)(_items + 1);                    // The number of fields is increased

                            binaryWriter.Seek(0, SeekOrigin.Begin);         //
                            binaryWriter.Write(_size);                      // Skip over just re-write size
                            binaryWriter.Write(_pointer);                   // Write pointer to new current record
                            binaryWriter.Write(_data);                      // Write pointer to new data area
                            binaryWriter.Write(_items);                     // write new number of records
                            binaryWriter.Close();                           //
                            binaryWriter.Dispose();                         //

                        }
                        else
                        {
                            throw new ArgumentException("Field name not defined");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Field type not defined");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot add field as data already written");
                }
            }
        }

        /// <summary>
        /// Delete an existing database field
        /// </summary>
        /// <param name="field"></param>
        internal bool Remove(Property field)
        {
            int index = 0;
            bool remove = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            lock (_lockObject)
            {
                // The problem here is i dont know the length of the field
                // without reading the actual record

                BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                UInt16 pointer = 0;
                byte flag = 0;
                for (int counter = 0; counter < _size; counter++)
                {
                    binaryReader.BaseStream.Seek(_start + pointer, SeekOrigin.Begin);

                    // Could read the 5 bytes in one go or move the pointer but need some of the field data

                    byte offset = binaryReader.ReadByte();                      // Read the field offset
                    flag = binaryReader.ReadByte();                             // Read the status flag
                    byte order = binaryReader.ReadByte();                       // Read the order
                    TypeCode typeCode = (TypeCode)binaryReader.ReadByte();      // Read the field Type
                    sbyte length = binaryReader.ReadSByte();                    // Read the field Length
                    bool primary = false;                                       // Read if the primary key
                    if (binaryReader.ReadByte() == 1)
                    {
                        primary = true;
                    }
                    string name = binaryReader.ReadString();                    // Read the field Name

                    if ((flag == 0) && (name == field.Name))
                    {
                        index = counter;
                        remove = true;
                        break;
                    }
                    else
                    {
                        pointer = (UInt16)(pointer + length);
                    }
                }
                binaryReader.Close();

                BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                binaryWriter.Seek(_start + pointer + 1, SeekOrigin.Begin);  // Skip the offset as unchanged
                flag = 1;                                                   // Set the delete flag
                binaryWriter.Write(flag);                                   // write the field Flag
                binaryWriter.Close();                                       //
                binaryWriter.Dispose();                                     //

                // Move the cache data downwards
                // The challnge is then ensuring that the 
                // deleted field is skipped when read in
                // the other methods

                for (int i = index; i < _items - 1; i++)
                {
                    _properties[i] = _properties[i + 1];
                }
                _items--;
                Array.Resize(ref _properties, _items);

            }
            return (remove);
        }

        /// <summary>
        /// Delete an existing database field by index
        /// </summary>
        /// <param name="item"></param>
        internal void RemoveAt(int item)
        {
            string filenamePath = System.IO.Path.Combine(_path, _name);
            lock (_lockObject)
            {
                if ((item >= 0) && (item < _items))
                {
                    // The problem here is that i dont have a pointer index
                    // and i dont know the length of the field
                    // without reading the actual reecord and then itterate
                    // through the list

                    BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    UInt16 pointer = 0;
                    byte offset = 0;
                    for (int counter = 0; counter < _items; counter++)
                    {
                        binaryReader.BaseStream.Seek(_start + pointer, SeekOrigin.Begin);
                        offset = binaryReader.ReadByte();
                        if (counter != item)
                        {
                            pointer = (UInt16)(pointer + offset);
                        }
                        else
                        {
                            break;
                        }
                    }
                    binaryReader.Close();

                    BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    binaryWriter.Seek(_start + pointer + 1, SeekOrigin.Begin);
                    byte flag = 1;                                  // Set the delete flag
                    binaryWriter.Write(flag);                       // write the field Flag
                    binaryWriter.Close();                           //
                    binaryWriter.Dispose();                         //

                    // Move the cache data downwards
                    // The challenge is then ensuring that the 
                    // deleted field is skipped when read in the
                    // other methods

                    for (int i = item; i < _items-1; i++)
                    {
                        _properties[i] = _properties[i + 1];
                    }
                    _items--;
                    Array.Resize(ref _properties, _items);

                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Set or update the field attributes by index
        /// </summary>
        /// <param name="field"></param>
        /// <param name="item"></param>
        internal void Set(Property field, int item)
        {
            // Update the local cache then write to disk but
            // this is more complex as need to reinsert the field name 
            // if it is longer then move the data.

            string filenamePath = System.IO.Path.Combine(_path, _name);
            lock (_lockObject)
            {
                if ((item >=0) && (item < _items))
                {
                    _properties[item] = field;

                    // Calculate the new data size

                    int offset = 6;                    // 6 = number of bytes before the field name
                    int l = field.Name.Length;
                    offset = offset + LEB128.Size(l) + l;

                    // The problem here is i dont know the length of the field
                    // without reading the actual reecord
                    // could assume the cache is correct

                    BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    UInt16 pointer = 0;
                    byte length = 0;
                    for (int counter = 0; counter < _items; counter++)
                    {
                        binaryReader.BaseStream.Seek(_start + pointer, SeekOrigin.Begin);
                        length = binaryReader.ReadByte();
                        if (counter != item)
                        {
                            pointer = (UInt16)(pointer + length);
                        }
                        else
                        {
                            break;
                        }
                    }
                    binaryReader.Close();

                    if (offset > length)
                    {
                        // The new field is longer than the old field
                        // not sure what im doing here now as looks wrong
                        // what it should do it mark the field as deleted and 
                        // insert the new field at the end if no data written yet

                        if (_size == 0)
                        {

                            BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                            binaryWriter.Seek(_data, SeekOrigin.Begin);

                            byte flag = 0;
                            binaryWriter.Write((byte)length);               // write the length
                            binaryWriter.Write(flag);                       // write the field Flag
                            binaryWriter.Write((byte)field.Order);          // write the field Order
                            binaryWriter.Write((byte)field.Type);           // write the field Type
                            binaryWriter.Write((sbyte)field.Length);        // write the field Length
                            if (field.Primary == true)                      // write the primary key indicator (byte)
                            {
                                binaryWriter.Write((byte)1);
                            }
                            else
                            {
                                binaryWriter.Write((byte)0);
                            }
                            binaryWriter.Write(field.Name);                 // Write the field Name

                            _data = (UInt16)(_data + offset);
                            //_items = (byte)(_items + 1);                  // Number of fields remains the same

                            binaryWriter.Seek(0, SeekOrigin.Begin);
                            _size++;                                        //
                            binaryWriter.Write(_size);                      // Write the number of records - size
                            binaryWriter.Write(_pointer);                   // Write pointer to new current record but this is offset from _data
                            binaryWriter.Write(_data);                      // Write pointer to new data area
                            binaryWriter.Write(_items);                     // write new number of records
                            binaryWriter.Close();                           //
                            binaryWriter.Dispose();                         //

                            // This looks wrong as the order has changed
                            // and so this will affect the storage

                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot add field as Data already written");
                        }
                    }
                    else
                    {
                        // The new field name is shorter so can overrite

                        BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                        binaryWriter.Seek(_start + pointer + 3, SeekOrigin.Begin);  // 3 = Skip the bytes

                        // Keep the field space as original
                        // No need to overwrite the flag

                        binaryWriter.Write((byte)field.Type);          // write the field Type
                        binaryWriter.Write((sbyte)field.Length);       // write the field Length
                        if (field.Primary == true)                     // write the primary key indicator (byte)
                        {
                            binaryWriter.Write((byte)1);
                        }
                        else
                        {
                            binaryWriter.Write((byte)0);
                        }
                        binaryWriter.Write(field.Name);                // Write the field Name
                        binaryWriter.Dispose();
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Get the field by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal Property Get(int index)
        {
            if ((index >= 0) && (index < _items))
            {
                // Build from cache, but could 
                // have an option to rebuild from disk

                Property field = _properties[index];
                return (field);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        #endregion
        #region Row Methods

        /// <summary>
        /// Create new record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        internal bool Create(object[] record)
        {
            bool created = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);
            lock (_lockObject)
            {
                // append the new pointer the new index file

                BinaryWriter indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.Append));
                indexWriter.Write((UInt16)_size);   // write the index assume row
                indexWriter.Write(_pointer);        // Write the pointer

                int offset = 0;
                offset += 1;    // Including the flag
                for (int i = 0; i < record.Length; i++)
                {
                    object data = record[i];
                    switch (_properties[i].Type)
                    {
                        case TypeCode.Int16:
                            {
                                offset += 2;
                                break;
                            }
                        case TypeCode.Int32:
                            {
                                offset += 4;
                                break;
                            }
                        case TypeCode.String:
                            {
                                int length = _properties[i].Length;
                                if (length < 0)
                                {
                                    length = Convert.ToString(data).Length;
                                }
                                offset = offset + LEB128.Size(length) + length;     // Includes the byte length parameter
                                                                                    // ** need to watch this as can be 2 bytes if length is > 127 characters
                                                                                    // ** https://en.wikipedia.org/wiki/LEB128

                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException();
                            }
                    }
                }

                // Update the index length

                indexWriter.Write((UInt16)offset);  // Write the length
                indexWriter.Close();
                indexWriter.Dispose();

                // Write the header

                BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                binaryWriter.Seek(0, SeekOrigin.Begin);                         // Move to start of the file
                _size++;                                                        // Update the size
                binaryWriter.Write(_size);                                      // Write the size
                _pointer = (UInt16)(_pointer + offset);                         //
                binaryWriter.Write((UInt16)(_pointer));                         // Write the pointer
                binaryWriter.Close();
                binaryWriter.Dispose();

                // Write the data

                // Appending will only work if the file is deleted and the updates start again
                // Not sure if this is the best approach.
                // Need to update the ...

                binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Append));
                byte flag = 0;
                binaryWriter.Write(flag);
                for (int i = 0; i < record.Length; i++)
                {
                    object data = record[i];
                    switch (_properties[i].Type)
                    {
                        case TypeCode.Int16:
                            {
                                binaryWriter.Write((Int16)data);
                                break;
                            }
                        case TypeCode.Int32:
                            {
                                binaryWriter.Write((int)data);
                                break;
                            }
                        case TypeCode.String:
                            {
                                string text = Convert.ToString(data);
                                if (_properties[i].Length < 0)
                                {
                                    binaryWriter.Write(text);
                                }
                                else
                                {
                                    if (text.Length > _properties[i].Length)
                                    {
                                        text = text.Substring(0, _properties[i].Length);
                                    }
                                    else
                                    {
                                        text = text.PadRight(_properties[i].Length, '\0');
                                    }
                                    binaryWriter.Write(text);
                                }
                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException();
                            }
                    }
                }
                binaryWriter.Close();
                binaryWriter.Dispose();
                created = true;
            }
            return (created);
        }

        /// <summary>
        /// Insert new record at index
        /// </summary>
        /// <param name="record"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool Insert(object[] record, int row)
        {
            bool insert = false;

            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);
            lock (_lockObject)
            {
                if ((row >= 0) && (row < _size))
                {

                    // insert the pointer into the index file

                    FileStream stream = new FileStream(indexPath + ".idx", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    BinaryReader indexReader = new BinaryReader(stream);
                    BinaryWriter indexWriter = new BinaryWriter(stream);

                    // copy the ponter and length data upwards 

                    UInt16 keyLength = 4;
                    keyLength = indexReader.ReadUInt16();
                    for (int counter = _size; counter > row; counter--)
                    {
                        indexReader.BaseStream.Seek(_begin + (counter - 1) * (keyLength + 4), SeekOrigin.Begin);         // Move to location of the index
                        UInt16 key = indexReader.ReadUInt16();                                      // Read the key from the index file
                        UInt16 pointer = indexReader.ReadUInt16();                                  // Read the pointer from the index file
                        UInt16 length = indexReader.ReadUInt16();                                   // Read the length from the index file
                        indexWriter.Seek(_begin + counter * (keyLength + 4), SeekOrigin.Begin);     // Move to location of the index
                        indexWriter.Write(key);
                        indexWriter.Write(pointer);
                        indexWriter.Write(length);
                    }
                    indexWriter.Close();
                    indexReader.Close();
                    stream.Close();

                    // insert the new record

                    indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.Open));
                    indexWriter.Seek(_begin + row * (keyLength + 4), SeekOrigin.Begin);              // Move to location of the index
                    indexWriter.Write(_size);           // Write the key
                    indexWriter.Write(_pointer);        // Write the pointer

                    int offset = 0;
                    offset += 1;    // Including the flag
                    for (int i = 0; i < record.Length; i++)
                    {
                        object data = record[i];
                        switch (_properties[i].Type)
                        {
                            case TypeCode.Int16:
                                {
                                    offset += 2;
                                    break;
                                }
                            case TypeCode.Int32:
                                {
                                    offset += 4;
                                    break;
                                }
                            case TypeCode.String:
                                {
                                    int length = _properties[i].Length;
                                    if (length < 0)
                                    {
                                        length = Convert.ToString(data).Length;
                                    }
                                    offset = offset + LEB128.Size(length) + length;     // Includes the byte length parameter
                                                                                        // ** need to watch this as can be 2 bytes if length is > 127 characters
                                                                                        // ** https://en.wikipedia.org/wiki/LEB128

                                    break;
                                }
                            default:
                                {
                                    throw new NotImplementedException();
                                }
                        }
                    }

                    // Update the index length

                    indexWriter.Write((UInt16)offset);  // Write the length
                    indexWriter.Close();
                    indexWriter.Dispose();

                    // Write the header

                    BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    binaryWriter.Seek(0, SeekOrigin.Begin);                         // Move to start of the file
                    _size++;                                                        // Update the size
                    binaryWriter.Write(_size);                                      // Write the size
                    _pointer = (UInt16)(_pointer + offset);                         //
                    binaryWriter.Write((UInt16)(_pointer));                         // Write the pointer
                    binaryWriter.Close();
                    binaryWriter.Dispose();

                    // Write the data

                    // Appending will only work if the file is deleted and the updates start again
                    // Not sure if this is the best approach.
                    // Need to update the ...

                    binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Append));
                    byte flag = 0;
                    binaryWriter.Write(flag);
                    for (int i = 0; i < record.Length; i++)
                    {
                        object data = record[i];
                        switch (_properties[i].Type)
                        {
                            case TypeCode.Int16:
                                {
                                    binaryWriter.Write((Int16)data);
                                    break;
                                }
                            case TypeCode.Int32:
                                {
                                    binaryWriter.Write((int)data);
                                    break;
                                }
                            case TypeCode.String:
                                {
                                    string text = Convert.ToString(data);
                                    if (_properties[i].Length < 0)
                                    {
                                        binaryWriter.Write(text);
                                    }
                                    else
                                    {
                                        if (text.Length > _properties[i].Length)
                                        {
                                            text = text.Substring(0, _properties[i].Length);
                                        }
                                        else
                                        {
                                            text = text.PadRight(_properties[i].Length, '\0');
                                        }
                                        binaryWriter.Write(text);
                                    }
                                    break;
                                }
                            default:
                                {
                                    throw new NotImplementedException();
                                }
                        }
                    }
                    binaryWriter.Close();
                    binaryWriter.Dispose();
                    insert = true;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            return (insert);
        }


        /// <summary>
        /// Read record by index
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        internal object[] Read(int row)
        {
            object[] data = null;

            lock (_lockObject)
            {
                if (_size > 0)
                {
                    if ((row >= 0) && (row < _size))
                    {
                        data = new object[Items];
                        string filenamePath = System.IO.Path.Combine(_path, _name);
                        string indexPath = System.IO.Path.Combine(_path, _index);

                        // Need to search the index file

                        BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                        BinaryReader indexReader = new BinaryReader(new FileStream(indexPath + ".idx", FileMode.Open));

                        UInt16 keyLength = 4;
                        keyLength = indexReader.ReadUInt16();
                        indexReader.BaseStream.Seek(_begin + row * (keyLength + 4), SeekOrigin.Begin);                               // Get the pointer from the index file
                        UInt16 key = indexReader.ReadUInt16();
                        UInt16 pointer = indexReader.ReadUInt16();                                              // Reader the pointer from the index file
                        binaryReader.BaseStream.Seek(_data + pointer, SeekOrigin.Begin);                                // Move to the correct location in the data file

                        byte flag = binaryReader.ReadByte();
                        for (int count = 0; count < _items; count++)
                        {
                            switch (_properties[count].Type)
                            {
                                case TypeCode.Int16:
                                    {
                                        data[count] = binaryReader.ReadInt16();
                                        break;
                                    }
                                case TypeCode.Int32:
                                    {
                                        data[count] = binaryReader.ReadInt32();
                                        break;
                                    }
                                case TypeCode.String:
                                    {
                                        // should not need to lenght check again here
                                        data[count] = binaryReader.ReadString();
                                        break;
                                    }
                                default:
                                    {
                                        throw new NotImplementedException();
                                    }
                            }
                        }
                        binaryReader.Close();
                        binaryReader.Dispose();
                        indexReader.Close();
                        indexReader.Dispose();
                    }
                    else
                    {
                        throw new IndexOutOfRangeException();
                    }
                }
            }
            return (data);
        }

		/// <summary>
        /// Update the record by index
        /// </summary>
        /// <param name="record"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool Update(object[] record, int row)
        {
            bool updated = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);
            lock (_lockObject)
            {
                if ((row >= 0) && (row < _size))
                {
                    // Calculate the size of the new record
                    // if greater than the space append and update
                    // the index
                    // if less then overwite the space with the new record

                    BinaryReader indexReader = new BinaryReader(new FileStream(indexPath + ".idx", FileMode.Open));
                    UInt16 keyLength = 4;
                    keyLength = indexReader.ReadUInt16();
                    indexReader.BaseStream.Seek(_begin + row * (keyLength + 4), SeekOrigin.Begin);                               // Get the pointer from the index file
                    UInt16 key = indexReader.ReadUInt16();
                    UInt16 pointer = indexReader.ReadUInt16();                                      // Reader the pointer from the index file
                    UInt16 offset = indexReader.ReadUInt16();
                    indexReader.Close();
                    indexReader.Dispose();

                    int length = 0;
                    length += 1;    // Including the flag
                    for (int i = 0; i < record.Length; i++)
                    {
                        object data = record[i];
                        switch (_properties[i].Type)
                        {
                            case TypeCode.Int16:
                                {
                                    length += 2;
                                    break;
                                }
                            case TypeCode.Int32:
                                {
                                    length += 4;
                                    break;
                                }
                            case TypeCode.String:
                                {
                                    int l = _properties[i].Length;
                                    if (l < 0)
                                    {
                                        l = Convert.ToString(data).Length;
                                    }
                                    length = length + LEB128.Size(l) + l;       // Includes the byte length parameter
                                                                                // ** need to watch this as can be 2 bytes if length is > 127 characters
                                                                                // ** https://en.wikipedia.org/wiki/LEB128

                                    break;
                                }
                            default:
                                {
                                    throw new NotImplementedException();
                                }
                        }
                    }

                    BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    if (offset >= length)
                    {
                        // If there is space write the data

                        binaryWriter.Seek(0, SeekOrigin.Begin);     // Move to start of the file
                        binaryWriter.Seek(_data + pointer, SeekOrigin.Begin);

                        byte flag = 0;
                        binaryWriter.Write(flag);
                        for (int i = 0; i < record.Length; i++)
                        {
                            object data = record[i];
                            switch (_properties[i].Type)
                            {
                                case TypeCode.Int16:
                                    {
                                        binaryWriter.Write((Int16)data);
                                        break;
                                    }
                                case TypeCode.Int32:
                                    {
                                        binaryWriter.Write((int)data);
                                        break;
                                    }
                                case TypeCode.String:
                                    {
                                        string text = Convert.ToString(data);
                                        if (_properties[i].Length < 0)
                                        {
                                            binaryWriter.Write(text);
                                        }
                                        else
                                        {
                                            if (text.Length > _properties[i].Length)
                                            {
                                                text = text.Substring(0, _properties[i].Length);
                                            }
                                            else
                                            {
                                                text = text.PadRight(_properties[i].Length, '\0');
                                            }
                                            binaryWriter.Write(text);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        throw new NotImplementedException();
                                    }
                            }
                        }
                    }
                    else
                    {
                        // There is no space so flag the record to indicate its spare

                        binaryWriter.Seek(_data + pointer, SeekOrigin.Begin);
                        byte flag = 2;
                        binaryWriter.Write(flag);

                        // Overwrite the index to use the new location at the end of the file

                        BinaryWriter indexWriter = new BinaryWriter(new FileStream(indexPath + ".idx", FileMode.Open));
                        indexWriter.Seek(_begin + row * (keyLength + 4), SeekOrigin.Begin);   // Get the index pointer
                        indexWriter.Write((UInt16)key); // check the key
                        indexWriter.Write(_pointer);
                        indexWriter.Close();

                        // Write the header

                        binaryWriter.Seek(0, SeekOrigin.Begin);     // Move to start of the file
                        binaryWriter.Write(_size);                  // Write the size
                        _pointer = (UInt16)(_pointer + length);     //
                        binaryWriter.Write(_pointer);               // Write the pointer
                        binaryWriter.Close();

                        // Write the data

                        // Appending will only work if the file is deleted and the updates start again
                        // Not sure if this is the best approach.
                        // Need to update the 

                        binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Append));
                        flag = 0;
                        binaryWriter.Write(flag);
                        for (int i = 0; i < record.Length; i++)
                        {
                            object data = record[i];
                            switch (_properties[i].Type)
                            {
                                case TypeCode.Int16:
                                    {
                                        binaryWriter.Write((Int16)data);
                                        break;
                                    }
                                case TypeCode.Int32:
                                    {
                                        binaryWriter.Write((int)data);
                                        break;
                                    }
                                case TypeCode.String:
                                    {
                                        string text = Convert.ToString(data);
                                        if (_properties[i].Length < 0)
                                        {
                                            binaryWriter.Write(text);
                                        }
                                        else
                                        {
                                            if (text.Length > _properties[i].Length)
                                            {
                                                text = text.Substring(0, _properties[i].Length);
                                            }
                                            else
                                            {
                                                text = text.PadRight(_properties[i].Length, '\0');
                                            }
                                            binaryWriter.Write(text);
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        throw new NotImplementedException();
                                    }
                            }
                        }
                    }
                    updated = true;
                    binaryWriter.Close();
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            return (updated);
        }

        /// <summary>
        /// Delete the record by index
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        internal bool Delete(int row)
        {
            bool deleted = false;
            string filenamePath = System.IO.Path.Combine(_path, _name);
            string indexPath = System.IO.Path.Combine(_path, _index);
            lock (_lockObject)
            {
                if ((row >= 0) && (row < _size))
                {
                    // Write the header

                    BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    binaryWriter.Seek(0, SeekOrigin.Begin);     // Move to start of the file
                    _size--;
                    binaryWriter.Write(_size);                  // Write the new size

                    UInt16 keyLength = 4;
                    BinaryReader indexReader = new BinaryReader(new FileStream(indexPath + ".idx", FileMode.Open));
                    keyLength = indexReader.ReadUInt16();
                    indexReader.BaseStream.Seek(_begin + row * (keyLength + 4), SeekOrigin.Begin);                               // Get the pointer from the index file
                    UInt16 KEY = indexReader.ReadUInt16();
                    UInt16 pointer = indexReader.ReadUInt16();
                    indexReader.Close();

                    // There is no space so flag the record to indicate its deleted

                    binaryWriter.Seek(_data + pointer, SeekOrigin.Begin);
                    byte flag = 1;
                    binaryWriter.Write(flag);
                    binaryWriter.Close();

                    // Overwrite the index

                    FileStream stream = new FileStream(indexPath + ".idx", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    indexReader = new BinaryReader(stream);
                    BinaryWriter indexWriter = new BinaryWriter(stream);

                    // copy the ponter and length data downwards 

                    for (int counter = row; counter < _size; counter++)
                    {
                        indexReader.BaseStream.Seek(_begin + (counter + 1) * (keyLength + 4), SeekOrigin.Begin); // Move to location of the index
                        pointer = indexReader.ReadUInt16();                                           // Read the pointer from the index file
                        UInt16 offset = indexReader.ReadUInt16();
                        indexWriter.Seek(_begin + counter * (keyLength + 4), SeekOrigin.Begin); // Move to location of the index
                        indexWriter.Write(pointer);
                        indexWriter.Write(offset);
                    }
                    indexWriter.BaseStream.SetLength(_size * 4);    // Trim the file as Add uses append
                    indexWriter.Close();
                    indexReader.Close();
                    stream.Close();

                    deleted = true;

                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            return (deleted);
        }

        #endregion
        #region Private

        internal Property GetField(int index)
        {
            if (index < _size)
            {
                Property property = new Property();
                string filenamePath = System.IO.Path.Combine(_path, _name);
                lock (_lockObject)
                {
                    BinaryReader binaryReader = new BinaryReader(new FileStream(filenamePath + ".dbf", FileMode.Open));
                    UInt16 pointer = _start; // Skip over header and size
                    byte offset = 0;
                    for (int counter = 0; counter < _size; counter++)
                    {
                        binaryReader.BaseStream.Seek(pointer, SeekOrigin.Begin);
                        offset = binaryReader.ReadByte();
                        if (counter != index)
                        {
                            pointer = (UInt16)(pointer + offset);
                        }
                        else
                        {
                            byte flag = binaryReader.ReadByte();                    // Read the status flag
                            byte order = binaryReader.ReadByte();                   // Read the Order
                            TypeCode typeCode = (TypeCode)binaryReader.ReadByte();  // Read the field Type
                            sbyte length = binaryReader.ReadSByte();                // Read the field Length
                            bool primary = false;                                   // Read if the primary key
                            if (binaryReader.ReadByte() == 1)
                            {
                                primary = true;
                            }
                            string name = binaryReader.ReadString();                // Read the field Name
                            property = new Property(name, flag, order, typeCode, length, primary);
                            break;
                        }
                    }
                    binaryReader.Close();
                }
                return (property);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        #endregion
        #endregion
    }

    #region Static
    internal static class LEB128
    {
        public static byte[] Encode(int value)
        {
            byte[] data = new byte[5];  // Assume 32 bit max as its an int32
            int size = 0;
            do
            {
                byte byt = (byte)(value & 0x7f);
                value >>= 7;
                if (value != 0)
                {
                    byt = (byte)(byt | 128);
                }
                data[size] = byt;
                size += 1;
            } while (value != 0);
            return (data);
        }

        public static int Size(int value)
        {
            int size = 0;
            do
            {
                value >>= 7;
                size += 1;
            } while (value != 0);
            return (size);
        }
    }
    #endregion
}
