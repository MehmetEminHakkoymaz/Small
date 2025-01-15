using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Small.Data;
using System.Linq;

public class CategoriesFilter : ActionFilterAttribute
{
    private readonly ApplicationDbContext _context;

    public CategoriesFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.Controller as Controller;
        if (controller != null)
        {
            controller.ViewBag.Categories = _context.Categories.ToList();
        }
        base.OnActionExecuting(context);
    }
}
