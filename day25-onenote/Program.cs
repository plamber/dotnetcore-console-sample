﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;

namespace ConsoleGraphTest
{
    class Program
    {
        private static GraphServiceClient _graphServiceClient;
        private static HttpClient _httpClient;

        static void Main(string[] args)
        {
            // Load appsettings.json
            var config = LoadAppSettings();
            if (null == config)
            {
                Console.WriteLine("Missing or invalid appsettings.json file. Please see README.md for configuration instructions.");
                return;
            }

            //Query using Graph SDK (preferred when possible)
            GraphServiceClient graphClient = GetAuthenticatedGraphClient(config);

            //Direct query using HTTPClient (for beta endpoint calls or not available in Graph SDK)
            HttpClient httpClient = GetAuthenticatedHTTPClient(config);
            
            // Call your method wrapping construction and calls to the helper
            OneNoteHelperCall();
        }

        // Add a private method to do any necessary setup and make calls to your helper
        private static void OneNoteHelperCall()
        {
            const string userPrincipalName = "adelev@m365x974797.onmicrosoft.com";
            const string notebookName = "Microsoft Graph notes";
            const string sectionName = "Required Reading";
            const string pageName = "30DaysMSGraph";

            var onenoteHelper = new OneNoteHelper(_graphServiceClient);
            var onenoteHelperHttp = new OneNoteHelper(_httpClient);
            
            var notebookResult = onenoteHelper.GetNotebook(userPrincipalName, notebookName) ?? onenoteHelper.CreateNoteBook(userPrincipalName, notebookName).GetAwaiter().GetResult();
            Console.WriteLine("Found / created notebook: " + notebookResult.DisplayName);

            var sectionResult = onenoteHelper.GetSection(userPrincipalName, notebookResult, sectionName) ?? onenoteHelper.CreateSection(userPrincipalName, notebookResult, sectionName).GetAwaiter().GetResult();
            Console.WriteLine("Found / created section: " + sectionResult.DisplayName);

            var pageCreateResult = onenoteHelperHttp.CreatePage(userPrincipalName, sectionResult, pageName).GetAwaiter().GetResult();

            var pageGetResult = onenoteHelper.GetPage(userPrincipalName, sectionResult, pageName);
            Console.WriteLine("Found / created page: " + pageGetResult.Title);
        }

        private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config)
        {
            var authenticationProvider = CreateAuthorizationProvider(config);
            _graphServiceClient = new GraphServiceClient(authenticationProvider);
            return _graphServiceClient;
        }

        private static HttpClient GetAuthenticatedHTTPClient(IConfigurationRoot config)
        {
            var authenticationProvider = CreateAuthorizationProvider(config);
            _httpClient = new HttpClient(new AuthHandler(authenticationProvider, new HttpClientHandler()));
            return _httpClient;
        }

        private static IAuthenticationProvider CreateAuthorizationProvider(IConfigurationRoot config)
        {
            var clientId = config["applicationId"];
            var clientSecret = config["applicationSecret"];
            var redirectUri = config["redirectUri"];
            var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

            List<string> scopes = new List<string>();
            scopes.Add("https://graph.microsoft.com/.default");

            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .WithClientSecret(clientSecret)
                                                    .Build();
            return new MsalAuthenticationProvider(cca, scopes.ToArray());
        }

        private static IConfigurationRoot LoadAppSettings()
        {
            try
            {
                var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

                // Validate required settings
                if (string.IsNullOrEmpty(config["applicationId"]) ||
                    string.IsNullOrEmpty(config["applicationSecret"]) ||
                    string.IsNullOrEmpty(config["redirectUri"]) ||
                    string.IsNullOrEmpty(config["tenantId"]) ||
                    string.IsNullOrEmpty(config["domain"]))
                {
                    return null;
                }

                return config;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }
    }
}
