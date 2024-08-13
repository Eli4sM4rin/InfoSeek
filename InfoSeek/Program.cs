using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Playwright;
using Nest;
using PlaywrightPage = Microsoft.Playwright.IPage; // Alias para el IPage de Playwright

class Program
{
    static async Task Main(string[] args)
    {
        var seedUrls = new List<string>
        {
            "https://developer.mozilla.org/",
            "https://www.w3schools.com/",
            "https://stackoverflow.com/",
            "https://github.com/",
            "https://techcrunch.com/",
            "https://www.wikipedia.org/"
        };

        var visitedUrls = new HashSet<Uri>();
        var urlsToProcess = new Queue<Uri>();

        // Agregar las URLs iniciales a la cola
        foreach (var url in seedUrls)
        {
            urlsToProcess.Enqueue(new Uri(url));
        }

        var elasticClient = new ElasticClient(new Uri("http://localhost:9200"));
        var indexName = "example-pages";
        await elasticClient.Indices.CreateAsync(indexName);

        // Initialize Playwright
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var browserContext = await browser.NewContextAsync();
        var page = await browserContext.NewPageAsync();

        // Add URLs from RSS feeds
        var rssUrls = new List<string>
        {
            "https://rss.cnn.com/rss/edition_technology.rss",
            "https://feeds.bbci.co.uk/news/technology/rss.xml"
        };

        foreach (var rssUrl in rssUrls)
        {
            try
            {
                var newUrls = GetUrlsFromRss(rssUrl);
                foreach (var url in newUrls)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        urlsToProcess.Enqueue(uri);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing RSS feed {rssUrl}: {ex.Message}");
            }
        }

        // Add URLs from search results
        var apiKey = "AIzaSyA87mEC46iLon49xXYP47eGHekgNVQZzhU";
        var cx = "60d98489408e345a0";

        var searchQueries = new List<string> { "technology", "web development", "software engineering" };

        foreach (var query in searchQueries)
        {
            try
            {
                var newUrls = await SearchForNewUrls(query, apiKey, cx);
                foreach (var url in newUrls)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        urlsToProcess.Enqueue(uri);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for URLs with query '{query}': {ex.Message}");
            }
        }

        // Process urls
        while (urlsToProcess.Count > 0)
        {
            var uri = urlsToProcess.Dequeue();
            if (!visitedUrls.Add(uri))
                continue;

            try
            {
                // Open the page
                var response = await page.GotoAsync(uri.AbsoluteUri, new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle });
                if (response == null || !response.Ok)
                {
                    Console.WriteLine($"Cannot open the page {uri}");
                    continue;
                }

                Console.WriteLine($"Indexing {uri}");

                // Extract data from the page
                var elasticPage = new ElasticWebPage
                {
                    Url = (await GetCanonicalUrl(page)).AbsoluteUri,
                    Contents = await GetMainContents(page),
                    Title = await page.TitleAsync(),
                };

                // Store the data in Elasticsearch
                await elasticClient.IndexAsync(elasticPage, i => i.Index(indexName));

                // Find all links and add them to the queue
                var links = await GetLinks(page);
                foreach (var link in links)
                {
                    if (!visitedUrls.Contains(link))
                        urlsToProcess.Enqueue(link);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing URL {uri}: {ex.Message}");
            }
        }
    }

    static async Task<IEnumerable<string>> SearchForNewUrls(string query, string apiKey, string cx)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://www.googleapis.com/customsearch/v1?q={query}&key={apiKey}&cx={cx}");
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var searchResults = JsonDocument.Parse(jsonString);

        var urls = new List<string>();
        foreach (var item in searchResults.RootElement.GetProperty("items").EnumerateArray())
        {
            urls.Add(item.GetProperty("link").GetString());
        }

        return urls;
    }

    static IEnumerable<string> GetUrlsFromRss(string rssUrl)
    {
        using var reader = XmlReader.Create(rssUrl);
        var feed = SyndicationFeed.Load(reader);

        foreach (var item in feed.Items)
        {
            yield return item.Links.First().Uri.AbsoluteUri;
        }
    }

    static async Task<Uri> GetCanonicalUrl(PlaywrightPage page)
    {
        var pageUrl = new Uri(page.Url, UriKind.Absolute);
        var link = await page.QuerySelectorAsync("link[rel=canonical]");
        if (link != null)
        {
            var href = await link.GetAttributeAsync("href");
            if (Uri.TryCreate(pageUrl, href, out var result))
                return result;
        }

        return pageUrl;
    }

    static async Task<IReadOnlyCollection<Uri>> GetLinks(PlaywrightPage page)
    {
        var result = new List<Uri>();
        var anchors = await page.QuerySelectorAllAsync("a[href]");
        foreach (var anchor in anchors)
        {
            var href = await anchor.EvaluateAsync<string>("node => node.href");
            if (!Uri.TryCreate(href, UriKind.Absolute, out var url))
                continue;

            result.Add(url);
        }

        return result;
    }

    static async Task<IReadOnlyCollection<string>> GetMainContents(PlaywrightPage page)
    {
        var result = new List<string>();
        var elements = await page.QuerySelectorAllAsync("main, *[role=main]");
        if (elements.Any())
        {
            foreach (var element in elements)
            {
                var innerText = await element.EvaluateAsync<string>("node => node.innerText");
                result.Add(innerText);
            }
        }
        else
        {
            var innerText = await page.InnerTextAsync("body");
            result.Add(innerText);
        }

        return result;
    }

    [ElasticsearchType(IdProperty = nameof(Url))]
    public sealed class ElasticWebPage
    {
        [Keyword]
        public string? Url { get; set; }

        [Text]
        public string? Title { get; set; }

        [Text]
        public IEnumerable<string>? Contents { get; set; }
    }
}
