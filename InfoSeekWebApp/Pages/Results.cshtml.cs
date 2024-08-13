using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Microsoft.AspNetCore.Html;

namespace InfoSeeKWebApp.Pages
{
    public class ResultsModel : PageModel
    {
        public string Search { get; set; } = string.Empty;
        public List<MatchResult> Matches { get; set; } = new List<MatchResult>();
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }

        public async Task OnGet(string search, int pageNumber = 1)
        {
            Search = search;
            PageNumber = pageNumber;

            if (!string.IsNullOrEmpty(Search))
            {
                var settings = new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("example-pages");
                var client = new ElasticClient(settings);
                int pageSize = 10;

                var result = await client.SearchAsync<IndexedPage>(s => s
                    .From((PageNumber - 1) * pageSize)
                    .Size(pageSize)
                    .Query(q => q
                        .Bool(b => b
                            .Should(
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
                        .Fields(f => f.Field("*"))
                    )
                );

                TotalPages = (int)Math.Ceiling((double)result.Total / pageSize);

                foreach (var hit in result.Hits.OrderByDescending(hit => hit.Score).DistinctBy(hit => hit.Source.Url))
                {
                    var highlight = string.Join(" ... ", hit.Highlight.SelectMany(field => field.Value));
                    Matches.Add(new MatchResult(hit.Source.Title!, hit.Source.Url!, new HtmlString(highlight)));
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
