using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,staff")]
    public class AdminBaseController : Controller
    {
    }
}
