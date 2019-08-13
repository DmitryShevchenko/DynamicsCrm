using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Simulations
{
    public static class SerializerWrapper
    {
        public static string Serialize<T>(this T srcObject)
        {
            using (var serializeStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(serializeStream, srcObject);
                return Encoding.Default.GetString(serializeStream.ToArray());
            }
        }

        public static T Deserialize<T>(this string jsonString)
        {
            using (var deserializeStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var writer = new System.IO.StreamWriter(deserializeStream);
                writer.Write(jsonString);
                writer.Flush();
                deserializeStream.Position = 0;
                return (T) serializer.ReadObject(deserializeStream);
            }
        }
    }
}