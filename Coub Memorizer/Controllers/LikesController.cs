using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Coub_Memorizer.Controllers
{
    [Route("[controller]")]
    public class LikesController: ControllerBase
    {
        public IActionResult Index()
        {
            return this.Content("Hello ASP.NET MVC 6.");
        }

        /*public class WorkController : Controller
        {
            public DateTime Index()
            {
                return DateTime.Now;
            }
        }*/

    }

    [Route("[controller]")]
    public class Controller : ControllerBase
    {
        public IActionResult Index()
        {
            return this.Content("Main Page");
        }
    }
}
