using Microsoft.EntityFrameworkCore;
using myfoodapp.Business;
using myfoodapp.Model;
using myfoodapp.WebServer;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace myfoodapp.Headless
{
    public sealed class StartupTask : IBackgroundTask
    {
        private LogModel logModel = LogModel.GetInstance;
        //private HTTPServer webServer;
        private WebServerEngine webServer;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            logModel.AppendLog(Log.CreateLog("Local Webserver starting...", Log.LogType.System));

            webServer = new WebServerEngine();
            webServer.Run().Wait();

            logModel.AppendLog(Log.CreateLog("Local Webserver initialized", Log.LogType.System));

            logModel.AppendLog(Log.CreateLog("Database starting...", Log.LogType.System));
            using (var db = new LocalDataContext())
            {
                db.Database.Migrate();
                LocalDataContextExtension.EnsureSeedData(db);
            }
            logModel.AppendLog(Log.CreateLog("Database initialized", Log.LogType.System));

            logModel.AppendLog(Log.CreateLog("User Settings Init", Log.LogType.System));

            var taskUserFile = Task.Run(async () => { await UserSettingsModel.GetInstance.InitFileFolder(); });
            taskUserFile.Wait();

            logModel.AppendLog(Log.CreateLog("Background Service Init", Log.LogType.System));

            var mesureBackgroundTask = MeasureBackgroundTask.GetInstance;
            mesureBackgroundTask.Run();

            while (true)
            {

            };
        }
    }
}
