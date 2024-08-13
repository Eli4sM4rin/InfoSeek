using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Nest;

namespace InfoSeeKWebApp.Pages
{
    public class IndexModel : PageModel
    {
        public List<MatchResult> Matches { get; set; } = new List<MatchResult>();

        [FromQuery(Name = "search")]
        public string Search { get; set; } = string.Empty;

        public List<NewsArticle>? NewsArticles { get; set; }

        public async Task OnGet()
        {
            if (!string.IsNullOrEmpty(Search))
            {
                var settings = new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("example-pages");
                var client = new ElasticClient(settings);

                var result = await client.SearchAsync<IndexedPage>(s => s
                    .Query(q => q
                        .Bool(b =>
                            b.Should(
                                bs => bs.Match(m => m.Field(f => f.Contents).Query(Search)),
                                bs => bs.Match(m => m.Field(f => f.Headers).Query(Search)),
                                bs => bs.Match(m => m.Field(f => f.Title).Query(Search)),
                                bs => bs.Match(m => m.Field(f => f.Description).Query(Search))
                            )
                         )
                    )
                    .Highlight(h => h
                        .PreTags("<strong>")
                        .PostTags("</strong>")
                        .Fields(f => f
                            .Field("*")
                        )
                    )
                );

                foreach (var hit in result.Hits.OrderByDescending(hit => hit.Score).DistinctBy(hit => hit.Source.Url))
                {
                    var highlight = "";
                    foreach (var field in hit.Highlight)
                    {
                        foreach (var value in field.Value)
                        {
                            if (highlight != "")
                            {
                                highlight += " ... ";
                            }

                            highlight += value;
                        }
                    }

                    Matches.Add(new MatchResult(hit.Source.Title!, hit.Source.Url!, new HtmlString(highlight)));
                }
            }
            else
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var requestUrl = "https://newsapi.org/v2/everything?q=technology&from=2024-07-18&sortBy=popularity&apiKey=2f90734a728f49dbb2ebf51cb3045602";

                        var response = await httpClient.GetStringAsync(requestUrl);
                        var newsApiResponse = JsonConvert.DeserializeObject<NewsApiResponse>(response);
                        NewsArticles = newsApiResponse?.Articles ?? new List<NewsArticle>();
                    }
                    catch (HttpRequestException ex)
                    {
                        // Manejar errores de solicitud HTTP aquí
                        Console.WriteLine($"Error al obtener noticias: {ex.Message}");
                    }
                    catch (JsonException ex)
                    {
                        // Manejar errores de deserialización JSON aquí
                        Console.WriteLine($"Error al deserializar respuesta JSON: {ex.Message}");
                    }
                }
            }
        }

        public class MatchResult
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public HtmlString Highlight { get; set; }

            public MatchResult(string title, string url, HtmlString highlight)
            {
                Title = title;
                Url = url;
                Highlight = highlight;
            }
        }

        public class NewsArticle
        {
            public string Title { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string UrlToImage { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class NewsApiResponse
        {
            public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
        }

        [ElasticsearchType(IdProperty = nameof(Url))]
        public class IndexedPage
        {
            public string Url { get; set; } = string.Empty;
            public IEnumerable<string> Contents { get; set; } = new List<string>();
            public IEnumerable<string> Headers { get; set; } = new List<string>();
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}