using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    /// <summary>
    /// Get available income categories
    /// </summary>
    [HttpGet("income")]
    public ActionResult<object> GetIncomeCategories()
    {
        return Ok(new
        {
            categories = IncomeCategories.All,
            displayNames = IncomeCategories.DisplayNames
        });
    }

    /// <summary>
    /// Get available expense categories
    /// </summary>
    [HttpGet("expense")]
    public ActionResult<object> GetExpenseCategories()
    {
        return Ok(new
        {
            categories = ExpenseCategories.All,
            displayNames = ExpenseCategories.DisplayNames,
            essentialByDefault = ExpenseCategories.EssentialByDefault
        });
    }
}
