using System.Text;

namespace RabbitMQ.Library.Helper
{
    public static class RabbitMqValueHelper
    {
        public static string ConvertHeaderValue(object value)
        {
            if (value is byte[] byteValue)
            {
                return Encoding.UTF8.GetString(byteValue);
            }

            return value.ToString();
        }
    }
}