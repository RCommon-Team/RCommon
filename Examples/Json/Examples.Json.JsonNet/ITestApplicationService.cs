namespace Examples.Json.JsonNet
{
    public interface ITestApplicationService
    {
        TestDto Deserialize(string json);
        string Serialize(TestDto testDto);
    }
}