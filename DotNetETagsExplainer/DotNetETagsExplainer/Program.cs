﻿using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace AzureSearch.SDKHowTo
{
    class Program
    {
        // This sample shows how ETags work by performing conditional updates and deletes
        // on an Azure Search index.
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchIndexClient searchIndexClient = CreateSearchServiceClient(configuration);

            Console.WriteLine("Deleting index...\n");
            DeleteTestIndexIfExists(searchIndexClient);

            // Every top-level resource in Azure Search has an associated ETag that keeps track of which version
            // of the resource you're working on. When you first create a resource such as an index, its ETag is
            // empty.
            SearchIndex index = DefineTestIndex();
            Console.WriteLine(
                $"Test index hasn't been created yet, so its ETag should be blank. ETag: '{index.ETag}'");

            // Once the resource exists in Azure Search, its ETag will be populated. Make sure to use the object
            // returned by the SearchServiceClient! Otherwise, you will still have the old object with the
            // blank ETag.
            //Console.WriteLine("Creating index...\n");
            index = searchIndexClient.CreateIndex(index);
            Console.WriteLine($"Test index created; Its ETag should be populated. ETag: '{index.ETag}'");

            // ETags let you do some useful things you couldn't do otherwise. For example, by using an If-Match
            // condition, we can update an index using CreateOrUpdate and be guaranteed that the update will only
            // succeed if the index already exists.
            index.Fields.Add(new SearchField("name", SearchFieldDataType.String) { AnalyzerName = LexicalAnalyzerName.EnMicrosoft });
            index = searchIndexClient.CreateOrUpdateIndex(index);

            Console.WriteLine(
                $"Test index updated; Its ETag should have changed since it was created. ETag: '{index.ETag}'");

            // More importantly, ETags protect you from concurrent updates to the same resource. If another
            // client tries to update the resource, it will fail as long as all clients are using the right
            // access conditions.
            SearchIndex indexForClientUpdate = index;
            SearchIndex indexForClientUpdateFailed = searchIndexClient.GetIndex("test");

            Console.WriteLine("Simulating concurrent update. To start, both clients see the same ETag.");
            Console.WriteLine($"ClientUpdate ETag: '{indexForClientUpdate.ETag}' ClientUpdateFailed ETag: '{indexForClientUpdateFailed.ETag}'");

            // indexForClientUpdate successfully updates the index.
            indexForClientUpdate.Fields.Add(new SearchField("a", SearchFieldDataType.Int32));
            indexForClientUpdate = searchIndexClient.CreateOrUpdateIndex(indexForClientUpdate);

            Console.WriteLine($"Test index updated by ClientUpdate; ETag: '{indexForClientUpdate.ETag}'");

            // indexForClientUpdateFailed tries to update the index, but fails, thanks to the ETag check.
            try
            {
                indexForClientUpdateFailed.Fields.Add(new SearchField("b", SearchFieldDataType.Boolean));
                searchIndexClient.CreateOrUpdateIndex(indexForClientUpdateFailed);

                Console.WriteLine("Whoops; This shouldn't happen");
                Environment.Exit(1);
            }
            catch (RequestFailedException e) when (e.Status == 400)
            {
                Console.WriteLine("ClientUpdateFailed failed to update the index, as expected.");
            }

            // You can also use access conditions with Delete operations. For example, you can implement an
            // atomic version of the DeleteTestIndexIfExists method from this sample like this:
            Console.WriteLine("Deleting index...\n");
            searchIndexClient.DeleteIndex("test");

            // This is slightly better than using the Exists method since it makes only one round trip to
            // Azure Search instead of potentially two. It also avoids an extra Delete request in cases where
            // the resource is deleted concurrently, but this doesn't matter much since resource deletion in
            // Azure Search is idempotent.

            // And we're done! Bye!
            Console.WriteLine("Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }
        /// <summary>
        /// Configure the Azure Cognitive Search Endpoint and AdminApiKey in appsetting.json to create a SearchServicClient 
        /// which use to interact with Azure Cogntive Search Service.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private static SearchIndexClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServicEndpoint = configuration["SearchServicEndpoint"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchIndexClient searchIndexClient = new SearchIndexClient(new Uri(searchServicEndpoint), new AzureKeyCredential(adminApiKey));
            return searchIndexClient;
        }

        private static void DeleteTestIndexIfExists(SearchIndexClient searchIndexClient)
        {
            if (searchIndexClient.GetIndexNames().Contains("test"))
            {
                searchIndexClient.DeleteIndex("test");
            }
        }

        private static SearchIndex DefineTestIndex() =>
            new SearchIndex("test", new[] { new SearchField("id", SearchFieldDataType.String) { IsKey = true } });

    }
}
