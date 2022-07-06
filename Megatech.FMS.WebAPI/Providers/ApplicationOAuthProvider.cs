using FMS.Data;
using Megatech.FMS.WebAPI.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Megatech.FMS.WebAPI.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();
            //var roleManager = context.OwinContext.GetUserManager<ApplicationRoleManager>();

            ApplicationUser user = await userManager.FindAsync(context.UserName, context.Password);

            if (user == null)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            //if (!user.IsEnabled)
            //{
            //    context.SetError("suspended_user", "The user is suspended.");
            //    return;
            //}



            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);


            AuthenticationProperties properties = CreateProperties(user);

            if (properties.Dictionary.ContainsKey("client_id"))
            {
                properties.ExpiresUtc = DateTime.UtcNow.AddMinutes(15);
                context.Options.AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(15);
            }
            else
                context.Options.AccessTokenExpireTimeSpan = TimeSpan.FromDays(2);

            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);

            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }


            return Task.FromResult<object>(null);
        }

        public override Task ValidateTokenRequest(OAuthValidateTokenRequestContext context)
        {
            return base.ValidateTokenRequest(context);
        }
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {

            if (context.Parameters["grant_type"] == null)
                ;

            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }
            //context.Validated();
            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(ApplicationUser user)
        {

            if (user.UserName.ToUpper() == "TAPETCO")
                return new AuthenticationProperties(new Dictionary<string, string> { { "client_id", "FHS" } });
            using (DataContext db = new DataContext())
            {

                var dbUser = db.Users.Include(u => u.Airport).FirstOrDefault(u => u.Id == user.UserId);
                dbUser.LastLogin = DateTime.Now;
                db.SaveChanges();

                IDictionary<string, string> data = new Dictionary<string, string>
            {

                { "userName", user.UserName },
                { "userId", user.UserId.ToString()},
                { "permission", ((int)dbUser.Permission).ToString()},
                { "airport", dbUser.Airport==null?"":dbUser.Airport.Code },
                {"address", dbUser.Airport==null?"":dbUser.Airport.Address },
                {"taxcode", dbUser.Airport==null?"":dbUser.Airport.TaxCode??"" },
                {"invoiceName", dbUser.Airport==null?"":dbUser.Airport.InvoiceName??"" }

                };
                return new AuthenticationProperties(data);
            }

        }
    }
}