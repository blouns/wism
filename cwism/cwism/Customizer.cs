using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;
using wism;

namespace cwism
{
    public class Customizer
    {
        public void WriteTemplates(string path)
        {
            if (!Directory.Exists(path))
                throw new ArgumentException("Templates path is not valid.", "path");

            IList<ICustomizable> objects = new List<ICustomizable>();
            objects.Add(new AffiliationInfo());
            objects.Add(new UnitInfo());
            objects.Add(new TerrainInfo());

            foreach (ICustomizable obj in objects)
                SerializeType(path, obj);
        }
        
        private static void SerializeType(string path, ICustomizable obj)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            serializer.WriteObject(stream, obj);

            string fileName = String.Format("{0}\\{1}", path, obj.FileName);
            Console.WriteLine("Writing: {0}", fileName);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            File.WriteAllText(fileName, sr.ReadToEnd());            
        }
    }
}
