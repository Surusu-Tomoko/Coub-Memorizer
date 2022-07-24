using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Coub_Memorizer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            string text = "";
            Program.MainForm.Invoke(new Action(() =>
            {
                text = "test 002";
            }));
            return text;
        }

        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            Program.MainForm.Invoke(new Action(() =>
            {
                Program.MainForm.textlog(id.ToString());
                //Program.MainForm.textBox1.Text = id;
            }));
            return Ok();
        }
    }
}
