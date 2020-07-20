using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace AutocompleteTutorial.Controllers
{
    public class HomeController : Controller
    {
        private static SearchIndexClient _searchIndexClient;
        private static SearchClient _searchClient;
        private static string IndexName = "nycjobs";

        public static string errorMessage;

        private void InitSearch()
        {

            string searchServiceEndpoint = ConfigurationManager.AppSettings["SearchServiceEndPoint"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            _searchIndexClient = new SearchIndexClient(new Uri(searchServiceEndpoint), new AzureKeyCredential(apiKey));
            _searchClient = _searchIndexClient.GetSearchClient(IndexName);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult IndexJavaScript()
        {
            return View();
        }

        public ActionResult Suggest(bool highlights, bool fuzzy, string term)
        {
            InitSearch();

            // Call suggest API and return results
            SuggestOptions so = new SuggestOptions()
            {
                UseFuzzyMatching = fuzzy,
                Size = 5
            };

            if (highlights)
            {
                so.HighlightPreTag = "<b>";
                so.HighlightPostTag = "</b>";
            }

            SuggestResults<SearchDocument> suggestResult = _searchClient.Suggest<SearchDocument>(term, "sg", so);

            // Convert the suggest query results to a list that can be displayed in the client.
            List<string> suggestions = suggestResult.Results.Select(x => x.Text).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = suggestions
            };
        }

        public ActionResult AutoComplete(string term)
        {
            InitSearch();
            //Call autocomplete API and return results
            AutocompleteOptions ao = new AutocompleteOptions()
            {
                Mode = AutocompleteMode.OneTermWithContext,
                UseFuzzyMatching = false,
                Size = 5
            };
            AutocompleteResults autocompleteResult = _searchClient.Autocomplete(term, "sg", ao);

            // Conver the Suggest results to a list that can be displayed in the client.
            List<string> autocomplete = autocompleteResult.Results.Select(x => x.Text).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = autocomplete
            };
        }

        public ActionResult Facets()
        {
            InitSearch();

            // Call suggest API and return results
            SearchOptions so = new SearchOptions()
            {
                Size = 0
            };
            if (so.Facets.Count != 0)
                so.Facets.Clear();
            so.Facets.Add("agency,count:500");

            SearchResults<SearchDocument> searchResult = _searchClient.Search<SearchDocument>("*", so);

            // Convert the suggest query results to a list that can be displayed in the client.

            List<string> facets = searchResult.Facets["agency"].Select(x => x.Value.ToString()).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = facets
            };
        }
    }
}
