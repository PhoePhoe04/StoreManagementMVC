using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Store.Shared;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin")]
    [Route("Admin/Home")]
    [Route("Admin/Home/Index")]
    public class HomeController : Controller
    {
        private readonly DashboardService _dashboardService;

        public HomeController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public IActionResult Index()
        {
            // Lấy số liệu thống kê
            var viewModel = _dashboardService.GetDashboardStats();
            return View(viewModel);
        }
    }
}
