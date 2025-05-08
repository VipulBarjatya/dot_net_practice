using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Excel;
using Newtonsoft.Json;
using QlikReport.Interface.Compensation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using ReportingPortalCore;
using Microsoft.Graph.Models.CallRecords;
using Microsoft.AspNetCore.Http.Extensions;

namespace QlikReport.Controllers
{
    //[MyExceptionHandler]
    //[DateSelectionFilterAttribute]
    public class ReportsController : BaseController
    {
        private readonly ICommonClass objCommon;
        private readonly IBillingCollectionReport _billingCollectionReport;
        private readonly IStaffCompensationPlanning _staffCompensationPlanning;
        private readonly IStaffCompensationPlanningDemo _staffCompensationPlanningDemo;
        private readonly ITimekeeperCompensationPlanning _timekeeperCompensationPlanning;
        private readonly INEPCompensationPlanning _nEPCompensationPlanning;
        private readonly ICounselCompensationPlanning _counselCompensationPlanning;
        private readonly IParalegalCompensationPlanning _paralegalCompensationPlanning;
        private readonly IPartnerMasterSummary _partnerMasterSummary;
        private readonly IService _service;
        private readonly ICompensationClassYear CompClass;
        private readonly IEdiscovery _Ediscovery;
        private readonly IChief _Chief;
        private readonly ICompensationSummary _compensationSummary;
        private readonly IDirectorCompensationPlanning _directorCompensationPlanning;
        private readonly IEquityPartner _equityPartner;
        private readonly IParticipationPercentage _participationPercentage;
        private readonly ICompensationCommon _compensationCommon;
        private readonly ICompensationPercent _compensationPercent;
        private readonly ICompValidation _CompValidation;
        private readonly IDBTables objTable;
        private readonly IHttpContextAccessor _httpContextAccessor;

        string sWebsiteUrl = "";

        public ReportsController(IService service, ICommonClass commonClass, IBillingCollectionReport billingCollectionReport
            , IStaffCompensationPlanning staffCompensationPlanning, IStaffCompensationPlanningDemo staffCompensationPlanningDemo, ITimekeeperCompensationPlanning timekeeperCompensationPlanning
            , INEPCompensationPlanning nEPCompensationPlanning, ICounselCompensationPlanning counselCompensationPlanning, IParalegalCompensationPlanning paralegalCompensationPlanning, IPartnerMasterSummary partnerMasterSummary,
            ICompensationClassYear _CompClass, IEdiscovery Ediscovery, IChief Chief, ICompensationSummary compensationSummary, IDirectorCompensationPlanning directorCompensationPlanning, IEquityPartner equityPartner,
           IParticipationPercentage ParticipationPercentage, ICompensationCommon compensationCommon, ICompValidation compValidation, ICompensationPercent compensationPercent,
           IDBTables objTable, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : base(service, commonClass, configuration)

        {
            this.objCommon = commonClass;
            this._billingCollectionReport = billingCollectionReport;
            this._staffCompensationPlanning = staffCompensationPlanning;
            this._staffCompensationPlanningDemo = staffCompensationPlanningDemo;
            this._timekeeperCompensationPlanning = timekeeperCompensationPlanning;
            this._nEPCompensationPlanning = nEPCompensationPlanning;
            this._service = service;
            this._counselCompensationPlanning = counselCompensationPlanning;
            this._paralegalCompensationPlanning = paralegalCompensationPlanning;
            this._partnerMasterSummary = partnerMasterSummary;
            this.CompClass = _CompClass;
            this._Ediscovery = Ediscovery;
            this._Chief = Chief;
            this._compensationSummary = compensationSummary;
            this._directorCompensationPlanning = directorCompensationPlanning;
            this._equityPartner = equityPartner;
            this._participationPercentage = ParticipationPercentage;
            this._compensationCommon = compensationCommon;
            this._compensationPercent = compensationPercent;
            this._CompValidation = compValidation;
            this.objTable = objTable;
            _httpContextAccessor = httpContextAccessor;
            sWebsiteUrl = configuration["WebsiteURL"];
        }

        #region Pages
        [Route("billing-collection-report")]
        public ActionResult SplitReports(int isSplit = 0, int ReportID = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            UserVariables userVariables = ViewBag.UserVariables as UserVariables;
            session.SetString("iMenuID", userVariables.iMenuID.ToString());

            var jsonData = session.GetString("PageWiseDateSelection");
            List<PageDateSelection> lstPageDateSelection = string.IsNullOrEmpty(jsonData) ? new List<PageDateSelection>() : System.Text.Json.JsonSerializer.Deserialize<List<PageDateSelection>>(jsonData);

            DateTime endDate = DateTime.Now, startDate = new DateTime(DateTime.Now.Year, 1, 1);
            if (lstPageDateSelection != null && lstPageDateSelection.Count > 0)
            {
                if (lstPageDateSelection.Where(a => a.Url.ToLower().Contains("billing-collection-report")).ToList() != null
                    && lstPageDateSelection.Where(a => a.Url.ToLower().Contains("billing-collection-report")).ToList().Count > 0)
                {
                    PageDateSelection pageDateSelection = lstPageDateSelection.Where(S => S.Url.ToLower() == _httpContextAccessor.HttpContext.Request.GetDisplayUrl().ToLower()).FirstOrDefault();
                    if (pageDateSelection != null)
                    {
                        objCommon.GetPageSetDate(sWebsiteUrl + "billing-collection-report", pageDateSelection.StartDate, pageDateSelection.EndDate);
                    }
                }
                else
                {
                    objCommon.GetPageSetDate(sWebsiteUrl + "billing-collection-report", startDate.ToString("yyyy-MMM"), endDate.ToString("yyyy-MMM"));
                }
            }
            else
            {
                objCommon.GetPageSetDate(sWebsiteUrl + "billing-collection-report", startDate.ToString("yyyy-MMM"), endDate.ToString("yyyy-MMM"));
            }
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_billingCollectionReport.GetSplitReport(isSplit, ReportID, userVariables.suserID, userVariables.sUserDirectory, objTable.GetUserRoleDetails(userVariables.suserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0));
        }

        [Route("report/compensation-planning")]
        public ActionResult CompensationPlanningReports(int Year = 0, bool isFrozeData = false)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);

            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            ViewBag.isFrozeData = isFrozeData;
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_staffCompensationPlanning.GetCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, true, Year));
        }

        [Route("report/associate-compensation-planning")]
        public ActionResult TimekeeperCompensationPlanningReports(int Year = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);

            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];

            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            TimekeeperCompensationPlanningModel objTCP = _timekeeperCompensationPlanning.GetTimekeeperCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year);
            return View(objTCP);
        }

        [Route("report/nep-compensation-planning")]
        public ActionResult NEPCompensationPlanningReports(int Year = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_nEPCompensationPlanning.GetNEPCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year));
        }

        [Route("report/counsel-compensation-planning")]
        public ActionResult CounselCompensationPlanningReports(int Year = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            CounselCompensationPlanningModel objCCP = _counselCompensationPlanning.GetCounselCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year);
            return View(objCCP);
        }

        [Route("report/paralegal")]
        public ActionResult ParalegalCompensationPlanningReports(int Year = 0, bool isFrozeData = false)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            ViewBag.isFrozeData = isFrozeData;
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            ParalegalCompensationPlanningModel objParalegal = _paralegalCompensationPlanning.GetParalegalCompensationPlanning(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year);
            return View(objParalegal);
        }

        [Route("report/ediscovery")]
        public ActionResult Ediscovery(int Year = 0, bool isFrozeData = false)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            ViewBag.isFrozeData = isFrozeData;
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            EdiscoveryModel objEdis = _Ediscovery.GetEdiscoveryDetails(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year);
            return View(objEdis);
        }

        [Route("report/chief")]
        public ActionResult Chief(int Year = 0, bool isFrozeData = false)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            ViewBag.isFrozeData = isFrozeData;
            var user = session.GetString("user").Split('/')[1];

            ViewBag.sRoleID = 1;

            //objCommon.ClearManagerFilter(userAppID);

            //return View(_Chief.GetChiefDetails(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), iRoleID, iMenuID, TempData.Peek("ActionList") as DataTable, isDateSetManual, 0, userAppID, ViewBag.UserRoleID, Year));
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            ChiefModel chief = _Chief.GetChiefDetails(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "", ViewBag.UserRoleID, Year);
            return View(chief);
        }

        //change by Shashwat on 10/11 remove isFreezedTab parameter to check filter issue
        [Route("report/director-compensation-planning")]
        public ActionResult DirectorCompensationPlanning(int Year = 0, bool isFrozeData = false)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);

            var user = session.GetString("user").Split('/')[1];
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            ViewBag.isFrozeData = isFrozeData;
            //objCommon.ClearManagerFilter(userAppID);
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_directorCompensationPlanning.GetDirectorCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "", ViewBag.UserRoleID, true, true, Year));
        }

        [Route("report/equity-partner")]
        public ActionResult EquityPartnerCompensationPlanning(int Year = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            ViewBag.isFreez = TempData["isfreeze"] == null ? false : TempData["isfreeze"];
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_equityPartner.GetEquityPartnerCompensationPlanning(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, ViewBag.UserRoleID, true, Year));
        }

        [Route("report/partner-master-summary")]
        public ActionResult PartnerMasterSummary()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            UserVariables userVariables = ViewBag.UserVariables as UserVariables;
            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            ViewBag.SessionTime = objCommon.GetSessionTimeOut();
            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1).AddMonths(1).AddDays(-1),
                startDate = new DateTime(DateTime.Now.Year, 1, 1).AddYears(-2);

            var jsonData = session.GetString("PageWiseDateSelection");
            List<PageDateSelection> lstPageDateSelection = System.Text.Json.JsonSerializer.Deserialize<List<PageDateSelection>>(jsonData);
            string url = objCommon.GetCurrentURLOfPage();
            if (lstPageDateSelection != null && lstPageDateSelection.Count > 0)
            {
                if (lstPageDateSelection.Where(a => a.Url.ToLower() == url.ToLower()).ToList() != null && lstPageDateSelection.Where(a => a.Url.ToLower() == url.ToLower()).ToList().Count > 0)
                {
                    PageDateSelection pageDateSelection = lstPageDateSelection.Where(S => S.Url.ToLower() == url.ToLower()).FirstOrDefault();
                    if (pageDateSelection != null)
                    {
                        objCommon.GetPageSetDate(url, pageDateSelection.StartDate, pageDateSelection.EndDate);
                    }
                }
                else
                {
                    objCommon.GetPageSetDate(sWebsiteUrl + "report/partner-master-summary", startDate.ToString("yyyy-MMM"), endDate.ToString("yyyy-MMM"));
                }
            }
            else
            {
                objCommon.GetPageSetDate(sWebsiteUrl + "report/partner-master-summary", startDate.ToString("yyyy-MMM"), endDate.ToString("yyyy-MMM"));
            }
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_partnerMasterSummary.GetPartnerMasterSummary(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "ServerSide"));
        }

        [Route("report/compensation-summary")]
        public ActionResult CompensationSummary(int Year = 0)
        {
            ViewBag.sRoleID = 1;
            ViewBag.SessionTime = objCommon.GetSessionTimeOut();

            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);

            var user = session.GetString("user").Split('/')[1];
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_compensationSummary.GetCompensationSummary(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "", ViewBag.UserRoleID, Year));
        }

        //[Route("report/compensation-planning-demo")]
        //public ActionResult StaffReportsDemo(int Year = 0)
        //{
        //    string sUser = User.Identity.Name.ToString();
        //    objCommon.LogErrorToFile(sUser.Split('\\')[0], sUser.Split('\\')[1]);
        //    string sUserID = sUser.Split('\\')[1];
        //    int iRoleID = 0, iMenuID = 0;

        //    if (TempData.Peek("RoleID") != null)
        //    {
        //        iRoleID = Convert.ToInt32(TempData.Peek("RoleID"));
        //    }

        //    bool isDateSetManual = false;
        //    if (TempData.Peek("isDateSetManual") != null)
        //    {
        //        isDateSetManual = true;
        //    }

        //    if (Request.QueryString["userID"] != null)
        //    {
        //        sUserID = Request.QueryString["userID"].ToString();
        //    }

        //    DataTable dtPer = _service.DAGetDataSet("EXEC[usp_User_Page_Permission] @Action ='Get', @Criteria ='Check_Page_Permission', @UserID='" + sUser.Split('\\')[1] + "', @Url='report/compensation-planning', @RoleID=" + 0).Tables[0];

        //    ViewBag.sAnotherUserID = "";
        //    if (dtPer != null && dtPer.Rows.Count > 0)
        //    {
        //        if (Request.QueryString["userID"] != null && Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]) == (int)GlobalVariable.UserRole.SuperUser)
        //        {
        //            sUserID = Request.QueryString["userID"].ToString();
        //            ViewBag.sAnotherUserID = sUserID;
        //        }
        //        else
        //        {
        //            sUserID = sUser.Split('\\')[1];
        //        }
        //    }

        //    int pageCount = 0;
        //    ViewBag.sRoleID = 1;

        //    dtPer = _service.DAGetDataSet("EXEC[usp_User_Page_Permission] @Action ='Get', @Criteria ='Check_Page_Permission', @UserID='" + sUserID + "', @Url='report/compensation-planning', @RoleID=" + iRoleID).Tables[0];
        //    if (dtPer != null && dtPer.Rows.Count > 0)
        //    {
        //        iMenuID = Convert.ToInt32(dtPer.Rows[0]["ID"]);
        //        pageCount = Convert.ToInt32(dtPer.Rows[0]["URLCount"]);
        //        ViewBag.sOrgRoleID = Convert.ToInt32(dtPer.Rows[0]["LU_User_View_ID"]);
        //        ViewBag.UserID = Convert.ToInt32(dtPer.Rows[0]["UserID"]);
        //        ViewBag.UserRoleID = Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]);

        //        if (iRoleID > 0)
        //            ViewBag.sRoleID = iRoleID;
        //        else
        //        {
        //            ViewBag.sRoleID = Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]);
        //            iRoleID = 0;
        //        }
        //    }

        //    Session["user"] = sUser.Split('\\')[0] + "/" + sUserID;
        //    ViewBag.SessionTime = CommonClass.GetSessionTimeOut();

        //    objCommon.LogUserToFile("report/compensation-planning", sUser.Split('\\')[0] + "/" + sUserID);

        //    if (pageCount > 0)
        //    {
        //        Session["userAppID"] = null;
        //        Session["iMenuID"] = iMenuID;

        //        Year = SetYearCompForm(Year);

        //        setCompensationDate(isDateSetManual, Year);

        //        var user = Session.GetString("user").Split('/')[1];
        //        string userAppID = CommonClass.Get_LoginUser_AppID(user);

        //        objCommon.ClearManagerFilter(userAppID);

        //        return View(_staffCompensationPlanningDemo.GetCompensationPlanningDemo(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), iRoleID, iMenuID, TempData.Peek("ActionList") as DataTable, isDateSetManual, 0, userAppID, ViewBag.UserRoleID, true, Year));
        //    }
        //    else
        //    {
        //         if (!string.IsNullOrEmpty(url))
        //            return Redirect(sWebsiteUrl + url);
        //        else
        //            return RedirectToAction("NotFound");
        //    }
        //}

        [Route("report/compensation-percent")]
        public ActionResult CompensationPercentReport(int Year = 0)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);

            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }

            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sUserDirectory = sUser.Split('/')[0];
            Year = SetYearCompForm(Year);
            setCompensationDate(userVariables.isDateSetManual, Year);
            
            List<CompensationSummaryModel> lstCompensationSummaryModel = new List<CompensationSummaryModel>();
            for (int year = 2023; year <= DateTime.Now.Year; year++)
            {
                CompensationSummaryModel compensationSummaryModel = _compensationSummary.GetCompensationSummary(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, null, userVariables.isDateSetManual, 0, "", userVariables.UserRoleID, year, new List<int>() { 1 });
                compensationSummaryModel.PercentYear = year;
                lstCompensationSummaryModel.Add(compensationSummaryModel);
            }
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_compensationPercent.GetCompensationPercent1(sUserID, sUserDirectory, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "", ViewBag.UserRoleID, lstCompensationSummaryModel, true, Year));

            //string sUser = User.Identity.Name.ToString();
            //objCommon.LogErrorToFile(sUser.Split('\\')[0], sUser.Split('\\')[1]);
            //string sUserID = sUser.Split('\\')[1];
            //int iRoleID = 0, iMenuID = 0;

            //if (TempData.Peek("RoleID") != null)
            //{
            //    iRoleID = Convert.ToInt32(TempData.Peek("RoleID"));
            //}

            //bool isDateSetManual = false;
            //if (TempData.Peek("isDateSetManual") != null)
            //{
            //    isDateSetManual = true;
            //}

            //if (Request.QueryString["userID"] != null)
            //{
            //    sUserID = Request.QueryString["userID"].ToString();
            //}

            //DataTable dtPer = _service.DAGetDataSet("EXEC[usp_User_Page_Permission] @Action ='Get', @Criteria ='Check_Page_Permission', @UserID='" + sUser.Split('\\')[1] + "', @Url='report/compensation-percent', @RoleID=" + 0).Tables[0];

            //ViewBag.sAnotherUserID = "";
            //if (dtPer != null && dtPer.Rows.Count > 0)
            //{
            //    if (Request.QueryString["userID"] != null && Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]) == (int)GlobalVariable.UserRole.SuperUser)
            //    {
            //        sUserID = Request.QueryString["userID"].ToString();
            //        ViewBag.sAnotherUserID = sUserID;
            //    }
            //    else
            //    {
            //        sUserID = sUser.Split('\\')[1];
            //    }
            //}

            //DBTables objTable = new DBTables();
            //int pageCount = 0;
            //ViewBag.sRoleID = 1;

            //dtPer = _service.DAGetDataSet("EXEC[usp_User_Page_Permission] @Action ='Get', @Criteria ='Check_Page_Permission', @UserID='" + sUserID + "', @Url='report/compensation-percent', @RoleID=" + iRoleID).Tables[0];
            //if (dtPer != null && dtPer.Rows.Count > 0)
            //{
            //    iMenuID = Convert.ToInt32(dtPer.Rows[0]["ID"]);
            //    pageCount = Convert.ToInt32(dtPer.Rows[0]["URLCount"]);
            //    ViewBag.sOrgRoleID = Convert.ToInt32(dtPer.Rows[0]["LU_User_View_ID"]);
            //    ViewBag.UserID = Convert.ToInt32(dtPer.Rows[0]["UserID"]);
            //    ViewBag.UserRoleID = Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]);

            //    if (iRoleID > 0)
            //        ViewBag.sRoleID = iRoleID;
            //    else
            //    {
            //        ViewBag.sRoleID = Convert.ToInt32(dtPer.Rows[0]["UserRole_ID"]);
            //        iRoleID = 0;
            //    }
            //}

            //Session["user"] = sUser.Split('\\')[0] + "/" + sUserID;
            //ViewBag.SessionTime = CommonClass.GetSessionTimeOut();

            //objCommon.LogUserToFile("report/compensation-percent", sUser.Split('\\')[0] + "/" + sUserID);

            //if (pageCount > 0)
            //{
            //    Session["userAppID"] = null;
            //    Session["iMenuID"] = iMenuID;

            //    Year = SetYearCompForm(Year);

            //    setCompensationDate(isDateSetManual, Year);

            //    var user = Session["user"].ToString().Split('/')[1];
            //    string userAppID = CommonClass.Get_LoginUser_AppID(user);

            //    objCommon.ClearManagerFilter(userAppID);

            //    return View(_compensationPercent.GetCompensationPercent(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), iRoleID, iMenuID, TempData.Peek("ActionList") as DataTable, isDateSetManual, 0, userAppID, ViewBag.UserRoleID, true, Year));
            //}
            //else
            //{
            //    string url = CommonClass.Get_LoginUser_Page_Permission(sUserID, iRoleID);
            //    if (!string.IsNullOrEmpty(url))
            //        return Redirect(sWebsiteUrl + url);
            //    else
            //        return RedirectToAction("NotFound");
            //}
        }

        [Route("report/salary-validation")]
        public ActionResult SalaryValidation()
        {
            return View(_CompValidation.GetSalaryValidation());
        }
        [Route("report/participation-percentage")]
        public ActionResult ParticipationPercentage()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            if (!userVariables.IsFilterPageSpecific)
            {
                userVariables.IsFilterPageSpecific = true;
                TempData["IsFilterPageSpecific"] = true;
                ViewBag.IsFilterPageSpecific = true;
                session.SetString("UserVariables", System.Text.Json.JsonSerializer.Serialize(userVariables));
            }
                        
            session.SetString("iMenuID", userVariables.iMenuID.ToString());
            string url = objCommon.GetCurrentURLOfPage();

            jsonData = session.GetString("PageWiseDateSelection");
            List<PageDateSelection> lstPageDateSelection = System.Text.Json.JsonSerializer.Deserialize<List<PageDateSelection>>(jsonData);

            if (lstPageDateSelection != null && lstPageDateSelection.Count > 0)
            {
                if (lstPageDateSelection.Where(a => a.Url.ToLower() == url.ToString().ToLower()).ToList() != null && lstPageDateSelection.Where(a => a.Url.ToLower() == url.ToString().ToLower()).ToList().Count > 0)
                {
                    PageDateSelection pageDateSelection = lstPageDateSelection.Where(S => S.Url.ToLower() == _httpContextAccessor.HttpContext.Request.GetDisplayUrl().ToLower()).FirstOrDefault();
                    if (pageDateSelection != null)
                    {
                        objCommon.GetPageSetDate(sWebsiteUrl + "report/participation-percentage", pageDateSelection.StartDate, pageDateSelection.EndDate);
                    }
                }
                else
                {
                    List<DateTime> lstDate = objCommon.GetStartEndDate();
                    objCommon.GetPageSetDate(sWebsiteUrl + "report/participation-percentage", lstDate[0].ToString("yyyy-MMM"), lstDate[1].ToString("yyyy-MMM"));
                }
            }
            else
            {
                List<DateTime> lstDate = objCommon.GetStartEndDate();
                objCommon.GetPageSetDate(sWebsiteUrl + "report/participation-percentage", lstDate[0].ToString("yyyy-MMM"), lstDate[1].ToString("yyyy-MMM"));
            }
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return View(_participationPercentage.GetParticipationPercentageDetails(userVariables.sUserDirectory, userVariables.suserID, objTable.GetUserRoleDetails(userVariables.suserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0));
        }
        #endregion

        //for compensation page we are setting date from 
        //dec to previous month
        private void setCompensationDate(bool isDateSetManual, int Year = 0)
        {
            // if (!isDateSetManual)
            //{
            List<DateTime> lstDate = objCommon.setDateClick("", Year);
            DateTime endDate = lstDate[1];
            ViewBag.EndMonth = endDate.ToString("MMM");
            TempData["ActionList"] = null;

            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("PageWiseDateSelection");
            List<PageDateSelection> lstPageDateSelection = System.Text.Json.JsonSerializer.Deserialize<List<PageDateSelection>>(jsonData);

            if (lstPageDateSelection != null && lstPageDateSelection.Count > 0)
            {
                if (lstPageDateSelection.Where(a => a.Url.ToLower() == _httpContextAccessor.HttpContext.Request.GetDisplayUrl().ToLower()).ToList() != null && lstPageDateSelection.Where(a => a.Url.ToLower() == _httpContextAccessor.HttpContext.Request.GetDisplayUrl().ToLower()).ToList().Count > 0)
                {
                    lstPageDateSelection.Where(S => S.Url.ToLower() == _httpContextAccessor.HttpContext.Request.GetDisplayUrl().ToLower())
                   .Select(S =>
                   {
                       S.EndDate = endDate.ToString("yyyy-MMM");
                       S.StartDate = lstDate[0].ToString("yyyy-MMM");
                       return S;
                   }).ToList();
                }
            }
            // }
        }

        private int SetYearCompForm(int Year)
        {
            if (Year == 0)
            {
                if (TempData.Peek("CompYear") == null)
                    Year = DateTime.Now.Year;
            }
            if (TempData.Peek("CompYear") != null)
            {
                if (Year != Convert.ToInt32(TempData.Peek("CompYear")) && Year > 0)
                {
                    TempData["CompYear"] = Year;

                    Year = Convert.ToInt32(TempData.Peek("CompYear"));
                }
                else
                {
                    Year = Convert.ToInt32(TempData.Peek("CompYear"));
                }
            }
            else
            {
                TempData["CompYear"] = Year;
            }

            return Year;
        }

        [Route("report/get-Salary-class-year-data")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public PartialViewResult GetSalaryClassYearData(int iCompensationFormId, int Year = 0)
        {
            return PartialView("_CompensationClassYear", CompClass.GetCompensationClassYear(iCompensationFormId, Year));
        }

        [Route("report/generate-billing-collection-report-excel")]
        public JsonResult GenerateBillingCollectionReportExcel(int isSplit, int ReportID)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            _billingCollectionReport.GenerateBillingCollectionReport(isSplit, ReportID, userVariables);
            return Json(true);
        }

        [Route("report/insert-update-staff-compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Staff_Compensation(string lstSC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];

            int result = _staffCompensationPlanning.Insert_Update_Staff_Compensation_Trans(lstSC, sUserID);
            return result;
        }

        [Route("report/delete-staff-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Staff_Compensation(int ID)
        {
            int Result = _staffCompensationPlanning.Delete_Staff_Compensation(ID);
            return Result;
        }

        [Route("report/check-duplicate-staff-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Staff_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Staff] @Action ='Get', @Criteria ='Check_Duplicate_Staff',@EmployeeName=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/insert-update-staff-compensation-demo")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Staff_Compensation_Demo(string lstSC)
        {
            string sUser = User.Identity.Name.ToString();
            string sUserID = sUser.Split('\\')[1];
            int result = _staffCompensationPlanningDemo.Insert_Update_Staff_Compensation_Trans_Demo(lstSC, sUserID);
            return result;
        }

        [Route("report/insert-update-nep-compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Nep_Compensation(string lstNEPC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];

            int result = _nEPCompensationPlanning.Insert_Update_NEP_Compensation_Trans(lstNEPC, sUserID);
            return result;
        }

        [Route("report/check-duplicate-nep-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_NEP_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_NEP] @Action ='Get', @Criteria ='Check_Duplicate_NEP',@Name='" + EmployeeName + "',@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/delete-nep-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_NEP_Compensation(int ID)
        {
            int Result = _nEPCompensationPlanning.Delete_NEP_Compensation(ID);
            return Result;
        }

        [Route("report/insert-update-Timekeeper-Compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Timekeeper_Compensation(string lstTC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];

            int result = _timekeeperCompensationPlanning.Insert_Update_Timekeeper_Compensation_Trans(lstTC, sUserID);

            return result;
        }

        [Route("report/insert-update-Counsel-Compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Counsel_Compensation(string lstCC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _counselCompensationPlanning.Insert_Update_Counsel_Compensation_Trans(lstCC, sUserID);

            return result;
        }

        [Route("report/check-duplicate-counsel-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Counsel_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Counsel] @Action ='Get', @Criteria ='Check_Duplicate_Counsel',@Name=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/delete-counsel-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Counsel_Compensation(int ID)
        {
            int Result = _counselCompensationPlanning.Delete_Counsel_Compensation(ID);
            return Result;
        }

        [Route("report/insert-update-Paralegal")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Paralegal(string lstParalegal)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _paralegalCompensationPlanning.Insert_Update_Paralegal_Trans(lstParalegal, sUserID);
            return result;
        }

        [Route("report/check-duplicate-paralegal-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Paralegal_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Paralegal] @Action ='Get', @Criteria ='Check_Duplicate_Paralegal',@Name=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/delete-paralegal-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Paralegal_Compensation(int ID)
        {
            int Result = _paralegalCompensationPlanning.Delete_Paralegal_Compensation(ID);
            return Result;
        }

        //[Route("report/insert-update-nep-compensation")]
        //[ValidateAntiForgeryToken]
        //public int Insert_Update_NEP_Compensation(string NEP)
        //{
        //    string sUser = User.Identity.Name.ToString();
        //    string sUserID = sUser.Split('\\')[1];
        [Route("report/insert-update-EDiscovery")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_EDiscovery(string lstEDiscovery)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _Ediscovery.Insert_Update_EDiscovery_Trans(lstEDiscovery, sUserID);
            return result;
        }

        [Route("report/check-duplicate-ediscovery-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_EDiscovery_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_EDiscovery] @Action ='Get', @Criteria ='Check_Duplicate_Ediscovery',@Name=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/delete-ediscovery-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_EDiscovery_Compensation(int ID)
        {
            int Result = _Ediscovery.Delete_EDiscovery_Compensation(ID);
            return Result;
        }

        [Route("report/insert-update-chief-compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Chief_Compensation(string lstSC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _Chief.Insert_Update_Chief_Compensation_Trans(lstSC, sUserID);
            return result;
        }

        [Route("report/delete-chief-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Chief_Compensation(int ID)
        {
            int Result = _Chief.Delete_Chief_Compensation(ID);
            return Result;
        }

        [Route("report/check-duplicate-chief-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Chief_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Chief] @Action ='Get', @Criteria ='Check_Duplicate_Chief',@EmployeeName=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/insert-update-Director-compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Director_Compensation(string lstDSC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _directorCompensationPlanning.Insert_Update_Director_Compensation_Trans(lstDSC, sUserID);

            return result;
        }

        [Route("report/delete-director-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Director_Compensation(int ID)
        {
            int Result = _directorCompensationPlanning.Delete_Director_Compensation(ID);
            return Result;
        }

        [Route("report/check-duplicate-director-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Director_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Director] @Action ='Get', @Criteria ='Check_Duplicate_Director',@EmployeeName=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/insert-update_equity_partner-compensation")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Equity_Partner_Compensation(string lstEPC)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _equityPartner.Insert_Update_Equity_Partner_Compensation_Trans(lstEPC, sUserID);
            return result;
        }

        [Route("report/InsertExtraColumn")]
        [ValidateAntiForgeryToken]
        public int InsertExtraColumn(string ColumnName, string DataType, int ID, int FormID, int Year, bool IsYearEnd)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sCriteria = (ID > 0 ? "Update_ExtraColumn" : "Insert_ExtraColumn");
            int result = _service.InsertExtraColumn_Trans("Insert_Update", sCriteria, ColumnName, DataType, ID, FormID, sUserID, Year, IsYearEnd);
            return result;
        }

        [Route("report/InactiveExtraColumn")]
        [ValidateAntiForgeryToken]
        public int InactiveExtraColumn(int ID, int FormID, int Year)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _service.InactiveExtraColumn_Trans("Insert_Update", "Inactive_ExtraColumn", ID, FormID, Year, sUserID);
            return result;
        }

        [Route("report/CheckDuplicateExtraColumnName")]
        [ValidateAntiForgeryToken]
        public JsonResult DuplicateExtraColumnName(string ColumnName, int ID, int FormID, int Year)
        {
            return Json(JsonConvert.SerializeObject(_service.DAGetDataSet("EXEC[usp_Compensation_Class_Year] @Action ='Get', @Criteria ='CheckDuplicateColumnName', @Name='" + ColumnName + "', @FormID='" + FormID + "',@Year='" + Year + "', @ID=" + ID).Tables[0]));
        }

        [Route("report/GetCompensationClassYear")]
        [ValidateAntiForgeryToken]
        public JsonResult GetCompensationClassYear(int Year, int FormID, int YearToFetch)
        {
            List<Associate_Compensation> lstCompensation = _timekeeperCompensationPlanning.GetCompensationClassYear(Year, FormID, YearToFetch);

            return Json(JsonConvert.SerializeObject(lstCompensation));
        }

        [Route("report/get-bonus-by-class-year-hours")]
        [ValidateAntiForgeryToken]
        public JsonResult GetBonusClassYear_Hours(int ClassYear, int FormID, int YearToFetch, int FinalHour, int AxinnYrCurrentYear)
        {
            return Json(_compensationCommon.GetBonusBy_Class_Year_Hours(ClassYear, FormID, YearToFetch, FinalHour, AxinnYrCurrentYear));
        }

        [Route("report/setdateonclick")]
        [ValidateAntiForgeryToken]
        public string setDateOnClick(int Year)
        {
            //int Year = DateTime.Now.Year;
            if (TempData.Peek("CompYear") != null)
            {
                Year = Convert.ToInt32(TempData.Peek("CompYear"));
            }
            DateTime endDate = objCommon.setDateClick("", Year)[1];
            string EndMonth = endDate.ToString("MMM");
            return EndMonth;
        }

        [Route("report/insert-update-comp-race-bonus-pool")]
        [ValidateAntiForgeryToken]
        public int Insert_Update_Comp_Race_Bonus_Pool(string compRaceBonusPollInfo)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            //string sUser = User.Identity.Name.ToString();
            //string sUserID = sUser.Split('\\')[1];
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            int result = _compensationCommon.Insert_Update_Comp_Race_Bonus_Pool(compRaceBonusPollInfo, sUserID);
            return result;
        }

        [Route("report/get-raise-bonus-details")]
        [ValidateAntiForgeryToken]
        public JsonResult GetComp_Race_Bonus_Pool_Details(int FormID, int Year)
        {
            return Json(_compensationCommon.GetComp_Race_Bonus_Pool(FormID, Year));
        }

        [Route("report/InsertCompensationFreezeDetails")]
        [ValidateAntiForgeryToken]
        public int InsertCompensationFreezeDetails(int CompensationFormID, string CompHTMLDetails, string CompBudgetHTMLDetails, int Year)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            string sCriteria = "InsertCompensationFreezeDetails";
            int result = _service.InsertCompensationFreezeDetails_Trans("Insert_Update", sCriteria, CompensationFormID, CompHTMLDetails, CompBudgetHTMLDetails, Year, sUserID);

            return result;
        }

        [Route("report/GetCompensationFreezeDetailsFormIDYear")]
        [ValidateAntiForgeryToken]
        public JsonResult GetCompensationFreezeDetailsFormIDYear(int CompensationFormID, int Year)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            objCommon.LogErrorToFile(sUser.Split('/')[0], sUser.Split('/')[1]);
            CompensationFreezeDetailsModel objCompensationFreezeDetailsModel = _compensationCommon.GetCompensationFreezeDetailsFormIDYear(CompensationFormID, Year, sUser.Split('\\')[0], sUserID, "");

            return Json(JsonConvert.SerializeObject(objCompensationFreezeDetailsModel));
        }

        [Route("report/get-iscompensation-freezeset")]
        public JsonResult GetIsCompensationFreezeSet()
        {
            TempData["isfreeze"] = true;
            return Json(1);
        }



        [Route("report/delete-timekeeper-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Timekeeper_Compensation(int ID)
        {
            int Result = _timekeeperCompensationPlanning.Delete_TimeKeeper_Compensation(ID);
            return Result;
        }

        [Route("report/delete-equity_partner-compensation")]
        [ValidateAntiForgeryToken]
        public int Delete_Equity_Partner_Compensation(int ID)
        {
            int Result = _equityPartner.Delete_Equity_Partner_Compensation(ID);
            return Result;
        }

        [Route("report/check-duplicate-associate-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Associate_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Associate] @Action ='Get', @Criteria ='Check_Duplicate_Associate',@EmployeeName=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }

        [Route("report/check-duplicate-equity-partner-compensation")]
        [ValidateAntiForgeryToken]
        public JsonResult Duplicate_Equity_Partner_Compensation(string EmployeeName, int ID)
        {
            var jsonResult = Json(_service.ExecuteScaler("EXEC[usp_Compensation_Equity_Partner] @Action ='Get', @Criteria ='Check_Duplicate_EquityPartner',@Name=" + EmployeeName + ",@ID = " + ID + ""));
            return jsonResult;
        }


        [Route("report/get-partner-master-summary")]
        [ValidateAntiForgeryToken]
        public PartialViewResult Get_PartnerMasterSummary()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var jsonData = session.GetString("UserVariables");
            UserVariables userVariables = System.Text.Json.JsonSerializer.Deserialize<UserVariables>(jsonData);
            string sUser = session.GetString("user");
            string sUserID = sUser.Split('/')[1];
            DataTable dtAction = TempData.Peek("ActionList") != null && TempData.Peek("ActionList") != string.Empty ? JsonConvert.DeserializeObject<DataTable>(TempData.Peek("ActionList").ToString()) : null;
            return PartialView("~/Views/Reports/_PartnerMasterSummary.cshtml", _partnerMasterSummary.GetPartnerMasterSummary(sUser.Split('\\')[0], sUserID, objTable.GetUserRoleDetails(sUserID), userVariables.iRoleID, userVariables.iMenuID, dtAction, userVariables.isDateSetManual, 0, "ClientSide"));
        }

    }
}
