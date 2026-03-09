using Microsoft.AspNetCore.Mvc;

namespace Student_Portal2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
