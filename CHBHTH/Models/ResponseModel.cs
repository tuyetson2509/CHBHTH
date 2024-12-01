using Newtonsoft.Json;

public class ResponseModel
{
    [JsonProperty("payUrl")]
    public string PayUrl { get; set; }
}