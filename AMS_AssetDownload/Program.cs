using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AMS_AssetDownload
{
    class Program
    {
        public static async Task Main(string[] args)
        {

            ConfigWrapper config = new ConfigWrapper(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build());

            var storageConfig = GetConfiguration();
            var downloadTo = storageConfig.GetSection("downloadBasePath").Value;
            var storageConnectionString = storageConfig.GetSection("storageConnectionString").Value;

            try
            {
                IAzureMediaServicesClient client = await CreateMediaServicesClientAsync(config);
                BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);

                Console.WriteLine("connected");

                var assets = client.Assets.List(config.ResourceGroup, config.AccountName);

                Directory.CreateDirectory(downloadTo);

                foreach(var asset in assets)
                {
                    Console.WriteLine("========" + asset.Container + "========");

                    string downloadDir = downloadTo + @"\" + asset.Container;
                    Directory.CreateDirectory(downloadDir);

                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(asset.Container);
                    var blobs = blobContainerClient.GetBlobs();
                    foreach(var blob in blobs)
                    {
                        string downloadFilePath = downloadDir + @"\" + blob.Name;
                        BlobClient blobClient = blobContainerClient.GetBlobClient(blob.Name);
                        BlobDownloadInfo download = await blobClient.DownloadAsync();

                        Console.WriteLine("Downloading : " + downloadFilePath);
                        using (FileStream downloadFileStream = File.OpenWrite(downloadFilePath))
                        {
                            await download.Content.CopyToAsync(downloadFileStream);
                            downloadFileStream.Close();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception.Source.Contains("ActiveDirectory"))
                {
                    Console.Error.WriteLine("TIP: Make sure that you have filled out the appsettings.json file before running this sample.");
                }

                Console.Error.WriteLine($"{exception.Message}");

                ApiErrorException apiException = exception.GetBaseException() as ApiErrorException;
                if (apiException != null)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
            }

            Console.WriteLine("Press Enter to continue.");
            Console.ReadLine();
        }

        private static async Task<ServiceClientCredentials> GetCredentialsAsync(ConfigWrapper config)
        {
            // Use ApplicationTokenProvider.LoginSilentWithCertificateAsync or UserTokenProvider.LoginSilentAsync to get a token using service principal with certificate
            //// ClientAssertionCertificate
            //// ApplicationTokenProvider.LoginSilentWithCertificateAsync

            // Use ApplicationTokenProvider.LoginSilentAsync to get a token using a service principal with symmetric key
            ClientCredential clientCredential = new ClientCredential(config.AadClientId, config.AadSecret);
            return await ApplicationTokenProvider.LoginSilentAsync(config.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
        }

        private static async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync(ConfigWrapper config)
        {
            var credentials = await GetCredentialsAsync(config);

            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }

        static IConfiguration GetConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configBuilder.AddJsonFile(@"StorageConfig.json");

            return configBuilder.Build();
        }
    }
}
