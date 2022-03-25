using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace LargeFileFunctions;
public class GetTempUploadUrl
{
    private AzureFunctionSettings settings;

    public GetTempUploadUrl(AzureFunctionSettings settings)
    {
        this.settings = settings;
        Console.WriteLine("Connection string: " + settings.StorageAccountConnectionString);
    }

    [Function("GetTempUploadUrl")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GetTempUploadUrl")]
            HttpRequestData req,
            FunctionContext executionContext
        )
    {
        var logger = executionContext.GetLogger("GetTempUploadUrl");

        var requestBody = new StreamReader(req.Body).ReadToEnd();
        var postData = GetSasUrlPostBody.FromPayload(requestBody);
        if (string.IsNullOrEmpty(postData.FileName))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var blob = new BlobClient(
            settings.StorageAccountConnectionString, 
            settings.StorageContainerName, 
            WebUtility.UrlEncode(postData.FileName));

        var result = new
        {
            url = GetServiceSasUriForBlob(blob).AbsoluteUri,
            account = blob.AccountName,
            container = blob.BlobContainerName,
            name = WebUtility.UrlEncode(postData.FileName)
        };


        var response = req.CreateResponse(HttpStatusCode.OK);
        //response.Headers.Add("Content-Type", "text/json");
        response.Headers.Add("Custom-Header", "custom value");

        Console.WriteLine(JsonSerializer.Serialize(result));

        await response.WriteAsJsonAsync(result);

        return response;
    }

    private static Uri GetServiceSasUriForBlob(BlobClient blobClient, string storedPolicyName = null)
    {
        // Check whether this BlobClient object has been authorized with Shared Key.
        if (blobClient.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one hour.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                BlobName = blobClient.Name,
                Resource = "b"
            };

            if (storedPolicyName == null)
            {
                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                sasBuilder.SetPermissions(BlobSasPermissions.Read |
                    BlobSasPermissions.Write);
            }
            else
            {
                sasBuilder.Identifier = storedPolicyName;
            }

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            Console.WriteLine("SAS URI for blob is: {0}", sasUri);
            Console.WriteLine();

            return sasUri;
        }
        else
        {
            Console.WriteLine(@"BlobClient must be authorized with Shared Key 
                          credentials to create a service SAS.");
            return null;
        }
    }
}
