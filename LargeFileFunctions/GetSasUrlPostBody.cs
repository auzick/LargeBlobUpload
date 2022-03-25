using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LargeFileFunctions;

[Serializable]
public class GetSasUrlPostBody
{
    public string FileName { get; set; }

    public static GetSasUrlPostBody FromPayload(string payload)
    {
        return JsonSerializer.Deserialize<GetSasUrlPostBody>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}


