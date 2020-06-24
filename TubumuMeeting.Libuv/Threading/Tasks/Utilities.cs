using System.Threading.Tasks;

namespace TubumuMeeting.Libuv.Utilities
{
    public static partial class UtilitiesExtensions
    {
        public static Task PumpAsync<T>(this IUVStream<T> readStream, IUVStream<T> writeStream)
        {
            return HelperFunctions.Wrap(writeStream, readStream.Pump);
        }
    }
}
