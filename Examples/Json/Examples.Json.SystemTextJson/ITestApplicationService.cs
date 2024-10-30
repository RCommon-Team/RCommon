namespace Examples.Json.SystemTextJson
{
    public interface ITestApplicationService
    {
        TestDto Deserialize(string json);
        string Serialize(TestDto testDto);
    }
}