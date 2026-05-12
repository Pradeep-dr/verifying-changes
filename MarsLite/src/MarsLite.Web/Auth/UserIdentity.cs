using System.Collections.Generic;
using Nancy.Security;

namespace MarsLite.Web.Auth
{
    /// <summary>
    /// Authenticated staff principal — mirrors the <c>UserIdentity</c> in real Mars.
    /// Nancy modules cast <c>Context.CurrentUser</c> to this type to read claims.
    /// </summary>
    public class UserIdentity : IUserIdentity
    {
        public string UserName    { get; set; }
        public string DisplayName { get; set; }
        public string Role        { get; set; }
        public int    UserId      { get; set; }

        public IEnumerable<string> Claims { get; set; }

        public bool HasClaim(string claim)
        {
            if (Claims == null) return false;
            foreach (var c in Claims)
                if (c == claim) return true;
            return false;
        }
    }
}
