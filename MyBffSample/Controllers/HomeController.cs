using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyBffSample.Controllers
{
    public class HomeController : Controller
    {
        // GET: HomeController
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }
    }
}
