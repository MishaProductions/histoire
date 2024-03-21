using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace neptunebot
{
    public class DataStorage
    {
        public static DataStorageMain DB { get; set; }

        static DataStorage()
        {
            if (!File.Exists("data.json"))
            {
                Console.WriteLine("Creating new data file...");
                DB = new DataStorageMain();
                Save();
            }

            var d = JsonConvert.DeserializeObject<DataStorageMain>(File.ReadAllText("data.json"));
            if (d == null)
                throw new Exception("failed to open data base");
            DB = d;
        }

        public static void Save()
        {
            File.WriteAllText("data.json", JsonConvert.SerializeObject(DB, Formatting.Indented));
        }
    }
    [DataContract]
    public class DataStorageMain
    {
        [DataMember]
        public List<DataStorageServerMember> Member = new List<DataStorageServerMember>();
    }
    [DataContract]
    public class DataStorageServerMember
    {
        [DataMember]
        public ulong Level { set; get; } = 0;
        [DataMember]
        public ulong ID { get; set; } = 0;

        [DataMember]
        public DateTime LastMessage { get; set; }
    }
}
