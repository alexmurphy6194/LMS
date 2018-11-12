using JobPlacementDashboard.Migrations;
using JobPlacementDashboard.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using System.Linq;
using ApplicationDbContext = JobPlacementDashboard.Models.ApplicationDbContext;

[assembly: OwinStartupAttribute(typeof(JobPlacementDashboard.Startup))]
namespace JobPlacementDashboard
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            createRolesandUsers();
        }

        public static int getNotifications()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var notifications = db.JPNotifications.Count();
            
            return notifications;
        }

        // In this method we will create default User roles and Admin user for login    
        private void createRolesandUsers()
        {
            ApplicationDbContext context = new ApplicationDbContext();

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));


            // In Startup i am creating first Admin Role and creating a default Admin User  
            if (!roleManager.RoleExists("Admin"))
            {

                // first we create Admin role    
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Admin";
                roleManager.Create(role);

                //Here we create a Admin super user who will maintain the website                   

                var user = new ApplicationUser();
                user.UserName = "admin@web.com";
                user.Email = "admin@web.com";

                string userPWD = "Pass1234!";

                var chkUser = UserManager.Create(user, userPWD);

                //Add default User to Role Admin    
                if (chkUser.Succeeded)
                {
                    var result1 = UserManager.AddToRole(user.Id, "Admin");

                }
            }


            // creating Creating Manager role     
            if (!roleManager.RoleExists("Manager"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Manager";
                roleManager.Create(role);

            }           
        }
    }
}