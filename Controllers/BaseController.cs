using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Small.Data;

public class BaseController : Controller
{
    protected readonly ApplicationDbContext _context;

    public BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.Categories = _context.Categories.ToList();
        base.OnActionExecuting(context);
    }
}
