using myfoodapp.Hub.Business;
using myfoodapp.Hub.Models;
using myfoodapp.Hub.Models.OpenData;
using myfoodapp.Hub.Services.OpenData;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using WebApi.OutputCache.V2;

namespace myfoodapp.Hub.Controllers.Api
{
    public class OpenDataController : ApiController
    {
        [HttpGet]
        [Route("opendata/productionunits/")]
        [CacheOutput(ClientTimeSpan = 43200, ServerTimeSpan = 100)]
        public List<OpenProductionUnitViewModel> GetAll()
        {
            var db = new ApplicationDbContext();
            var openDataService = new OpenDataService(db);

            var all = openDataService.GetAll();

            return all.ToList();
        }

        [HttpGet]
        [Route("opendata/productionunits/{Id:int}")]
        [CacheOutput(ClientTimeSpan = 43200, ServerTimeSpan = 100)]
        public List<OpenProductionUnitViewModel> GetById(int Id)
        {
            var db = new ApplicationDbContext();
            var openDataService = new OpenDataService(db);

            var one = openDataService.One(Id);

            return one.ToList();
        }

        [HttpGet]
        [Route("opendata/productionunits/{Id:int}/measures")]
        [CacheOutput(ClientTimeSpan = 43200, ServerTimeSpan = 100)]
        public List<OpenMeasureViewModel> GetMeasures(int Id)
        {
            var db = new ApplicationDbContext();
            var openDataService = new OpenDataService(db);

            var measures = openDataService.GetMeasures(Id);

            return measures.ToList();
        }

        [HttpGet]
        [Route("opendata/productionunits/stats")]
        [CacheOutput(ClientTimeSpan = 43200, ServerTimeSpan = 100)]
        public OpenProductionUnitsStatsViewModel GetProductionUnitStats()
        {
            var db = new ApplicationDbContext();
            var stats = PerformanceManager.GetNetworkStatistics(db);

            return stats;
        }
    }
}