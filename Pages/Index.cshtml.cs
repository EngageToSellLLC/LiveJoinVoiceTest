using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveJoinVoiceTest.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiveJoinVoiceTest.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            CallId = EventsController.GetNextId();
        }

        public int CallId { get; set; }
    }
}
