using Raven.Serializer;

namespace Raven.Message.RabbitMQ
{
    public static class SerializerService
    {
        public static byte[] Serialize<T>(T obj, SerializerType serializerType)
        {
            IDataSerializer serializer = SerializerFactory.Create(serializerType);
            return serializer.Serialize(obj);
        }

        public static T Deserialize<T>(byte[] obj, SerializerType serializerType)
        {
            IDataSerializer serializer = SerializerFactory.Create(serializerType);
            return serializer.Deserialize<T>(obj);
        }
    }
}
