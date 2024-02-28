using System.Text.Json;
using System.Threading;
using System.Web;

namespace RestfulHelpers.UnitTest
{
    public class UnitTest1
    {
        public static readonly JsonSerializerOptions CamelCaseOption = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        [Fact]
        public async void Test1()
        {
        }
    }
}