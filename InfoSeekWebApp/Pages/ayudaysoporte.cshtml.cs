using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InfoSeeKWebApp.Pages
{
    public class ayudaysoporteModel : PageModel
    {
        // Propiedad pública que puede ser utilizada en la vista Razor
        public string PageTitle { get; set; }

        public void OnGet()
        {
            // Asignar valor a la propiedad
            PageTitle = "Ayuda y Soporte";
        }
    }
}
