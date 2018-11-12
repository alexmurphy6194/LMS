# The Tech Academy - Learning Management System

## Introduction
   Spent two weeks building out a new feature for The Tech Academy's development team to implement in the school’s learning management system. The project entailed creating a user interface for students and admin in the Job Placement course. The interface allows students to log information, see aggregate statistics for the student body and allows the Job Placement Director to track student progress, reach out to students, and post information to a bulletin. During my time on the project, I worked on a variety of different features of the website involving both frontend and backend tasks. It was my first time taking on a legacy code base, which taught me how to learn new systems quickly. I used my skills in C#, HTML/CSS, Javascript and SQL to move the code-base forward with cleaner more performant solutions.

Below are a selection of stories I worked on, along with code snippets. 

## Back End Stories
* [Notify Director if Hired or Graduated](#Notify-Director-if-Hired-or-Graduated)
* [Create Admin Role and Authorize Access to certain pages](#Create-Admin-Role-and-Authorize-Access-to-certain-pages)
* [Created an Export Email button for New Students in Job Placement](#Created-an-Export-Email-button-for-New-Students-in-Job-Placement)
* [Fixed null ApplicationID bug](#Fixed-null-ApplicationID-bug)
* [Rendered _OutsideNetworkings Partial View on Networking View](#Rendered-_OutsideNetworkings-Partial-View-on-Networking-View)
* [Added Seed Data for the OutsideNetworkings table](#Added-Seed-Data-for-the-OutsideNetworkings-table)

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
   
### Create Admin Role and Authorize Access to certain pages
   Using built in Identity/Entity framework functionality, added an Admin role to the site, and altered certain aspects of the site to only be available to users in that role.  An example is the JobPlacementDashboard drop down menu, where the links to all the 'Job Placement Director only' views are displayed. 
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
### Rendered _OutsideNetworkings Partial View on Networking View
	The outside Networkings view displays contact info of people outside the Tech Academy, 
	that Students in Job Placement will have the ability to contact. We wanted to combine this
	information in the same view as the original Networking view, which uses a ViewModel to get the
	relevant data. However, the outsideNetworkings view needed only its model, so this called for the use
	of a partial view to keep the data access simplified and relevant.
```
<br />
@Html.Action("_OutsideNetworking","JPOutsideNetworkings",null)
<br />
```
 public ActionResult _OutsideNetworking()
        {
            List<JPOutsideNetworking> partialViewList = new List<JPOutsideNetworking>();
            partialViewList = db.JPOutsideNetworkings.ToList();
            return PartialView("_OutsideNetworking", partialViewList);
        }
```
### Added Seed Data for the OutsideNetworkings table
	To test the functionality of the controller and view, I created test seed data to populate
	the OutsideNetworkings table upon start of the application. I pushed a migration to update the solution
	and allow the team to access the information.
```
var outsideNetworkings = new List<JPOutsideNetworking>
            {
                new JPOutsideNetworking { OutsideNetworkingID = 1, Name = "Travis Scott", Position = "Senior Developer", Company = "Google", CompanyURL = "www.google.com", LinkedIn = "www.linkedin.com/TravisScott1", Location = "Portland, OR", Stack= "Front End", Contact = "travisscott@email.com", },
                new JPOutsideNetworking { OutsideNetworkingID = 2, Name = "Cynthia Cox", Position = "IT Manager", Company = "Amazon", CompanyURL = "www.amazon.com", LinkedIn = "www.linkedin.com/CynthiaCox1", Location = "Los Angeles, CA", Stack= "Back End", Contact = "cynthiacox@email.com", },
                new JPOutsideNetworking { OutsideNetworkingID = 3, Name = "Ed Sheeran", Position = "Code Monkey", Company = "Nickelodeon", CompanyURL = "www.nick.com", LinkedIn = "www.linkedin.com/TheRealEdSheeran", Location = "Seattle, WA", Stack= "Back End", Contact = "edsheeran@email.com", }

            };
            outsideNetworkings.ForEach(s => context.JPOutsideNetworkings.AddOrUpdate(p => p.OutsideNetworkingID, s));
            context.SaveChanges();
```

## Front End Stories
* [Remove Duplicates from MeetupAPI](#Remove-Duplicates-from-MeetupAPI)
* [Updates notification for the Admin display](#Updates-notification-for-the-Admin-display)
* [Page Title beneath NavBar bug](#Page-Title-beneath-NavBar-bug)
* [Add link, image, and local image path buttons to text editor](#Add-link,-image,-and-local-image-path-buttons-to-text-editor)
* [Adjusted styling of Snapshot View](#Adjusted-styling-of-Snapshot-View)


### Remove Duplicates from MeetupAPI
    One of the features available to Job Placement students, was a page that displayed a list of current and upcoming
	Meetup Group Events. In the interest of efficiency and readability we opted to remove duplicate events,
	i.e. events that occurred regularly and were displayed more than once on the list. I used a filter call on the array
	containing the meetup event objects, found the events where the names were duplicates, and created a new
	array that contained only one instance of the event.
```
function filterLatestMeetUpWithSameName(arr) {
            
            var newArr = arr.filter(function (obj, index, self) {
                return index === self.findIndex(function (t) {
                    return t['name'] === obj['name']
                });
            });
            return newArr;
```
### Updates notification for the Admin display
	I created a notification visual that would be displayed only to the Job Placement director
notifying them of the number of students that got job offers or submitted the required 35 applications
and are ready to Graduate. This was tricky because where the notification is being displayed in the Nav Bar
is being rendered in the Layout view. Every solution that involved passing data model to the Layout that I found
was overly complicated and messy. I managed a clever work-around by adding a method in the Startup.cs, 
which gets called every time the application is started. This meant I could get the data automatically 
at the beginning of the site load, call the method from the Layout view which would return the number of students 
in the Notifications table, and then display it however I needed. It required combination of 
backend database access and logic, getting that info to the correct controller and calling it, 
and then displaying it back on the front end. It encompassed all aspects of MVC and took a bit of trickery
to be able to get it to work. The idea to get the data in startup was kind of on a whim,
so I was super pleased that it worked so well! Also, learning to call a method from the View 
and assign it to varibles, which were then displayed on the view, was a new skill I learned in the process.
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
### Page Title beneath NavBar bug
    Fixed an issue with the layout where the title of each page would fall behind the navbar.
	Found an interesting solution where I wrapped the RenderBody action in a div, and gave it a padding-top of 80.
	By doing this, every view would load the Layout and then have proper spacing from the top before loading that
	specific view's content.
	
```
 </header>
    <div class="pt-80">
    @RenderBody()
    </div>
    <footer class="footer">
```
### Add link, image, and local image path buttons to text editor
	On the Create Bulletin view there is a text editor for the director of Job Placement create 
	and post bulletins for the students. I added three buttons, two that would prompt for a URL/File path
	and then wrap the URL in appropriate hyperlink tag, and one that would open a file explorer and 
	let the user pick the file, and again wrap it in correct markup to be displayed. 
```
	//Custom image upload button
        document.getElementById('addImage').onclick = function () {
            document.getElementById('my_file').click();
        };

        document.getElementById("my_file").addEventListener('change', function () {
            if (this.value != "" && this.value != null) {
                var $text = $("#bulletinBody");
                $text.val($text.val() + ("<img src=" + '"' + this.value + '"' + '>'));
            }
        });
```
### Adjusted styling of Snapshot View
	The tables on the Snapshot view were not aligned horizontally so I added custom css
	using margins and 'table-layout: fixed' to get them displaying properly. The 'Export Student Email Addresses'
	button was also too close to the footer, so in the  Razr HTML.actionLink I gave it a custom ID and added margin-bottom.
```
/*Re-styling Snapshot Viewmodel*/
#tablestuff{
   table-layout: fixed;
}
#tablestuff2{
    table-layout: fixed;
}
#spaceFoot{
    margin-bottom:20px;
}
```
```
<table class="table margin10" id="tablestuff">
    <tr>
        <th></th>
        <th class="col-md-4">
        <td class="col-md-4">
            @Html.LabelFor(model => model.TotalWeeklyApplications_Seattle)
        </td>
        <td>
        <td class="ml-5">
            @Html.DisplayFor(model => model.TotalWeeklyApplications_Seattle)
        </td>
        <td>
    }
</table>
```
### Other Skills Learned
	In addition to these specific technologies, the project overal was a great foundation for working in a team environment.
There were numerous times where I or other group members would run into a wall, and we would talk the code through and 
work out solutions together. This environment was also an opportunity to practice effective communication and conflict
resolution. I learned how to talk about bugs in the other developer's code, listen to critique on my own, and divy up tasks on stories 
where we would be working on the same code. This experience I know will benefit myself and any team I work with going forward.
The project also provided an experience on understanding and getting up to speed quickly on a pre-existing code base.
Being able to decipher others' code and follow their logic expanded my understanding of these technologies,
while simultaneously reinforcing my previous knowledge.