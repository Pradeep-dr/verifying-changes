namespace MarsLite.Web.Modules
{
    /// <summary>
    /// Root route. Inherits from <c>MarsModule</c>, so authentication is enforced.
    /// </summary>
    public class HomeModule : MarsModule
    {
        public HomeModule() : base("/")
        {
            Get[""] = _ => View["Home/Index"];
        }
    }
}
