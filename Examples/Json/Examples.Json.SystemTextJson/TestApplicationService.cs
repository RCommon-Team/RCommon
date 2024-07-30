using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Json.SystemTextJson
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly IJsonSerializer _serializer;

        public TestApplicationService(IJsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public string Serialize(TestDto testDto)
        {
            return _serializer.Serialize(testDto);
        }

        public TestDto Deserialize(string json)
        {
            return _serializer.Deserialize<TestDto>(json);
        }
    }
}
