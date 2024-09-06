using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using twitter_clone.Services;

namespace Application.Services
{
    public class JwtAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var token = request.Cookies["jwt"];

            if (token != null)
            {
                var userId = Authentication.ValidateToken(token);
                if (userId == null)
                {
                    filterContext.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                }
            }
            else
            {
                filterContext.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
