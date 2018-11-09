# The Tech Academy - Learning Management System

## Introduction
   Spent two weeks building out a new feature for The Tech Academy's development team to implement in the school’s learning management system. The project entailed creating a user interface for students and admin in the Job Placement course. The interface allows students to log information, see aggregate statistics for the student body and allows the Job Placement Director to track student progress, reach out to students, and post information to a bulletin. During my time on the project, I worked on a variety of different features of the website involving both frontend and backend tasks. It was my first time taking on a legacy code base, which taught me how to learn new systems quickly. I used my skills in C#, HTML/CSS, Javascript and SQL to move the code-base forward with cleaner more performant solutions.

Below are descriptions of the stories I worked on along with code snippets. 

## Back End Stories
* [Notify Director if Hired or Graduated](#Notify-Director-if-Hired-or-Graduated)
* [Create Admin Role and Authorize Access to certain pages]
* [Created an Export Email button for New Students in Job Placement]
* [Fixed null ApplicationID bug]

### Notify Director if Hired or Graduated
   The Students in the Job Placement Program submit a record of any time they submit a job application, and eventually if they got a job offer. I added functionality to the JPHires controller, where they create the record of the job offer, so that any time a user logged one, an entry of that student would be submitted to the JPNotifications table, which would ultimately go on to alert the head of Job Placement. 
     
```
// Grabs the active users ID and uses it to identify the users row in JPStudents table to edit JPGraduated and JPHired from false to true.
                string userID = User.Identity.GetUserId();
                JPStudent jpStudent = db.JPStudents.Where(x => x.ApplicationUserId == userID).FirstOrDefault();
                jpStudent.JPGraduated = true;
                jpStudent.JPHired = true;
                
//Create JPNotification record 

                JPNotification jPNotification = new JPNotification();
                jPNotification.JPStudent = jpStudent;
                jPNotification.Hire = true;
                jPNotification.NotificationDate = DateTime.Now;


                db.JPNotifications.Add(jPNotification);
                db.SaveChanges();
```
   Also added functionality to the application create page, where the students record all the job applications they submit. I added a check to the Applications database table for how many the student has submitted, and if it is the students' 35th application, it marks them as “ready to graduate” by changing the student object's boolean 'Graduate' property to true. The controller then gets the student's object and submits it to the JPNotifications table for the Job Placement Director to be notified. 
```
var applications = from s in db.JPApplications.Where(x => x.ApplicationUserId == userID)
                                   select s;
                int appCount = applications.Count();
                if (appCount==34)
                {
                    JPNotification jPNotification = new JPNotification();
                    jPNotification.Graduate = true;
                    jPNotification.NotificationDate = DateTime.Now;
                    jPNotification.JPStudent = jpStudent;
                    db.JPNotifications.Add(jPNotification);
                    
                }
                db.SaveChanges();
                return RedirectToAction("StudentIndex");
```
   I then created a notification visual that would be displayed only to the Job Placement director (How I created and authorized the Admin role was seperate story), notifying them of the number of students that got job offers or submitted the required 35 applications and are ready to Graduate. This was tricky because where the notification is being displayed in the Nav Bar is being rendered in the Layout view. Every solution that involved passing data model to the Layout that I found was overly complicated and messy. I managed a clever work-around by adding a method in the Startup.cs, which gets called every time the application is started. This meant I could get the data automatically at the beginning of the site load, call the method from the Layout view which would return the number of students in the Notifications table, and then display it however I needed. It required combination of backend database access and logic, getting that info to the correct controller and calling it, and then displaying it back on the front end. It encompassed all aspects of MVC and took a bit of trickery to be able to get it to work. The idea to get the data in startup was kind of on a whim, so I was super pleased that it worked so well! Also, learning to call a method from the View and assign it to varibles, which were then displayed on the view, was a new skill I learned in the process.
```
public partial class Startup
    {
        public static int getNotifications()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var notifications = db.JPNotifications.Count();
            
            return notifications;
        }
```
```
@if (User.IsInRole("Admin"))
                                {
                                    var notes = Startup.getNotifications();     
                                    <li class="job-placement-dropdown-item">Job Placement Dashboard</li>
                                    <li class="JPDropDown">@Html.ActionLink("Students", "Index", "JPStudentRundown")</li>
                                    <li class="JPDropDown">@Html.ActionLink("Applications", "Index", "JPApplications")</li>
                                    <li class="JPDropDown">@Html.ActionLink("Hires", "Index", "JPHires")</li>
                                    <li class="JPDropDown">@Html.ActionLink("Bulletin", "Create", "JPBulletins")</li>
                                    <li class="JPDropDown">@Html.ActionLink("Snapshot", "Snapshot", "SnapshotViewModel")</li>
                                    <li class="JPDropDown">@Html.ActionLink("Updates (" +notes +")", "Update", "JPNotifications")</li>
                                    
                                }
```
### Create Admin Role and Authorize Access to certain pages
   Using built in Identity/Entity framework functionality, added an Admin role to the site, and altered certain aspects of the site to only be available to users in that role.  An example is the JobPlacementDashboard drop down menu, where the links to all the Admin views are displayed. This is also where the Notification from earlier would be displayed. Only someone within an Admin role can even see that section of the menu, to get to the Instructor/Director pages.
```
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
                }
```
```
@if (User.IsInRole("Admin"))
{
<p>
    @Html.ActionLink("Create New", "Create")
</p>
}
```
### Created an Export Email button for New Students in Job Placement
   The director of Job Placement has access to a page that displays New Students to the program as well as those hired in the past week. I added the button to export the emails of the New Students so the director can reach out to them. This required checking when the student entered the Job Placement program, and if it was within 7 days of datetime.Now, pull their information from the database, append that info to a stringBuilder object, and then download it as an excel spreadsheet. In order to reduce redundancy, I combined the methods for the two exportCSV buttons on the page. I used a nullable bool that is passed from the View based on which button is clicked as a conditional for which email list needed to be created.
```
// CSV Export Option
        
        public ActionResult ExportCSV(bool? forHires)
        {
            var beginDate = DateTime.Now.AddDays(-7);
            string FileName = "";
            StringBuilder sb = new StringBuilder();
            var emailList = Enumerable.Empty<Object>().AsQueryable().Select(r => new { JPEmail = "", ApplicationUserId = "" });
            if (forHires == true)
            {
                var weeklyHires = from student in db.JPStudents
                                  join hire in db.JPHires
                                  on student.ApplicationUserId
                                  equals hire.ApplicationUserId
                                  where (hire.JPHireDate >= beginDate && hire.JPHireDate <= DateTime.Now)
                                  select new { student.JPEmail, student.ApplicationUserId};
                emailList = weeklyHires;
                //creating CSV
                sb.Append("Weekly Hired Student Report");
                
                FileName = "WeeklyHiredEmailList.csv";
                
            }
            else
            {
            sb.Append("New Student Report");
                
                FileName = "NewStudentEmailList.csv";
                
                var newStudentEmails = from student in db.JPStudents
                                       where (student.JPStartDate >= beginDate && student.JPStartDate <= DateTime.Now
                                       && student.JPHired == false && student.JPGraduated == false)
                                       select new { student.JPEmail, student.ApplicationUserId };
                emailList = newStudentEmails;
                foreach (var email in emailList)
            {
                sb.AppendLine();
                sb.Append(email.JPEmail.ToString());
                sb.Append(", ");
                Console.WriteLine(sb);
            }
            string FilePath = Server.MapPath("/App_Data/");
            System.IO.File.WriteAllText(FilePath + FileName, sb.ToString());
            System.Web.HttpResponse response = System.Web.HttpContext.Current.Response;
            response.ClearContent();
            response.Clear();
            response.ContentType = "text/csv";
            response.AddHeader("Content-Disposition", "attachment; filename=" + FileName + ";");
            response.TransmitFile(FilePath + FileName);
            response.Flush();
            // Deletes the file on server
            System.IO.File.Delete(FilePath + FileName);
            response.End();
            db.SaveChanges();
            return RedirectToAction("Snapshot");
        }
```
### Fixed null ApplicationID bug
   Originally the save changes button on the Edit view of the JPChecklist view redirected student back to Index page, but now Index is Admin only. Instead the student had to be redirected to the StudentIndex view. However because the student's ApplicationID was never actually used on the Edit view, it would come back as null to the Controller even though it was a part of the  view's model. When the student was redirected back to the StudentIndex page it no longer had the user's ID and no longer displayed their checklist or allowed for changes. In order to correct this I had to use a Html.HiddenFor(model => model.ApplicationUserId) on the Edit view so that it would bind to the JPChecklist edit method in the Controller, and then render the correct view. It now operates functionally, allow student to save changes and being redirected to proper page with changes showing. 
```
<h4>JPChecklist</h4>
    <hr />
    @Html.ValidationSummary(true, "", new { @class = "text-danger" })
    @Html.HiddenFor(model => model.JPChecklistid)
    @Html.HiddenFor(model => model.ApplicationUserid)
```

