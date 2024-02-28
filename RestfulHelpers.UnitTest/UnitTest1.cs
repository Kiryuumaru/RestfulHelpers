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

        public class TelemetryEntity
        {
            public required string Id { get; set; }

            public required DateTimeOffset DateTime { get; set; }

            public List<string> Tags { get; set; } = [];

            public required JsonDocument Data { get; set; }
        }


        [Fact]
        public async void Test1()
        {
            var apiEndpoint = "awdawd";

            var query = HttpUtility.ParseQueryString(string.Empty);

            query.Add("seconds", "989999");

            var ss = await new HttpClient().Execute<IEnumerable<TelemetryEntity>>(HttpMethod.Get, apiEndpoint + "?" + query.ToString(), CamelCaseOption);

            Assert.NotNull(ss);
        }
    }
}