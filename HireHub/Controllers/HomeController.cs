using Microsoft.AspNetCore.Mvc;

namespace HireHub.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
           
            var job = new HireHub.Models.Entities.Job

            {
                Title = "Junior .NET Developer",
                Description = "Test job",
                JobType = HireHub.Models.Enums.JobType.FullTime,
                ExperienceLevel = HireHub.Models.Enums.ExperienceLevel.Fresher,
                Company = new HireHub.Models.Entities.Company { Name = "HireHub Inc." }
            };

            ViewBag.TestTitle = job.Title;
            ViewBag.TestCompany = job.Company.Name;

            return View();
        }
    }
}